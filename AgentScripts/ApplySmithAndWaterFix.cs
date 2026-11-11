using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
if(EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var report=CopperbeardSetup.RepairShoulders();
var path=Directory.GetFiles("Assets/Art/Blacksmith/Materials/재료","*.png").Single(p=>Path.GetFileName(p).Contains("물방울")).Replace('\\','/');
var importer=(TextureImporter)AssetImporter.GetAtPath(path);
importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
var catalog=Resources.Load<ForgeMaterialIcons>("BlacksmithMaterialIcons");catalog.textures[3]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(path));var pixels=source.GetPixels32();int minX=source.width,minY=source.height,maxX=0,maxY=0;
for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)if(pixels[y*source.width+x].a>128){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
int pad=Mathf.CeilToInt(Mathf.Max(maxX-minX,maxY-minY)*.03f);minX=Mathf.Max(0,minX-pad);minY=Mathf.Max(0,minY-pad);maxX=Mathf.Min(source.width,maxX+pad+1);maxY=Mathf.Min(source.height,maxY+pad+1);
catalog.regions[3]=Rect.MinMaxRect((float)minX/source.width,(float)minY/source.height,(float)maxX/source.width,(float)maxY/source.height);Object.DestroyImmediate(source);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
report+="\nWater="+path+" region="+catalog.regions[3]+"\n"+MaterialIconSetup.ConnectTree();
return report;
