using UnityEngine;
using UnityEngine.UI;

// Geometry keeps the resize cue sharp at every UI scale, without font glyph dependencies.
public sealed class ForgeResizeArrow : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var r=GetPixelAdjustedRect();
        Vector2 Point(float x,float y)=>new Vector2(r.xMin+r.width*x,r.yMin+r.height*y);
        void Line(Vector2 a,Vector2 b){var normal=new Vector2(-(b-a).y,(b-a).x).normalized*Mathf.Max(1,r.width*.055f);int first=mesh.currentVertCount;mesh.AddVert(a-normal,color,Vector2.zero);mesh.AddVert(a+normal,color,Vector2.zero);mesh.AddVert(b+normal,color,Vector2.zero);mesh.AddVert(b-normal,color,Vector2.zero);mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);}
        Line(Point(.23f,.77f),Point(.77f,.23f));
        Line(Point(.23f,.77f),Point(.23f,.48f));Line(Point(.23f,.77f),Point(.52f,.77f));
        Line(Point(.77f,.23f),Point(.48f,.23f));Line(Point(.77f,.23f),Point(.77f,.52f));
    }
    public static void AddTo(RectTransform handle)
    {
        if(handle.Find("ResizeArrow"))return;
        var child=new GameObject("ResizeArrow",typeof(RectTransform),typeof(CanvasRenderer),typeof(ForgeResizeArrow));child.transform.SetParent(handle,false);
        var rect=(RectTransform)child.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var arrow=child.GetComponent<ForgeResizeArrow>();arrow.color=Color.white;arrow.raycastTarget=false;
        var hint=handle.GetComponent<ForgeUIHover>();if(!hint)hint=handle.gameObject.AddComponent<ForgeUIHover>();hint.description="드래그하여 코드창 크기 조절";
    }
}
