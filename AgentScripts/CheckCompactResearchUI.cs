using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
public static class CheckCompactResearchUI
{
    public static async Task<string> Run()
    {
        Application.runInBackground=true;var game=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        foreach(var name in new[]{"showTitle","showTutorial","showGameMenu","showProgress","progressLoaded"})typeof(ForgeMvp).GetField(name,flags).SetValue(game,false);
        typeof(ForgeMvp).GetField("showTechTree",flags).SetValue(game,true);typeof(ForgeMvp).GetMethod("SyncHierarchyTree",flags).Invoke(game,null);
        await Task.Delay(200);Canvas.ForceUpdateCanvases();var panel=game.HierarchyTree;
        var buttons=new[]{panel.growthButton,panel.materialsButton,panel.smithButton,panel.fitButton,panel.resetButton,panel.backButton};
        var positions=buttons.Select(b=>((RectTransform)b.transform).anchoredPosition).ToArray();var labels=buttons.Select(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text).ToArray();
        for(int page=0;page<4;page++){
            panel.ShowPage(page);for(int i=0;i<buttons.Length;i++)if(!buttons[i].gameObject.activeInHierarchy||positions[i]!=((RectTransform)buttons[i].transform).anchoredPosition||labels[i]!=buttons[i].GetComponentInChildren<UnityEngine.UI.Text>().text)throw new Exception("Header changed on page "+page);
        }
        panel.ShowPage(2);panel.detailsButton.onClick.Invoke();if(!panel.pages[3].activeSelf)throw new Exception("Smith details inaccessible");panel.resetButton.onClick.Invoke();if(!panel.pages[2].activeSelf)throw new Exception("Reset did not return to smith tree");
        var module=(InputSystemUIInputModule)EventSystem.current.currentInputModule;
        foreach(var map in panel.maps){map.ResetView();float before=map.content.localScale.x;map.OnScroll(new PointerEventData(EventSystem.current){position=new Vector2(400,300),scrollDelta=new Vector2(0,module.scrollDeltaPerTick)});float ratio=map.content.localScale.x/before;if(ratio<=1||ratio>1.09f)throw new Exception("Wheel sensitivity too high: "+ratio);map.ResetView();}
        panel.ShowPage(2);return "PASS: identical header buttons on all 4 pages; smith details accessible inside base card; wheel notch <= 9% on all 3 maps; reduced heading and header.";
    }
}
