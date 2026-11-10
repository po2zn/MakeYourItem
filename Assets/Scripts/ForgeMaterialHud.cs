using System;
using System.Globalization;
using UnityEngine;

// Stocks for future materials stay hidden until gameplay explicitly unlocks them.
[Serializable]
public sealed class ForgeMaterialStock
{
    [Range(2,9)] public int material;
    public bool unlocked;
    [Min(0)] public long count;
}

public sealed class ForgeMaterialHud
{
    const float IconSize=63, CellWidth=216, RowHeight=75;
    readonly Texture2D[] icons=new Texture2D[10];
    GUIStyle numberStyle,tooltipStyle;
    static readonly string[] descriptions={"", "검날의 바탕이 되는 금속입니다.", "손잡이와 화로 연료에 쓰이는 재료입니다.", "달군 작업물을 냉각하는 재료입니다.", "화로의 열을 높이는 연료입니다.", "검날을 도금하는 귀금속입니다.", "마력을 담는 수정입니다.", "별무늬를 품은 특수 금속입니다.", "서리 문양을 만드는 냉각 재료입니다.", "특수 손잡이에 쓰이는 뼈입니다."};
    static readonly string[] suffixes={"","K","M","B","T","Q"};
    public static string FormatCount(long count)
    {
        decimal value=Math.Max(0,count);int unit=0;
        while(value>=1000 && unit<suffixes.Length-1){value/=1000;unit++;}
        // Truncate so a balance just below a threshold never overstates stock.
        value=decimal.Floor(value*10)/10;
        return value.ToString("0.#",CultureInfo.InvariantCulture)+suffixes[unit];
    }
    static ForgeMaterialStock Find(ForgeMaterialStock[] stocks,int id)
    {return stocks==null?null:Array.Find(stocks,s=>s!=null&&s.material==id&&s.unlocked);}
    public Texture2D GetIcon(int id)
    {
        if(id<0||id>=icons.Length)throw new ArgumentOutOfRangeException(nameof(id));
        if(!icons[id])icons[id]=MakeIcon(id);
        return icons[id];
    }
    public float Height(float width,ForgeMaterialStock[] stocks,float top=42)
    {
        int count=1;for(int id=2;id<=9;id++)if(Find(stocks,id)!=null)count++;
        int columns=Math.Max(1,(int)((width-24)/CellWidth));
        return top+((count+columns-1)/columns)*RowHeight+4;
    }
    public void Draw(int iron,ForgeMaterialStock[] stocks,Font font,float availableWidth,float top=42)
    {
        if(numberStyle==null){
            numberStyle=new GUIStyle(GUI.skin.label){font=font,fontSize=36,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(0,0,0,0),richText=false};
            tooltipStyle=new GUIStyle(GUI.skin.box){font=font,fontSize=24,wordWrap=true,alignment=TextAnchor.UpperLeft,padding=new RectOffset(16,16,14,14),richText=false};
        }
        int columns=Math.Max(1,(int)((availableWidth-24)/CellWidth)),index=0;string tooltip=null;
        for(int id=1;id<=9;id++){
            var stock=Find(stocks,id);if(id!=1&&stock==null)continue;
            long count=id==1?iron:stock.count;
            Rect icon=new Rect(12+(index%columns)*CellWidth,top+(index/columns)*RowHeight,IconSize,IconSize);index++;
            GUI.DrawTexture(icon,GetIcon(id));
            GUI.Label(new Rect(icon.xMax+6,icon.y,CellWidth-IconSize-10,IconSize),FormatCount(count),numberStyle);
            if(icon.Contains(Event.current.mousePosition))tooltip=ForgeMaterialGuide.Entries[id].Name+" · "+count.ToString("N0",CultureInfo.InvariantCulture)+"개\n"+descriptions[id];
        }
        if(tooltip!=null){
            float width=Mathf.Min(440,Screen.width-8),height=tooltipStyle.CalcHeight(new GUIContent(tooltip),width);
            Vector2 mouse=Event.current.mousePosition;
            GUI.Box(new Rect(Mathf.Clamp(mouse.x+14,4,Mathf.Max(4,Screen.width-width-4)),Mathf.Clamp(mouse.y+32,4,Mathf.Max(4,Screen.height-height-4)),width,height),tooltip,tooltipStyle);
        }
    }
    static Texture2D MakeIcon(int id)
    {
        // Small pixel silhouettes, independent of installed emoji fonts.
        var texture=new Texture2D(24,24,TextureFormat.RGBA32,false){name="Material icon "+id,filterMode=FilterMode.Point,hideFlags=HideFlags.HideAndDontSave};
        Color[] palette={Color.clear,new Color(.62f,.72f,.82f),new Color(.65f,.36f,.16f),new Color(.2f,.65f,1f),new Color(.26f,.29f,.36f),new Color(.85f,.88f,.96f),new Color(.7f,.36f,1f),new Color(.25f,.5f,.73f),new Color(.5f,.9f,1f),new Color(.94f,.85f,.65f)};
        var pixels=new Color[24*24];var mask=new bool[24*24];
        for(int y=2;y<22;y++)for(int x=2;x<22;x++){
            bool filled;
            if(id==0)filled=(y==7&&x>=4&&x<=17)||(y==12&&x>=6&&x<=20)||(y==17&&x>=3&&x<=15); // air currents
            else if(id==2)filled=x>=5&&x<=18&&y>=4&&y<=19; // timber
            else if(id==3||id==8)filled=y<11?(x-12)*(x-12)+(y-10)*(y-10)<=64:Math.Abs(x-12)<=(22-y)/2; // drop
            else if(id==6)filled=Math.Abs(x-12)<=7-Math.Abs(y-12)/2; // crystal
            else if(id==9)filled=(Math.Abs(x-y)<=2&&x>=5&&x<=18)||((x-5)*(x-5)+(y-5)*(y-5)<16)||((x-18)*(x-18)+(y-18)*(y-18)<16);
            else if(id==4)filled=Math.Abs(x-12)+Math.Abs(y-11)<=11&&y>=4&&y<=19;
            else filled=y>=6&&y<=17&&x>=4+(y-6)/3&&x<=19; // ingot
            mask[y*24+x]=filled;
        }
        for(int y=2;y<22;y++)for(int x=2;x<22;x++)if(mask[y*24+x]){
            bool edge=!mask[y*24+x-1]||!mask[y*24+x+1]||!mask[(y-1)*24+x]||!mask[(y+1)*24+x];
            Color color=id==0?new Color(.7f,.9f,1f):palette[id];
            if(edge)color*=.55f;else if(y>13)color=Color.Lerp(color,Color.white,.3f);
            if(id==2&&(x==8||x==14)&&y>5&&y<19)color*=.65f;
            if(id==7&&(x==13||y==12)&&Math.Abs(x-13)+Math.Abs(y-12)<4)color=Color.white;
            if(id==8&&(x==12||y==10)&&y<16)color=Color.white;
            color.a=1;pixels[y*24+x]=color;
        }
        texture.SetPixels(pixels);texture.Apply(false,true);return texture;
    }
    public void Dispose(){foreach(var icon in icons)if(icon)UnityEngine.Object.Destroy(icon);}
}
