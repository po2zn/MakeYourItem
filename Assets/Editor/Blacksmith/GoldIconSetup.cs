using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
public static class GoldIconSetup
{
 public static string Build(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit Play first");AssetDatabase.Refresh();
 string path=Directory.GetFiles("Assets/Art/Blacksmith/Materials/재료","*금화.png").Single().Replace('\\','/');
 var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
 var data=ScriptableObject.CreateInstance<ForgeGoldArtwork>();data.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(path));var pixels=t.GetPixels32();int minX=t.width,minY=t.height,maxX=0,maxY=0;
 for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(pixels[y*t.width+x].a>128){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
 data.region=Rect.MinMaxRect((float)minX/t.width,(float)minY/t.height,(float)(maxX+1)/t.width,(float)(maxY+1)/t.height);Object.DestroyImmediate(t);
 var shader=Shader.Find("Forge/UI Gold Text");if(!shader)throw new System.Exception("Gold UI shader missing");var mat=new Material(shader);mat.name="GoldCurrencyText";mat.SetTexture("_GoldTex",data.texture);
 AssetDatabase.CreateAsset(mat,AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Materials/GoldCurrencyText.mat"));data.uiMaterial=mat;
 string catalog="Assets/Art/Blacksmith/Prefabs/Resources/BlacksmithGoldArtwork.asset";if(AssetDatabase.LoadAssetAtPath<ForgeGoldArtwork>(catalog))throw new System.Exception("Gold catalog already exists");AssetDatabase.CreateAsset(data,catalog);AssetDatabase.SaveAssets();return path+" -> "+catalog;
 }
}
