using UnityEngine;
using UnityEngine.UI;

// A warm, unscaled-time border. Does not change button sprites or consume input.
public sealed class ForgeTutorialAttention : MonoBehaviour
{
    Outline border;
    Color tint=new Color(1,.76f,.22f);
    public static void Set(Selectable target,bool active,Color? color=null)
    {
        if(!target)return;
        var cue=target.GetComponent<ForgeTutorialAttention>();
        if(!cue&&!active)return;
        if(!cue)cue=target.gameObject.AddComponent<ForgeTutorialAttention>();
        cue.tint=color??new Color(1,.76f,.22f);
        cue.enabled=active;
        if(cue.border)cue.border.enabled=active;
        if(active)cue.Update();
    }
    void Update()
    {
        if(!border){border=gameObject.AddComponent<Outline>();border.useGraphicAlpha=false;}
        float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*5f);
        border.enabled=true;border.effectColor=new Color(tint.r,tint.g,tint.b,.3f+.7f*pulse);
        border.effectDistance=Vector2.one*(2+3*pulse);
    }
    void OnDisable(){if(border)border.enabled=false;}
}
