using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Receipts from the interpreter are the sole source of physical actions.
public sealed class ForgeCraftMotion : MonoBehaviour
{
    ForgeWorkshopVisuals workshop;
    ForgeActionAudio fallbackAudio;
    uint motionGeneration;
    public bool UsesWorkshopAssets => workshop != null;
    public void CancelCurrentAction(){motionGeneration++;if(fallbackAudio)fallbackAudio.Stop();ActiveProcess=0;if(workshop)workshop.Cancel();else{if(fallbackCoal)fallbackCoal.SetActive(false);if(arm){arm.localRotation=Quaternion.identity;var tool=arm.Find("Hammer");if(tool)tool.gameObject.SetActive(true);}}}
    public void ReactToCodeError(){CancelCurrentAction();if(workshop)workshop.ReactToCodeError();}
    void OnDisable(){CancelCurrentAction();}
    readonly Dictionary<int, GameObject> pieces = new Dictionary<int, GameObject>();
    Transform smith, arm;
    GameObject initial;
    GameObject fallbackCoal;
    Renderer apron;
    bool burning;
    public Vector3 SmithPosition=>workshop?workshop.SmithPosition:smith?smith.position:transform.position;
    public Bounds SmithBounds { get {var b=new Bounds(SmithPosition+Vector3.up*.9f,new Vector3(.9f,1.9f,.7f));return b;} }
    public void SetStationOffset(Vector3 offset){transform.localPosition=offset;}
    public void SetBurning(bool value){if(workshop){workshop.SetBurning(value);return;}if(value==burning||!apron)return;burning=value;apron.material.color=value?new Color(1f,.05f,.05f):new Color(.3f,.18f,.1f);}
    readonly Vector3 bench = new Vector3(2, 1, 0);
    readonly Vector3 supply = new Vector3(3.5f, 1, 0);
    readonly Vector3 furnace = new Vector3(2, 1, 2);
    readonly Vector3 cooling = new Vector3(3.5f, 1, 2);
    public int ActiveProcess { get; private set; }
    public int CompletedActions { get; private set; }
    public Vector3 TutorialFocus => SmithPosition+Vector3.up*.9f;

    GameObject Shape(string label, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        var item = GameObject.CreatePrimitive(type);
        item.name = label;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().material.color = color;
        Destroy(item.GetComponent<Collider>());
        return item;
    }
    public void Initialize(GameObject work)
    {
        initial = work;
        initial.SetActive(false);
        var assets=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");
        if(assets&&assets.Ready){workshop=gameObject.AddComponent<ForgeWorkshopVisuals>();workshop.Initialize(assets);return;}
        fallbackAudio=gameObject.AddComponent<ForgeActionAudio>();
        smith = new GameObject("Blacksmith").transform;
        smith.SetParent(transform, false);
        smith.localPosition = new Vector3(1.2f, 0, 0);
        Shape("Apron", PrimitiveType.Capsule, smith, new Vector3(0, .85f, 0), new Vector3(.5f, .5f, .4f), new Color(.3f,.18f,.1f));
        apron=smith.Find("Apron").GetComponent<Renderer>();
        Shape("Head", PrimitiveType.Sphere, smith, new Vector3(0, 1.55f, 0), Vector3.one*.4f, new Color(.8f,.6f,.4f));
        Shape("Left leg", PrimitiveType.Cube, smith, new Vector3(-.15f,.25f,0), new Vector3(.18f,.5f,.2f), Color.black);
        Shape("Right leg", PrimitiveType.Cube, smith, new Vector3(.15f,.25f,0), new Vector3(.18f,.5f,.2f), Color.black);
        arm = new GameObject("Hammer arm").transform;
        arm.SetParent(smith, false);
        arm.localPosition = new Vector3(.25f,1.2f,0);
        Shape("Arm", PrimitiveType.Cube, arm, new Vector3(.2f,0,0), new Vector3(.5f,.16f,.16f), new Color(.8f,.6f,.4f));
        Shape("Hammer", PrimitiveType.Cube, arm, new Vector3(.5f,.05f,0), new Vector3(.22f,.3f,.24f), Color.gray);
        Shape("Furnace", PrimitiveType.Cube, transform, furnace-new Vector3(0,.5f,0), new Vector3(1,.8f,1), new Color(.5f,.18f,.07f));
        Shape("Air cooling rack", PrimitiveType.Cube, transform, cooling-new Vector3(0,.5f,0), new Vector3(1,.6f,1), new Color(.25f,.4f,.5f));
    }
    void ApplyCraftAppearance(GameObject work,ForgeMvp.Step step)
    {
        if(step.absorbedPiece>0&&pieces.TryGetValue(step.absorbedPiece,out var part))part.SetActive(false);
        if(step.data==null)return;var d=step.data;
        if(d.destroyed||d.recovered){work.SetActive(false);return;}
        var previous=work.transform.Find("Craft details");if(previous)Destroy(previous.gameObject);
        var details=new GameObject("Craft details").transform;details.SetParent(work.transform,false);
        if(d.handle>0){
            Color handleColor=d.handle==1?Color.gray:d.handle==5?new Color(.8f,.85f,.95f):d.handle==7?new Color(.35f,.4f,.85f):d.handle==9?Color.white:new Color(.4f,.2f,.08f);
            Shape("Handle · "+ForgeMaterialGuide.Find(d.handle).Name,PrimitiveType.Cylinder,details,new Vector3(0,0,-.65f),new Vector3(1,.2f,.15f),handleColor);
        }
        if(d.plated)work.GetComponent<Renderer>().material.color=new Color(.8f,.85f,.95f);
        if(d.star)work.GetComponent<Renderer>().material.color=new Color(.35f,.4f,.85f);
        if(d.frost)work.GetComponent<Renderer>().material.color=Color.cyan;
        for(int i=0;i<d.runes.Count;i++)Shape("Rune",PrimitiveType.Sphere,details,new Vector3(0,.7f,-.3f+i*.3f),Vector3.one*.3f,Color.magenta);
        for(int i=0;i<d.seals.Count;i++)Shape("Seal layer",PrimitiveType.Cube,details,new Vector3(0,0,-.4f+i*.16f),new Vector3(1.2f,1.2f,.08f),new Color(.3f,.6f,1f));
        if(d.activated||d.charge>=3)Shape("Crystal",PrimitiveType.Sphere,details,new Vector3(0,.8f,-.5f),Vector3.one*.4f,Color.cyan);
        if(step.process==4&&step.consumed!=null&&(step.consumed.ContainsKey(3)||step.consumed.ContainsKey(8))){var steam=Shape("Cooling steam",PrimitiveType.Sphere,transform,work.transform.position+Vector3.up*.5f,Vector3.one*.3f,new Color(.8f,.9f,1));Destroy(steam,1);}
    }
    public void BeginRun()
    {
        CancelCurrentAction();
        if(workshop)workshop.Clear();
        foreach(var item in pieces.Values) if(item) Destroy(item);
        pieces.Clear();
        ActiveProcess = 0;
        CompletedActions = 0;
    }
    IEnumerator StokeFallback(ForgeMvp.Step step)
    {
        uint generation=motionGeneration;
        if(!fallbackCoal)fallbackCoal=Shape("Furnace coal",PrimitiveType.Sphere,transform,Vector3.zero,Vector3.one*.12f,new Color(.06f,.055f,.05f));
        fallbackCoal.SetActive(false);var tool=arm.Find("Hammer");if(tool)tool.gameObject.SetActive(false);
        Vector3 start=smith.localPosition,end=new Vector3(furnace.x-.8f,0,furnace.z);
        float elapsed=0,duration=Mathf.Max(.08f,step.duration);Vector3 released=Vector3.zero;bool tossed=false;
        try{
            while(elapsed<duration&&generation==motionGeneration){
                float t=Mathf.Clamp01(elapsed/duration),move=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.35f,t));
                Vector3 old=smith.localPosition;
                smith.localPosition=Vector3.Lerp(start,end,move)+Vector3.up*(Mathf.Sin(t*Mathf.PI*8)*.025f*(1-move));
                fallbackAudio.Frame(t,(smith.localPosition-old).sqrMagnitude>.000001f);
                float reach=Mathf.Sin(Mathf.InverseLerp(.35f,.7f,t)*Mathf.PI);
                float stoke=t>.76f?Mathf.Sin(Mathf.InverseLerp(.76f,1,t)*Mathf.PI*4):0;
                arm.localRotation=Quaternion.Euler(0,0,-25*reach-12*stoke);
                fallbackCoal.SetActive(t>.43f&&t<.76f);
                if(t<.64f)fallbackCoal.transform.position=arm.TransformPoint(new Vector3(.5f,0,0));
                else{if(!tossed){released=fallbackCoal.transform.position;tossed=true;}float toss=Mathf.InverseLerp(.64f,.76f,t);fallbackCoal.transform.position=Vector3.Lerp(released,transform.TransformPoint(furnace),toss)+Vector3.up*(Mathf.Sin(toss*Mathf.PI)*.15f);}
                elapsed+=Time.deltaTime;yield return null;
            }
            if(generation==motionGeneration){fallbackAudio.Finish();smith.localPosition=end;CompletedActions++;}
        }finally{if(generation==motionGeneration){fallbackAudio.AbortIfActive();fallbackCoal.SetActive(false);arm.localRotation=Quaternion.identity;if(tool)tool.gameObject.SetActive(true);ActiveProcess=0;}}
    }
    public IEnumerator Play(ForgeMvp.Step step)
    {
        if(step.debugOutput) yield break;
        if(workshop){
            uint generation=motionGeneration;
            ActiveProcess=step.process;
            try{yield return workshop.Play(step);if(generation==motionGeneration&&step.process>0)CompletedActions++;}
            finally{if(generation==motionGeneration){ActiveProcess=0;workshop.Cancel();}}
            yield break;
        }
        if(step.finished){
            if(pieces.TryGetValue(step.pieceId,out var submitted)){
                if(submitted){submitted.SetActive(false);Destroy(submitted);}pieces.Remove(step.pieceId);
            }
            ActiveProcess=0;yield break;
        }
        ActiveProcess = step.process;
        fallbackAudio.Begin(step);
        if(step.process==8){yield return StokeFallback(step);yield break;}
        if(!pieces.TryGetValue(step.pieceId, out var work))
        {
            work = Instantiate(initial, transform);
            work.SetActive(true);
            work.transform.localPosition = supply;
            pieces.Add(step.pieceId, work);
        }
        Vector3 destination = step.process==2 ? furnace : step.process==4 ? cooling : bench;
        Vector3 start = work.transform.localPosition;
        Vector3 smithStart = smith.localPosition;
        Vector3 smithEnd = new Vector3(destination.x-.8f,0,destination.z);
        Color from = work.GetComponent<Renderer>().material.color;
        Color to = step.heat>0 ? Color.Lerp(Color.gray,Color.red,step.heat/1200f) : Color.gray;
        float elapsed = 0;
        float duration = step.duration;
        while(elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed/duration);
            float move = Mathf.SmoothStep(0,1,Mathf.Min(1,t*2));
            Vector3 old=smith.localPosition;
            smith.localPosition = Vector3.Lerp(smithStart,smithEnd,move) + Vector3.up*(Mathf.Sin(t*Mathf.PI*4)*.035f);
            fallbackAudio.Frame(t,(smith.localPosition-old).sqrMagnitude>.000001f);
            work.transform.localPosition = Vector3.Lerp(start,destination,move);
            float swing = step.process==3 ? Mathf.Sin(Mathf.Clamp01((t-.4f)/.6f)*Mathf.PI*4)*55 : Mathf.Sin(t*Mathf.PI)*20;
            arm.localRotation = Quaternion.Euler(0,0,swing);
            work.GetComponent<Renderer>().material.color = Color.Lerp(from,to,t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        fallbackAudio.Finish();
        smith.localPosition=smithEnd;
        arm.localRotation=Quaternion.identity;
        work.transform.localPosition=destination;
        work.transform.localScale=step.shaped ? new Vector3(.25f,.15f,2) : new Vector3(1,.4f,.5f);
        work.GetComponent<Renderer>().material.color=to;
        ApplyCraftAppearance(work,step);
        work.name="Workpiece · "+ForgeMaterialGuide.Find(step.material).Name;
        if(step.process>0)CompletedActions++;
        ActiveProcess=0;
    }
}
