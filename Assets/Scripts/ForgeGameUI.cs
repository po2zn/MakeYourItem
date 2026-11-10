using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public sealed class ForgeGameUI:MonoBehaviour
{
 public RectTransform codeHelp;public Text codeHelpText;public Canvas canvas; public RectTransform workspace; public ForgeCodeView codePrefab; public Button salePrefab;public Transform salesList;
 readonly Dictionary<string,Transform> nodes=new Dictionary<string,Transform>();readonly Dictionary<string,Text> texts=new Dictionary<string,Text>();readonly Dictionary<string,Button> buttons=new Dictionary<string,Button>();readonly Dictionary<string,InputField> inputs=new Dictionary<string,InputField>();
 public readonly Dictionary<int,ForgeCodeView> codes=new Dictionary<int,ForgeCodeView>();readonly Dictionary<int,Button> sales=new Dictionary<int,Button>();
 ForgeMvp game; float nextRefresh;int screenWidth,screenHeight,layoutPasses;public bool Bound=>game!=null;
 public void Bind(ForgeMvp owner){if(game==owner)return;game=owner;Font.textureRebuilt+=FontRebuilt;foreach(var t in GetComponentsInChildren<Transform>(true)){nodes[t.name]=t;if(t.TryGetComponent<Text>(out var text))texts[t.name]=text;if(t.TryGetComponent<Button>(out var button))buttons[t.name]=button;if(t.TryGetComponent<InputField>(out var input))inputs[t.name]=input;}foreach(var a in GetComponentsInChildren<ForgeUIAction>(true)){string key=a.action;a.button.onClick.AddListener(()=>{ForgeMouseTrace.Mark("UI_CLICK",key);game.GameUIAction(key);Refresh();});}foreach(var hover in GetComponentsInChildren<ForgeUIHover>(true))hover.Bind(this);inputs["GuideSearch"].onValueChanged.AddListener(_=>game.GameGuideSearch());inputs["TutorialInput"].onValueChanged.AddListener(value=>game.GameTutorialInput(value));}
 public void ShowCodeHelp(string value,Vector2 pointer){codeHelp.gameObject.SetActive(true);codeHelpText.text=value;Canvas.ForceUpdateCanvases();float height=Mathf.Min(((RectTransform)transform).rect.height-24,codeHelpText.preferredHeight+24);codeHelp.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,pointer,null,out var p);var root=(RectTransform)transform;codeHelp.anchoredPosition=new Vector2(Mathf.Clamp(p.x+16,root.rect.xMin,root.rect.xMax-codeHelp.rect.width),Mathf.Clamp(p.y-16,root.rect.yMin+height,root.rect.yMax));}
 public void HideCodeHelp(){if(codeHelp)codeHelp.gameObject.SetActive(false);}
 public void Refresh(){nextRefresh=Time.unscaledTime+.15f;if(game)game.RefreshGameUI();}
 void FontRebuilt(Font font){layoutPasses=2;}
 void OnDestroy(){Font.textureRebuilt-=FontRebuilt;}
 void OnEnable(){layoutPasses=2;}
 void LateUpdate(){if(layoutPasses<=0)return;layoutPasses--;foreach(var group in GetComponentsInChildren<VerticalLayoutGroup>(false))LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);}
 void OnDisable(){HideCodeHelp();}
 void Update(){if(!game)return;if(screenWidth!=Screen.width||screenHeight!=Screen.height){screenWidth=Screen.width;screenHeight=Screen.height;layoutPasses=3;game.ClampGameCodeWindows();}
#if ENABLE_INPUT_SYSTEM
 if(UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true){game.GameUIAction("menu");Refresh();}
#endif
 if(Time.unscaledTime>=nextRefresh)Refresh();}
 public Transform Node(string id)=>nodes[id];public void Active(string id,bool active){var go=nodes[id].gameObject;if(go.activeSelf!=active){go.SetActive(active);if(active&&go.GetComponent<ForgeUIWindow>())go.transform.SetAsLastSibling();}}
 public void Text(string id,string value){if(texts[id].text!=value)texts[id].text=value;}
 public void Enabled(string id,bool value){buttons[id].interactable=value;}
 public void Label(string id,string value){var t=buttons[id].GetComponentInChildren<Text>();if(t.text!=value)t.text=value;}
 public string Input(string id)=>inputs[id].text;public void Input(string id,string value){if(inputs[id].text!=value)inputs[id].SetTextWithoutNotify(value);}
 public void Tooltip(string id,string value){nodes[id].GetComponent<ForgeUIHover>().description=value;}
 public void Tint(string id,Color color){texts[id].color=color;}
 public void Code(int id,string name,string source,Rect rect,bool collapsed,bool running,bool canRun,int line,string diagnostic){if(!codes.TryGetValue(id,out var view)){view=Instantiate(codePrefab,workspace);ForgeLegacyUIFont.Apply(view.transform);view.gameObject.name="CodeWindow_"+id;view.Bind(game,id);codes.Add(id,view);}view.Sync(name,source,rect,collapsed,running,canRun,line,diagnostic);}
 public void PruneCodes(HashSet<int> ids){var remove=new List<int>();foreach(var pair in codes)if(!ids.Contains(pair.Key)){pair.Value.gameObject.SetActive(false);Destroy(pair.Value.gameObject);remove.Add(pair.Key);}foreach(int id in remove)codes.Remove(id);}
 public void Sale(int id,string value,bool selected){if(!sales.TryGetValue(id,out var b)){b=Instantiate(salePrefab,salesList);ForgeLegacyUIFont.Apply(b.transform);b.gameObject.name="Product_"+id;b.onClick.AddListener(()=>{game.GameUIAction("product:"+id);Refresh();});sales.Add(id,b);}b.GetComponentInChildren<Text>().text=(selected?"● ":"○ ")+value;}
 public void PruneSales(HashSet<int> ids){var remove=new List<int>();foreach(var p in sales)if(!ids.Contains(p.Key)){p.Value.gameObject.SetActive(false);Destroy(p.Value.gameObject);remove.Add(p.Key);}foreach(int id in remove)sales.Remove(id);}
}
