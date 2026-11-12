using UnityEngine;

// Only the button background shimmers; text, hit area and layout stay fixed.
public sealed class ForgeGuideNewShimmer : UnityEngine.UI.BaseMeshEffect
{
 protected override void OnEnable(){base.OnEnable();if(graphic)graphic.SetVerticesDirty();}
 void Update(){if(graphic)graphic.SetVerticesDirty();}
 public override void ModifyMesh(UnityEngine.UI.VertexHelper mesh)
 {
  if(!IsActive())return;
  float glow=.5f+.5f*Mathf.Sin(Time.unscaledTime*3.5f);
  for(int i=0;i<mesh.currentVertCount;i++){
   var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,i);
   Color color=vertex.color;
   vertex.color=Color.Lerp(new Color(.78f,.43f,.08f,color.a),new Color(1f,.90f,.42f,color.a),glow);
   mesh.SetUIVertex(vertex,i);
  }
 }
 public static void Apply(Transform button)
 {
  if(!button)return;
  var selectable=button.GetComponent<UnityEngine.UI.Button>();
  var background=selectable?selectable.targetGraphic:null;
  // The legacy sprite is near-black and translucent, which suppresses vertex tint.
  if(background is UnityEngine.UI.Image surface){surface.sprite=null;surface.color=Color.white;}
  var label=button.GetComponentInChildren<UnityEngine.UI.Text>(true);
  if(label)label.color=new Color(.16f,.10f,.04f);
  if(background&&!background.GetComponent<ForgeGuideNewShimmer>())background.gameObject.AddComponent<ForgeGuideNewShimmer>();
 }
}
