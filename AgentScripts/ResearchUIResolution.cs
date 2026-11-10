using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;
public static class ResearchUIResolution
{
    public static string Set(int width,int height)
    {
        var assembly=typeof(Editor).Assembly;var viewType=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        var selected=viewType.GetProperty("selectedSizeIndex",flags);if(!SessionState.GetBool("ForgeTreeQA.saved",false)){SessionState.SetInt("ForgeTreeQA.index",(int)selected.GetValue(view));SessionState.SetBool("ForgeTreeQA.saved",true);}
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var sizes=sizesType.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");var group=sizesType.GetMethod("GetGroup",flags).Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});var type=group.GetType();
        int count=(int)type.GetMethod("GetTotalCount",flags).Invoke(group,null);int index=-1;
        for(int i=0;i<count;i++){var item=type.GetMethod("GetGameViewSize",flags).Invoke(group,new object[]{i});if((int)item.GetType().GetProperty("width",flags).GetValue(item)==width&&(int)item.GetType().GetProperty("height",flags).GetValue(item)==height){index=i;break;}}
        if(index<0){var sizeType=assembly.GetType("UnityEditor.GameViewSize");var kind=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(sizeType,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,new[]{Enum.Parse(kind,"FixedResolution"),(object)width,height,"ForgeTree QA "+width+"x"+height},null);type.GetMethod("AddCustomSize",flags).Invoke(group,new[]{size});index=count;}
        selected.SetValue(view,index);view.Repaint();return "Game View set to "+width+"x"+height;
    }
    public static void Restore(){var viewType=typeof(Editor).Assembly.GetType("UnityEditor.GameView");if(SessionState.GetBool("ForgeTreeQA.saved",false)){viewType.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(EditorWindow.GetWindow(viewType),SessionState.GetInt("ForgeTreeQA.index",0));SessionState.SetBool("ForgeTreeQA.saved",false);}}
    public static async Task<string> Inspect(int width,int height,int page,string tooltipId)
    {
        Set(width,height);await Task.Delay(400);Canvas.ForceUpdateCanvases();
        var panel=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>().HierarchyTree;panel.ShowPage(page);panel.maps[Mathf.Min(page,2)].ResetView();
        if(!string.IsNullOrEmpty(tooltipId))panel.Hover(panel.cards.First(c=>c.offerId==tooltipId),new Vector2(Screen.width*.6f,Screen.height*.55f));
        await Task.Delay(150);Canvas.ForceUpdateCanvases();
        var failures=new List<string>();
        foreach(var text in panel.GetComponentsInChildren<Text>(false)){
            if(string.IsNullOrEmpty(text.text))continue;
            float needed=text.preferredHeight; // Public UI layout metric at the authored maximum font size.
            if(needed>text.rectTransform.rect.height+4)failures.Add(text.transform.parent.name+"/"+text.name+" "+needed+">"+text.rectTransform.rect.height);
        }
        string result="Screen "+Screen.width+"x"+Screen.height+" page "+page+" overflow="+string.Join(";",failures);
        System.IO.File.AppendAllText("Docs/Performance/RetainedTree/layout-checks.txt",result+"\n");
        return result;
    }
    public static async Task<string> AuditAll()
    {
        System.IO.File.WriteAllText("Docs/Performance/RetainedTree/layout-checks.txt","");
        var results=new List<string>();
        foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)})
            for(int page=0;page<4;page++)results.Add(await Inspect(size.x,size.y,page,page==0?"automation-root":""));
        var panel=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>().HierarchyTree;
        Set(1920,1080);await Task.Delay(300);panel.ShowPage(0);panel.HideTooltip();
        int refreshes=panel.RefreshCount,objects=panel.GetComponentsInChildren<Transform>(true).Length,frame=Time.frameCount;
        await Task.Delay(3000);
        results.Add("Idle 3s: frames="+(Time.frameCount-frame)+", card refreshes="+(panel.RefreshCount-refreshes)+", added UI objects="+(panel.GetComponentsInChildren<Transform>(true).Length-objects));
        var watch=System.Diagnostics.Stopwatch.StartNew();for(int n=0;n<20;n++)panel.Refresh();watch.Stop();results.Add("Explicit state refresh average ms="+(watch.Elapsed.TotalMilliseconds/20).ToString("F3"));
        string result=string.Join("\n",results);System.IO.File.WriteAllText("Docs/Performance/RetainedTree/verification.txt",result);return result;
    }
}
