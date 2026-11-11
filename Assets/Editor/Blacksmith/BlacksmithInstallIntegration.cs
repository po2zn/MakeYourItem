using UnityEngine;
using UnityEditor;
using System.IO;
public static class BlacksmithInstallIntegration
{
    public static string Run(){
        const string folder="Assets/Art/Blacksmith/Prefabs/Resources";
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        string path=folder+"/BlacksmithWorkshopAssets.asset";
        var a=AssetDatabase.LoadAssetAtPath<ForgeWorkshopAssets>(path);
        if(!a){a=ScriptableObject.CreateInstance<ForgeWorkshopAssets>();AssetDatabase.CreateAsset(a,path);}
        const string source="Assets/Art/Blacksmith/Prefabs/Set_20261111_171955/";
        if(!a.blacksmith)a.blacksmith=AssetDatabase.LoadAssetAtPath<GameObject>(source+"Blacksmith.prefab");
        if(!a.forge)a.forge=AssetDatabase.LoadAssetAtPath<GameObject>(source+"Forge.prefab");
        a.bucket=AssetDatabase.LoadAssetAtPath<GameObject>(source+"QuenchBucket.prefab");
        if(!a.anvil)a.anvil=AssetDatabase.LoadAssetAtPath<GameObject>(source+"Anvil.prefab");
        a.sword=AssetDatabase.LoadAssetAtPath<GameObject>(source+"Sword.prefab");
        a.hammer=AssetDatabase.LoadAssetAtPath<GameObject>(source+"Hammer.prefab");
        if(!a.Ready)throw new System.Exception("Missing workshop prefab");
        EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();return path;
    }
}
