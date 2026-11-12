using UnityEditor;
using UnityEngine;
using System.IO;

public static class ForgeTitleRackBuilder
{
    public static string Build(){
        const string prefab="Assets/Resources/BlacksmithTitleRack.prefab";
        if(File.Exists(prefab))return "Existing rack preserved";
        const string folder="Assets/Art/Blacksmith/Materials";
        Directory.CreateDirectory(folder);
        var shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
        Material Make(string name,Color color,float metallic){
            string path=folder+"/"+name+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;
            var m=new Material(shader){name=name,color=color};m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",metallic>.5f?.5f:.15f);
            AssetDatabase.CreateAsset(m,path);return m;
        }
        var wood=Make("TitleRackWood",new Color(.29f,.14f,.065f),0);
        var metal=Make("TitleRackIron",new Color(.16f,.18f,.21f),.75f);
        var root=new GameObject("BlacksmithTitleRack");
        try{
            void Block(string name,Vector3 position,Vector3 scale,Material mat,float angle=0){
                var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root.transform,false);
                g.transform.localPosition=position;g.transform.localScale=scale;g.transform.localRotation=Quaternion.Euler(0,0,angle);
                g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());
            }
            Block("Wood plinth",new Vector3(0,.025f,0),new Vector3(2.25f,.05f,.95f),wood);
            foreach(float x in new[]{-.91f,.91f}){
                Block("Wood upright",new Vector3(x,1.05f,0),new Vector3(.14f,2.1f,.16f),wood);
                Block("Foot",new Vector3(x,.08f,0),new Vector3(.3f,.16f,.72f),wood);
                Block("Iron collar",new Vector3(x,.4f,0),new Vector3(.155f,.12f,.175f),metal);
            }
            Block("Top beam",new Vector3(0,1.92f,0),new Vector3(2.05f,.22f,.18f),wood);
            Block("Lower beam",new Vector3(0,.48f,.04f),new Vector3(1.82f,.13f,.14f),wood);
            for(int i=0;i<3;i++){
                float x=(i-1)*.55f;
                var slot=new GameObject("Slot"+i).transform;slot.SetParent(root.transform,false);slot.localPosition=new Vector3(x,1.74f,-.22f);
                foreach(float side in new[]{-.10f,.10f}){
                    Block("Guard hook",new Vector3(x+side,1.61f,-.16f),new Vector3(.045f,.045f,.3f),metal);
                    Block("Hook lip",new Vector3(x+side,1.65f,-.29f),new Vector3(.045f,.12f,.045f),metal);
                }
                Block("Iron bolt",new Vector3(x,1.94f,-.105f),new Vector3(.06f,.06f,.04f),metal,45);
            }
            PrefabUtility.SaveAsPrefabAsset(root,prefab);AssetDatabase.SaveAssets();return prefab;
        }finally{Object.DestroyImmediate(root);}
    }
}
