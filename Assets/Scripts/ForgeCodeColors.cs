using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Keep editable text unchanged; color the generated glyphs using the full source.
public sealed class ForgeCodeColors : BaseMeshEffect
{
 public static Color TokenColor(ForgeSyntax.Kind kind){
 switch(kind){
 case ForgeSyntax.Kind.Comment:return new Color(.42f,.78f,.42f);
 case ForgeSyntax.Kind.Material:return new Color(1f,.55f,.76f);
 case ForgeSyntax.Kind.Keyword:return new Color(1f,.65f,.30f);
 case ForgeSyntax.Kind.Function:return new Color(1f,.92f,.60f);
 case ForgeSyntax.Kind.Reserved:return new Color(197f/255f,134f/255f,192f/255f,1f);
 case ForgeSyntax.Kind.Blacksmith:return new Color(.55f,.83f,1f);
 default:return Color.white;
 }}

    string cached;
    List<ForgeSyntax.Token> tokens;
    ForgeCodeInput input;
    int previousStart=-1;
    string previousVisible;

    public static void Apply(Text text)
    {
        if(!text)return;
        text.supportRichText=false;
        var colors=text.GetComponent<ForgeCodeColors>();
        if(!colors)colors=text.gameObject.AddComponent<ForgeCodeColors>();
        colors.enabled=true;
        text.SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();input=GetComponentInParent<ForgeCodeInput>();
        cached=null;tokens=null;previousStart=-1;previousVisible=null;
    }

    void LateUpdate()
    {
        if(!graphic)return;
        var label=(Text)graphic;
        string source=input?input.text:label.text;
        int start=input?input.DrawStart:0;
        if(cached!=source||previousStart!=start||previousVisible!=label.text){
            previousStart=start;previousVisible=label.text;graphic.SetVerticesDirty();
        }
    }

    // TextGenerator versions differ in whether whitespace receives a quad.
    // Match the generated quad count, allowing its trailing dummy quad.
    public static List<int> GlyphIndices(string visible,int quads)
    {
        var all=new List<int>();var noLines=new List<int>();var glyphs=new List<int>();
        for(int i=0;i<visible.Length;i++){
            all.Add(i);
            if(visible[i]!='\r'&&visible[i]!='\n')noLines.Add(i);
            if(!char.IsWhiteSpace(visible[i]))glyphs.Add(i);
        }
        foreach(var candidate in new[]{all,noLines,glyphs})if(candidate.Count==quads)return candidate;
        foreach(var candidate in new[]{all,noLines,glyphs})if(candidate.Count+1==quads)return candidate;
        return glyphs;
    }

    public override void ModifyMesh(VertexHelper mesh)
    {
        if(!IsActive())return;
        var label=(Text)graphic;
        string visible=label.text??"";
        string source=input?input.text:visible;
        int start=input?input.DrawStart:0;
        // IME composition can temporarily make the rendered slice differ from source.
        if(start<0||start+visible.Length>source.Length||string.CompareOrdinal(source,start,visible,0,visible.Length)!=0){source=visible;start=0;}
        if(cached!=source||tokens==null){cached=source;tokens=ForgeSyntax.Scan(source);}
        var indices=GlyphIndices(visible,mesh.currentVertCount/4);
        int tokenIndex=0;
        for(int quad=0;quad<indices.Count&&quad*4+3<mesh.currentVertCount;quad++){
            int index=start+indices[quad];
            while(tokenIndex<tokens.Count&&tokens[tokenIndex].start+tokens[tokenIndex].length<=index)tokenIndex++;
            var kind=tokenIndex<tokens.Count&&tokens[tokenIndex].start<=index?tokens[tokenIndex].kind:ForgeSyntax.Kind.Plain;
            Color32 color=TokenColor(kind);
            for(int corner=0;corner<4;corner++){
                int vertexIndex=quad*4+corner;var vertex=new UIVertex();
                mesh.PopulateUIVertex(ref vertex,vertexIndex);color.a=vertex.color.a;vertex.color=color;mesh.SetUIVertex(vertex,vertexIndex);
            }
        }
    }
}
