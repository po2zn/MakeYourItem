using UnityEngine;

public sealed partial class ForgeGameUI
{
 readonly float[] materialUseUntil=new float[10];
 readonly Transform[] materialUseIcons=new Transform[10];
 readonly Vector3[] materialUseScale=new Vector3[10];
 public void AnimateMaterialUse(int material)
 {
  if(material<1||material>=materialUseUntil.Length)return;
  if(!materialUseIcons[material]){
   if(!nodes.TryGetValue("MaterialIcon"+material,out var icon))return;
   materialUseIcons[material]=icon;materialUseScale[material]=icon.localScale;
  }
  materialUseUntil[material]=Time.unscaledTime+.55f;
 }
 void UpdateMaterialUseAnimation()
 {
  for(int i=1;i<materialUseIcons.Length;i++){
   var icon=materialUseIcons[i];if(!icon)continue;
   float remaining=materialUseUntil[i]-Time.unscaledTime;
   float scale=1;
   if(remaining>0){float t=1-remaining/.55f;scale=1+.4f*Mathf.Sin(Mathf.PI*t)*Mathf.Exp(-1.4f*t);}
   icon.localScale=materialUseScale[i]*scale;
  }
 }
 void ResetMaterialUseAnimation()
 {
  for(int i=1;i<materialUseIcons.Length;i++){
   materialUseUntil[i]=0;
   if(materialUseIcons[i])materialUseIcons[i].localScale=materialUseScale[i];
  }
 }
}
