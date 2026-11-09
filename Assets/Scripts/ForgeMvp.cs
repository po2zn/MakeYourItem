using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Temporary single-scene frontend; the evaluator owns no Unity or inventory state.
public partial class ForgeMvp : MonoBehaviour
{
    [SerializeField] Color reservedColor = new Color(197f/255f, 134f/255f, 192f/255f, 1f);
    const string InitialSource = "";
    string source = InitialSource, report = "▷로 한 줄씩 실행하거나 ▶로 전체 실행하세요. 실제 재료가 소비됩니다.", hover = "";
    Rect workspaceWindow = new Rect(30, 135, 360, 240);
    Rect InitialCodeWindow {
        get {
            Vector3 smith=workspaceCamera?workspaceCamera.WorldToScreenPoint(new Vector3(1.2f,.9f,0)):new Vector3(Screen.width*.65f,Screen.height*.5f,0);
            float x=Mathf.Clamp(smith.x-360-104,12,Mathf.Max(12,Screen.width-372));
            float y=Mathf.Clamp(Screen.height-smith.y-180,ToolbarHeight+15,Mathf.Max(ToolbarHeight+15,Screen.height-252));
            return new Rect(x,y,360,240);
        }
    }
    Vector2 workspaceOffset;
    float workspaceZoom = 1f;
    class CodeWindow
    {
        public Rect rect;
        public bool collapsed;
        public float expandedHeight = 240;
        public string text = "";
        public string coloredSource;
        public List<ForgeSyntax.Token> coloredTokens;
        public string name;
        public float clickedAt = -100f;
        public ForgeCompileDiagnostic diagnostic;
        public string diagnosticCaption="컴파일 오류";
        public bool showDiagnostic, revealDiagnostic, revealExecution;
        public int executionLine;
        public string executionSource;
        public Vector2 diagnosticScroll;
        public Vector2 scroll;
        public int pendingCaret = -1;
        public List<EditSnapshot> undo = new List<EditSnapshot>();
        public int id;
        public uint seed = 1;
    }
    readonly List<CodeWindow> codeWindows = new List<CodeWindow>();
    int activeWindow, movingWindow = -1;
    int nextWindowId;
    int deleteWindowId = -1;
    bool windowsInitialized;
    int dragButton = -1, dragControl;
    int codePointerControl;
    Vector2 dragStartMouse, dragStartPosition;
    bool showSalesPanel = false;
    bool showGuideBook,showTechTree;
    readonly ForgeTechTreeScreen techTree=new ForgeTechTreeScreen();
    readonly ForgeTechnology technology=new ForgeTechnology();
    string purchaseMessage="재료는 반복 구매, 기능은 한 번 구매해 해금합니다.";
    void PurchaseTechnology(ForgeTechnology.Offer offer)
    {if(technology.TryPurchase(offer,ref gold,ref iron,ref materialStocks,busy||executionSession!=null,out purchaseMessage))SaveProgress();}
    [SerializeField, Tooltip("안내서의 영문 문법 표시 여부. 추후 구매/저장 상태에서 설정합니다.")]
    bool englishGuideUnlocked;
    public bool EnglishGuideUnlocked => englishGuideUnlocked;
    // Presentation only: never changes parser support, inventory or funds.
    public void SetEnglishGuideUnlocked(bool unlocked) { englishGuideUnlocked=unlocked; }
    readonly ForgeBookWindow guideBook=new ForgeBookWindow();
    Rect salesWindow = new Rect(430, 135, 350, 480);
    bool movingSales;
    int salesResizeEdge = -1;
    Rect salesResizeStart;
    string resultOwner = "아직 실행하지 않음";
    bool reportError;
    int renamingWindow = -1;
    string renameDraft = "";
    bool focusRename, draggedTitle;
    Rect NameRect(Rect r) { return new Rect(r.x + 108, r.y + 5, r.width - 180, 24); }
    void CommitName()
    {
        if (renamingWindow < 0) return;
        string name = renameDraft.Trim();
        if (name.Length > 0) codeWindows[renamingWindow].name = name;
        renamingWindow = -1;
        GUI.FocusControl(null);
    }
    Vector2 workspaceMouse;
    void EndWorkspaceDrag()
    {
        if (GUIUtility.hotControl == dragControl) GUIUtility.hotControl = 0;
        movingWindow = -1; movingSales = false; salesResizeEdge = -1; panning = false; dragButton = -1;
        codePointerControl=0;
    }
    string controlName = CodeControl;

    bool panning;
    Camera workspaceCamera;
    Vector3 cameraOrigin;
    const float WorkspaceUnitsPerPixel = .01f;
    void UpdateWorkspaceCamera()
    {
        if (!workspaceCamera) return;
        workspaceCamera.orthographicSize = Screen.height * WorkspaceUnitsPerPixel * .5f / workspaceZoom;
        workspaceCamera.transform.position = cameraOrigin
            + workspaceCamera.transform.right * (Screen.width * .5f * (1f / workspaceZoom - 1f)) * WorkspaceUnitsPerPixel
            - workspaceCamera.transform.up * (Screen.height * .5f * (1f / workspaceZoom - 1f)) * WorkspaceUnitsPerPixel
            - workspaceCamera.transform.right * workspaceOffset.x * WorkspaceUnitsPerPixel
            + workspaceCamera.transform.up * workspaceOffset.y * WorkspaceUnitsPerPixel;
    }
    void LateUpdate() { UpdateWorkspaceCamera(); }
    void OnApplicationFocus(bool focused) { if (!focused) { EndWorkspaceDrag(); techTree.CancelDrag(); guideBook.CancelDrag(); edge = -1; SetResizeCursor(0); } }
    Vector2 scroll, logs, lastMouse;
    float hoverSince;
    int edge = -1, gold = 100, iron = 10;
    readonly ForgeMaterialHud materialHud=new ForgeMaterialHud();
    [SerializeField] ForgeMaterialStock[] materialStocks={
        new ForgeMaterialStock{material=2},new ForgeMaterialStock{material=3},
        new ForgeMaterialStock{material=4},new ForgeMaterialStock{material=5},
        new ForgeMaterialStock{material=6},new ForgeMaterialStock{material=7},
        new ForgeMaterialStock{material=8},new ForgeMaterialStock{material=9}};
    float StatusTop => Screen.width>=960?4:124;
    float MaterialTop => StatusTop+124;
    float ToolbarHeight => Mathf.Max(156,materialHud.Height(Mathf.Max(144,Screen.width-496),materialStocks,MaterialTop));
    int products => shop.Count;
    int selectedProductId = -1;
    bool busy;
    struct EditSnapshot
    {
        public string text;
        public int cursor, selection;
        public Vector2 scroll;
    }
    List<EditSnapshot> undoHistory = new List<EditSnapshot>();
    const string CodeControl = "ForgeCodeEditor";

    Texture2D horizontalResizeCursor, verticalResizeCursor;
    int resizeCursorShown;
    void SetResizeCursor(int direction)
    {
        if (resizeCursorShown == direction) return;
        if (direction != 0 && !horizontalResizeCursor)
        {
            const int size = 32;
            horizontalResizeCursor = new Texture2D(size, size, TextureFormat.RGBA32, false);
            horizontalResizeCursor.name = "Horizontal resize cursor";
            horizontalResizeCursor.filterMode = FilterMode.Point;
            var pixels = new Color32[size * size];
            var shape = new bool[size * size];
            // White double arrow with a dark outline, centered on the edge hotspot.
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int distance = Mathf.Abs(y - 16);
                    shape[y * size + x] = (x >= 5 && x <= 26 && distance <= 1)
                        || (x >= 3 && x <= 10 && distance <= x - 3 && distance >= x - 5)
                        || (x >= 21 && x <= 28 && distance <= 28 - x && distance >= 26 - x);
                }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool outline = false;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (x + dx >= 0 && x + dx < size && y + dy >= 0 && y + dy < size)
                                outline |= shape[(y + dy) * size + x + dx];
                    pixels[y * size + x] = shape[y * size + x] ? new Color32(255,255,255,255)
                        : outline ? new Color32(20,20,20,255) : new Color32(0,0,0,0);
                }
            horizontalResizeCursor.SetPixels32(pixels);
            horizontalResizeCursor.Apply(false, false);
            verticalResizeCursor = new Texture2D(size,size,TextureFormat.RGBA32,false);
            verticalResizeCursor.name = "Vertical resize cursor";
            verticalResizeCursor.filterMode = FilterMode.Point;
            var verticalPixels = new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)verticalPixels[y*size+x]=pixels[x*size+y];
            verticalResizeCursor.SetPixels32(verticalPixels);
            verticalResizeCursor.Apply(false,false);
        }
        Cursor.SetCursor(direction==1?horizontalResizeCursor:direction==2?verticalResizeCursor:null, direction!=0?new Vector2(16,16):Vector2.zero, CursorMode.Auto);
        resizeCursorShown = direction;
    }
    void OnDisable() { StopExecution(false); techTree.CancelDrag(); SetResizeCursor(0); ReleaseImeKeyboard(); guideBook.CancelDrag(); }
#if ENABLE_INPUT_SYSTEM
    UnityEngine.InputSystem.Keyboard imeKeyboard;
    bool imeComposing;
    int imeChangedFrame=-1;
    void ImeChanged(UnityEngine.InputSystem.LowLevel.IMECompositionString value)
    { imeComposing=value.Count>0; imeChangedFrame=Time.frameCount; }
#endif
    void ReleaseImeKeyboard()
    {
#if ENABLE_INPUT_SYSTEM
        if(imeKeyboard!=null)imeKeyboard.onIMECompositionChange-=ImeChanged;
        imeKeyboard=null;imeComposing=false;
#endif
    }
    bool IsImeEditing()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard=UnityEngine.InputSystem.Keyboard.current;
        if(imeKeyboard!=keyboard){ReleaseImeKeyboard();imeKeyboard=keyboard;if(imeKeyboard!=null)imeKeyboard.onIMECompositionChange+=ImeChanged;}
        return imeComposing||imeChangedFrame==Time.frameCount;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return !string.IsNullOrEmpty(Input.compositionString);
#else
        return false;
#endif
    }
    void ApplyCodeEdit(BlueprintIndentation.Edit edit,TextEditor editor)
    {
        source=edit.Text;editor.text=source;
        editor.cursorIndex=edit.Cursor;editor.selectIndex=edit.Selection;
    }
    GUIStyle codeStyle, inputStyle, tokenStyle, scrollTrack, scrollThumb, actionButton, errorLabel;
    Texture2D windowFill, trackFill, thumbFill, buttonFill;
    Texture2D FlatColor(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color); texture.Apply();
        return texture;
    }
    void InitializeCodeStyle()
    {
        if (codeStyle != null) return;
        codeStyle = new GUIStyle { fontSize = 18, wordWrap = false, alignment = TextAnchor.UpperLeft, padding = new RectOffset(8, 8, 8, 8), richText = false };
        codeStyle.font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Malgun Gothic" }, 18);
        foreach (var state in new[] { codeStyle.normal, codeStyle.hover, codeStyle.active, codeStyle.focused, codeStyle.onNormal, codeStyle.onHover, codeStyle.onActive, codeStyle.onFocused })
        { state.background = null; state.scaledBackgrounds = new Texture2D[0]; state.textColor = Color.white; }
        inputStyle = new GUIStyle(codeStyle);
        foreach (var state in new[] { inputStyle.normal, inputStyle.hover, inputStyle.active, inputStyle.focused, inputStyle.onNormal, inputStyle.onHover, inputStyle.onActive, inputStyle.onFocused })
            state.textColor = Color.clear;
        tokenStyle = new GUIStyle(codeStyle) { padding = new RectOffset(0, 0, 0, 0) };
        windowFill = FlatColor(new Color(.12f, .14f, .17f, .78f));
        buttonFill = FlatColor(new Color(.48f, .51f, .56f, 1f));
        actionButton = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 16, font = codeStyle.font };
        foreach (var state in new[] { actionButton.normal, actionButton.hover, actionButton.active, actionButton.focused,
            actionButton.onNormal, actionButton.onHover, actionButton.onActive, actionButton.onFocused })
        { state.background = buttonFill; state.textColor = Color.white; }
        errorLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
        errorLabel.normal.textColor = new Color(1f, .3f, .3f, 1f);
        trackFill = FlatColor(new Color(.3f, .32f, .35f, .6f));
        thumbFill = FlatColor(new Color(.7f, .73f, .77f, 1f));
        scrollTrack = new GUIStyle { fixedWidth = 6, normal = { background = trackFill } };
        scrollThumb = new GUIStyle { fixedWidth = 6, fixedHeight = 28, normal = { background = thumbFill }, hover = { background = thumbFill }, active = { background = thumbFill } };
    }
    void OnDestroy()
    {
        materialHud.Dispose();
        SetResizeCursor(0);
        if (horizontalResizeCursor) Destroy(horizontalResizeCursor);
        if (verticalResizeCursor) Destroy(verticalResizeCursor);
        if (windowFill) Destroy(windowFill);
        if (buttonFill) Destroy(buttonFill);
        if (trackFill) Destroy(trackFill);
        if (thumbFill) Destroy(thumbFill);
    }
    GameObject work;
    ForgeCraftMotion craftMotion;
    public class Piece { public ForgeItemData data=new ForgeItemData(); public int id; public int material=1; public int heat; public bool shaped; public ForgeTraits traits; }
    public class Step { public int day,minute,absorbedPiece; public bool warning; public ForgeItemData data; public Dictionary<int,int> consumed; public float duration=.8f; public int setting, settingValue, refund; public double productionSeconds; public int line, cost, iron, heat, process, material, materialCount, pieceId; public bool shaped, finished, debugOutput; public ForgeTraits traits; public string text, module; }
    public class Evaluation { public List<Step> steps = new List<Step>(); public string error; public int cost, ironConsumed, errorLine; public string errorMessage, errorModule; public bool compileError; public ForgeProduct product; public bool skipped, debugOnly; public Dictionary<string, object> values = new Dictionary<string, object>(); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleScene" && FindAnyObjectByType<ForgeMvp>() == null) new GameObject("Forge MVP").AddComponent<ForgeMvp>(); }
    public ForgeDayCycle DayCycle { get; private set; }
    readonly ForgeShop shop = new ForgeShop();
    void Start()
    {
        work = GameObject.CreatePrimitive(PrimitiveType.Cube);
        work.name = "Workpiece"; work.transform.position = new Vector3(2, 1, 0);
        work.transform.localScale = new Vector3(1, .4f, .5f);
        craftMotion = gameObject.AddComponent<ForgeCraftMotion>();
        craftMotion.Initialize(work);
        workspaceCamera = Camera.main;
        if (workspaceCamera)
        {
            Vector3 smithCenter = new Vector3(1.2f, .9f, 0);
            workspaceCamera.transform.position = smithCenter + new Vector3(0, 4, -9);
            workspaceCamera.transform.LookAt(smithCenter);
            workspaceCamera.orthographic = true;
            workspaceCamera.clearFlags = CameraClearFlags.SolidColor;
            workspaceCamera.backgroundColor = new Color(.24f, .25f, .27f, 1f);
            cameraOrigin = workspaceCamera.transform.position;
            UpdateWorkspaceCamera();
        }
        DayCycle = GetComponent<ForgeDayCycle>();
        if (!DayCycle) DayCycle = gameObject.AddComponent<ForgeDayCycle>();
        InitializeTitle();
        var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bench.name = "Temporary Workbench";
        bench.transform.position = new Vector3(2, .5f, 0);
        bench.transform.localScale = new Vector3(3, .3f, 2);
    }
    public static Evaluation Evaluate(string source, int availableGold = int.MaxValue, int availableIron = int.MaxValue, string owner = "", uint seed = 1, IReadOnlyDictionary<int,long> materialStocks = null, ForgeTechnology technology = null, IReadOnlyDictionary<string,string> modules = null, Func<double> clockHour = null, ForgeRunContext context = null)
    { return BlueprintRuntime.Evaluate(source, availableGold, availableIron, owner, seed, materialStocks, technology, modules, clockHour, context); }
    bool SellProduct(int id)
    {
        if(busy||executionSession!=null)return false;
        bool sold=SellProductInternal(id);if(sold)SaveProgress();return sold;
    }
    readonly ForgeDebugOutput debugOutput=new ForgeDebugOutput();
    Rect debugWindow=new Rect(30,320,480,240);
    Vector2 debugScroll,debugResizeMouse;
    Rect debugResizeStart;
    bool showDebugWindow,debugFollow,debugInteracting;
    int debugResizeEdge=-1;
    const float DebugHeaderHeight=34f;
    GUIStyle debugTextStyle,debugTitleStyle;
    void AppendDebug(string message){debugOutput.Append(message);showDebugWindow=true;debugFollow=true;}
    int DebugResizeEdge(Vector2 mouse)
    {
        if(!debugWindow.Contains(mouse))return -1;
        Vector2 local=mouse-debugWindow.position;
        if(local.x<6&&local.y>DebugHeaderHeight)return 0;
        if(local.x>debugWindow.width-6&&local.y>DebugHeaderHeight)return 1;
        if(local.y>debugWindow.height-6)return 2;
        return -1;
    }
    int DebugCursorDirection(Vector2 mouse)
    {
        int side=debugResizeEdge>=0?debugResizeEdge:debugInteracting?-1:DebugResizeEdge(mouse);
        return side==2?2:side==0||side==1?1:0;
    }
    void DrawDebugWindow()
    {
        if(!showDebugWindow)return;
        if(debugTextStyle==null)debugTextStyle=new GUIStyle(codeStyle){wordWrap=true,richText=false,padding=new RectOffset(4,4,4,4),fontSize=16};
        if(debugTitleStyle==null)debugTitleStyle=new GUIStyle(debugTextStyle){wordWrap=false,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(0,0,0,0),contentOffset=Vector2.zero,fixedHeight=0};
        Rect before=debugWindow;
        Rect drawn=GUI.Window(197601,debugWindow,DrawDebugContents,GUIContent.none);
        if(debugWindow==before)debugWindow=drawn; // Preserve size changes made by the callback.
        if(Event.current.rawType==EventType.MouseUp)debugInteracting=false;
    }
    void DrawDebugContents(int id)
    {
        var ev=Event.current;
        if(ev.type==EventType.MouseDown)debugInteracting=true;
        int resizeId=GUIUtility.GetControlID("DebugResize".GetHashCode(),FocusType.Passive);
        int edge=DebugResizeEdge(ev.mousePosition+debugWindow.position);
        if(ev.type==EventType.MouseDown&&ev.button==0&&edge>=0){debugResizeEdge=edge;debugResizeStart=debugWindow;debugResizeMouse=GUIUtility.GUIToScreenPoint(ev.mousePosition);GUIUtility.hotControl=resizeId;ev.Use();}
        if(debugResizeEdge>=0&&ev.type==EventType.MouseDrag){
            Vector2 delta=GUIUtility.GUIToScreenPoint(ev.mousePosition)-debugResizeMouse;
            Rect rect=debugResizeStart;
            if(debugResizeEdge==0)rect.xMin=Mathf.Min(rect.xMin+delta.x,rect.xMax-320);
            else if(debugResizeEdge==1)rect.width=Mathf.Max(320,rect.width+delta.x);
            else rect.height=Mathf.Max(160,rect.height+delta.y);
            debugWindow=rect;ev.Use();
        }
        if(debugResizeEdge>=0&&ev.rawType==EventType.MouseUp){debugResizeEdge=-1;GUIUtility.hotControl=0;ev.Use();}
        // Draw the title explicitly: the skin window caption clips the Korean font.
        GUI.Label(new Rect(10,2,debugWindow.width-110,DebugHeaderHeight-4),"디버그 출력",debugTitleStyle);
        if(GUI.Button(new Rect(debugWindow.width-90,5,52,24),"지우기")){debugOutput.Clear();debugScroll=Vector2.zero;}
        if(GUI.Button(new Rect(debugWindow.width-32,5,26,24),"×")){showDebugWindow=false;debugInteracting=false;}
        GUI.Label(new Rect(10,DebugHeaderHeight+2,debugWindow.width-20,24),"print 출력 · 읽기 전용");
        Rect view=new Rect(10,DebugHeaderHeight+30,debugWindow.width-20,debugWindow.height-DebugHeaderHeight-41);
        string text=debugOutput.Text;
        float height=Mathf.Max(view.height,debugTextStyle.CalcHeight(new GUIContent(text+"\n "),Mathf.Max(1,view.width-18)));
        if(debugFollow){debugScroll.y=Mathf.Max(0,height-view.height);if(ev.type==EventType.Repaint)debugFollow=false;}
        debugScroll=GUI.BeginScrollView(view,debugScroll,new Rect(0,0,view.width-18,height));
        GUI.Label(new Rect(0,0,view.width-18,height),text,debugTextStyle);
        GUI.EndScrollView();
        GUI.DragWindow(new Rect(6,0,Mathf.Max(0,debugWindow.width-100),DebugHeaderHeight));
    }
    Vector2 BookWorkspacePoint(Vector2 screenPoint) => screenPoint/workspaceZoom-workspaceOffset;
    void InitializeGuideBook()
    {
        guideBook.EnsureInitialized(BookWorkspacePoint(new Vector2(Mathf.Max(12,Screen.width-432),ToolbarHeight+15)));
    }
    void OpenTechTree()
    {
        CommitName();EndWorkspaceDrag();guideBook.CancelDrag();techTree.CancelDrag();
        edge=-1;debugResizeEdge=-1;debugInteracting=false;hover="";
        GUIUtility.hotControl=0;GUI.FocusControl(null);SetResizeCursor(0);showTechTree=true;
    }
    void OnGUI()
    {
        InitializeCodeStyle();
        if(showTitle){DrawTitle();return;}
        if(showProgress){DrawProgress();return;}
        if(showTechTree){if(techTree.Draw(codeStyle.font,technology,gold,iron,materialStocks,busy||executionSession!=null,purchaseMessage,PurchaseTechnology)){showTechTree=false;GUI.FocusControl(null);}return;}
        if (!windowsInitialized) { workspaceWindow=InitialCodeWindow; windowsInitialized = true; nextWindowId = 1; codeWindows.Add(new CodeWindow { rect = workspaceWindow, id = 0, name = "f1" }); }
        EventType resourceEvent = Event.current.type;
        bool maskResources = deleteWindowId < 0 && DebugResourcesRect.Contains(Event.current.mousePosition) && (Event.current.isMouse || Event.current.type == EventType.ScrollWheel);
        if(maskResources)Event.current.type=EventType.Ignore;
        EventType modalEvent = Event.current.type;
        bool modalOpen = deleteWindowId >= 0;
        if (modalOpen && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint) Event.current.type = EventType.Ignore;
        var ev = Event.current;
        // Transfer a held text-selection gesture only after it leaves the code window.
        if(codePointerControl!=0 && ev.rawType==EventType.MouseDrag && ev.button==0
            && GUIUtility.hotControl==codePointerControl && codeWindows.Count>0 && !modalOpen){
            Rect selectionWindow=codeWindows[activeWindow].rect;
            selectionWindow.position+=workspaceOffset;
            if(!selectionWindow.Contains(ev.mousePosition/workspaceZoom)){
                GUIUtility.hotControl=0;GUI.FocusControl(null);
                panning=true;dragButton=0;dragStartMouse=ev.mousePosition;dragStartPosition=workspaceOffset;
                codePointerControl=0;
            }
        }
        if(ev.rawType==EventType.MouseUp)codePointerControl=0;
        EventType toolbarEvent=ev.type;
        bool maskToolbar=!panning&&(ev.mousePosition.y<ToolbarHeight||new Rect(12,Screen.height-36,190,28).Contains(ev.mousePosition))&&(ev.isMouse||ev.type==EventType.ScrollWheel);
        if(maskToolbar)ev.type=EventType.Ignore;
        if(showGuideBook)InitializeGuideBook();
        EventType bookUnderlyingEvent=ev.type;
        bool maskBook=!panning&&showGuideBook&&(guideBook.Dragging||guideBook.Contains(BookWorkspacePoint(ev.mousePosition)))&&(ev.isMouse||ev.type==EventType.ScrollWheel);
        if(maskBook)ev.type=EventType.Ignore;
        EventType debugUnderlyingEvent=ev.type;
        bool maskDebug=!panning&&showDebugWindow&&(debugInteracting||debugWindow.Contains(ev.mousePosition))&&(ev.isMouse||ev.type==EventType.ScrollWheel);
        if(maskDebug)ev.type=EventType.Ignore;
        CodeWindow diagnosticOwner = codeWindows.Count > 0 ? codeWindows[activeWindow] : null;
        EventType diagnosticEvent = ev.type;
        bool maskDiagnostic = !panning && diagnosticOwner != null && diagnosticOwner.showDiagnostic && diagnosticOwner.diagnostic != null
            && DiagnosticRect().Contains(ev.mousePosition) && (ev.isMouse || ev.type == EventType.ScrollWheel);
        if (maskDiagnostic) ev.type = EventType.Ignore;
        Vector2 mouse = ev.mousePosition / workspaceZoom;
        workspaceMouse = mouse;
        dragControl = GUIUtility.GetControlID("WorkspaceDrag".GetHashCode(), FocusType.Passive);
        if(panning&&dragButton>=0&&GUIUtility.hotControl==0)GUIUtility.hotControl=dragControl;
        bool overWindow = false;
        int hit = -1;
        for (int i = 0; i < codeWindows.Count; i++)
        {
            Rect r = codeWindows[i].rect; r.position += workspaceOffset;
            Rect bounds = new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8);
            if (bounds.Contains(mouse)) { overWindow = true; hit = i; }
        }
        if (codeWindows.Count > 0)
        {
        Rect activeRect = codeWindows[activeWindow].rect; activeRect.position += workspaceOffset;
        Rect activeBounds = new Rect(activeRect.x - 4, activeRect.y - 4, activeRect.width + 8, activeRect.height + 8);
        // Active editor is drawn last and receives overlapping clicks first.
        if (activeBounds.Contains(mouse)) { hit = activeWindow; overWindow = true; }
        }
        if (ev.type == EventType.MouseDown && (ev.button == 0 || ev.button == 1) && hit >= 0 && ev.mousePosition.y > ToolbarHeight)
            codeWindows[hit].clickedAt = Time.realtimeSinceStartup;
        Rect salesScreen = salesWindow; salesScreen.position += workspaceOffset;
        int overSalesEdge = -1;
        if (showSalesPanel)
        {
            if (new Rect(salesScreen.x - 4, salesScreen.y, 8, salesScreen.height).Contains(mouse)) overSalesEdge = 0;
            else if (new Rect(salesScreen.xMax - 4, salesScreen.y, 8, salesScreen.height).Contains(mouse)) overSalesEdge = 1;
            else if (new Rect(salesScreen.x, salesScreen.yMax - 4, salesScreen.width, 8).Contains(mouse)) overSalesEdge = 2;
        }
        bool overSales = showSalesPanel && (salesScreen.Contains(mouse) || overSalesEdge >= 0 || salesResizeEdge >= 0);
        if (overSales) { overWindow = true; hit = -1; }
        bool horizontalEdge = edge == 0 || edge == 1 || salesResizeEdge == 0 || salesResizeEdge == 1
            || (!panning && movingWindow < 0 && !movingSales && (overSalesEdge == 0 || overSalesEdge == 1));
        bool verticalEdge = edge == 2 || salesResizeEdge == 2
            || (!panning && movingWindow < 0 && !movingSales && overSalesEdge == 2);
        if (hit >= 0 && !codeWindows[hit].collapsed && !overSales && ev.mousePosition.y > ToolbarHeight && !panning && movingWindow < 0 && !movingSales)
        {
            Rect hovered = codeWindows[hit].rect; hovered.position += workspaceOffset;
            verticalEdge |= new Rect(hovered.x,hovered.yMax-4,hovered.width,8).Contains(mouse);
            horizontalEdge |= mouse.y >= hovered.y + 4 && mouse.y <= hovered.yMax - 4
                && (Mathf.Abs(mouse.x - hovered.x) <= 4 || Mathf.Abs(mouse.x - hovered.xMax) <= 4);
        }
        // Cursor hit-testing must also run on Repaint; event masking is only for input.
        // Match the visible window stacking order, retaining resize cursors during capture.
        Vector2 screenPointer=ev.mousePosition;
        int cursorDirection=verticalEdge?2:horizontalEdge?1:0;
        if(modalOpen)cursorDirection=0;
        else if(showGuideBook&&(guideBook.Dragging||guideBook.Contains(BookWorkspacePoint(screenPointer))))cursorDirection=guideBook.CursorDirection(BookWorkspacePoint(screenPointer));
        else if(showDebugWindow&&debugResizeEdge>=0)cursorDirection=DebugCursorDirection(screenPointer);
        else if(diagnosticOwner!=null&&diagnosticOwner.showDiagnostic&&diagnosticOwner.diagnostic!=null&&DiagnosticRect().Contains(screenPointer))cursorDirection=0;
        else if(showDebugWindow&&(debugInteracting||debugWindow.Contains(screenPointer)))cursorDirection=DebugCursorDirection(screenPointer);
        else if(screenPointer.y<ToolbarHeight)cursorDirection=0;
        SetResizeCursor(cursorDirection);
        if (renamingWindow >= 0 && ev.type == EventType.MouseDown)
        {
            Rect renameRect = codeWindows[renamingWindow].rect; renameRect.position += workspaceOffset;
            if (!NameRect(renameRect).Contains(mouse)) CommitName();
        }
        if (ev.type == EventType.ScrollWheel && !overWindow && ev.mousePosition.y > ToolbarHeight && dragButton < 0)
        {
            Vector2 anchor = mouse - workspaceOffset;
            workspaceZoom = Mathf.Clamp(workspaceZoom * Mathf.Pow(1.05f, -ev.delta.y), .4f, 2f);
            workspaceOffset = ev.mousePosition / workspaceZoom - anchor;
            hover = ""; hoverSince = Time.realtimeSinceStartup;
            UpdateWorkspaceCamera(); ev.Use();
        }
        if (ev.type == EventType.MouseDown && (ev.button == 0 || ev.button == 1)
            && ev.mousePosition.y > ToolbarHeight && GUIUtility.hotControl == 0)
        {
            if (overSalesEdge >= 0 && ev.button == 0 && edge < 0)
            { salesResizeEdge = overSalesEdge; salesResizeStart = salesWindow; }
            else if (overSales && new Rect(salesScreen.x + 8, salesScreen.y + 4, salesScreen.width - 50, 30).Contains(mouse))
            { movingSales = true; draggedTitle = false; dragStartPosition = salesWindow.position; }
            else if (hit >= 0 && edge < 0)
            {
                activeWindow = hit;
                Rect r = codeWindows[hit].rect; r.position += workspaceOffset;
                Rect title = new Rect(r.x + 4, r.y + 4, r.width - 8, 30);
                Rect stepButton = new Rect(r.x + 40, r.y + 5, 28, 24);
                Rect runButton = new Rect(r.x + 8, r.y + 5, 28, 24);
                Rect stopButton = new Rect(r.x + 72, r.y + 5, 28, 24);
                if (title.Contains(mouse) && !stepButton.Contains(mouse) && !runButton.Contains(mouse) && !stopButton.Contains(mouse) && mouse.x < r.xMax - 68
                    && !(renamingWindow == hit && NameRect(r).Contains(mouse)))
                {
                    movingWindow = hit;
                    draggedTitle = false;
                    dragStartPosition = codeWindows[hit].rect.position;
                }
            }
            else if (!overWindow) { panning = true; dragStartPosition = workspaceOffset; }
            if (movingWindow >= 0 || movingSales || salesResizeEdge >= 0 || panning)
            {
                dragButton = ev.button; dragStartMouse = ev.mousePosition;
                GUIUtility.hotControl = dragControl; GUI.FocusControl(null); ev.Use();
            }
        }
        if (GUIUtility.hotControl == dragControl && dragButton >= 0)
        {
            if (ev.type == EventType.MouseDrag && ev.button == dragButton)
            {
                if ((ev.mousePosition - dragStartMouse).sqrMagnitude > 16) draggedTitle = true;
                Vector2 movement = ev.mousePosition - dragStartMouse;
                movement = Vector2.Scale(movement, new Vector2(.8f, 1f));
                Vector2 position = dragStartPosition + movement / workspaceZoom;
                if (salesResizeEdge >= 0)
                {
                    Vector2 delta = (ev.mousePosition - dragStartMouse) / workspaceZoom;
                    Rect resized = salesResizeStart;
                    if (salesResizeEdge == 0) resized.xMin = Mathf.Min(salesResizeStart.xMin + delta.x, salesResizeStart.xMax - 340);
                    else if (salesResizeEdge == 1) resized.xMax = Mathf.Max(salesResizeStart.xMax + delta.x, salesResizeStart.xMin + 340);
                    else resized.yMax = Mathf.Max(salesResizeStart.yMax + delta.y, salesResizeStart.yMin + 180);
                    salesWindow = resized;
                }
                else if (movingWindow >= 0 && draggedTitle) codeWindows[movingWindow].rect.position = position;
                else if (movingSales && draggedTitle) salesWindow.position = position;
                else if (panning) workspaceOffset = position;
                hover = ""; hoverSince = Time.realtimeSinceStartup;
                UpdateWorkspaceCamera(); ev.Use();
            }
            if (ev.rawType == EventType.MouseUp && ev.button == dragButton)
            {
                if (movingWindow >= 0 && !draggedTitle && dragButton == 0)
                {
                    Rect r = codeWindows[movingWindow].rect; r.position += workspaceOffset;
                    if (NameRect(r).Contains(mouse))
                    {
                        renamingWindow = movingWindow;
                        renameDraft = codeWindows[movingWindow].name;
                        focusRename = true;
                    }
                }
                EndWorkspaceDrag(); ev.Use();
            }
        }
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(workspaceZoom, workspaceZoom, 1));
        // Inactive windows are previews; selecting one gives it the full editor controls.
        for (int i = 0; i < codeWindows.Count; i++)
        {
            if (i == activeWindow) continue;
            Rect r = codeWindows[i].rect; r.position += workspaceOffset;
            DrawCodeBackground(r, codeWindows[i]);
            GUI.Label(NameRect(r), codeWindows[i].name);
            if (codeWindows[i].collapsed) continue;
            DrawCodeGutter(r);
            Rect preview = new Rect(r.x + 40, r.y + 40, r.width - 52, r.height - 58);
            GUI.BeginGroup(preview);
            Vector2 previewSize = MeasureCode(codeWindows[i].text);
            DrawSyntax(new Rect(-codeWindows[i].scroll.x, -codeWindows[i].scroll.y,
                Mathf.Max(preview.width, previewSize.x), Mathf.Max(preview.height, previewSize.y)), codeWindows[i].text, codeWindows[i]);
            GUI.EndGroup();
        }
        if (codeWindows.Count > 0)
        {
        CodeWindow current = codeWindows[activeWindow];
        workspaceWindow = current.rect; source = current.text; scroll = current.scroll; undoHistory = current.undo;
        controlName = CodeControl + current.id;
        EventType underlyingEvent = ev.type;
        bool maskEditor = overSales && ev.isMouse || overSales && ev.type == EventType.ScrollWheel;
        if (maskEditor) ev.type = EventType.Ignore;
        DrawCodeWindow();
        if (maskEditor) ev.type = underlyingEvent;
        current.rect = workspaceWindow; current.text = source; current.scroll = scroll;
        }
        DrawSalesWindow();
        GUI.matrix = previous;
        if(maskToolbar)ev.type=toolbarEvent;
        DrawToolbar();
        if(showTechTree)return;
        if(maskDebug)ev.type=debugUnderlyingEvent;
        DrawDebugWindow();
        if (maskDiagnostic) ev.type = diagnosticEvent;
        DrawCompileErrorWindow(diagnosticOwner);
        if(maskBook)ev.type=bookUnderlyingEvent;
        if(showGuideBook&&!modalOpen){
            Matrix4x4 bookMatrix=GUI.matrix;
            GUI.matrix=bookMatrix*Matrix4x4.TRS(new Vector3(workspaceOffset.x*workspaceZoom,workspaceOffset.y*workspaceZoom,0),Quaternion.identity,new Vector3(workspaceZoom,workspaceZoom,1));
            guideBook.Draw(ref showGuideBook,codeStyle.font,codeStyle.fontSize,englishGuideUnlocked);
            GUI.matrix=bookMatrix;
        }
        if(maskResources)Event.current.type=resourceEvent;
        if(!modalOpen)DrawDebugResources();
        if (modalOpen) Event.current.type = modalEvent;
        if(deleteWindowId>=0)DrawDeleteDialog();
    }
    Vector2 debugResourceScroll;
    Rect DebugResourcesRect {
        get {
            float width=Mathf.Min(816,Mathf.Max(1,Screen.width-24));
            float height=Mathf.Min(644,Mathf.Max(1,Screen.height-ToolbarHeight-24));
            return new Rect(Screen.width-width-12,Screen.height-height-12,width,height);
        }
    }
    void DrawDebugResources()
    {
        Rect panel=DebugResourcesRect;
        GUI.Box(panel,"디버그 · 자원 +1,000 / 하루 넘기기");
        var debugButtonStyle=new GUIStyle(GUI.skin.button){font=codeStyle.font,fontSize=36};
        Rect viewport=new Rect(panel.x+8,panel.y+28,panel.width-16,panel.height-36);
        debugResourceScroll=GUI.BeginScrollView(viewport,debugResourceScroll,new Rect(0,0,780,594));
        bool enabled=GUI.enabled;
        GUI.enabled=enabled&&!busy&&executionSession==null;
        for(int i=0;i<12;i++){
            string name=i==11?"게임 초기화":i==10?"하루 넘기기":(i==0?"골드":ForgeMaterialGuide.Find(i).Name)+" +1,000";
            Rect button=new Rect((i%2)*390,(i/2)*99,378,87);
            if(GUI.Button(button,new GUIContent(name,i==11?"자원·날짜·해금·완성품·기록·작성한 도면을 초기화하고 저장합니다.":i==10?"현재 시각을 유지하며 다음 날로 이동합니다.":"제작을 멈춘 상태에서 자원을 추가합니다."),debugButtonStyle)){
                if(i==11){ResetProgressForDebug();continue;}
                if(i==10){
                    if(DayCycle!=null&&DayCycle.Day<int.MaxValue)DayCycle.Clock.Restore(DayCycle.Day+1,DayCycle.CurrentHour);
                    SaveProgress();
                    continue;
                }
                if(i==0)gold=(int)Math.Min(int.MaxValue,(long)gold+1000);
                else if(i==1)iron=(int)Math.Min(int.MaxValue,(long)iron+1000);
                else {
                    var stock=Array.Find(materialStocks,s=>s!=null&&s.material==i);
                    if(stock==null){
                        stock=new ForgeMaterialStock{material=i};
                        var list=new List<ForgeMaterialStock>(materialStocks);
                        list.Add(stock);materialStocks=list.ToArray();
                    }
                    stock.unlocked=true;
                    stock.count=stock.count>long.MaxValue-1000?long.MaxValue:stock.count+1000;
                }
                SaveProgress();
            }
        }
        GUI.enabled=enabled;
        GUI.EndScrollView();
    }
    GUIStyle statusTextStyle;
    void DrawStatusTile(Rect rect,string title,string value,string tooltip)
    {
        if(statusTextStyle==null)statusTextStyle=new GUIStyle(GUI.skin.label){font=codeStyle.font,fontSize=28,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(0,0,0,0),richText=false};
        // Same 112px footprint and button skin as the action icons on the right.
        GUI.Box(rect,new GUIContent("",tooltip),GUI.skin.button);
        GUI.Label(new Rect(rect.x+4,rect.y+12,rect.width-8,36),title,statusTextStyle);
        GUI.Label(new Rect(rect.x+4,rect.y+58,rect.width-8,36),value,statusTextStyle);
    }
    void DrawToolbar()
    {
        DrawProgressButton();
        DrawStatusTile(new Rect(12,StatusTop,112,112),"자금",ForgeMaterialHud.FormatCount(gold)+"G",gold.ToString("N0")+"G");
        DrawStatusTile(new Rect(132,StatusTop,112,112),"완성품",ForgeMaterialHud.FormatCount(products),products.ToString("N0")+"개");
        DrawStatusTile(new Rect(252,StatusTop,112,112),"확대",Mathf.RoundToInt(workspaceZoom*100)+"%","현재 작업 화면 배율");
        materialHud.Draw(iron,materialStocks,codeStyle.font,Mathf.Max(144,Screen.width-496),MaterialTop);
        Rect tech=new Rect(Screen.width-122,4,112,112);
        if(ForgeGuideUI.IconButton(tech,ForgeGuideUI.Icon.TechTree,"테크 트리")){OpenTechTree();return;}
        Rect book=new Rect(Screen.width-242,4,112,112);
        Rect shopButton=new Rect(Screen.width-362,4,112,112);
        Rect add=new Rect(Screen.width-482,4,112,112);
        if(GUI.Button(new Rect(Screen.width-122,124,112,28),"디버그 창"))showDebugWindow=!showDebugWindow;
        if(ForgeGuideUI.IconButton(add,ForgeGuideUI.Icon.Add,"코드 창 추가"))
        {
            CommitName();
            int id=nextWindowId++;
            codeWindows.Add(new CodeWindow {id=id,name="f"+(id+1),rect=new Rect(new Vector2(70,ToolbarHeight+15)/workspaceZoom-workspaceOffset,new Vector2(360,240))});
            activeWindow=codeWindows.Count-1;edge=-1;movingWindow=-1;GUI.FocusControl(null);
        }
        if(ForgeGuideUI.IconButton(shopButton,ForgeGuideUI.Icon.Shop,showSalesPanel?"상점 숨기기":"상점 열기",showSalesPanel))showSalesPanel=!showSalesPanel;
        if(ForgeGuideUI.IconButton(book,ForgeGuideUI.Icon.Book,"도면 안내서",showGuideBook))
        {
            showGuideBook=!showGuideBook;InitializeGuideBook();guideBook.CancelDrag();CommitName();EndWorkspaceDrag();edge=-1;
            debugResizeEdge=-1;debugInteracting=false;hover="";
            GUIUtility.hotControl=0;GUI.FocusControl(null);SetResizeCursor(0);
        }
        string tip=tech.Contains(Event.current.mousePosition)?"테크 트리":book.Contains(Event.current.mousePosition)?"도면 안내서":shopButton.Contains(Event.current.mousePosition)?(showSalesPanel?"상점 숨기기":"상점 열기"):add.Contains(Event.current.mousePosition)?"코드 창 추가":"";
        if(tip.Length>0&&!showGuideBook)GUI.Box(new Rect(Mathf.Max(0,Screen.width-180),120,170,26),tip);
    }
    Rect DiagnosticRect()
    {
        float width = Mathf.Min(380, Mathf.Max(1, Screen.width - 16));
        float height = Mathf.Min(190, Mathf.Max(1, Screen.height - 56));
        return new Rect(Mathf.Max(8, Screen.width - width - 12), 48, width, height);
    }
    void DrawCompileErrorWindow(CodeWindow owner)
    {
        if (owner == null || !owner.showDiagnostic || owner.diagnostic == null) return;
        Rect panel = DiagnosticRect();
        Color previous = GUI.color;
        GUI.color = new Color(.16f, .08f, .09f, 1f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(1f, .25f, .25f, 1f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(new Rect(panel.x + 10, panel.y + 7, panel.width - 48, 24), owner.diagnosticCaption + " · " + owner.name);
        if (GUI.Button(new Rect(panel.xMax - 32, panel.y + 5, 24, 24), "×")) owner.showDiagnostic = false;
        GUILayout.BeginArea(new Rect(panel.x + 10, panel.y + 36, panel.width - 20, Mathf.Max(1, panel.height - 46)));
        owner.diagnosticScroll = GUILayout.BeginScrollView(owner.diagnosticScroll);
        GUILayout.Label(owner.diagnostic.Line + "줄: " + owner.diagnostic.Message, errorLabel);
        GUILayout.EndScrollView();
        if (GUILayout.Button("오류 줄 보기"))
        {
            owner.revealDiagnostic = true;
            if (owner.collapsed) { owner.collapsed = false; owner.rect.height = owner.expandedHeight; }
        }
        GUILayout.EndArea();
        if (panel.Contains(Event.current.mousePosition) && (Event.current.isMouse || Event.current.type == EventType.ScrollWheel)) Event.current.Use();
    }
    void DrawDiagnosticUnderline(Rect content, string text, CodeWindow owner)
    {
        if (owner.diagnostic == null || owner.diagnostic.Source != text) return;
        owner.diagnostic.GetLineSpan(out int start, out int length);
        Vector2 first = codeStyle.GetCursorPixelPosition(content, new GUIContent(text), start);
        Vector2 last = codeStyle.GetCursorPixelPosition(content, new GUIContent(text), start + length);
        Color previous = GUI.color;
        GUI.color = new Color(1f, .2f, .2f, 1f);
        GUI.DrawTexture(new Rect(first.x, first.y + codeStyle.lineHeight - 1, Mathf.Max(20, last.x - first.x), 2), Texture2D.whiteTexture);
        GUI.color = previous;
    }
    void DrawCodeBackground(Rect rect, CodeWindow codeWindow)
    {
        // One fill for both the gutter and editor; no separate focused editor background.
        float elapsed = Time.realtimeSinceStartup - codeWindow.clickedAt;
        float flash = Mathf.Clamp01(1f - elapsed / .25f);
        Color previous = GUI.color;
        GUI.color = new Color(.12f + .045f * flash, .14f + .045f * flash, .17f + .045f * flash, .78f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(.48f, .52f, .58f, .28f);
        GUI.DrawTexture(new Rect(rect.x + 8, rect.y + 35, rect.width - 16, 1), Texture2D.whiteTexture);
        GUI.color = previous;
    }
    void DrawSyntax(Rect content, string text, CodeWindow owner)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (owner.coloredSource != text || owner.coloredTokens == null)
        {
            owner.coloredSource = text;
            owner.coloredTokens = ForgeSyntax.Scan(text);
        }
        var fullText = new GUIContent(text);
        DrawExecutionHighlight(content,text,owner);
        DrawDiagnosticUnderline(content, text, owner);
        foreach (var token in owner.coloredTokens)
        {
            Color color = Color.white;
            switch (token.kind)
            {
                case ForgeSyntax.Kind.Reserved: color = reservedColor; break;
                case ForgeSyntax.Kind.Comment: color = new Color(.42f, .78f, .42f); break;
                case ForgeSyntax.Kind.Material: color = new Color(1f, .55f, .76f); break;
                case ForgeSyntax.Kind.Keyword: color = new Color(1f, .65f, .30f); break;
                case ForgeSyntax.Kind.Function: color = new Color(1f, .92f, .60f); break;
            }
            tokenStyle.normal.textColor = color;
            tokenStyle.hover.textColor = color;
            var value = new GUIContent(text.Substring(token.start, token.length));
            Vector2 position = codeStyle.GetCursorPixelPosition(content, fullText, token.start);
            Vector2 size = tokenStyle.CalcSize(value);
            GUI.Label(new Rect(position.x, position.y, size.x + 2, Mathf.Max(size.y, codeStyle.lineHeight)), value, tokenStyle);
        }
    }
    void DrawCodeGutter(Rect window)
    {
        Color previous = GUI.color;
        GUI.color = new Color(.48f, .52f, .58f, .4f);
        GUI.DrawTexture(new Rect(window.x + 36, window.y + 42, 1, Mathf.Max(0, window.height - 62)), Texture2D.whiteTexture);
        GUI.color = previous;
    }
    Vector2 MeasureCode(string text)
    {
        // Explicit line count includes trailing empty lines; CalcSize alone is not
        // a reliable scroll extent for an editor ending in a newline.
        Vector2 measured=codeStyle.CalcSize(new GUIContent(text+"\n "));
        measured.y=Mathf.Max(measured.y,BlueprintViewport.ContentHeight(text,codeStyle.lineHeight,codeStyle.padding.vertical));
        return measured;
    }
    void FollowCodeCaret(Rect content,Rect viewport,int index)
    {
        index=Mathf.Clamp(index,0,source.Length);
        Vector2 caret=codeStyle.GetCursorPixelPosition(content,new GUIContent(source+" "),index);

        const float margin=4f;
        float lineHeight=Mathf.Max(1f,codeStyle.lineHeight);
        scroll.y=BlueprintViewport.FollowY(source,index,lineHeight,codeStyle.padding.top,viewport.height,content.height,scroll.y);
        if(caret.x<scroll.x+margin)scroll.x=caret.x-margin;
        else if(caret.x+2f>scroll.x+viewport.width-margin)scroll.x=caret.x+2f-viewport.width+margin;
        scroll.x=Mathf.Clamp(scroll.x,0,Mathf.Max(0,content.width-viewport.width));
        scroll.y=Mathf.Clamp(scroll.y,0,Mathf.Max(0,content.height-viewport.height));
    }
    void DrawCodeWindow()
    {
        InitializeCodeStyle();
        var ev = Event.current; Vector2 mouse = workspaceMouse;
        GUI.skin.font = codeStyle.font;
        Rect window = workspaceWindow;
        window.position += workspaceOffset;

        Rect[] handles = { new Rect(window.x-4,window.y,8,window.height), new Rect(window.xMax-4,window.y,8,window.height), new Rect(window.x,window.yMax-4,window.width,8) };
        int over = -1; for(int i=0;i<handles.Length;i++) if(handles[i].Contains(mouse)) over=i;
        if (ev.type == EventType.MouseDown && ev.button == 0 && !panning && over >= 0 && !codeWindows[activeWindow].collapsed) { edge=over; ev.Use(); }
        if (edge >= 0 && ev.type == EventType.MouseDrag)
        {
            if(edge==0) window.xMin=Mathf.Min(mouse.x,window.xMax-340);
            if(edge==1) window.xMax=Mathf.Max(mouse.x,window.xMin+340);
            if(edge==2) window.yMax=Mathf.Max(mouse.y,window.yMin+180);
            ev.Use();
        }
        if(ev.rawType==EventType.MouseUp) edge=-1;
        workspaceWindow = window;
        workspaceWindow.position -= workspaceOffset;

        DrawCodeBackground(window, codeWindows[activeWindow]);
        if (renamingWindow == activeWindow)
        {
            if (ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter))
            { CommitName(); ev.Use(); }
            else if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape)
            { renamingWindow = -1; GUI.FocusControl(null); ev.Use(); }
            if (renamingWindow == activeWindow)
            {
                GUI.SetNextControlName("RenameFunction");
                var nameStyle = new GUIStyle(codeStyle) { fontSize = 16, padding = new RectOffset(0, 0, 2, 2) };
                renameDraft = GUI.TextField(NameRect(window), renameDraft, 32, nameStyle);
                if (focusRename) { GUI.FocusControl("RenameFunction"); focusRename = false; }
            }
        }
        else GUI.Label(NameRect(window), new GUIContent(codeWindows[activeWindow].name));
        GUI.enabled=!busy;
        if(GUI.Button(new Rect(window.x+40,window.y+5,28,24),new GUIContent("▷", "한 줄 실행 · 실제 재료가 소비됩니다"), actionButton)) Execute(true);
        if(GUI.Button(new Rect(window.x+8,window.y+5,28,24),new GUIContent("▶", "전체 실행 · 일시 정지 중이면 이어서 실행"), actionButton)) Execute(false);
        GUI.enabled=!busy&&executionSession!=null;
        if(GUI.Button(new Rect(window.x+72,window.y+5,28,24),new GUIContent("■", "실행 중단 · 이미 소비한 비용은 유지됩니다"),actionButton))StopExecution(true);
        GUI.enabled=true;
        if (GUI.Button(new Rect(window.xMax - 64, window.y + 5, 24, 24), "−", actionButton))
        {
            var selected = codeWindows[activeWindow];
            if (!selected.collapsed) { selected.expandedHeight = workspaceWindow.height; workspaceWindow.height = 36; }
            else workspaceWindow.height = selected.expandedHeight;
            selected.collapsed = !selected.collapsed;
        }
        if (GUI.Button(new Rect(window.xMax - 34, window.y + 5, 24, 24), "X", actionButton))
        { CommitName(); deleteWindowId = codeWindows[activeWindow].id; EndWorkspaceDrag(); }
        if (codeWindows[activeWindow].collapsed) return;
        DrawCodeGutter(window);
        Rect viewport=new Rect(window.x+40,window.y+40,window.width-52,window.height-58);
        Vector2 size=MeasureCode(source);
        Rect content=new Rect(0,0,Mathf.Max(viewport.width,size.x),Mathf.Max(viewport.height,size.y));
        if (codeWindows[activeWindow].revealDiagnostic && codeWindows[activeWindow].diagnostic != null)
        {
            codeWindows[activeWindow].diagnostic.GetLineSpan(out int errorStart, out int errorLength);
            Vector2 errorPosition = codeStyle.GetCursorPixelPosition(content, new GUIContent(source), errorStart);
            scroll.x = 0;
            scroll.y = Mathf.Clamp(errorPosition.y - viewport.height * .35f, 0, Mathf.Max(0, content.height - viewport.height));
            codeWindows[activeWindow].revealDiagnostic = false;
        }
        if(codeWindows[activeWindow].revealExecution&&codeWindows[activeWindow].executionSource==source){
            new ForgeCompileDiagnostic(source,codeWindows[activeWindow].executionLine,"").GetLineSpan(out int nextStart,out int nextLength);
            FollowCodeCaret(content,viewport,nextStart);codeWindows[activeWindow].revealExecution=false;
        }
        // Apply on the next GUI pass, after the newly inserted lines have a layout.
        // BeginScrollView must receive both the new content size and the desired scroll.
        var scrollOwner=codeWindows[activeWindow];
        if(scrollOwner.pendingCaret>=0)
        {
            FollowCodeCaret(content,viewport,scrollOwner.pendingCaret);

        }
        Vector2 oldScroll=scroll;
        Vector2 requestedScroll=scroll;
        scroll=GUI.BeginScrollView(viewport,scroll,content, false, false, GUIStyle.none, GUIStyle.none);
        if(scrollOwner.pendingCaret>=0 && ev.type==EventType.Repaint)
        {
            if((scroll-requestedScroll).sqrMagnitude<.01f)scrollOwner.pendingCaret=-1;
            else scroll=requestedScroll; // Retry if native wheel state overrode this reveal.
        }
        string old=source;
        bool focused = GUI.GetNameOfFocusedControl() == controlName;
        var editor = focused ? (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) : null;
        var before = new EditSnapshot { text = source, cursor = editor != null ? editor.cursorIndex : 0,
            selection = editor != null ? editor.selectIndex : 0, scroll = scroll };
        // TextArea consumes KeyDown, so remember navigation before it handles the event.
        bool followCaret = focused && ev.type == EventType.KeyDown &&
            (ev.keyCode == KeyCode.DownArrow || ev.keyCode == KeyCode.UpArrow ||
             ev.keyCode == KeyCode.LeftArrow || ev.keyCode == KeyCode.RightArrow ||
             ev.keyCode == KeyCode.Home || ev.keyCode == KeyCode.End ||
             ev.keyCode == KeyCode.PageDown || ev.keyCode == KeyCode.PageUp ||
             ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter);
        bool imeEditing = IsImeEditing();
        bool undoKey = focused && ev.type == EventType.KeyDown && ev.control && !ev.alt && !ev.shift && ev.keyCode == KeyCode.Z;
        if (undoKey)
        {
            if (undoHistory.Count > 0)
            {
                var snapshot = undoHistory[undoHistory.Count - 1];
                undoHistory.RemoveAt(undoHistory.Count - 1);
                source = snapshot.text;
                scroll = snapshot.scroll;
                editor.text = source;
                editor.cursorIndex = Mathf.Clamp(snapshot.cursor, 0, source.Length);
                editor.selectIndex = Mathf.Clamp(snapshot.selection, 0, source.Length);
            }
            hover = "";
            hoverSince = Time.realtimeSinceStartup;
            ev.Use(); // Prevent the native TextArea undo path from handling this again.
        }
        if(focused && !imeEditing && ev.type==EventType.KeyDown && !ev.control && !ev.alt && !ev.command)
        {
            bool handled=false;
            if(ev.keyCode==KeyCode.Tab)
            { ApplyCodeEdit(BlueprintIndentation.Tab(source,editor.cursorIndex,editor.selectIndex,ev.shift),editor); handled=true; }
            else if(ev.keyCode==KeyCode.Backspace && !ev.shift && BlueprintIndentation.Backspace(source,editor.cursorIndex,editor.selectIndex,out var edit))
            { ApplyCodeEdit(edit,editor); handled=true; }
            if(handled){followCaret=true;ev.Use();}
        }
        GUI.SetNextControlName(controlName);
        // Keep native selection/caret and plain-text editing; only the glyph layer is colored.
        bool startingCodeSelection=ev.type==EventType.MouseDown&&ev.button==0&&content.Contains(ev.mousePosition);
        source=GUI.TextArea(content,source,inputStyle);
        if(startingCodeSelection&&GUI.GetNameOfFocusedControl()==controlName&&GUIUtility.hotControl!=0)
            codePointerControl=GUIUtility.hotControl;
        // Windows can send Return-key and newline-character events separately.
        // Never insert a newline on the key event: TextArea is the sole insertion path.
        bool editorStillFocused=GUI.GetNameOfFocusedControl()==controlName;
        if(focused && editorStillFocused && !undoKey && !imeEditing && source!=old &&
            BlueprintIndentation.IndentNativeNewline(old,source,before.cursor,before.selection,out var newlineEdit))
        {
            var nativeEditor=(TextEditor)GUIUtility.GetStateObject(typeof(TextEditor),GUIUtility.keyboardControl);
            ApplyCodeEdit(newlineEdit,nativeEditor);
        }
        if(editorStillFocused && source!=old && !undoKey)followCaret=true;
        var currentOwner = codeWindows[activeWindow];
        if (currentOwner.diagnostic != null && currentOwner.diagnostic.Source != source)
        {
            currentOwner.diagnostic = null;
            currentOwner.showDiagnostic = false; currentOwner.revealDiagnostic = false;
        }
        DrawSyntax(content, source, currentOwner);
        if (!undoKey && source != old)
        {
            undoHistory.Add(before);
            if (undoHistory.Count > 256) undoHistory.RemoveAt(0);
        }
        GUI.EndScrollView();
        if(source!=old)
        {
            size=MeasureCode(source);
            content.width=Mathf.Max(viewport.width,size.x);content.height=Mathf.Max(viewport.height,size.y);
        }
        if (followCaret && GUI.GetNameOfFocusedControl() == controlName)
        {
            var movedEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            currentOwner.pendingCaret=Mathf.Clamp(movedEditor.cursorIndex,0,source.Length);
            FollowCodeCaret(content,viewport,currentOwner.pendingCaret);
        }
        // Slim bars sit inside the outer frame, outside the symmetric text viewport.
        if (content.height > viewport.height)
            scroll.y = GUI.VerticalSlider(new Rect(window.xMax - 9, viewport.y, 6, viewport.height), scroll.y, 0, content.height - viewport.height, scrollTrack, scrollThumb);
        if (content.width > viewport.width)
        {
            var horizontalTrack = new GUIStyle(scrollTrack) { fixedWidth = 0, fixedHeight = 6 };
            var horizontalThumb = new GUIStyle(scrollThumb) { fixedWidth = 28, fixedHeight = 6 };
            scroll.x = GUI.HorizontalSlider(new Rect(viewport.x, window.yMax - 10, viewport.width, 6), scroll.x, 0, content.width - viewport.width, horizontalTrack, horizontalThumb);
        }
        bool reset=old!=source || oldScroll!=scroll || panning || edge>=0 || movingWindow>=0 || ev.type==EventType.KeyDown || ev.type==EventType.ScrollWheel || mouse!=lastMouse;
        if(reset) { hoverSince=Time.realtimeSinceStartup; hover=""; }
        lastMouse=mouse;
        if(viewport.Contains(mouse) && !panning && movingWindow<0 && edge<0 && Time.realtimeSinceStartup-hoverSince>.5f)
        {
            hover="";
            Vector2 local=mouse-viewport.position+scroll;
            int index=codeStyle.GetCursorStringIndex(content,new GUIContent(source),local);
            foreach(var token in BlueprintLexer.Scan(source, true))
            {
                if (token.Length == 0 || index < token.Start || index > token.Start + token.Length) continue;
                Vector2 start = codeStyle.GetCursorPixelPosition(content,new GUIContent(source),token.Start);
                Vector2 end = codeStyle.GetCursorPixelPosition(content,new GUIContent(source),token.Start+token.Length);
                if (!new Rect(start.x,start.y,end.x-start.x,codeStyle.lineHeight).Contains(local)) continue;
                hover = BlueprintHelp.Describe(source, token);
            }
        }
        else hover="";
        if(hover.Length>0) GUI.Box(new Rect(Mathf.Max(0,Mathf.Min(mouse.x+16,Screen.width/workspaceZoom-420)),Mathf.Max(0,Mathf.Min(mouse.y+24,Screen.height/workspaceZoom-100)),420,100),hover,new GUIStyle(GUI.skin.box) { wordWrap = true, richText = false, alignment = TextAnchor.MiddleLeft });
    }
    void DrawDeleteDialog()
    {
        Color previous = GUI.color;
        GUI.color = new Color(0, 0, 0, .7f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
        Rect dialog = new Rect((Screen.width - 360) / 2f, (Screen.height - 150) / 2f, 360, 150);
        GUI.Box(dialog, "코드 창 삭제");
        GUI.Label(new Rect(dialog.x + 20, dialog.y + 35, 320, 50), "이 코드 창을 삭제하시겠습니까?\n작성한 코드는 복구할 수 없습니다.");
        if (GUI.Button(new Rect(dialog.x + 65, dialog.y + 100, 100, 30), "Yes"))
        {
            int index = codeWindows.FindIndex(w => w.id == deleteWindowId);
            if (index >= 0) codeWindows.RemoveAt(index);
            activeWindow = Mathf.Clamp(activeWindow, 0, Mathf.Max(0, codeWindows.Count - 1));
            renamingWindow = -1; deleteWindowId = -1; edge = -1; EndWorkspaceDrag(); GUI.FocusControl(null);
        }
        if (GUI.Button(new Rect(dialog.x + 195, dialog.y + 100, 100, 30), "No")) deleteWindowId = -1;
        if (Event.current.isMouse || Event.current.isKey || Event.current.type == EventType.ScrollWheel) Event.current.Use();
    }
    void DrawSalesWindow()
    {
        if (!showSalesPanel) return;
        Rect panel = salesWindow; panel.position += workspaceOffset;
        GUI.Box(panel, "");
        GUI.Label(new Rect(panel.x + 10, panel.y + 6, panel.width - 50, 26), "공용 결과 / 상점 판매");
        if (GUI.Button(new Rect(panel.xMax - 34, panel.y + 6, 24, 24), "×")) showSalesPanel = false;
        GUILayout.BeginArea(new Rect(panel.x + 10, panel.y + 38, panel.width - 20, panel.height - 48));
        logs = GUILayout.BeginScrollView(logs);
        GUILayout.Label("실행 도면: " + resultOwner + (busy ? " · 제작 중" : ""));
        if(executionSession!=null)GUILayout.Label("다음 실행: "+(executionSession.NextModule??executionOwner.name)+" · "+executionSession.NextLine+"줄 · ▷ 한 줄 / ▶ 계속 / ■ 중단");
        GUILayout.Label(report, reportError ? errorLabel : GUI.skin.label);
        GUILayout.Space(12);
        GUILayout.Label("판매 가능한 완성품 · " + shop.Count + "개");
        foreach (var item in shop.Items)
        {
            string label = (selectedProductId == item.Id ? "● " : "○ ") + "#" + item.Id + " " + item.Product.DisplayName
                + " · 제작비 " + item.Product.CashCost + "G";
            if (GUILayout.Button(label)) selectedProductId = item.Id;
        }
        var selected = shop.Find(selectedProductId);
        if (selected != null)
        {
            GUILayout.Space(8);
            GUILayout.Label("선택 #" + selected.Id + " · 도면 " + selected.Product.BlueprintName);
            GUILayout.Label(ForgePricing.Describe(selected.Product, false));
        }
        else GUILayout.Label("완성품을 선택하면 판매 견적이 표시됩니다.");
        GUILayout.EndScrollView();
        GUI.enabled = selected != null && !busy && executionSession==null;
        if (GUILayout.Button(selected == null ? "판매할 완성품을 선택하세요" : "선택 완성품 판매 · +" + ForgePricing.Quote(selected.Product).SalePrice + "G", GUILayout.Height(30)))
            SellProduct(selected.Id);
        GUI.enabled = true;
        GUILayout.EndArea();
        if (salesResizeEdge == 2)
            GUI.Box(new Rect(workspaceMouse.x + 12, workspaceMouse.y + 12, 40, 24), "↕");
        if (panel.Contains(workspaceMouse) && (Event.current.isMouse || Event.current.type == EventType.ScrollWheel)) Event.current.Use();
    }
}
