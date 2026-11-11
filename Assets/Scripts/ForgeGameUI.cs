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
 public void Bind(ForgeMvp owner){if(game==owner)return;game=owner;EnsureAirMaterial();Font.textureRebuilt+=FontRebuilt;foreach(var t in GetComponentsInChildren<Transform>(true)){nodes[t.name]=t;if(t.TryGetComponent<Text>(out var text))texts[t.name]=text;if(t.TryGetComponent<Button>(out var button))buttons[t.name]=button;if(t.TryGetComponent<InputField>(out var input))inputs[t.name]=input;}foreach(var a in GetComponentsInChildren<ForgeUIAction>(true)){string key=a.action;a.button.onClick.AddListener(()=>{ForgeMouseTrace.Mark("UI_CLICK",key);game.GameUIAction(key);Refresh();});}foreach(var hover in GetComponentsInChildren<ForgeUIHover>(true))hover.Bind(this);inputs["GuideSearch"].onValueChanged.AddListener(_=>game.GameGuideSearch());ForgeCodeColors.Apply(inputs["TutorialInput"].textComponent);inputs["TutorialInput"].onValueChanged.AddListener(value=>game.GameTutorialInput(value));}
 void EnsureAirMaterial(){
 var source=GetComponentsInChildren<ForgeUIHover>(true);foreach(var h in source)if(h.name=="MaterialTile0")return;
 foreach(var h in source)if(h.name=="MaterialTile1"){
 var tile=Instantiate(h.gameObject,h.transform.parent);tile.name="MaterialTile0";tile.transform.SetSiblingIndex(h.transform.GetSiblingIndex());
 foreach(var t in tile.GetComponentsInChildren<Transform>(true)){if(t.name=="MaterialIcon1"){t.name="MaterialIcon0";airIcons=new ForgeMaterialHud();t.GetComponent<RawImage>().texture=airIcons.GetIcon(0);}else if(t.name=="MaterialCount1")t.name="MaterialCount0";}return;}}
 ForgeMaterialHud airIcons;bool smithHelp;
 readonly List<RaycastResult> hoverHits=new List<RaycastResult>();
 void UpdateSmithHover(){
#if ENABLE_INPUT_SYSTEM
 var mouse=UnityEngine.InputSystem.Mouse.current;
 if(mouse==null)return;
 Vector2 point=mouse.position.ReadValue();
 hoverHits.Clear();if(EventSystem.current)EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hoverHits);
 bool blocked=false;foreach(var hit in hoverHits){if(hit.gameObject.GetComponent<ForgeWorkspacePan>()!=null)continue;if(smithHelp&&hit.gameObject.transform.IsChildOf(codeHelp))return;blocked=true;break;}
 string info=blocked?null:game.SmithHoverInfo(point);
 if(info!=null){ShowCodeHelp(info,point);smithHelp=true;}else if(smithHelp){HideCodeHelp();smithHelp=false;}
#endif
 }
 readonly List<RaycastResult> traceHits=new List<RaycastResult>();
 static string TracePath(Transform target){if(!target)return "none";string path=target.name;for(var p=target.parent;p!=null;p=p.parent)path=p.name+"/"+path;return path;}
 void TracePointerHit(){
#if ENABLE_INPUT_SYSTEM
 if(!ForgeMouseTrace.Enabled)return;
 var mouse=UnityEngine.InputSystem.Mouse.current;var system=EventSystem.current;
 if(mouse==null||system==null||(!mouse.leftButton.wasPressedThisFrame&&!mouse.leftButton.wasReleasedThisFrame))return;
 var pointer=new PointerEventData(system){position=mouse.position.ReadValue()};traceHits.Clear();system.RaycastAll(pointer,traceHits);
 var detail=new System.Text.StringBuilder("position=").Append(pointer.position).Append(" origin=bottom-left focused=").Append(Application.isFocused).Append(" selected=").Append(TracePath(system.currentSelectedGameObject?system.currentSelectedGameObject.transform:null));
 detail.Append(" hits=").Append(traceHits.Count);
 for(int i=0;i<Mathf.Min(4,traceHits.Count);i++)detail.Append(" | ").Append(i).Append(":").Append(TracePath(traceHits[i].gameObject.transform));
 if(nodes.TryGetValue("Developer",out var developer))detail.Append(" developerActive=").Append(developer.gameObject.activeInHierarchy);
 string state=detail.ToString();
 if(mouse.leftButton.wasPressedThisFrame)ForgeMouseTrace.Mark("UI_POINTER_DOWN",state);
 if(mouse.leftButton.wasReleasedThisFrame)ForgeMouseTrace.Mark("UI_POINTER_UP",state);
#endif
 }
 public void ShowCodeHelp(string value,Vector2 pointer){codeHelp.gameObject.SetActive(true);codeHelp.SetAsLastSibling();codeHelp.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Min(420,((RectTransform)transform).rect.width-24));codeHelpText.text=value;Canvas.ForceUpdateCanvases();float height=Mathf.Min(((RectTransform)transform).rect.height-24,codeHelpText.preferredHeight+24);codeHelp.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,pointer,null,out var p);var root=(RectTransform)transform;codeHelp.anchoredPosition=new Vector2(Mathf.Clamp(p.x+16,root.rect.xMin,root.rect.xMax-codeHelp.rect.width),Mathf.Clamp(p.y-16,root.rect.yMin+height,root.rect.yMax));}
 public void HideCodeHelp(){if(codeHelp)codeHelp.gameObject.SetActive(false);}
 public void Refresh(){nextRefresh=Time.unscaledTime+.15f;if(game)game.RefreshGameUI();}
 void FontRebuilt(Font font){layoutPasses=2;}
 void OnDestroy(){airIcons?.Dispose();Font.textureRebuilt-=FontRebuilt;}
 void OnEnable(){layoutPasses=2;}
 void LateUpdate(){if(game)UpdateSmithHover();if(layoutPasses<=0)return;layoutPasses--;foreach(var group in GetComponentsInChildren<VerticalLayoutGroup>(false))LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);}
 void OnDisable(){HideCodeHelp();}
 void Update(){if(!game)return;TracePointerHit();if(screenWidth!=Screen.width||screenHeight!=Screen.height){screenWidth=Screen.width;screenHeight=Screen.height;layoutPasses=3;game.ClampGameCodeWindows();}
#if ENABLE_INPUT_SYSTEM
 if(UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true){game.GameUIAction("menu");Refresh();}
#endif
 if(Time.unscaledTime>=nextRefresh)Refresh();}
 ForgeGuideExample guideExample;
 public void GuideExample(string source,string notes){if(!guideExample)guideExample=ForgeGuideExample.Create(texts["GuideBody"]);guideExample.Show(source,notes);}
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
