using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ForgeTechnologyUpdateChecks {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 public static void RunBatch(){try{
 var bone=ForgeTechnology.Find("function-Refine");Check(bone.Parents.SequenceEqual(new[]{"material-7","function-Resonate"}),"Parents");
 var tech=new ForgeTechnology();tech.Restore(null,new[]{"material-7","function-Charge"});Check(tech.MissingPrerequisites(bone)!=null,"501 required");
 tech.Restore(null,new[]{"function-Resonate"});Check(tech.MissingPrerequisites(bone)!=null,"Star iron required");
 tech.Restore(null,new[]{"material-7","function-Resonate"});Check(tech.MissingPrerequisites(bone)==null,"Both parents unlock");
 tech.Restore(null,new[]{"function-Refine"});Check(tech.Owns("function-Resonate"),"Legacy purchase migration");
 var tree=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeResearchUI.prefab").GetComponent<ForgeTreePanel>();
 var edge=tree.edges.Single(e=>e.parentOffer=="function-Resonate");Check(edge.lines.Length==3,"Edge count");
 Check(edge.lines[0].rectTransform.anchoredPosition==new Vector2(2024,-712),"Edge source");Check(edge.lines[2].rectTransform.anchoredPosition+new Vector2(edge.lines[2].rectTransform.sizeDelta.x,0)==new Vector2(2404,-312),"Edge destination");
 var fuel=tree.cards.Single(c=>c.offerId=="function-Fuel");var dragon=tree.cards.Single(c=>c.offerId=="function-Refine");
 Check(fuel.GetComponentInChildren<RawImage>(true).texture==dragon.GetComponentInChildren<RawImage>(true).texture,"Dragon bone image");
 var canvas=new GameObject("Verification",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
 var camera=new GameObject("Camera",typeof(Camera)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.18f);camera.orthographic=true;canvas.worldCamera=camera;canvas.planeDistance=10;
 var clone=UnityEngine.Object.Instantiate(fuel,canvas.transform,false);var rect=(RectTransform)clone.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;clone.title.text=ForgeTechnology.Find("function-Fuel").Title;clone.state.text="선행 구매 필요";clone.price.text="2.4KG";
 foreach(var size in new[]{new Vector2Int(640,480),new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)}){
 var rt=new RenderTexture(size.x,size.y,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();Check(clone.title.preferredHeight<=clone.title.rectTransform.rect.height+1,"Title clipped "+size);var icon=clone.GetComponentInChildren<RawImage>();Check(icon.rectTransform.anchoredPosition.x+icon.rectTransform.sizeDelta.x<clone.title.rectTransform.anchoredPosition.x,"Icon overlap");camera.Render();RenderTexture.active=rt;var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes("Artifacts/TechnologyUpdate/card-"+size.x+".png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
 }
 File.WriteAllText("Artifacts/TechnologyUpdate/results.txt","PASS prerequisite gates, legacy migration, edge endpoints, bone artwork, text metrics at 640/800/1280/1920/3840.");Debug.Log("TECHNOLOGY_UPDATE_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);File.WriteAllText("Artifacts/TechnologyUpdate/failure.txt",e.ToString());EditorApplication.Exit(1);}}
}
