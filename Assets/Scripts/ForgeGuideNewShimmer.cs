using UnityEngine;

// Only the button background shimmers; text, hit area and layout stay fixed.
public sealed class ForgeGuideNewShimmer : UnityEngine.UI.BaseMeshEffect
{
 protected override void OnEnable(){base.OnEnable();if(graphic)graphic.SetVerticesDirty();}
 void Update(){if(graphic)graphic.SetVerticesDirty();}
 public override void ModifyMesh(UnityEngine.UI.VertexHelper mesh)
 {
  if(!IsActive())return;
  float glow=.12f+.28f*Mathf.Pow(.5f+.5f*Mathf.Sin(Time.unscaledTime*2.4f),3f);
  for(int i=0;i<mesh.currentVertCount;i++){
   var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,i);
   Color color=vertex.color;
   vertex.color=Color.Lerp(color,new Color(1f,.88f,.48f,color.a),glow);
   mesh.SetUIVertex(vertex,i);
  }
 }
 public static void Apply(Transform button)
 {
  if(!button)return;
  var selectable=button.GetComponent<UnityEngine.UI.Button>();
  var background=selectable?selectable.targetGraphic:null;
  if(background&&!background.GetComponent<ForgeGuideNewShimmer>())background.gameObject.AddComponent<ForgeGuideNewShimmer>();
 }
}
