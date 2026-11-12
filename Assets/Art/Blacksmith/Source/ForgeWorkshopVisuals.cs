using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Presentation follows successful interpreter receipts; never spends resources or changes quality.
public sealed class ForgeWorkshopVisuals : MonoBehaviour
{
    sealed class PieceView { public GameObject root; public Renderer blade; public Material material,handleMaterial; public float heat; }
    readonly Dictionary<int,PieceView> pieces=new Dictionary<int,PieceView>();
    readonly Dictionary<Transform,Quaternion> rest=new Dictionary<Transform,Quaternion>();
    readonly List<Material> owned=new List<Material>();
    ForgeWorkshopAssets assets;
    Transform smith,leftArm,leftFore,leftHand,leftSocket,rightArm,rightFore,rightHand,rightSocket,leftLeg,rightLeg;
    Transform heatTarget,strikeTarget,waterTarget,hammer,polishingStone;
    Transform leftKnee,rightKnee,leftFoot,rightFoot,headBone,spine,tongs;
    readonly List<Bounds> obstacles=new List<Bounds>();
    Vector3 leftFootRest,rightFootRest;
    Light fireLight;
    float furnaceGlow=ForgeBalance.DefaultFurnaceTemperature/1200f;
    ParticleSystem coolingVapor;
    Transform furnaceCoal,furnacePoker;
    float furnacePulse;
    Renderer apron;
    Material apronMaterial;
    Material runeMaterial,sealMaterial,crystalMaterial;
    Color apronColor;
    bool acting;
    PieceView heldPiece;
    ForgeSmithIdle idle;
    ForgeLuckySparkles luckySparkles;
    int epoch;
    public Vector3 SmithPosition=>smith?smith.position:transform.position;
    public int PieceCount=>pieces.Count;
    public int LastProcess {get;private set;}
    public bool IsActing=>acting;
    Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    GameObject Spawn(GameObject prefab,string name,Vector3 position){var g=Instantiate(prefab,transform);g.name=name;g.transform.localPosition=position;return g;}
    public void Initialize(ForgeWorkshopAssets set)
    {
        assets=set;
        if(set.floor)Spawn(set.floor,"Workshop floor",Vector3.zero);
        smith=Spawn(set.blacksmith,"Blacksmith",new Vector3(1.2f,0,0)).transform;
        smith.localRotation=Quaternion.Euler(0,155,0);
        // Imported clips remain preserved; the validated procedural rig owns these bones.
        foreach(var a in smith.GetComponentsInChildren<Animator>())a.enabled=false;
        foreach(var a in smith.GetComponentsInChildren<Animation>())a.enabled=false;
        leftArm=Find(smith,"LeftArm");leftFore=Find(smith,"LeftForeArm");leftHand=Find(smith,"LeftHand");leftSocket=Find(smith,"LeftHandSocket");
        rightArm=Find(smith,"RightArm");rightFore=Find(smith,"RightForeArm");rightHand=Find(smith,"RightHand");rightSocket=Find(smith,"RightHandSocket");
        leftLeg=Find(smith,"LeftUpLeg");rightLeg=Find(smith,"RightUpLeg");
        leftKnee=Find(smith,"LeftLeg");rightKnee=Find(smith,"RightLeg");leftFoot=Find(smith,"LeftFoot");rightFoot=Find(smith,"RightFoot");headBone=Find(smith,"Head");spine=Find(smith,"Spine");
        leftFootRest=smith.InverseTransformPoint(leftFoot.position);rightFootRest=smith.InverseTransformPoint(rightFoot.position);
        foreach(var t in new[]{leftArm,leftFore,leftHand,rightArm,rightFore,rightHand,leftLeg,rightLeg,leftKnee,rightKnee,leftFoot,rightFoot,headBone,spine})rest[t]=t.localRotation;
        // The tool shaft passes across the palm, not along the fingers.
        leftSocket.localRotation=rightSocket.localRotation=Quaternion.Euler(90,0,0);
        apron=smith.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="attach-coat-mesh_apron");
        if(apron){apronMaterial=new Material(apron.sharedMaterial);owned.Add(apronMaterial);apron.sharedMaterial=apronMaterial;apronColor=apronMaterial.GetColor("_BaseColor");}
        var forge=Spawn(set.forge,"Forge",new Vector3(2,0,2));heatTarget=Find(forge.transform,"HeatTarget");fireLight=forge.GetComponentInChildren<Light>();
        var anvil=Spawn(set.anvil,"Anvil",new Vector3(2,0,0));strikeTarget=Find(anvil.transform,"StrikeTarget");
        var bucket=Spawn(set.bucket,"QuenchBucket",new Vector3(3.5f,0,2));waterTarget=Find(bucket.transform,"QuenchTarget");
        hammer=Instantiate(set.hammer,rightSocket,false).transform;hammer.name="HeldHammer";hammer.localPosition=Vector3.zero;
        var stone=GameObject.CreatePrimitive(PrimitiveType.Cube);stone.name="HeldPolishingStone";
        polishingStone=stone.transform;polishingStone.SetParent(transform,false);polishingStone.localScale=new Vector3(.12f,.045f,.085f);
        Destroy(stone.GetComponent<Collider>());
        var stoneMaterial=new Material(set.sword.GetComponentsInChildren<Renderer>().First(r=>r.name=="Blade").sharedMaterial);
        stoneMaterial.SetColor("_BaseColor",new Color(.36f,.39f,.42f));stoneMaterial.SetColor("_EmissionColor",Color.black);
        stoneMaterial.SetFloat("_Metallic",0);stoneMaterial.SetFloat("_Smoothness",.15f);owned.Add(stoneMaterial);
        stone.GetComponent<Renderer>().sharedMaterial=stoneMaterial;stone.SetActive(false);
        var sparkleRoot=new GameObject("Lucky sword sparkles");sparkleRoot.transform.SetParent(transform,false);
        luckySparkles=sparkleRoot.AddComponent<ForgeLuckySparkles>();luckySparkles.Initialize(stoneMaterial);
        if(set.tongs){tongs=Instantiate(set.tongs,transform).transform;tongs.name="HeldTongs";tongs.gameObject.SetActive(false);}
        foreach(var station in new[]{forge,anvil,bucket}){var rs=station.GetComponentsInChildren<Renderer>();if(rs.Length>0){var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);if(station==forge)bounds.Expand(new Vector3(.2f,0,.2f));bounds.center-=transform.position;obstacles.Add(bounds);}}
        ResetPose();
        idle=gameObject.AddComponent<ForgeSmithIdle>();idle.Initialize(smith,set.butterfly);
    }
    void EmitCoolingVapor(Vector3 position,bool wet)
    {
        if(!coolingVapor){
            var g=new GameObject("Cooling vapor");g.transform.SetParent(transform,false);
            coolingVapor=g.AddComponent<ParticleSystem>();coolingVapor.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=coolingVapor.main;main.playOnAwake=false;main.loop=false;main.maxParticles=40;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=coolingVapor.emission;emission.enabled=false;var shape=coolingVapor.shape;shape.enabled=false;
            var size=coolingVapor.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.3f,1),new Keyframe(1,0)));
            var renderer=g.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;
            renderer.mesh=Find(assets.sword.transform,"Pommel").GetComponent<MeshFilter>().sharedMesh;
            var material=new Material(assets.sword.GetComponentsInChildren<Renderer>().First(r=>r.name=="Blade").sharedMaterial);material.SetColor("_BaseColor",new Color(.8f,.9f,.95f));material.SetFloat("_Metallic",0);material.SetColor("_EmissionColor",new Color(.12f,.16f,.18f));owned.Add(material);renderer.sharedMaterial=material;
        }
        if(!coolingVapor.isPlaying)coolingVapor.Play();
        for(int i=0;i<(wet?24:10);i++){
            float angle=i*2.39996f;
            var particle=new ParticleSystem.EmitParams { position=position+new Vector3(Mathf.Cos(angle)*.1f,0,Mathf.Sin(angle)*.1f),velocity=new Vector3(Mathf.Cos(angle)*.12f,.5f+(i%4)*.1f,Mathf.Sin(angle)*.12f),startLifetime=wet?.7f:.45f,startSize=wet?1.5f:1f,startColor=Color.white };
            coolingVapor.Emit(particle,1);
        }
    }
    Material DetailMaterial(ref Material slot,Color color){if(!slot){slot=new Material(assets.sword.GetComponentsInChildren<Renderer>().First(r=>r.name=="Blade").sharedMaterial);slot.SetColor("_BaseColor",color);slot.SetColor("_EmissionColor",color*.6f);owned.Add(slot);}return slot;}
    void Details(PieceView p,ForgeMvp.Step step)
    {
        var appearance=p.root.GetComponent<ForgeSwordAppearance>()??p.root.AddComponent<ForgeSwordAppearance>();
        appearance.Apply(step.material,step.shaped,step.data,step.heat,false);
    }
    public void SetBurning(bool value){if(apronMaterial)apronMaterial.SetColor("_BaseColor",value?new Color(1f,.05f,.05f):apronColor);}
    void ResetPose()
    {
        foreach(var p in rest)if(p.Key)p.Key.localRotation=p.Value;
        if(leftArm)leftArm.localRotation=rest[leftArm]*Quaternion.Euler(0,0,65);
        if(rightArm)rightArm.localRotation=rest[rightArm]*Quaternion.Euler(0,0,-65);
        if(hammer)hammer.localRotation=Quaternion.identity;
    }
    void Update(){if(!acting&&smith&&heldPiece==null&&(!idle||!idle.IsReactingToError)){ResetPose();float breath=Mathf.Sin(Time.time*1.5f)*2.5f;leftArm.localRotation*=Quaternion.Euler(breath,0,0);rightArm.localRotation*=Quaternion.Euler(-breath,0,0);}if(idle)idle.Tick(Time.deltaTime,acting||heldPiece!=null);if(!acting&&heldPiece!=null){heldPiece.root.transform.position=leftSocket.position;}if(fireLight)fireLight.intensity=(.3f+furnaceGlow*(acting&&(LastProcess==2||LastProcess==8)?2.4f:1.2f))+furnacePulse+Mathf.Sin(Time.time*9)*.08f;}
    public void Clear()
    {
        if(luckySparkles)luckySparkles.Hide();
        if(idle)idle.CancelErrorReaction();
        HideFurnaceTools();furnaceGlow=ForgeBalance.DefaultFurnaceTemperature/1200f;
        epoch++;acting=false;LastProcess=0;ReleaseHeldPiece();ResetPose();if(coolingVapor)coolingVapor.Clear();if(polishingStone)polishingStone.gameObject.SetActive(false);
        foreach(var p in pieces.Values){if(p.root)Destroy(p.root);foreach(var m in new[]{p.material,p.handleMaterial})if(m){owned.Remove(m);Destroy(m);}}
        pieces.Clear();
    }
    void ReleaseHeldPiece(){
        if(heldPiece==null)return;
        // Cancellation must leave a supported workpiece rather than a floating one.
        heldPiece.root.transform.SetPositionAndRotation(strikeTarget.position+new Vector3(.34f,.025f,0),Quaternion.Euler(0,0,90));
        heldPiece=null;
    }
    public void ReactToCodeError(){if(idle)idle.ReactToCodeError();}
    public void Cancel(){HideFurnaceTools();if(luckySparkles)luckySparkles.Hide();if(idle)idle.CancelErrorReaction();epoch++;acting=false;LastProcess=0;ReleaseHeldPiece();ResetPose();if(coolingVapor)coolingVapor.Clear();if(hammer)hammer.gameObject.SetActive(true);if(tongs)tongs.gameObject.SetActive(false);if(polishingStone)polishingStone.gameObject.SetActive(false);}
    void OnDisable(){Cancel();}
    void OnDestroy(){foreach(var m in owned)if(m)Destroy(m);}
    static Color Metal(int material)=>material==5?new Color(.72f,.81f,.9f):material==7?new Color(.3f,.35f,.65f):material==9?Color.white:new Color(.48f,.52f,.56f);
    void Appearance(PieceView p,ForgeMvp.Step step,float heat)
    {
        Color color=ForgeSwordAppearance.BladeColor(step.material,step.data);var d=step.data;

        p.material.SetColor("_BaseColor",Color.Lerp(color,new Color(1,.22f,.025f),Mathf.Clamp01(heat/1200f)*.6f));
        p.material.SetColor("_EmissionColor",new Color(1,.16f,.015f)*Mathf.Clamp01(heat/1200f)*2.5f);
        p.material.SetFloat("_Smoothness",d!=null&&d.polished?.72f:.35f);
        p.heat=heat;
    }
    void Reach(Transform upper,Transform fore,Transform hand,Transform socket,Vector3 target,Quaternion rotation,bool left)
    {
        Vector3 pole=upper.position+smith.right*(left?-.45f:.45f)+Vector3.up*(left?-.55f:-.35f);
        Vector3 shaft=rotation*Vector3.up;
        for(int i=0;i<2;i++){
            Vector3 fingers=Vector3.ProjectOnPlane((target-fore.position)*(left?-1:1),shaft).normalized;
            if(fingers.sqrMagnitude<.01f)fingers=rotation*Vector3.right;
            Quaternion palm=Quaternion.LookRotation(Vector3.Cross(fingers,shaft),shaft);
            ForgePoseSolver.Hand(upper,fore,hand,socket,target,palm,pole);
        }
    }
    void Feet(float phase,float moving)
    {
        void Foot(Transform upper,Transform knee,Transform foot,Vector3 restPosition,float offset){
            float stride=Mathf.Sin(phase+offset)*.11f*moving,lift=Mathf.Max(0,Mathf.Cos(phase+offset))*.065f*moving;
            Vector3 p=smith.TransformPoint(restPosition+new Vector3(0,lift,stride));
            ForgePoseSolver.Limb(upper,knee,foot,p,upper.position+smith.forward);
            foot.rotation=smith.rotation;
        }
        Foot(leftLeg,leftKnee,leftFoot,leftFootRest,0);Foot(rightLeg,rightKnee,rightFoot,rightFootRest,Mathf.PI);
    }
    void EnsureFurnaceTools()
    {
        if(furnaceCoal)return;
        Transform Tool(string name,PrimitiveType shape,Vector3 scale,Color color){
            var obj=GameObject.CreatePrimitive(shape);obj.name=name;obj.transform.SetParent(transform,false);obj.transform.localScale=scale;
            var collider=obj.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var material=new Material(assets.sword.GetComponentsInChildren<Renderer>().First(r=>r.name=="Blade").sharedMaterial);
            material.SetColor("_BaseColor",color);material.SetColor("_EmissionColor",Color.black);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.15f);
            owned.Add(material);obj.GetComponent<Renderer>().sharedMaterial=material;obj.SetActive(false);return obj.transform;
        }
        furnaceCoal=Tool("Furnace coal",PrimitiveType.Sphere,new Vector3(.14f,.10f,.12f),new Color(.06f,.055f,.05f));
        furnacePoker=Tool("Furnace poker",PrimitiveType.Cylinder,new Vector3(.025f,.32f,.025f),new Color(.2f,.22f,.24f));
    }
    void HideFurnaceTools()
    {
        if(furnaceCoal)furnaceCoal.gameObject.SetActive(false);
        if(furnacePoker)furnacePoker.gameObject.SetActive(false);
        furnacePulse=0;
    }
    IEnumerator StokeFurnace(ForgeMvp.Step step,int run)
    {
        EnsureFurnaceTools();
        if(hammer)hammer.gameObject.SetActive(false);
        if(tongs)tongs.gameObject.SetActive(false);
        if(polishingStone)polishingStone.gameObject.SetActive(false);
        Vector3 target=heatTarget.position;
        Vector3 facing=new Vector3(-.45f,0,.89f).normalized;
        Vector3 stand=target-facing*.70f+Vector3.Cross(Vector3.up,facing)*.10f;stand.y=transform.position.y;
        var route=ForgePoseSolver.Route(smith.position,stand,obstacles.Select(b=>new Bounds(b.center+transform.position,b.size)).ToList(),.18f);
        Quaternion initialFacing=smith.rotation;float traveled=0,elapsed=0,duration=Mathf.Max(.08f,step.duration);
        float initialGlow=furnaceGlow;Vector3 releasePoint=target;bool released=false;
        try{
            while(elapsed<duration&&run==epoch){
                float t=Mathf.Clamp01(elapsed/duration);
                float move=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.34f,t));
                ResetPose();Vector3 old=smith.position;smith.position=ForgePoseSolver.Along(route,move);traveled+=Vector3.Distance(old,smith.position);
                Vector3 direction=ForgePoseSolver.Along(route,Mathf.Min(1,move+.02f))-smith.position;
                Quaternion walking=direction.sqrMagnitude>.00001f?Quaternion.LookRotation(direction):Quaternion.LookRotation(facing);
                smith.rotation=Quaternion.Slerp(initialFacing,walking,Mathf.Clamp01(t/.12f));
                smith.rotation=Quaternion.Slerp(smith.rotation,Quaternion.LookRotation(facing),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.34f,t)));
                float settle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.38f,t));
                float scoop=Mathf.Sin(Mathf.InverseLerp(.34f,.49f,t)*Mathf.PI);
                float stoke=t>=.76f?Mathf.Sin(Mathf.InverseLerp(.76f,.98f,t)*Mathf.PI*4):0;
                spine.localRotation=rest[spine]*Quaternion.Euler((12+10*scoop+3*stoke)*settle,0,0);
                headBone.localRotation=rest[headBone]*Quaternion.Euler(12*settle,0,0);
                Feet(traveled*22,1-settle);
                Vector3 carry=smith.TransformPoint(new Vector3(.27f,.86f,.19f));
                Vector3 feed=smith.TransformPoint(new Vector3(.18f,1.03f,.43f));
                Vector3 hand=Vector3.Lerp(carry-Vector3.up*(scoop*.18f),feed,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.46f,.64f,t)));
                if(t>=.76f)hand=smith.TransformPoint(new Vector3(.22f,.97f,.29f+stoke*.075f));
                float finish=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.97f,1,t));hand=Vector3.Lerp(hand,carry,finish);
                Quaternion grip=Quaternion.LookRotation(smith.right,smith.forward);
                Reach(rightArm,rightFore,rightHand,rightSocket,hand,grip,false);
                Reach(leftArm,leftFore,leftHand,leftSocket,smith.TransformPoint(new Vector3(-.28f,.85f,.20f)),grip,true);
                furnaceCoal.gameObject.SetActive(t>=.43f&&t<.76f);
                if(t<.64f)furnaceCoal.position=rightSocket.position;
                else {
                    if(!released){released=true;releasePoint=rightSocket.position;}
                    float toss=Mathf.Clamp01(Mathf.InverseLerp(.64f,.76f,t));
                    furnaceCoal.position=Vector3.Lerp(releasePoint,target,toss)+Vector3.up*(Mathf.Sin(toss*Mathf.PI)*.14f);
                    furnaceCoal.rotation=Quaternion.Euler(toss*240,toss*100,0);
                }
                furnacePoker.gameObject.SetActive(t>=.76f&&t<.97f);
                Vector3 tipDirection=(target-rightSocket.position).normalized;
                furnacePoker.SetPositionAndRotation(rightSocket.position+tipDirection*.28f,Quaternion.FromToRotation(Vector3.up,tipDirection));
                furnacePulse=t>=.70f?Mathf.Sin(Mathf.InverseLerp(.70f,1,t)*Mathf.PI)*.8f:0;
                furnaceGlow=Mathf.Lerp(initialGlow,step.heat/1200f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1,t)));
                elapsed+=Time.deltaTime;yield return null;
            }
            if(run==epoch)furnaceGlow=step.heat/1200f;
        }finally{
            if(run==epoch){HideFurnaceTools();acting=false;ResetPose();if(hammer)hammer.gameObject.SetActive(true);}
        }
    }
    void RemoveSubmittedPiece(int id)
    {
        if(!pieces.TryGetValue(id,out var piece))return;
        if(heldPiece==piece){heldPiece=null;ResetPose();}
        if(luckySparkles)luckySparkles.Hide();
        // Hide immediately: Destroy is deferred until the end of the frame.
        if(piece.root){piece.root.SetActive(false);Destroy(piece.root);}
        foreach(var material in new[]{piece.material,piece.handleMaterial})if(material){owned.Remove(material);Destroy(material);}
        pieces.Remove(id);
    }
    public IEnumerator Play(ForgeMvp.Step step)
    {
        if(step.debugOutput||step.process<0)yield break;
        if(step.finished){RemoveSubmittedPiece(step.pieceId);yield break;}
        int run=epoch;acting=true;
        if(step.process==2)furnaceGlow=step.heat/1200f;
        if(heldPiece!=null&&(!pieces.TryGetValue(step.pieceId,out var nextPiece)||nextPiece!=heldPiece))ReleaseHeldPiece();
        heldPiece=null;LastProcess=step.process;
        if(idle)idle.BeginWork();
        if(step.process==8){yield return StokeFurnace(step,run);yield break;}
        if(step.absorbedPiece>0&&pieces.TryGetValue(step.absorbedPiece,out var absorbed))absorbed.root.SetActive(false);
        if(!pieces.TryGetValue(step.pieceId,out var p)){
            p=new PieceView();p.root=Spawn(assets.sword,"Workpiece "+step.pieceId,new Vector3(3.4f,.9f,0));p.blade=Find(p.root.transform,"Blade").GetComponent<Renderer>();
            p.material=new Material(p.blade.sharedMaterial);p.blade.sharedMaterial=p.material;owned.Add(p.material);pieces.Add(step.pieceId,p);
            var handle=Find(p.root.transform,"Grip").GetComponent<Renderer>();p.handleMaterial=new Material(handle.sharedMaterial);handle.sharedMaterial=p.handleMaterial;owned.Add(p.handleMaterial);
        }
        if(step.data!=null&&(step.data.destroyed||step.data.recovered)){p.root.SetActive(false);acting=false;yield break;}
        p.root.SetActive(true);
        // A billet is the saved blade mesh compressed along its length; no runtime mesh generation.
        bool shaped=step.shaped;
        var blade=Find(p.root.transform,"Blade");blade.localScale=shaped?Vector3.one:new Vector3(2.2f,.35f,2.2f);
        bool hasHandle=shaped&&step.data!=null&&step.data.handle>0;
        foreach(string part in new[]{"Guard","Grip","Pommel"})Find(p.root.transform,part).gameObject.SetActive(hasHandle);
        Vector3 target=step.process==2?heatTarget.position:step.process==4?waterTarget.position:strikeTarget.position;
        bool wet=step.process==4&&step.consumed!=null&&(step.consumed.ContainsKey(3)||step.consumed.ContainsKey(8));
        // Air cooling remains over the anvil; only a water/ice receipt enters the bucket.
        if(step.process==4&&!wet)target=strikeTarget.position;
        Vector3 grip=target+new Vector3(.34f,.025f,0);
        Quaternion pose=Quaternion.Euler(0,0,90);
        bool heating=step.process==2;
        if(heating){grip=target+new Vector3(0,.03f,shaped?-.7f:-.25f);pose=Quaternion.Euler(90,0,0);}
        if(wet){grip=target+Vector3.up*.69f;pose=Quaternion.Euler(180,0,0);}
        bool cooling=step.process==4;
        bool lucky=step.process==777;
        // Approach above the surface, lower the blade, hold it in water, then lift it clear.
        float bladeLength=shaped?.94f:.94f*.35f;
        if(wet){grip=target+new Vector3(0,bladeLength*.707107f+.16f,bladeLength*.707107f);pose=Quaternion.Euler(225,0,0);}
        if(cooling&&!wet)grip=target+new Vector3(.34f,.32f,.28f);
        Vector3 start=p.root.transform.position,smithStart=smith.position;
        // Each workstation has its own stance. Heating faces the opening; forging faces
        // the player with the anvil in front. A camera lock must never force a rearward reach.
        Vector3 facing=heating?new Vector3(-.45f,0,.89f).normalized:Vector3.back;
        Vector3 handGrip=heating?grip+new Vector3(0,.12f,-.4f):grip;
        Vector3 stand=heating?handGrip+Vector3.Cross(Vector3.up,facing)*.25f-facing*.30f:
            wet?target+new Vector3(-.25f,0,.8f):target+new Vector3(.05f,0,.58f);
        stand.y=transform.position.y;
        var worldObstacles=obstacles.Select(b=>new Bounds(b.center+transform.position,b.size)).ToList();
        var route=ForgePoseSolver.Route(smithStart,stand,worldObstacles,.18f);
        Quaternion startFacing=smith.rotation;float traveled=0;
        float initialHeat=p.heat,duration=Mathf.Max(.08f,step.duration),elapsed=0;
        bool vaporEmitted=false;Quaternion initialRotation=p.root.transform.rotation;
        hammer.gameObject.SetActive(step.process==3);
        if(tongs)tongs.gameObject.SetActive(heating);
        try{
            while(elapsed<duration&&run==epoch){
                float t=Mathf.Clamp01(elapsed/duration),move=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.4f));
                ResetPose();var oldPosition=smith.position;smith.position=ForgePoseSolver.Along(route,move);
                traveled+=Vector3.Distance(oldPosition,smith.position);
                Vector3 travelDirection=ForgePoseSolver.Along(route,Mathf.Min(1,move+.02f))-smith.position;
                Quaternion travelRotation=travelDirection.sqrMagnitude>.00001f?Quaternion.LookRotation(travelDirection):Quaternion.LookRotation(facing);
                smith.rotation=Quaternion.Slerp(startFacing,travelRotation,Mathf.Clamp01(t/.12f));
                smith.rotation=Quaternion.Slerp(smith.rotation,Quaternion.LookRotation(facing),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.4f,t)));
                float settle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.43f,t));
                spine.localRotation=rest[spine]*Quaternion.Euler((lucky?4:heating?12:cooling?12:30)*settle,0,0);
                headBone.localRotation=rest[headBone]*Quaternion.Euler(10*settle,0,0);
                Feet(traveled*22,1-settle);
                p.root.transform.position=Vector3.Lerp(start,grip,move);p.root.transform.rotation=Quaternion.Slerp(initialRotation,pose,move);
                if(lucky&&t>.4f){
                    float lift=t<.65f?Mathf.SmoothStep(0,1,(t-.4f)/.25f):t<.82f?1:1-Mathf.SmoothStep(0,1,(t-.82f)/.18f);
                    Vector3 raised=smith.TransformPoint(new Vector3(-.27f,1.25f,.32f));
                    p.root.transform.position=Vector3.Lerp(grip,raised,lift);
                    p.root.transform.rotation=Quaternion.Slerp(pose,Quaternion.Euler(0,0,-12),lift);
                    headBone.localRotation=rest[headBone]*Quaternion.Euler(-6*lift,0,0);
                }
                if(cooling&&t>.4f){
                    float phase=(t-.4f)/.6f;
                    if(wet){
                        float dip=phase<.3f?Mathf.SmoothStep(0,1,phase/.3f):phase<.7f?1:1-Mathf.SmoothStep(0,1,(phase-.7f)/.3f);
                        float angle=Mathf.Lerp(45,25,dip)*Mathf.Deg2Rad;
                        p.root.transform.position=target+new Vector3(0,bladeLength*Mathf.Cos(angle)+.16f-Mathf.Min(.37f,bladeLength*.65f)*dip,bladeLength*Mathf.Sin(angle));
                        p.root.transform.rotation=Quaternion.Euler(180+angle*Mathf.Rad2Deg,0,0);
                    }else{
                        float wave=Mathf.Sin(phase*Mathf.PI*4);
                        p.root.transform.position=grip+new Vector3(0,Mathf.Sin(phase*Mathf.PI)*.06f,wave*.12f);
                        p.root.transform.rotation=pose*Quaternion.Euler(0,0,wave*14);
                    }
                    if(!vaporEmitted&&phase>.3f&&initialHeat>0){EmitCoolingVapor(wet?waterTarget.position:p.root.transform.position+Vector3.right*.35f,wet);vaporEmitted=true;}
                }
                // Carry the workpiece at the hand while walking, then present it to the station.
                {
                    Vector3 desired=heating?p.root.transform.position+new Vector3(0,.12f,-.4f):p.root.transform.position;
                    Quaternion handPose=heating?Quaternion.LookRotation(smith.right,(p.root.transform.position-desired).normalized):p.root.transform.rotation;
                    float present=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.4f,t));
                    desired=Vector3.Lerp(smith.TransformPoint(new Vector3(-.32f,.93f,.24f)),desired,present);
                    handPose=Quaternion.Slerp(Quaternion.LookRotation(smith.right,smith.forward),handPose,present);
                    Reach(leftArm,leftFore,leftHand,leftSocket,desired,handPose,true);
                    if(lucky&&t>.4f)p.root.transform.position=leftSocket.position;
                    if(t<.4f){Vector3 carry=leftSocket.position+(heating?smith.forward*.42f:Vector3.zero);p.root.transform.position=Vector3.Lerp(carry,p.root.transform.position,present);p.root.transform.rotation=Quaternion.Slerp(Quaternion.FromToRotation(Vector3.up,smith.forward),p.root.transform.rotation,present);}
                    if(heating&&tongs){tongs.position=leftSocket.position;tongs.rotation=Quaternion.FromToRotation(Vector3.forward,p.root.transform.position-leftSocket.position);tongs.localScale=new Vector3(1,1,Vector3.Distance(leftSocket.position,p.root.transform.position)/.42f);}
                }
                if(step.process==3&&t>.4f){
                    float cycle=Mathf.Repeat((t-.4f)/.6f*2,1);
                    float lift=cycle<.65f?Mathf.SmoothStep(0,1,cycle/.65f):1-Mathf.SmoothStep(0,1,(cycle-.65f)/.35f);
                    Vector3 shaft=Vector3.Slerp(smith.forward,Vector3.up,.88f*lift).normalized;
                    Vector3 face=Vector3.ProjectOnPlane(Vector3.down,shaft).normalized;
                    Quaternion hammerPose=Quaternion.LookRotation(Vector3.Cross(face,shaft),shaft);
                    Vector3 contact=strikeTarget.position+Vector3.up*(.035f+lift*.33f);
                    Vector3 held=contact-hammerPose*new Vector3(.14f,.28f,0);
                    Reach(rightArm,rightFore,rightHand,rightSocket,held,hammerPose,false);
                    hammer.rotation=hammerPose;
                }
                if(step.process==6&&t>.4f){
                    polishingStone.gameObject.SetActive(true);
                    float phase=(t-.4f)/.6f;
                    float stroke=.5f-.5f*Mathf.Cos(phase*Mathf.PI*4);
                    Vector3 contact=p.root.transform.TransformPoint(new Vector3(0,Mathf.Lerp(.25f,.52f,stroke),0));
                    Vector3 handTarget=contact+Vector3.up*.09f;
                    // Keep the wrist over the abrasive face, with the elbow outside the torso.
                    Quaternion stonePose=Quaternion.LookRotation(smith.forward,Vector3.up);
                    Reach(rightArm,rightFore,rightHand,rightSocket,handTarget,stonePose,false);
                    polishingStone.SetPositionAndRotation(rightSocket.position-Vector3.up*.055f,stonePose);
                }
                float coolingProgress=Mathf.Clamp01((t-.4f)/.48f);
                if(lucky&&t>.48f&&t<.92f)luckySparkles.Show(p.root.transform,Mathf.InverseLerp(.48f,.92f,t));
                else if(lucky)luckySparkles.Hide();
                Appearance(p,step,Mathf.Lerp(initialHeat,step.heat,cooling?coolingProgress:t));
                elapsed+=Time.deltaTime;yield return null;
            }
            if(run==epoch){
                if(cooling){heldPiece=p;p.root.transform.position=leftSocket.position;}
                else{p.root.transform.position=grip;p.root.transform.rotation=pose;}
                Appearance(p,step,step.heat);Details(p,step);
            }
        }finally{if(run==epoch){if(luckySparkles)luckySparkles.Hide();acting=false;if(heldPiece==null)ResetPose();if(hammer)hammer.gameObject.SetActive(heldPiece==null);if(tongs)tongs.gameObject.SetActive(false);if(polishingStone)polishingStone.gameObject.SetActive(false);}}
    }
}
