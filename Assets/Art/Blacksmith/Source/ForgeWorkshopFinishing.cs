using UnityEngine;

public sealed partial class ForgeWorkshopVisuals
{
    Transform curseStar;
    LineRenderer curseOutline,curseRing;
    Material curseInk;
    void HideCurse(){if(curseStar)curseStar.gameObject.SetActive(false);}
    void AnimateCurse(PieceView piece,float t)
    {
        if(t<.4f||t>=.97f){HideCurse();return;}
        if(!curseStar){
            curseStar=new GameObject("Curse red star").transform;curseStar.SetParent(transform,false);
            curseInk=FrostEffectMaterial();curseInk.SetColor("_BaseColor",new Color(1,.025f,.04f,.9f));
            LineRenderer Line(string name,int count){var obj=new GameObject(name);obj.transform.SetParent(curseStar,false);var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=count;line.widthMultiplier=.025f;line.numCornerVertices=3;line.sharedMaterial=curseInk;return line;}
            curseOutline=Line("Five pointed curse",5);curseRing=Line("Curse ring",48);
            for(int i=0;i<5;i++){float a=Mathf.PI*.5f+i*Mathf.PI*4/5;curseOutline.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a),0));}
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;curseRing.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*1.12f);}
        }
        float phase=Mathf.InverseLerp(.4f,.97f,t),fade=Mathf.Sin(phase*Mathf.PI);
        curseStar.gameObject.SetActive(true);
        curseStar.position=piece.root.transform.position+Vector3.up*(.3f+.16f*Mathf.Sin(phase*Mathf.PI));
        curseStar.rotation=Quaternion.LookRotation(smith.forward,Vector3.up)*Quaternion.Euler(0,0,phase*65);
        curseStar.localScale=Vector3.one*(.14f+.28f*Mathf.Sin(phase*Mathf.PI));
        curseInk.SetColor("_BaseColor",new Color(1,.025f,.04f,fade*.95f));
        var gesture=smith.TransformPoint(new Vector3(.32f,1.04f,.48f))+smith.right*(Mathf.Sin(phase*Mathf.PI*2)*.06f);
        Reach(rightArm,rightFore,rightHand,rightSocket,gesture,Quaternion.LookRotation(smith.forward,Vector3.up),false);
    }
    void AnimateHandleFitting(PieceView piece,ForgeMvp.Step step,float t)
    {
        // Crystal assembly also uses process 5; retain its existing ingredient animation.
        if(step.data==null||step.data.handle<=0||step.data.activated)return;
        var grip=Find(piece.root.transform,"Grip");var guard=Find(piece.root.transform,"Guard");var pommel=Find(piece.root.transform,"Pommel");
        bool fitted=t>=ForgeActionAudio.HandleContactTime;
        grip.gameObject.SetActive(fitted);guard.gameObject.SetActive(fitted);pommel.gameObject.SetActive(fitted);
        if(t<.4f)return;
        float insert=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,ForgeActionAudio.HandleContactTime,t));
        float release=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,t));
        Vector3 contact=grip.position+Vector3.up*.06f;
        Vector3 ready=contact-piece.root.transform.up*.16f+Vector3.up*.08f;
        Vector3 hand=Vector3.Lerp(ready,contact,insert)+smith.forward*(Mathf.Sin(Mathf.InverseLerp(.6f,.78f,t)*Mathf.PI*2)*.018f*(1-release));
        hand=Vector3.Lerp(hand,smith.TransformPoint(new Vector3(.34f,.98f,.42f)),release);
        Reach(rightArm,rightFore,rightHand,rightSocket,hand,Quaternion.LookRotation(smith.forward,Vector3.up)*Quaternion.Euler(0,0,Mathf.Sin(insert*Mathf.PI)*12),false);
        if(ingredient&&ingredient.gameObject.activeSelf)ingredient.position=rightSocket.position;
    }
}
