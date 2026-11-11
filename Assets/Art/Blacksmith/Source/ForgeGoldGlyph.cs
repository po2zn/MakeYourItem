using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

// Display currency before its amount; underlying strings and values stay intact.
// The shader samples the coin for negative U coordinates and the font for normal coordinates.
[DisallowMultipleComponent,RequireComponent(typeof(Text))]
public sealed class ForgeGoldGlyph : BaseMeshEffect
{
    static ForgeGoldArtwork artwork,diamondArtwork;
    static readonly Regex Tags=new Regex("<[^>]*>",RegexOptions.Compiled);
    public int ReplacedGlyphs {get;private set;}
    public static bool CurrencyAt(string s,int i){
        if(i<=0||(s[i]!='G'&&s[i]!='D')||(i+1<s.Length&&((s[i+1]>='A'&&s[i+1]<='Z')||(s[i+1]>='a'&&s[i+1]<='z'))))return false;
        int n=i-1;if("KMBTQ".IndexOf(s[n])>=0)n--;return n>=0&&char.IsDigit(s[n]);
    }
    public static int AmountStart(string value,int suffix){
        int start=suffix-1;if(start>=0&&"KMBTQ".IndexOf(value[start])>=0)start--;
        while(start>=0&&(char.IsDigit(value[start])||value[start]=='.'||value[start]==','))start--;
        if(start>=0&&(value[start]=='+'||value[start]=='-'))start--;
        return start+1;
    }
    public static void Attach(Text text){
        if(text.GetComponentInParent<InputField>(true)||text.name=="DebugText")return;
        if(!text.GetComponent<ForgeGoldGlyph>())text.gameObject.AddComponent<ForgeGoldGlyph>();
    }
    protected override void OnEnable(){base.OnEnable();if(!artwork)artwork=Resources.Load<ForgeGoldArtwork>("BlacksmithGoldArtwork");if(!diamondArtwork)diamondArtwork=Resources.Load<ForgeGoldArtwork>("BlacksmithDiamondArtwork");if(artwork&&artwork.uiMaterial)graphic.material=artwork.uiMaterial;}
    public override void ModifyMesh(VertexHelper mesh){
        ReplacedGlyphs=0;if(!IsActive()||!artwork||!artwork.texture)return;
        var text=(Text)graphic;string visible=text.supportRichText?Tags.Replace(text.text,""):text.text;
        var v=new UIVertex[4];
        int quad=0;
        for(int i=0;i<visible.Length;i++){
            // Unity Text emits geometry only for visible glyphs, not spaces/newlines.
            if(char.IsWhiteSpace(visible[i])||char.IsControl(visible[i]))continue;
            int first=quad++*4;if(first+3>=mesh.currentVertCount)break;
            if(!CurrencyAt(visible,i))continue;
            bool diamond=visible[i]=='D';var currency=diamond?diamondArtwork:artwork;if(!currency||!currency.texture)continue;
            for(int k=0;k<4;k++)mesh.PopulateUIVertex(ref v[k],first+k);
            float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
            for(int k=0;k<4;k++){minX=Mathf.Min(minX,v[k].position.x);maxX=Mathf.Max(maxX,v[k].position.x);minY=Mathf.Min(minY,v[k].position.y);maxY=Mathf.Max(maxY,v[k].position.y);}
            if(maxX-minX<.001f||maxY-minY<.001f)continue;
            int amountFirst=first-(i-AmountStart(visible,i))*4;
            var point=new UIVertex();float amountLeft=float.MaxValue,amountRight=float.MinValue;
            for(int j=amountFirst;j<first;j++){mesh.PopulateUIVertex(ref point,j);amountLeft=Mathf.Min(amountLeft,point.position.x);amountRight=Mathf.Max(amountRight,point.position.x);}
            var uv=currency.region;float ratio=currency.texture.width*uv.width/(currency.texture.height*uv.height);
            float glyphHeight=maxY-minY,h=glyphHeight*1.15f,w=h*ratio,gap=Mathf.Max(3,glyphHeight*.25f);
            float extra=w+gap-(maxX-amountRight);
            float alignment=(int)text.alignment%3*.5f;
            Vector2 originalCenter=new Vector2((minX+maxX)/2,(minY+maxY)/2);
            // Reserve both the larger coin and its gap, preserving the line's alignment.
            for(int q=0;q<mesh.currentVertCount;q+=4){
                if(q==first)continue;
                float bottom=float.MaxValue,top=float.MinValue;
                for(int k=0;k<4;k++){mesh.PopulateUIVertex(ref point,q+k);bottom=Mathf.Min(bottom,point.position.y);top=Mathf.Max(top,point.position.y);}
                if(Mathf.Abs((bottom+top)*.5f-originalCenter.y)>glyphHeight*.75f)continue;
                float shift=q<amountFirst?-extra*alignment:q<first?w+gap-extra*alignment:extra*(1-alignment);
                for(int k=0;k<4;k++){mesh.PopulateUIVertex(ref point,q+k);point.position.x+=shift;mesh.SetUIVertex(point,q+k);}
            }
            Vector2 center=new Vector2(amountLeft-extra*alignment+w*.5f,originalCenter.y);
            for(int k=0;k<4;k++){
                bool right=v[k].position.x>originalCenter.x,top=v[k].position.y>originalCenter.y;
                v[k].position=new Vector3(center.x+(right?.5f:-.5f)*w,center.y+(top?.5f:-.5f)*h,0);
                v[k].uv0=new Vector2((diamond?-3:-1)-(right?uv.xMax:uv.xMin),top?uv.yMax:uv.yMin);
                v[k].color=new Color32(255,255,255,v[k].color.a);mesh.SetUIVertex(v[k],first+k);
            }
            ReplacedGlyphs++;
        }
    }
}
