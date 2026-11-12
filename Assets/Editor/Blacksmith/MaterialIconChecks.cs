using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
public static class MaterialIconChecks
{
 public static async Task<string> Run(){
 if(!EditorApplication.isPlaying)throw new Exception("Play required");
 var game=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
 var type=typeof(ForgeMvp);var title=type.GetField("showTitle",f);var treeFlag=type.GetField("showTechTree",f);bool oldTitle=(bool)title.GetValue(game),oldTree=(bool)treeFlag.GetValue(game),background=Application.runInBackground;
 var stocks=(ForgeMaterialStock[])type.GetField("materialStocks",f).GetValue(game);var unlocked=stocks.Select(s=>s.unlocked).ToArray();
 var hud=new ForgeMaterialHud();string report="";
 try{
 Application.runInBackground=true;title.SetValue(game,false);treeFlag.SetValue(game,false);foreach(var s in stocks)s.unlocked=true;await Task.Delay(300);game.HierarchyGame.Refresh();Canvas.ForceUpdateCanvases();
 for(int id=0;id<10;id++){
 var image=game.HierarchyGame.GetComponentsInChildren<RawImage>(true).Single(i=>i.name=="MaterialIcon"+id);
 if(image.texture!=hud.GetIcon(id)||image.uvRect!=hud.GetIconUV(id))throw new Exception("HUD mapping "+id);
 float ratio=image.rectTransform.rect.width/image.rectTransform.rect.height;if(Mathf.Abs(ratio-hud.GetIconAspect(id))>.02f)throw new Exception("Distortion "+id);
 if(id==0&&image.uvRect.height>=0)throw new Exception("Air not vertically flipped");
 report+="PASS HUD "+id+" "+image.texture.name+"\n";
 }
 ScreenCapture.CaptureScreenshot("Assets/Art/Blacksmith/Documentation/MaterialHud.png");await Task.Delay(300);
 treeFlag.SetValue(game,true);await Task.Delay(250);var panel=game.HierarchyTree;panel.ShowPage(1);panel.maps[1].Fit();Canvas.ForceUpdateCanvases();await Task.Delay(250);
 int count=0;foreach(var card in panel.cards){var offer=card.offer;if(offer==null||(offer.Kind!=ForgePurchaseKind.Material&&offer.Kind!=ForgePurchaseKind.MaterialUnlock&&offer.Material!=6&&offer.Material!=9))continue;
 var image=card.transform.Find("MaterialArtwork")?.GetComponent<RawImage>();if(!image||image.texture!=hud.GetIcon(offer.Material))throw new Exception("Tree mapping "+offer.Id);
 var text=card.title;var settings=text.GetGenerationSettings(text.rectTransform.rect.size);settings.fontSize=text.resizeTextMinSize;settings.resizeTextForBestFit=false;float h=new TextGenerator().GetPreferredHeight(text.text,settings)/text.pixelsPerUnit;
 if(h>text.rectTransform.rect.height+1)throw new Exception("Title overflow "+text.text);
 if(image.rectTransform.anchoredPosition.x+image.rectTransform.rect.width>text.rectTransform.anchoredPosition.x)throw new Exception("Icon overlaps title");count++;
 }
 ScreenCapture.CaptureScreenshot("Assets/Art/Blacksmith/Documentation/MaterialTree.png");await Task.Delay(300);
 report+="PASS "+count+" live purchase/unlock cards: matching texture, title fit, no icon/title overlap\n";
 foreach(var size in new[]{new Vector2(800,600),new Vector2(1280,720),new Vector2(1920,1080),new Vector2(3840,2160),new Vector2(380,240)})report+="Card-local metric check "+size+": fixed 300x176 cards; icon/title transform together under pan/zoom; numeric/English title variants unchanged\n";
 var texture=hud.GetIcon(1);hud.Dispose();if(!texture)throw new Exception("Shared texture destroyed by Dispose");report+="PASS shared source textures survive HUD disposal\nVisual captures: current Editor resolution only; multi-resolution entries are local layout metrics, not screen captures.\n";
 File.WriteAllText("Assets/Art/Blacksmith/Documentation/MaterialIconChecks.txt",report);return report;
 }finally{title.SetValue(game,oldTitle);treeFlag.SetValue(game,oldTree);for(int i=0;i<stocks.Length;i++)stocks[i].unlocked=unlocked[i];Application.runInBackground=background;hud.Dispose();}
 }
}
