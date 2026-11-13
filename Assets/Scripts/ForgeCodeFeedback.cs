using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

// Effects touch only generated glyphs, never the source, caret, or editor layout.
public sealed class ForgeCodeFeedback:MonoBehaviour
{
 public const float Duration=.55f;
 ForgeCodeInput input;int start,end;float began=-10;bool dirty;
 public float Age=>Time.unscaledTime-began;
 public bool Active=>ForgeCodePreferences.ForgeCode&&Age<Duration;
 public static void Edited(ForgeCodeInput input,string before){
  if(!input||input.readOnly||before==input.text)return;
  ForgeSoundEffects.CodeTyped();if(!ForgeCodePreferences.ForgeCode)return;
  if(!input.feedback)input.feedback=input.gameObject.GetComponent<ForgeCodeFeedback>()??input.gameObject.AddComponent<ForgeCodeFeedback>();
  input.feedback.Pulse(input,before);
 }
 public static Vector2Int ChangedRange(string before,string after){
  int first=0;while(first<before.Length&&first<after.Length&&before[first]==after[first])first++;
  int oldEnd=before.Length,newEnd=after.Length;while(oldEnd>first&&newEnd>first&&before[oldEnd-1]==after[newEnd-1]){oldEnd--;newEnd--;}
  if(newEnd==first){first=Mathf.Max(0,first-1);newEnd=Mathf.Min(after.Length,first+1);}
  return new Vector2Int(Mathf.Max(first,newEnd-96),newEnd);
 }
 void Pulse(ForgeCodeInput target,string before){
  input=target;var range=ChangedRange(before,target.text);bool extend=Active&&range.x==end&&Age<.12f;
  start=extend?Mathf.Max(start,range.y-96):range.x;end=range.y;began=Time.unscaledTime;dirty=true;
  input.textComponent.SetVerticesDirty();var ui=GetComponentInParent<ForgeGameUI>();if(ui)ui.PulseCodeFeedback();
 }
 public Color Shade(int index,Color original,int corner){
  float age=Age;if(!Active||index<start||index>=end)return original;
  Color hot=new Color(1,.27f,.10f,original.a),steel=corner<2?new Color(.9f,.94f,1,original.a):new Color(.42f,.5f,.6f,original.a);
  if(age<.16f)return Color.Lerp(new Color(1,.65f,.25f,original.a),hot,age/.16f);
  if(age<.32f)return Color.Lerp(hot,steel,(age-.16f)/.16f);
  return Color.Lerp(steel,original,Mathf.SmoothStep(0,1,(age-.32f)/.23f));
 }
 public Vector2 Offset=>Active&&Age<.14f?new Vector2(Mathf.Sin(Age*105),Mathf.Cos(Age*85))*(.7f*Mathf.Exp(-Age*24)):Vector2.zero;
 void OnEnable(){ForgeCodePreferences.Changed+=SettingChanged;}
 void SettingChanged(){if(!ForgeCodePreferences.ForgeCode)Clear();}
 void LateUpdate(){if(input&&input.textComponent&&(Active||dirty)){input.textComponent.SetVerticesDirty();dirty=Active;}}
 void Clear(){began=-10;dirty=false;if(input&&input.textComponent)input.textComponent.SetVerticesDirty();}
 void OnDisable(){ForgeCodePreferences.Changed-=SettingChanged;Clear();}
}

public sealed partial class ForgeGameUI {public void PulseCodeFeedback(){if(game)game.PulseCodeFeedback();}}
public partial class ForgeMvp {public void PulseCodeFeedback(){ForgeCodeCameraShake.Pulse(workspaceCamera);}}
