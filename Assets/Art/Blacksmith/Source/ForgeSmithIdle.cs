using System;
using System.Linq;
using UnityEngine;

// Cosmetic only. Uses its own random stream, never the crafting RNG.
public sealed class ForgeSmithIdle : MonoBehaviour
{
    public Vector2 visitInterval = new Vector2(25,55);
    public float approachSeconds=3, perchSeconds=4, departureSeconds=2.5f;
    public enum VisitState { Waiting, Approaching, Perched, Departing }
    public VisitState State { get; private set; }
    public Transform Visitor => visitor;
    public Transform Nose => nose;
    Transform smith, spine, head, nose, visitor;
    Quaternion spineRest,headRest;
    Transform[] wings;
    Quaternion[] wingRest;
    System.Random random;
    float timer,phase,clock;
    Vector3 start,end;
    bool initialized;
    float errorAge=-1;
    public bool IsReactingToError => errorAge>=0;
    public int ErrorReactionCount {get;private set;}
    public void ReactToCodeError(){if(!initialized||IsReactingToError)return;BeginWork();errorAge=0;ErrorReactionCount++;}
    public void CancelErrorReaction(){if(!IsReactingToError)return;errorAge=-1;if(head)head.localRotation=headRest;}
    void LateUpdate(){
        if(!IsReactingToError)return;
        errorAge+=Time.unscaledDeltaTime;
        // One backward drop, a short pause, then a slower return to neutral.
        float amount=errorAge<.32f?Mathf.SmoothStep(0,1,errorAge/.32f):errorAge<.48f?1:1-Mathf.SmoothStep(0,1,(errorAge-.48f)/.65f);
        head.localRotation=headRest*Quaternion.Euler(-32*amount,0,0);
        if(errorAge>=1.13f)CancelErrorReaction();
    }
    public void Initialize(Transform character,GameObject butterfly)
    {
        smith=character;random=new System.Random(GetInstanceID());
        spine=smith.GetComponentsInChildren<Transform>().First(t=>t.name=="Spine2");
        head=smith.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");
        spineRest=spine.localRotation;headRest=head.localRotation;
        nose=smith.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="ButterflyNoseAnchor");
        if(butterfly&&nose){
            visitor=Instantiate(butterfly,transform).transform;visitor.name="Idle Butterfly";
            wings=visitor.GetComponentsInChildren<Transform>().Where(t=>t.name.EndsWith("ForeWing")||t.name.EndsWith("HindWing")).ToArray();
            wingRest=wings.Select(t=>t.localRotation).ToArray();visitor.gameObject.SetActive(false);
        }
        initialized=true;Schedule();
    }
    void Schedule(){State=VisitState.Waiting;timer=Mathf.Lerp(visitInterval.x,visitInterval.y,(float)random.NextDouble());phase=0;}
    public void RestorePose(){if(!initialized)return;spine.localRotation=spineRest;head.localRotation=headRest;}
    public void BeginWork(){CancelErrorReaction();RestorePose();if(visitor&&(State==VisitState.Perched||State==VisitState.Approaching))Depart();}
    void Depart(){State=VisitState.Departing;phase=0;start=visitor.position;end=start+smith.right*1.8f+Vector3.up*1.1f+smith.forward*.8f;}
    public void Tick(float dt,bool working)
    {
        if(!initialized)return;
        clock+=dt;
        if(!working){
            spine.localRotation=spineRest*Quaternion.Euler(Mathf.Sin(clock*1.5f)*3f,0,Mathf.Sin(clock*.65f)*.8f);
            if(!IsReactingToError)head.localRotation=headRest*Quaternion.Euler(Mathf.Sin(clock*1.5f)*1.8f,Mathf.Sin(clock*.43f)*3,0);
        }
        if(!visitor)return;
        if(working)BeginWork();
        if(State==VisitState.Waiting){
            if(working)return;
            timer-=dt;if(timer>0)return;
            State=VisitState.Approaching;phase=0;start=nose.position+smith.right*1.5f+Vector3.up*.7f+smith.forward*.8f;
            visitor.position=start;visitor.gameObject.SetActive(true);
        }
        phase+=dt;
        if(State==VisitState.Approaching){
            float t=Mathf.Clamp01(phase/Mathf.Max(.01f,approachSeconds));
            visitor.position=Vector3.Lerp(start,nose.position,Mathf.SmoothStep(0,1,t))+smith.right*(Mathf.Sin(t*Mathf.PI*4)*.12f*(1-t))+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.18f);
            visitor.rotation=nose.rotation;
            if(t>=1){State=VisitState.Perched;phase=0;}
        }
        if(State==VisitState.Perched){visitor.SetPositionAndRotation(nose.position,nose.rotation);if(phase>=perchSeconds)Depart();}
        if(State==VisitState.Departing){float t=Mathf.Clamp01(phase/Mathf.Max(.01f,departureSeconds));visitor.position=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,t))+Vector3.up*(Mathf.Sin(t*Mathf.PI*3)*.06f);if(t>=1){visitor.gameObject.SetActive(false);Schedule();}}
        // Respect the source's declared hinge ranges (forewing 0..12, hindwing 0..6 degrees).
        float flap=(1+Mathf.Sin(clock*(State==VisitState.Perched?3:48)))*.5f;
        for(int i=0;i<wings.Length;i++)wings[i].localRotation=wingRest[i]*Quaternion.Euler(flap*(wings[i].name.EndsWith("ForeWing")?12:6),0,0);
    }
    void OnDisable(){CancelErrorReaction();RestorePose();if(visitor)visitor.gameObject.SetActive(false);if(initialized)Schedule();}
}
