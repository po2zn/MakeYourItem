using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Code-native 2D schematic: all text stays in the same logical canvas while panning/zooming.
public sealed class ForgeCoolingDiagram : MaskableGraphic {
    ForgeCoolingViewState state;
    readonly List<Text> labels=new List<Text>();Font font;
    public void Show(ForgeCoolingViewState view,BlueprintObservation observation,string distance,string previous,string visited,Font textFont){
        state=view;font=textFont;int used=0;var graph=view.Blade?.data.cooling;
        var bone=view.Blade?.material==9?view.Blade.data.dragonBone:null;
        if(bone!=null){
            Label(ref used,new Vector2(.5f,.94f),new Vector2(580,40),"최초 기준 0 ─ 전체 길이 "+bone.Length+" · 번호와 위치는 절단 후에도 고정",17,TextAnchor.MiddleCenter);
            for(int i=0;i<bone.Count;i++){int pos=bone.Position(i);float y=.83f-i*.10f;Label(ref used,new Vector2(.5f,y+.025f),new Vector2(580,32),"마디 "+i+" · 위치 "+pos+" · "+(bone.IsCut(pos)?"절단 완료":"미절단")+(pos==view.BonePosition?" ← 이번 대상":""),17,TextAnchor.MiddleCenter);}
            var parts=new List<string>();int left=0;for(int i=0;i<bone.Count;i++)if(bone.IsCut(bone.Position(i))){parts.Add("["+left+", "+bone.Position(i)+"]");left=bone.Position(i);}parts.Add("["+left+", "+bone.Length+"]");
            Label(ref used,new Vector2(.5f,.09f),new Vector2(600,76),"현재 조각: "+string.Join(" · ",parts)+"\n"+(view.BonePosition<0?bone.State:"이번 구간 ["+view.BoneLeft+", "+view.BoneRight+"] · 비용 "+(view.BoneRight-view.BoneLeft)),16,TextAnchor.MiddleCenter);
        }
        if(graph!=null&&bone==null){
            IList<int> d=null,p=null,v=null;if(observation.Enabled){observation.BindArray(distance,graph.Count,out d,out _);observation.BindArray(previous,graph.Count,out p,out _);observation.BindArray(visited,graph.Count,out v,out _);}
            for(int i=0;i<graph.Count;i++){
                var point=graph.Position(i);Label(ref used,point,new Vector2(38,38),i.ToString(),18,TextAnchor.MiddleCenter);
                if(i==graph.Start||i==graph.End)Label(ref used,point+new Vector2(0,i==graph.Start?.055f:-.055f),new Vector2(160,32),i==graph.Start?"입구":"출구",18,TextAnchor.MiddleCenter);
                string info="누적: "+(d==null?"—":d[i].ToString())+"\n이전: "+(p==null?"—":p[i].ToString())+"\n방문: "+(v==null?"—":v[i].ToString());
                var location=point.x<.5f?new Vector2(.115f,point.y):point.x>.5f?new Vector2(.885f,point.y):new Vector2(.78f,i==graph.Start?.93f:.07f);
                Label(ref used,location,new Vector2(142,66),info,14,TextAnchor.MiddleCenter);
            }
            for(int a=0;a<graph.Count;a++)for(int b=a+1;b<graph.Count;b++)if(graph.Cost(a,b)>0){var mid=CostLabelPosition(graph,a,b);Label(ref used,mid,new Vector2(108,30),"서리수 "+ForgeCoolingGraph.StockUnits(graph.Cost(a,b))+"개",15,TextAnchor.MiddleCenter);}
        }
        for(int i=used;i<labels.Count;i++)labels[i].transform.parent.gameObject.SetActive(false);SetVerticesDirty();
    }
    public static Vector2 CostLabelPosition(ForgeCoolingGraph graph,int a,int b){var mid=(graph.Position(a)+graph.Position(b))*.5f;mid.x=mid.x<.49f?.39f:mid.x>.51f?.61f:.5f;if(Math.Abs(mid.x-.5f)<.01f)mid.y=graph.Count==5?.43f:graph.Count<8?.45f:.5f;return mid;}
    void Label(ref int index,Vector2 position,Vector2 size,string text,int fontSize,TextAnchor alignment){
        if(index==labels.Count){var g=new GameObject("도면 설명",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));g.transform.SetParent(transform,false);g.GetComponent<Image>().color=new Color(.94f,.97f,1,.92f);g.GetComponent<Image>().raycastTarget=false;var inner=new GameObject("글씨",typeof(RectTransform),typeof(Text));inner.transform.SetParent(g.transform,false);var r=(RectTransform)inner.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var label=inner.GetComponent<Text>();label.raycastTarget=false;label.color=new Color(.08f,.16f,.26f);label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;labels.Add(label);}
        var t=labels[index++];t.transform.parent.gameObject.SetActive(true);t.gameObject.SetActive(true);t.font=font;t.fontSize=fontSize;t.alignment=alignment;t.text=text;var outer=(RectTransform)t.transform.parent;outer.anchorMin=outer.anchorMax=position;outer.pivot=new Vector2(.5f,.5f);outer.anchoredPosition=Vector2.zero;outer.sizeDelta=size;outer.GetComponent<Image>().color=new Color(.94f,.97f,1,size.x==38?0:.92f);
    }
    Vector2 Point(Vector2 normalized)=>new Vector2(rectTransform.rect.xMin+normalized.x*rectTransform.rect.width,rectTransform.rect.yMin+normalized.y*rectTransform.rect.height);
    static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color){int i=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color){var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Quad(vh,a+n,b+n,b-n,a-n,color);}
    static void Circle(VertexHelper vh,Vector2 center,float radius,Color color){int first=vh.currentVertCount;vh.AddVert(center,color,Vector2.zero);for(int i=0;i<=24;i++){float angle=i*Mathf.PI*2/24;vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,color,Vector2.zero);if(i>0)vh.AddTriangle(first,first+i,first+i+1);}}
    static void Arrow(VertexHelper vh,Vector2 from,Vector2 to){var direction=(to-from).normalized;var middle=Vector2.Lerp(from,to,.58f);var normal=new Vector2(-direction.y,direction.x);int i=vh.currentVertCount;vh.AddVert(middle+direction*12,new Color(.03f,.25f,.6f),Vector2.zero);vh.AddVert(middle-direction*9+normal*8,new Color(.03f,.25f,.6f),Vector2.zero);vh.AddVert(middle-direction*9-normal*8,new Color(.03f,.25f,.6f),Vector2.zero);vh.AddTriangle(i,i+1,i+2);}
    protected override void OnPopulateMesh(VertexHelper vh){
        vh.Clear();var bone=state?.Blade?.material==9?state.Blade.data.dragonBone:null;if(bone!=null){for(int i=0;i<bone.Count;i++){int pos=bone.Position(i);float y=.80f-i*.10f;var a=Point(new Vector2(.08f,y));var b=Point(new Vector2(.92f,y));Line(vh,a,b,4,new Color(.4f,.3f,.2f));if(pos==state.BonePosition)Line(vh,Point(new Vector2(.08f+.84f*state.BoneLeft/bone.Length,y)),Point(new Vector2(.08f+.84f*state.BoneRight/bone.Length,y)),12,new Color(.4f,.55f,.8f));for(int j=0;j<bone.Count;j++){int joint=bone.Position(j);float x=.08f+.84f*joint/bone.Length;Line(vh,Point(new Vector2(x,y-.012f)),Point(new Vector2(x,y+.012f)),bone.IsCut(joint)?5:2,bone.IsCut(joint)?Color.black:Color.gray);}float marker=.08f+.84f*pos/bone.Length;Circle(vh,Point(new Vector2(marker,y)),6,Color.white);}return;}if(state?.Blade?.data.cooling==null)return;var g=state.Blade.data.cooling;
        Quad(vh,Point(new Vector2(.44f,.12f)),Point(new Vector2(.56f,.12f)),Point(new Vector2(.55f,.83f)),Point(new Vector2(.45f,.83f)),new Color(.68f,.75f,.82f));
        int tip=vh.currentVertCount;vh.AddVert(Point(new Vector2(.45f,.83f)),Color.gray,Vector2.zero);vh.AddVert(Point(new Vector2(.55f,.83f)),Color.gray,Vector2.zero);vh.AddVert(Point(new Vector2(.5f,.96f)),Color.gray,Vector2.zero);vh.AddTriangle(tip,tip+1,tip+2);
        for(int a=0;a<g.Count;a++)for(int b=a+1;b<g.Count;b++)if(g.Cost(a,b)>0){Line(vh,Point((g.Position(a)+g.Position(b))*.5f),Point(CostLabelPosition(g,a,b)),1,new Color(.4f,.5f,.6f));bool active=state.A==a&&state.B==b||state.A==b&&state.B==a;Line(vh,Point(g.Position(a)),Point(g.Position(b)),active?7:3,active?new Color(.04f,.3f,.65f):new Color(.25f,.35f,.45f));}
        if(state.Path!=null)for(int i=1;i<state.Path.Length;i++){var a=Point(g.Position(state.Path[i-1]));var b=Point(g.Position(state.Path[i]));Line(vh,a,b,6,new Color(.08f,.5f,.85f));Arrow(vh,a,b);}
        for(int i=0;i<g.Count;i++){var p=Point(g.Position(i));bool active=i==state.A||i==state.B;Circle(vh,p,active?25:22,new Color(.08f,.2f,.35f));Circle(vh,p,active?19:19,new Color(.8f,.92f,1));}
        if(state.Path!=null&&state.Path.Length>1&&state.Progress<1){float t=state.Progress*(state.Path.Length-1);int edge=Math.Min(state.Path.Length-2,(int)t);var p=Vector2.Lerp(Point(g.Position(state.Path[edge])),Point(g.Position(state.Path[edge+1])),t-edge);Circle(vh,p,13,new Color(.1f,.7f,1,.5f));Circle(vh,p,7,Color.white);}
    }
}
