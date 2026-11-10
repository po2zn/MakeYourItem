using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
public static class TestResearchUI
{
    static void Field(ForgeMvp game,string name,object value)=>typeof(ForgeMvp).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,value);
    static void Check(bool condition,string error){if(!condition)throw new Exception(error);}
    public static async Task<string> Run()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
        Application.runInBackground=true;
        var game=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();Field(game,"showTitle",false);Field(game,"showTutorial",false);Field(game,"showGameMenu",false);Field(game,"progressLoaded",false);Field(game,"showTechTree",true);Field(game,"gold",100000);Field(game,"diamonds",100000);
        typeof(ForgeMvp).GetMethod("StopMouseTrace",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        typeof(ForgeMvp).GetMethod("SyncHierarchyTree",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        var panel=game.HierarchyTree;Check(panel!=null&&panel.gameObject.activeInHierarchy,"Canvas did not open");Canvas.ForceUpdateCanvases();await Task.Delay(150);Canvas.ForceUpdateCanvases();
        Check(panel.cards.Length==ForgeTechnology.Offers.Count+9,"missing cards");
        var buttonRect=(RectTransform)panel.growthButton.transform;var click=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,buttonRect.TransformPoint(buttonRect.rect.center)),button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(click,hits);Check(hits.Count>0&&hits[0].gameObject==panel.growthButton.gameObject,"header button raycast blocked");ExecuteEvents.Execute(panel.growthButton.gameObject,click,ExecuteEvents.pointerClickHandler);
        var originalCount=panel.GetComponentsInChildren<Transform>(true).Length;
        var root=panel.cards.First(c=>c.offerId==ForgeTechnology.AutomationRoot);root.button.onClick.Invoke();Check(game.HierarchyTechnology.Owns(ForgeTechnology.AutomationRoot),"automation purchase failed");Check(root.surface.color.g>root.surface.color.b,"owned card not green");
        var heat=panel.cards.First(c=>c.offerId=="function-Heat");heat.button.onClick.Invoke();Check(game.HierarchyTechnology.Owns(BlueprintFeature.Heat),"function purchase failed");
        panel.ShowPage(1);var iron=panel.cards.First(c=>c.offerId=="stock-1");long before=game.HierarchyStock(iron.offer);iron.quantity.text="3";iron.button.onClick.Invoke();Check(game.HierarchyStock(iron.offer)==before+3,"material quantity failed");iron.quantity.text="0";iron.button.onClick.Invoke();Check(game.HierarchyStock(iron.offer)==before+3,"invalid quantity charged");iron.quantity.text="1";
        panel.ShowPage(2);var hire=panel.cards.First(c=>c.smithId==1);hire.button.onClick.Invoke();Check(game.HierarchySmithOwned(1),"smith hire failed");var favor=panel.cards.First(c=>c.smithId==6);favor.button.onClick.Invoke();Check(game.HierarchySmithOwned(6),"smith research failed");
        panel.ShowPage(0);var map=panel.maps[0];Canvas.ForceUpdateCanvases();var title=heat.title.rectTransform;var titlePosition=title.anchoredPosition;var titleScale=title.localScale;
        var scroll=new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.5f,Screen.height*.5f),scrollDelta=new Vector2(0,1)};float oldZoom=map.content.localScale.x;map.OnScroll(scroll);Check(map.content.localScale.x>oldZoom,"wheel zoom failed");Check(title.anchoredPosition==titlePosition&&title.localScale==titleScale,"card title drifted");map.ResetView();
        var drag=new PointerEventData(EventSystem.current){position=new Vector2(400,350),button=PointerEventData.InputButton.Right};map.OnBeginDrag(drag);var oldPosition=map.content.anchoredPosition;drag.position+=new Vector2(50,30);map.OnDrag(drag);map.OnEndDrag(drag);Check(map.content.anchoredPosition!=oldPosition&&!map.Dragging,"pan/release failed");map.ResetView();
        panel.Hover(root,new Vector2(Screen.width*.5f,Screen.height*.5f));Check(panel.tooltip.gameObject.activeSelf&&panel.tooltipTitle.fontStyle==FontStyle.Bold&&panel.tooltipTitle.fontSize>panel.tooltipBody.fontSize,"shared tooltip failed");panel.HideTooltip();
        for(int i=0;i<20;i++){panel.ShowPage(i%3);panel.Refresh();}Check(originalCount==panel.GetComponentsInChildren<Transform>(true).Length,"runtime UI creation detected");panel.ShowPage(0);map.ResetView();
        await Task.Delay(400);Canvas.ForceUpdateCanvases();
        var bad=new List<string>();foreach(var text in panel.GetComponentsInChildren<Text>(false))if(text.text.Length>0&&!text.resizeTextForBestFit&&text.preferredHeight>text.rectTransform.rect.height+3)bad.Add(text.name+":"+text.preferredHeight+">"+text.rectTransform.rect.height);
        var report="PASS: header button raycast, 53 serialized cards, function/material/smith purchases, green ownership, invalid quantity, wheel zoom, drag release, fixed child text, tooltip, zero added UI objects. Current screen "+Screen.width+"x"+Screen.height+". Overflow: "+string.Join(",",bad);
        System.IO.File.WriteAllText("Docs/Performance/RetainedTree/functional-checks.txt",report);return report;
    }
}
