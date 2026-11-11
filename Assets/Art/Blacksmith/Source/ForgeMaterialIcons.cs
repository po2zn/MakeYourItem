using UnityEngine;

public sealed class ForgeMaterialIcons : ScriptableObject
{
    public Texture2D[] textures=new Texture2D[10];
    public Rect[] regions=new Rect[10];
    public bool flipAirVertical=true;
    public bool flipAirHorizontal;
    public Rect Region(int id){
        var r=regions[id];if(r.width<=0||r.height<=0)r=new Rect(0,0,1,1);
        if(id==0&&flipAirVertical){r.y+=r.height;r.height=-r.height;}
        if(id==0&&flipAirHorizontal){r.x+=r.width;r.width=-r.width;}
        return r;
    }
}
