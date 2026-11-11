using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

// Replace only the geometry of a currency suffix; underlying strings and values stay intact.
// The shader samples the coin for negative U coordinates and the font for normal coordinates.
[DisallowMultipleComponent,RequireComponent(typeof(Text))]
public sealed class ForgeGoldGlyph : BaseMeshEffect
{
    static ForgeGoldArtwork artwork;
    static readonly Regex Tags=new Regex("<[^>]*>",RegexOptions.Compiled);
    public int ReplacedGlyphs {get;private set;}
    public static bool CurrencyAt(string s,int i){
        if(i<=0||s[i]!='G'||(i+1<s.Length&&((s[i+1]>='A'&&s[i+1]<='Z')||(s[i+1]>='a'&&s[i+1]<='z'))))return false;
        int n=i-1;if("KMBTQ".IndexOf(s[n])>=0)n--;return n>=0&&char.IsDigit(s[n]);
    }
    public static void Attach(Text text){
        if(text.GetComponentInParent<InputField>(true)||text.name=="DebugText")return;
        if(!text.GetComponent<ForgeGoldGlyph>())text.gameObject.AddComponent<ForgeGoldGlyph>();
    }
    protected override void OnEnable(){base.OnEnable();if(!artwork)artwork=Resources.Load<ForgeGoldArtwork>("BlacksmithGoldArtwork");if(artwork&&artwork.uiMaterial)graphic.material=artwork.uiMaterial;}
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
            for(int k=0;k<4;k++)mesh.PopulateUIVertex(ref v[k],first+k);
            float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
            for(int k=0;k<4;k++){minX=Mathf.Min(minX,v[k].position.x);maxX=Mathf.Max(maxX,v[k].position.x);minY=Mathf.Min(minY,v[k].position.y);maxY=Mathf.Max(maxY,v[k].position.y);}
            if(maxX-minX<.001f||maxY-minY<.001f)continue;
            var uv=artwork.region;float ratio=artwork.texture.width*uv.width/(artwork.texture.height*uv.height);float w=maxX-minX,h=Mathf.Min(maxY-minY,w/ratio);w=h*ratio;
            Vector2 center=new Vector2((minX+maxX)/2,(minY+maxY)/2);
            for(int k=0;k<4;k++){
                bool right=v[k].position.x>center.x,top=v[k].position.y>center.y;
                v[k].position=new Vector3(center.x+(right?.5f:-.5f)*w,center.y+(top?.5f:-.5f)*h,0);
                v[k].uv0=new Vector2(-1-(right?uv.xMax:uv.xMin),top?uv.yMax:uv.yMin);
                v[k].color=new Color32(255,255,255,v[k].color.a);mesh.SetUIVertex(v[k],first+k);
            }
            ReplacedGlyphs++;
        }
    }
}
