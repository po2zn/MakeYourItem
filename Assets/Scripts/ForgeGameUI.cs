using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public sealed partial class ForgeGameUI:MonoBehaviour
{
 public RectTransform codeHelp;public Text codeHelpText;public Canvas canvas; public RectTransform workspace; public ForgeCodeView codePrefab; public Button salePrefab;public Transform salesList;
 readonly Dictionary<string,Transform> nodes=new Dictionary<string,Transform>();readonly Dictionary<string,Text> texts=new Dictionary<string,Text>();readonly Dictionary<string,Button> buttons=new Dictionary<string,Button>();readonly Dictionary<string,InputField> inputs=new Dictionary<string,InputField>();
 public readonly Dictionary<int,ForgeCodeView> codes=new Dictionary<int,ForgeCodeView>();readonly Dictionary<int,Button> sales=new Dictionary<int,Button>();
 ForgeMvp game; float nextRefresh;int screenWidth,screenHeight,layoutPasses;int enabledFrame=-1;public bool Bound=>game!=null;
 public void Bind(ForgeMvp owner){if(game==owner&&nodes.Count>0)return;if(nodes.Count==0)DiscardCodeViews();game=owner;EnsureAirMaterial();ConfigureMaterialInput();Font.textureRebuilt+=FontRebuilt;foreach(var t in GetComponentsInChildren<Transform>(true)){nodes[t.name]=t;if(t.TryGetComponent<Text>(out var text))texts[t.name]=text;if(t.TryGetComponent<Button>(out var button))buttons[t.name]=button;if(t.TryGetComponent<InputField>(out var input))inputs[t.name]=input;}foreach(var a in GetComponentsInChildren<ForgeUIAction>(true)){string key=a.action;a.button.onClick.AddListener(()=>{ForgeMouseTrace.Mark("UI_CLICK",key);game.GameUIAction(key);Refresh();});}foreach(var hover in GetComponentsInChildren<ForgeUIHover>(true))hover.Bind(this);ConfigureCurrencyDisplay();foreach(var guideText in nodes["Guide"].GetComponentsInChildren<Text>(true))ForgeGuideMaterialColors.Apply(guideText);inputs["GuideSearch"].onValueChanged.AddListener(_=>game.GameGuideSearch());ForgeCodeColors.Apply(inputs["TutorialInput"].textComponent);inputs["TutorialInput"].onValueChanged.AddListener(value=>game.GameTutorialInput(value));}
 void ConfigureCurrencyDisplay(){
 var label=texts["Gold"];
 label.fontSize=24;label.resizeTextForBestFit=true;label.resizeTextMinSize=16;label.resizeTextMaxSize=24;
 label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
 label.alignment=TextAnchor.MiddleCenter;
 // Leave room for currency artwork, which is wider than its G/D source glyph.
 var rect=label.rectTransform;rect.offsetMin=new Vector2(12,12);rect.offsetMax=new Vector2(-12,-12);
 ForgeGoldGlyph.Attach(label);
 label.raycastTarget=true;
 var hover=label.GetComponent<ForgeCurrencyHover>()??label.gameObject.AddComponent<ForgeCurrencyHover>();
 hover.show=point=>ShowCodeHelp(ForgeCurrencyHover.Details(game),point);
 hover.hide=HideCodeHelp;
 }
 void EnsureDiamondButton(){
 var actions=GetComponentsInChildren<ForgeUIAction>(true);
 foreach(var a in actions)if(a.action=="diamonds")return;
 foreach(var a in actions)if(a.action=="resource:0"){
 var clone=Instantiate(a.gameObject,a.transform.parent);clone.name="AddDiamonds";clone.transform.SetSiblingIndex(a.transform.GetSiblingIndex()+1);
 var action=clone.GetComponent<ForgeUIAction>();action.action="diamonds";action.button.onClick=new Button.ButtonClickedEvent();
 var label=clone.GetComponentInChildren<Text>(true);label.name="AddDiamondsLabel";label.text="다이아몬드 +1,000";return;}
 }
 void EnsureDaylightButton(){
 var actions=GetComponentsInChildren<ForgeUIAction>(true);
 foreach(var a in actions)if(a.action=="daylight")return;
 foreach(var a in actions)if(a.action=="day"){
 var clone=Instantiate(a.gameObject,a.transform.parent);clone.name="SetDaylight";clone.transform.SetSiblingIndex(a.transform.GetSiblingIndex()+1);
 var action=clone.GetComponent<ForgeUIAction>();action.action="daylight";action.button.onClick=new Button.ButtonClickedEvent();
 var label=clone.GetComponentInChildren<Text>(true);label.name="SetDaylightLabel";label.text="낮으로 변경";return;}
 }
 void EnsureAirMaterial(){
 EnsureDaylightButton();EnsureDiamondButton();
 var source=GetComponentsInChildren<ForgeUIHover>(true);foreach(var h in source)if(h.name=="MaterialTile0")return;
 foreach(var h in source)if(h.name=="MaterialTile1"){
 var tile=Instantiate(h.gameObject,h.transform.parent);tile.name="MaterialTile0";tile.transform.SetSiblingIndex(h.transform.GetSiblingIndex());
 foreach(var t in tile.GetComponentsInChildren<Transform>(true)){if(t.name=="MaterialIcon1"){var original=h.GetComponentInChildren<RawImage>(true);var rect=(RectTransform)t;if(original&&iconSlots.TryGetValue(original,out var slot)){rect.sizeDelta=new Vector2(slot.x,slot.y);rect.anchoredPosition=new Vector2(slot.z,slot.w);}t.name="MaterialIcon0";if(airIcons==null)airIcons=new ForgeMaterialHud();t.GetComponent<RawImage>().texture=airIcons.GetIcon(0);}else if(t.name=="MaterialCount1")t.name="MaterialCount0";}return;}}
 ForgeMaterialHud airIcons;bool smithHelp,materialHelp;
 readonly List<ForgeUIHover> materialHovers=new List<ForgeUIHover>();
 RectTransform materialViewport;
 readonly List<RaycastResult> hoverHits=new List<RaycastResult>();
 void UpdateSmithHover(){
#if ENABLE_INPUT_SYSTEM
 var mouse=UnityEngine.InputSystem.Mouse.current;
 if(mouse==null)return;
 Vector2 point=mouse.position.ReadValue();
 hoverHits.Clear();if(EventSystem.current)EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hoverHits);
 bool blocked=false;foreach(var hit in hoverHits){if(hit.gameObject.transform.IsChildOf(codeHelp))continue;blocked=hit.gameObject.GetComponent<ForgeWorkspacePan>()==null;break;}
 // Observe screen rectangles without making materials raycast targets. A
 // foreground window/button always gets both input and tooltip priority.
 var material=FindMaterialHover(point,blocked||mouse.leftButton.isPressed||mouse.rightButton.isPressed||mouse.middleButton.isPressed);
 if(material!=null){ShowCodeHelp(material.description,point);materialHelp=true;return;}
 bool burning=false;
 string info=blocked?null:game.SmithHoverInfo(point,out burning);
 if(info!=null){ShowCodeHelp(info,point,burning?new Color(1f,.2f,.2f):(Color?)null);smithHelp=true;}else if(smithHelp||materialHelp)HideCodeHelp();
#endif
 }
 ForgeUIHover FindMaterialHover(Vector2 point,bool blocked){
 if(blocked||!materialViewport||!materialViewport.gameObject.activeInHierarchy)return null;
 Camera camera=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
 if(!RectTransformUtility.RectangleContainsScreenPoint(materialViewport,point,camera))return null;
 foreach(var hover in materialHovers)if(hover&&hover.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(hover.description)&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)hover.transform,point,camera))return hover;
 return null;
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
 Color defaultCodeHelpColor;bool codeHelpColorCached;
 public void ShowCodeHelp(string value,Vector2 pointer,Color? textColor=null){if(!codeHelpColorCached){defaultCodeHelpColor=codeHelpText.color;codeHelpColorCached=true;}codeHelpText.color=textColor??defaultCodeHelpColor;smithHelp=false;materialHelp=false;codeHelp.gameObject.SetActive(true);codeHelp.SetAsLastSibling();codeHelp.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Min(420,((RectTransform)transform).rect.width-24));codeHelpText.text=value;Canvas.ForceUpdateCanvases();float height=Mathf.Min(((RectTransform)transform).rect.height-24,codeHelpText.preferredHeight+24);codeHelp.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,pointer,null,out var p);var root=(RectTransform)transform;codeHelp.anchoredPosition=new Vector2(Mathf.Clamp(p.x+16,root.rect.xMin,root.rect.xMax-codeHelp.rect.width),Mathf.Clamp(p.y-16,root.rect.yMin+height,root.rect.yMax));}
 public void HideCodeHelp(){smithHelp=false;materialHelp=false;if(codeHelp)codeHelp.gameObject.SetActive(false);}
 public void Refresh(){nextRefresh=Time.unscaledTime+.15f;if(game)game.RefreshGameUI();}
 void FontRebuilt(Font font){layoutPasses=2;}
 void OnDestroy(){airIcons?.Dispose();Font.textureRebuilt-=FontRebuilt;}
 void OnEnable(){enabledFrame=Time.frameCount;layoutPasses=2;ConfigureMaterialInput();}
 // The material strip is informational. Neither its transparent graphics nor
 // the toolbar background may win raycasts over windows/the workspace below.
 void ConfigureMaterialInput(){
 ApplyMaterialArtwork();
 materialHovers.Clear();materialViewport=null;
 if(codeHelp)foreach(var graphic in codeHelp.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
 foreach(var t in GetComponentsInChildren<Transform>(true)){
 if(t.name=="Toolbar"&&t.TryGetComponent<UnityEngine.UI.Graphic>(out var background))background.raycastTarget=false;
 if(t.name!="MaterialsScroll")continue;
 foreach(var graphic in t.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
 foreach(var scroll in t.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true)){materialViewport=scroll.viewport;scroll.StopMovement();scroll.enabled=false;}
 foreach(var hover in t.GetComponentsInChildren<ForgeUIHover>(true)){hover.enabled=false;materialHovers.Add(hover);}
 }
 }
 readonly Dictionary<RawImage,Vector4> iconSlots=new Dictionary<RawImage,Vector4>();
 void ApplyMaterialArtwork(){
 if(airIcons==null)airIcons=new ForgeMaterialHud();
 foreach(var icon in GetComponentsInChildren<RawImage>(true)){
 if(!icon.name.StartsWith("MaterialIcon")||!int.TryParse(icon.name.Substring(12),out int id)||id<0||id>9)continue;
 var rect=icon.rectTransform;if(!iconSlots.TryGetValue(icon,out var slot)){slot=new Vector4(rect.sizeDelta.x,rect.sizeDelta.y,rect.anchoredPosition.x,rect.anchoredPosition.y);iconSlots[icon]=slot;}
 icon.texture=airIcons.GetIcon(id);icon.uvRect=airIcons.GetIconUV(id);icon.color=Color.white;
 float aspect=airIcons.GetIconAspect(id);var box=new Vector2(slot.x,slot.y);var size=aspect>box.x/box.y?new Vector2(box.x,box.x/aspect):new Vector2(box.y*aspect,box.y);
 rect.sizeDelta=size;rect.anchoredPosition=new Vector2(slot.z,slot.w)+Vector2.Scale(box-size,new Vector2(.5f,.5f)-rect.pivot);
 }
 }
 void LateUpdate(){UpdateMaterialUseAnimation();UpdateTutorialPresentation();if(game)UpdateSmithHover();if(layoutPasses<=0)return;layoutPasses--;foreach(var group in GetComponentsInChildren<VerticalLayoutGroup>(false))LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);}
 void OnDisable(){ResetMaterialUseAnimation();HideCodeHelp();}
 void Update(){if(!game)return;TracePointerHit();if(screenWidth!=Screen.width||screenHeight!=Screen.height){screenWidth=Screen.width;screenHeight=Screen.height;layoutPasses=3;}
#if ENABLE_INPUT_SYSTEM
 if(Time.frameCount!=enabledFrame&&UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true){game.GameUIAction("menu");Refresh();}
#endif
 if(Time.unscaledTime>=nextRefresh)Refresh();}
 ForgeGuideExample guideExample;
 public void GuideExample(string source,string notes){if(!guideExample)guideExample=ForgeGuideExample.Create(texts["GuideBody"]);guideExample.Show(source,notes);}
 Button inventoryShortcut;
 public void InventoryShortcut(){if(inventoryShortcut)return;inventoryShortcut=Instantiate(salePrefab,salesList);inventoryShortcut.name="OpenProductInventory";ForgeLegacyUIFont.Apply(inventoryShortcut.transform);inventoryShortcut.GetComponentInChildren<Text>().text="완성품 목록 열기";inventoryShortcut.onClick.AddListener(()=>{game.GameUIAction("products");Refresh();});}
 ForgeProductInventory inventory;
 public void Inventory(bool visible,ForgeShop shop){if(!inventory)inventory=GetComponentInChildren<ForgeProductInventory>(true);if(!inventory)return;inventory.Bind(game,shop,texts["Products"].font);if(inventory.gameObject.activeSelf!=visible)inventory.gameObject.SetActive(visible);if(visible)inventory.Refresh();}
 public void CenterGuide(){StartCoroutine(CenterGuideAfterLayout());}
 System.Collections.IEnumerator CenterGuideAfterLayout(){
 yield return null;
 var rect=(RectTransform)Node("Guide");if(!rect.gameObject.activeInHierarchy)yield break;
 Canvas.ForceUpdateCanvases();
 var parent=(RectTransform)rect.parent;
 var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
 if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,new Vector2(Screen.width*.5f,Screen.height*.5f),camera,out var center)){
 var current=(Vector2)parent.InverseTransformPoint(rect.TransformPoint(rect.rect.center));rect.anchoredPosition+=center-current;
 }
 rect.SetAsLastSibling();
 }
 public Transform Node(string id)=>nodes[id];public void Active(string id,bool active){var go=nodes[id].gameObject;if(go.activeSelf!=active){go.SetActive(active);if(active&&go.GetComponent<ForgeUIWindow>())go.transform.SetAsLastSibling();}}
 public void Text(string id,string value){if(texts[id].text!=value)texts[id].text=value;}
 public void Enabled(string id,bool value){buttons[id].interactable=value;}
 public void Label(string id,string value){var t=buttons[id].GetComponentInChildren<Text>();if(t.text!=value)t.text=value;}
 public string Input(string id)=>inputs[id].text;public void Input(string id,string value){if(inputs[id].text!=value)inputs[id].SetTextWithoutNotify(value);}
 public void Tooltip(string id,string value){nodes[id].GetComponent<ForgeUIHover>().description=value;}
 public void Tint(string id,Color color){texts[id].color=color;}
 public void Code(int id,string name,string source,Rect rect,bool collapsed,bool running,bool canRun,int line,string diagnostic){if(!codes.TryGetValue(id,out var view)){view=Instantiate(codePrefab,workspace);ForgeLegacyUIFont.Apply(view.transform);view.gameObject.name="CodeWindow_"+id;view.Bind(game,id);codes.Add(id,view);}view.Sync(name,source,rect,collapsed,running,canRun,line,diagnostic);}
 public void CommitCodeWindowBounds(){foreach(var view in codes.Values)if(view)view.CommitBounds();}
 public void DiscardCodeViews(){foreach(var view in GetComponentsInChildren<ForgeCodeView>(true)){view.Detach();view.gameObject.SetActive(false);Destroy(view.gameObject);}codes.Clear();}
 public void PruneCodes(HashSet<int> ids){var remove=new List<int>();foreach(var pair in codes)if(!ids.Contains(pair.Key)){pair.Value.gameObject.SetActive(false);Destroy(pair.Value.gameObject);remove.Add(pair.Key);}foreach(int id in remove)codes.Remove(id);}
 public void Sale(int id,string value,bool selected){if(!sales.TryGetValue(id,out var b)){b=Instantiate(salePrefab,salesList);ForgeLegacyUIFont.Apply(b.transform);b.gameObject.name="Product_"+id;b.onClick.AddListener(()=>{game.GameUIAction("product:"+id);Refresh();});sales.Add(id,b);}b.GetComponentInChildren<Text>().text=(selected?"● ":"○ ")+value;}
 public void PruneSales(HashSet<int> ids){var remove=new List<int>();foreach(var p in sales)if(!ids.Contains(p.Key)){p.Value.gameObject.SetActive(false);Destroy(p.Value.gameObject);remove.Add(p.Key);}foreach(int id in remove)sales.Remove(id);}
}
