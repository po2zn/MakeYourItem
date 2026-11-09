using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Receipts from the interpreter are the sole source of physical actions.
public sealed class ForgeCraftMotion : MonoBehaviour
{
    readonly Dictionary<int, GameObject> pieces = new Dictionary<int, GameObject>();
    Transform smith, arm;
    GameObject initial;
    readonly Vector3 bench = new Vector3(2, 1, 0);
    readonly Vector3 supply = new Vector3(3.5f, 1, 0);
    readonly Vector3 furnace = new Vector3(2, 1, 2);
    readonly Vector3 cooling = new Vector3(3.5f, 1, 2);
    public int ActiveProcess { get; private set; }
    public int CompletedActions { get; private set; }

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
        smith = new GameObject("Blacksmith").transform;
        smith.SetParent(transform, false);
        smith.position = new Vector3(1.2f, 0, 0);
        Shape("Apron", PrimitiveType.Capsule, smith, new Vector3(0, .85f, 0), new Vector3(.5f, .5f, .4f), new Color(.3f,.18f,.1f));
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
    public void BeginRun()
    {
        foreach(var item in pieces.Values) if(item) Destroy(item);
        pieces.Clear();
        ActiveProcess = 0;
        CompletedActions = 0;
    }
    public IEnumerator Play(ForgeMvp.Step step)
    {
        if(step.debugOutput) yield break;
        ActiveProcess = step.process;
        if(!pieces.TryGetValue(step.pieceId, out var work))
        {
            work = Instantiate(initial, transform);
            work.SetActive(true);
            work.transform.position = supply;
            pieces.Add(step.pieceId, work);
        }
        Vector3 destination = step.process==2 ? furnace : step.process==4 ? cooling : bench;
        Vector3 start = work.transform.position;
        Vector3 smithStart = smith.position;
        Vector3 smithEnd = new Vector3(destination.x-.8f,0,destination.z);
        Color from = work.GetComponent<Renderer>().material.color;
        Color to = step.heat>0 ? Color.Lerp(Color.gray,Color.red,step.heat/9f) : Color.gray;
        float elapsed = 0;
        const float duration = .8f;
        while(elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed/duration);
            float move = Mathf.SmoothStep(0,1,Mathf.Min(1,t*2));
            smith.position = Vector3.Lerp(smithStart,smithEnd,move) + Vector3.up*(Mathf.Sin(t*Mathf.PI*4)*.035f);
            work.transform.position = Vector3.Lerp(start,destination,move);
            float swing = step.process==3 ? Mathf.Sin(Mathf.Clamp01((t-.5f)*2)*Mathf.PI*4)*55 : Mathf.Sin(t*Mathf.PI)*20;
            arm.localRotation = Quaternion.Euler(0,0,swing);
            work.GetComponent<Renderer>().material.color = Color.Lerp(from,to,t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        smith.position=smithEnd;
        arm.localRotation=Quaternion.identity;
        work.transform.position=destination;
        work.transform.localScale=step.shaped ? new Vector3(.25f,.15f,2) : new Vector3(1,.4f,.5f);
        work.GetComponent<Renderer>().material.color=to;
        work.name="Workpiece · "+ForgeMaterialGuide.Find(step.material).Name;
        if(step.process>0)CompletedActions++;
        ActiveProcess=0;
    }
}
