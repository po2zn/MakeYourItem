using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

// Color glyphs only: preserve guide text, search, copying and wrapping metrics.
public sealed class ForgeGuideMaterialColors : BaseMeshEffect
{
 static readonly Regex Materials=new Regex(
  @"(?<![\p{L}\p{N}_])(?:"+
  string.Join("|",ForgeMaterialGuide.Entries.Select(e=>e.Name).Concat(new[]{"마력수정","수정"}).OrderByDescending(s=>s.Length).Select(Regex.Escape))+
  @")(?=$|[^\p{L}_]|(?:으로부터|에서부터|으로는|으로|에는|에서|에게|만큼|보다|처럼|까지|부터|이라|이란|이며|이고|이나|와|과|을|를|은|는|이|가|의|에|로|도|만)(?=$|[^\p{L}_]))",
  RegexOptions.CultureInvariant);
 string cached;
 bool[] material;
 public static bool[] MaterialMask(string source)
 {
  var result=new bool[source.Length];
  foreach(Match match in Materials.Matches(source))for(int i=match.Index;i<match.Index+match.Length;i++)result[i]=true;
  return result;
 }
 // Guide prose uses dark ink on paper; editor token colors target black backgrounds.
 public static Color MaterialColor(Color ink)
 {
  if(ink.grayscale>.5f)return ForgeCodeColors.TokenColor(ForgeSyntax.Kind.Material);
  switch(ForgeCodeColors.Palette){
   case 1:return new Color32(25,75,125,255);
   case 2:return ink;
   default:return new Color32(145,35,80,255);
  }
 }
 public static void Apply(Text text)
 {
  if(!text||text.GetComponentInParent<InputField>()||text.GetComponent<ForgeCodeColors>())return;
  if(!text.GetComponent<ForgeGuideMaterialColors>())text.gameObject.AddComponent<ForgeGuideMaterialColors>();
 }
 public override void ModifyMesh(VertexHelper mesh)
 {
  if(!IsActive())return;
  var action=GetComponentInParent<ForgeUIAction>();
  if(action&&action.action!=null&&action.action.StartsWith("guide-section:"))return;
  bool black=name=="GuideHeading"||(action&&action.action!=null&&action.action.StartsWith("guide-material:"));
  if(black){
   Color32 color=Color.black;
   for(int i=0;i<mesh.currentVertCount;i++){
    var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,i);
    color.a=vertex.color.a;vertex.color=color;mesh.SetUIVertex(vertex,i);
   }
   return;
  }
  string source=((Text)graphic).text??"";
  if(cached!=source||material==null){cached=source;material=MaterialMask(source);}
  var indices=ForgeCodeColors.GlyphIndices(source,mesh.currentVertCount/4);
  Color32 pink=MaterialColor(((Text)graphic).color);
  for(int quad=0;quad<indices.Count&&quad*4+3<mesh.currentVertCount;quad++){
   if(!material[indices[quad]])continue;
   for(int corner=0;corner<4;corner++){
    int index=quad*4+corner;var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,index);
    pink.a=vertex.color.a;vertex.color=pink;mesh.SetUIVertex(vertex,index);
   }
  }
 }
}
