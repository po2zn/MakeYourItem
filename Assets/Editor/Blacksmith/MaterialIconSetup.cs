using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
public static class MaterialIconSetup
{
 const string Folder="Assets/Art/Blacksmith/Materials/재료";
 public static string ConnectTree(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit Play first");
 const string path="Assets/UI/ForgeResearchUI.prefab";var root=PrefabUtility.LoadPrefabContents(path);var icons=new ForgeMaterialHud();int count=0;
 try{foreach(var card in root.GetComponentsInChildren<ForgeTreeCard>(true)){
 var offer=ForgeTechnology.Find(card.offerId);if(offer==null||(offer.Kind!=ForgePurchaseKind.Material&&offer.Kind!=ForgePurchaseKind.MaterialUnlock))continue;
 var node=card.transform.Find("MaterialArtwork");if(!node)node=new GameObject("MaterialArtwork",typeof(RectTransform),typeof(UnityEngine.UI.RawImage)).transform;
 node.SetParent(card.transform,false);var rect=(RectTransform)node;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
 float aspect=icons.GetIconAspect(offer.Material);Vector2 size=aspect>=1?new Vector2(64,64/aspect):new Vector2(64*aspect,64);
 rect.sizeDelta=size;rect.anchoredPosition=new Vector2(18+(64-size.x)/2,-18-(64-size.y)/2);
 var image=node.GetComponent<UnityEngine.UI.RawImage>();image.texture=icons.GetIcon(offer.Material);image.uvRect=icons.GetIconUV(offer.Material);image.raycastTarget=false;image.color=Color.white;
 var title=card.title.rectTransform;title.anchoredPosition=new Vector2(94,-12);title.sizeDelta=new Vector2(188,offer.Kind==ForgePurchaseKind.Material?68:92);
 count++;
 }PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();return "Updated "+count+" material purchase/unlock cards in "+path;
 }finally{icons.Dispose();PrefabUtility.UnloadPrefabContents(root);}
 }
 public static string Build(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit Play first");AssetDatabase.Refresh();
 var catalog=ScriptableObject.CreateInstance<ForgeMaterialIcons>();string report="";
 string[] words={"바람","강철","통나무","물방울","석탄","은빛","수정","운철","얼음","용뼈"};
 for(int id=0;id<10;id++){
 string path=Directory.GetFiles(Folder,"*.png").Single(p=>Path.GetFileName(p).Contains(words[id])).Replace('\\','/');
 {var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}
 catalog.textures[id]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(path));var pixels=source.GetPixels32();int minX=source.width,minY=source.height,maxX=0,maxY=0;
 for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)if(pixels[y*source.width+x].a>128){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
 int pad=Mathf.CeilToInt(Mathf.Max(maxX-minX,maxY-minY)*.03f);minX=Mathf.Max(0,minX-pad);minY=Mathf.Max(0,minY-pad);maxX=Mathf.Min(source.width,maxX+pad+1);maxY=Mathf.Min(source.height,maxY+pad+1);
 catalog.regions[id]=Rect.MinMaxRect((float)minX/source.width,(float)minY/source.height,(float)maxX/source.width,(float)maxY/source.height);Object.DestroyImmediate(source);
 report+=id+" "+ForgeMaterialGuide.Find(id).Name+" <- "+path+"\n";
 }
 string asset="Assets/Art/Blacksmith/Prefabs/Resources/BlacksmithMaterialIcons.asset";if(AssetDatabase.LoadAssetAtPath<ForgeMaterialIcons>(asset))throw new System.Exception("Catalog already exists; edit it instead of overwriting");AssetDatabase.CreateAsset(catalog,asset);AssetDatabase.SaveAssets();File.WriteAllText("Assets/Art/Blacksmith/Documentation/MaterialIconMapping.txt",report+"Air: vertical UV flip. Original PNG pixels preserved. Water: supplied 3D droplet artwork.\n");return report;
 }
 public static string Inspect(){string report="";foreach(var path in Directory.GetFiles(Folder,"*.png")){var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(path));var pixels=t.GetPixels32();report+=Path.GetFileName(path)+" "+t.width+"x"+t.height+" transparent="+pixels.Count(c=>c.a<128)+"/"+pixels.Length+"\n";Object.DestroyImmediate(t);}return report;}
}
