using System.Linq;
using UnityEngine;

// Reconstruct visual features from the saved result, never by re-running a blueprint.
public sealed class ForgeSwordAppearance : MonoBehaviour
{
    Transform details;
    Transform Part(string name)=>transform.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    public static Color Metal(int id)=>id==2?new Color(.42f,.23f,.10f):id==5?new Color(.83f,.88f,.94f):id==7?new Color(.30f,.36f,.67f):id==9?new Color(.90f,.84f,.64f):new Color(.48f,.52f,.56f);
    public static Color BladeColor(int id,ForgeItemData d){
        var color=Metal(id);
        if(d!=null){if(d.plated)color=Metal(5);if(d.star&&!d.plated)color=Metal(7);if(d.frost&&!d.plated)color=new Color(.4f,.78f,.88f);}
        return color;
    }
    static void Paint(Renderer r,Color color,float gloss=.35f,float glow=0){
        var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);
        block.SetColor("_BaseColor",color);block.SetColor("_Color",color);
        block.SetFloat("_Smoothness",gloss);block.SetColor("_EmissionColor",color*glow);r.SetPropertyBlock(block);
    }
    public void Apply(int material,bool shaped,ForgeItemData data,float heat=0,bool paintBlade=true)
    {
        var d=data??new ForgeItemData();
        if(details){details.gameObject.SetActive(false);Destroy(details.gameObject);}
        details=new GameObject("Craft details").transform;details.SetParent(transform,false);
        var blade=Part("Blade");
        blade.localScale=shaped?new Vector3(material==7?1.18f:1,1,1):new Vector3(2.2f,.35f,2.2f);
        bool handle=shaped&&d.handle>0;
        foreach(var name in new[]{"Guard","Grip","Pommel"})Part(name).gameObject.SetActive(handle);
        // Material-dependent furniture, with no change to gameplay dimensions or prices.
        Part("Grip").localScale=new Vector3(d.handle==9?1.4f:1,1,d.handle==9?1.35f:1);
        Part("Guard").localScale=new Vector3(d.handle==7?1.3f:d.handle==9?1.15f:1,1,1);
        Part("Pommel").localScale=Vector3.one*(d.handle==9?1.3f:d.handle==5?1.12f:1);
        Paint(Part("Grip").GetComponent<Renderer>(),Metal(d.handle),d.handle==2?.15f:.5f);
        foreach(var name in new[]{"Guard","Pommel"})Paint(Part(name).GetComponent<Renderer>(),d.handle==2?Metal(1):Metal(d.handle),.55f);
        if(d.cursed)Paint(Part("Guard").GetComponent<Renderer>(),new Color(.3f,.08f,.4f),.65f,.2f);
        if(d.luckyDay>0)Paint(Part("Pommel").GetComponent<Renderer>(),new Color(1,.7f,.18f),.7f,.15f);
        if(paintBlade){
            Color cold=BladeColor(material,d);float hot=Mathf.Clamp01(heat/1200f);
            Paint(blade.GetComponent<Renderer>(),Color.Lerp(cold,new Color(1,.22f,.025f),hot*.6f),d.polished?.72f:.35f);
            var block=new MaterialPropertyBlock();blade.GetComponent<Renderer>().GetPropertyBlock(block);block.SetColor("_EmissionColor",new Color(1,.16f,.015f)*hot*2.5f);blade.GetComponent<Renderer>().SetPropertyBlock(block);
        }
        var surface=blade.GetComponent<Renderer>().sharedMaterial;
        void Mark(string name,Vector3 pos,Vector3 scale,Color color,float angle=0,float glow=0){
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(details,false);
            g.transform.localPosition=pos;g.transform.localScale=scale;g.transform.localRotation=Quaternion.Euler(0,0,angle);
            var collider=g.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var r=g.GetComponent<Renderer>();r.sharedMaterial=surface;Paint(r,color,.6f,glow);
        }
        if(!shaped)return;
        foreach(float side in new[]{-1f,1f}){
            if(d.star)for(int i=0;i<4;i++)Mark("Star inlay",new Vector3((i%2==0?-.016f:.016f),.31f+i*.13f,side*.018f),new Vector3(.018f,.018f,.004f),new Color(.8f,.87f,1),45,.25f);
            if(d.frost)for(int i=0;i<5;i++)Mark("Frost etching",new Vector3(0,.3f+i*.1f,side*.020f),new Vector3(.055f,.012f,.005f),Color.cyan,i%2==0?30:-30,.35f);
            for(int i=0;i<d.runes.Count;i++){
                int rune=d.runes[i],family=(rune-1)%3;Color color=family==0?new Color(1,.35f,.15f):family==1?Color.cyan:new Color(.8f,.35f,1);
                float y=.25f+i*.19f;
                Mark("Rune "+rune,new Vector3(0,y,side*.026f),new Vector3(.035f,.05f,.008f),color,45,.8f);
                for(int grade=0;grade<(rune-1)/3+1;grade++)Mark("Rune grade",new Vector3((grade-1)*.014f,y+.05f,side*.026f),new Vector3(.007f,.012f,.006f),color,0,.6f);
            }
            if(d.cracked)for(int i=0;i<3;i++)Mark("Blade crack",new Vector3(0,.48f+i*.035f,side*.032f),new Vector3(.038f,.006f,.003f),new Color(.07f,.06f,.08f),i%2==0?35:-35);
            for(int i=0;i<d.magicGrade;i++)Mark("Magic grade",new Vector3(0,.17f+i*.025f,side*.024f),new Vector3(.045f,.008f,.004f),new Color(.4f,.65f,1),0,.6f);
        }
        if(d.activated||d.charge>0)Mark("Charged crystal",new Vector3(0,-.125f,-.045f),Vector3.one*(.035f+.009f*d.charge),Color.cyan,45,.4f+d.charge*.25f);
        for(int i=0;i<d.seals.Count;i++)Mark("Seal "+d.seals[i],new Vector3(0,.3f+i*.055f,0),new Vector3(.10f,.025f,.055f),new Color(.3f,.6f,1));
    }
}
