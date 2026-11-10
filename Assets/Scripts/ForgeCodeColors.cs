using UnityEngine;using UnityEngine.UI;
// Recolor the existing text mesh without inserting markup into editable source.
public sealed class ForgeCodeColors:BaseMeshEffect {
 string cached;System.Collections.Generic.List<ForgeSyntax.Token> tokens;
 public override void ModifyMesh(VertexHelper mesh){if(!IsActive())return;var text=(Text)graphic;if(cached!=text.text||tokens==null){cached=text.text;tokens=ForgeSyntax.Scan(cached);}foreach(var token in tokens){Color color=token.kind==ForgeSyntax.Kind.Comment?new Color(.45f,.7f,.45f):token.kind==ForgeSyntax.Kind.Function?new Color(.9f,.8f,.5f):token.kind==ForgeSyntax.Kind.Reserved?new Color(.8f,.55f,.85f):token.kind==ForgeSyntax.Kind.Material?new Color(.5f,.8f,1):Color.white;for(int i=token.start;i<token.start+token.length;i++)for(int v=i*4;v<i*4+4&&v<mesh.currentVertCount;v++){var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,v);vertex.color=color;mesh.SetUIVertex(vertex,v);}}}
}
