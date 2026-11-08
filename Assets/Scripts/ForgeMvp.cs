using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Temporary single-scene frontend; the evaluator owns no Unity or inventory state.
public class ForgeMvp : MonoBehaviour
{
    [SerializeField] Color reservedColor = new Color(197f/255f, 134f/255f, 192f/255f, 1f);
    const string Example = "Material = 1\nBlade = 1(Material)\n2(Blade, 7)\n3(Blade)\n4(Blade, 0)\n!Blade";
    string source = Example, report = "시험 실행으로 도면을 확인하세요.", hover = "";
    Rect workspaceWindow = new Rect(30, 55, 360, 240);
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
        public bool showDiagnostic, revealDiagnostic;
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
    Vector2 dragStartMouse, dragStartPosition;
    bool showSalesPanel = true;
    Rect salesWindow = new Rect(430, 55, 350, 480);
    bool movingSales;
    int salesResizeEdge = -1;
    Rect salesResizeStart;
    string resultOwner = "아직 실행하지 않음";
    bool reportError;
    int renamingWindow = -1;
    string renameDraft = "";
    bool focusRename, draggedTitle;
    Rect NameRect(Rect r) { return new Rect(r.x + 76, r.y + 5, r.width - 148, 24); }
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
    }
    string controlName = CodeControl;

    bool panning;
    Camera workspaceCamera;
    Vector3 cameraOrigin;
    const float WorkspaceUnitsPerPixel = .01f;
    void ResetWorkspaceView()
    {
        workspaceOffset = Vector2.zero;
        workspaceZoom = 1f;
        panning = false;
        hover = "";
        hoverSince = Time.realtimeSinceStartup;
        UpdateWorkspaceCamera();
    }
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
    void OnApplicationFocus(bool focused) { if (!focused) { EndWorkspaceDrag(); edge = -1; SetHorizontalResizeCursor(false); } }
    Vector2 scroll, logs, lastMouse;
    float hoverSince;
    int edge = -1, gold = 100, iron = 10;
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

    Texture2D horizontalResizeCursor;
    bool horizontalCursorShown;
    void SetHorizontalResizeCursor(bool enabled)
    {
        if (horizontalCursorShown == enabled) return;
        if (enabled && !horizontalResizeCursor)
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
        }
        Cursor.SetCursor(enabled ? horizontalResizeCursor : null, enabled ? new Vector2(16,16) : Vector2.zero, CursorMode.Auto);
        horizontalCursorShown = enabled;
    }
    void OnDisable() { SetHorizontalResizeCursor(false); ReleaseImeKeyboard(); }
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
        SetHorizontalResizeCursor(false);
        if (horizontalResizeCursor) Destroy(horizontalResizeCursor);
        if (windowFill) Destroy(windowFill);
        if (buttonFill) Destroy(buttonFill);
        if (trackFill) Destroy(trackFill);
        if (thumbFill) Destroy(thumbFill);
    }
    GameObject work;
    public class Piece { public int heat; public bool shaped; public ForgeTraits traits; }
    public class Step { public int line, cost, iron, heat, process; public bool shaped, finished, debugOutput; public ForgeTraits traits; public string text; }
    public class Evaluation { public List<Step> steps = new List<Step>(); public string error; public int cost, ironConsumed, errorLine; public string errorMessage; public bool compileError; public ForgeProduct product; public bool skipped, debugOnly; public Dictionary<string, object> values = new Dictionary<string, object>(); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleScene" && FindAnyObjectByType<ForgeMvp>() == null) new GameObject("Forge MVP").AddComponent<ForgeMvp>(); }
    readonly ForgeShop shop = new ForgeShop();
    void Start()
    {
        work = GameObject.CreatePrimitive(PrimitiveType.Cube);
        work.name = "Workpiece"; work.transform.position = new Vector3(2, 1, 0);
        work.transform.localScale = new Vector3(1, .4f, .5f);
        workspaceCamera = Camera.main;
        if (workspaceCamera)
        {
            workspaceCamera.transform.position = new Vector3(0, 5, -9);
            workspaceCamera.transform.LookAt(new Vector3(0, 1, 0));
            workspaceCamera.orthographic = true;
            workspaceCamera.clearFlags = CameraClearFlags.SolidColor;
            workspaceCamera.backgroundColor = new Color(.24f, .25f, .27f, 1f);
            cameraOrigin = workspaceCamera.transform.position;
            UpdateWorkspaceCamera();
        }
        var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bench.name = "Temporary Workbench";
        bench.transform.position = new Vector3(2, .5f, 0);
        bench.transform.localScale = new Vector3(3, .3f, 2);
    }
    public static Evaluation Evaluate(string source, int availableGold = int.MaxValue, int availableIron = int.MaxValue, string owner = "", uint seed = 1)
    { return BlueprintRuntime.Evaluate(source, availableGold, availableIron, owner, seed); }
    void Execute(bool preview)
    {
        if (busy) return;
        var owner = codeWindows[activeWindow];
        var evaluation = Evaluate(source, gold, iron, owner.name, owner.seed);
        if (evaluation.compileError)
        {
            owner.diagnostic = new ForgeCompileDiagnostic(source, evaluation.errorLine, evaluation.errorMessage);
            owner.showDiagnostic = true; owner.revealDiagnostic = true;
            owner.diagnosticScroll = Vector2.zero;
            hover = "";
            return; // Do not open, clear or overwrite the shop/receipt on compilation failure.
        }
        owner.diagnostic = null; owner.showDiagnostic = false; owner.revealDiagnostic = false;
        BeginDebugRun(evaluation,owner.name,preview);
        if(evaluation.debugOnly)
        {
            foreach(var step in evaluation.steps)if(step.debugOutput)AppendDebug(step.text);
            if(evaluation.error!=null)AppendDebug(evaluation.error);
            return;
        }
        resultOwner = owner.name;
        reportError = evaluation.error != null;
        showSalesPanel = true;
        report = "";
        if (evaluation.skipped) { report = "조건에 맞지 않아 제작하지 않았습니다. 비용 0G"; return; }
        if (preview)
        {
            foreach (var step in evaluation.steps)
                if(step.debugOutput)AppendDebug(step.text);else report += step.line + "줄: " + step.text + "\n";
            report += evaluation.product != null
                ? "\n시험 견적\n" + ForgePricing.Describe(evaluation.product, true)
                : "예상 제작비 " + evaluation.cost + "G · 소비 예정 재료 · 철 " + evaluation.ironConsumed + "개\n" + evaluation.error;
            return;
        }
        if (evaluation.error != null && evaluation.steps.Count == 0) { report = evaluation.error; return; }
        StartCoroutine(Craft(evaluation, owner));
    }
    IEnumerator Craft(Evaluation evaluation, CodeWindow codeOwner)
    {
        busy = true;
        string owner = resultOwner;
        var ledger = new ForgeProductionLedger();
        ForgeProduct completed = null;
        bool seedConsumed = false;
        foreach (var step in evaluation.steps)
        {
            if(step.debugOutput){AppendDebug(step.text);continue;}
            if (!ledger.TrySpend(step.cost, step.iron, ref gold, ref iron))
            {
                reportError = true;
                report += step.line + "줄: 자금 또는 철 부족\n실제 제작비 " + ledger.CashCost + "G · 소비 재료 · 철 " + ledger.IronConsumed + "개";
                busy = false; yield break;
            }
            if (!seedConsumed && step.process > 0) { codeOwner.seed = unchecked(codeOwner.seed * 1664525u + 1013904223u); seedConsumed = true; }
            report += step.line + "줄: " + step.text + "\n";
            if (step.process > 0 || step.finished)
            {
                work.transform.localScale = step.shaped ? new Vector3(.25f, .15f, 2) : new Vector3(1, .4f, .5f);
                work.GetComponent<Renderer>().material.color = step.heat > 0 ? Color.red : Color.gray;
            }
            if (step.finished)
            {
                completed = ledger.Complete(owner, step.heat, step.shaped, step.traits);
                selectedProductId = shop.Add(completed);
            }
            yield return new WaitForSeconds(.65f);
        }
        reportError = evaluation.error != null;
        report += completed != null ? "\n제작 완료\n" + ForgePricing.Describe(completed, false)
            : evaluation.error + "\n실제 제작비 " + ledger.CashCost + "G · 소비 재료 · 철 " + ledger.IronConsumed + "개";
        busy = false;
    }
    bool SellProduct(int id)
    {
        if (busy || !shop.TrySell(id, ref gold, out ForgeProduct sold)) return false;
        selectedProductId = -1; // Never silently sell the next item on a second click.
        reportError = false;
        resultOwner = sold.BlueprintName;
        report = "판매 영수증 · #" + id + "\n" + ForgePricing.Describe(sold, false)
            + "\n지급액 +" + ForgePricing.Quote(sold).SalePrice + "G (제작비는 제작 때 지불됨)";
        return true;
    }
    readonly ForgeDebugOutput debugOutput=new ForgeDebugOutput();
    Rect debugWindow=new Rect(30,320,480,240);
    Vector2 debugScroll,debugResizeMouse;
    Rect debugResizeStart;
    bool showDebugWindow,debugFollow,debugInteracting;
    int debugResizeEdge=-1;
    GUIStyle debugTextStyle;
    void BeginDebugRun(Evaluation evaluation,string owner,bool preview)
    {
        if(!evaluation.debugOnly&&!evaluation.steps.Exists(step=>step.debugOutput))return;
        AppendDebug("── "+owner+" · "+(preview?"시험 실행":"실제 실행")+" ──");
    }
    void AppendDebug(string message){debugOutput.Append(message);showDebugWindow=true;debugFollow=true;}
    void DrawDebugWindow()
    {
        if(!showDebugWindow)return;
        if(debugTextStyle==null)debugTextStyle=new GUIStyle(codeStyle){wordWrap=true,richText=false,padding=new RectOffset(4,4,4,4),fontSize=16};
        Rect before=debugWindow;
        Rect drawn=GUI.Window(197601,debugWindow,DrawDebugContents,"디버그 출력");
        if(debugWindow==before)debugWindow=drawn; // Preserve size changes made by the callback.
        if(Event.current.rawType==EventType.MouseUp)debugInteracting=false;
    }
    void DrawDebugContents(int id)
    {
        var ev=Event.current;
        if(ev.type==EventType.MouseDown)debugInteracting=true;
        int resizeId=GUIUtility.GetControlID("DebugResize".GetHashCode(),FocusType.Passive);
        int edge=-1;
        if(ev.mousePosition.x<6&&ev.mousePosition.y>28)edge=0;
        else if(ev.mousePosition.x>debugWindow.width-6&&ev.mousePosition.y>28)edge=1;
        else if(ev.mousePosition.y>debugWindow.height-6)edge=2;
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
        if(GUI.Button(new Rect(debugWindow.width-90,3,52,23),"지우기")){debugOutput.Clear();debugScroll=Vector2.zero;}
        if(GUI.Button(new Rect(debugWindow.width-32,3,26,23),"×")){showDebugWindow=false;debugInteracting=false;}
        GUI.Label(new Rect(10,29,debugWindow.width-20,22),"print 출력 · 읽기 전용");
        Rect view=new Rect(10,54,debugWindow.width-20,debugWindow.height-65);
        string text=debugOutput.Text;
        float height=Mathf.Max(view.height,debugTextStyle.CalcHeight(new GUIContent(text+"\n "),Mathf.Max(1,view.width-18)));
        if(debugFollow){debugScroll.y=Mathf.Max(0,height-view.height);if(ev.type==EventType.Repaint)debugFollow=false;}
        debugScroll=GUI.BeginScrollView(view,debugScroll,new Rect(0,0,view.width-18,height));
        GUI.Label(new Rect(0,0,view.width-18,height),text,debugTextStyle);
        GUI.EndScrollView();
        GUI.DragWindow(new Rect(6,0,Mathf.Max(0,debugWindow.width-100),27));
    }
    void OnGUI()
    {
        InitializeCodeStyle();
        if (!windowsInitialized) { windowsInitialized = true; nextWindowId = 1; codeWindows.Add(new CodeWindow { rect = workspaceWindow, id = 0, name = "f1" }); }
        EventType modalEvent = Event.current.type;
        bool modalOpen = deleteWindowId >= 0;
        if (modalOpen && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint) Event.current.type = EventType.Ignore;
        var ev = Event.current;
        EventType debugUnderlyingEvent=ev.type;
        bool maskDebug=showDebugWindow&&(debugInteracting||debugWindow.Contains(ev.mousePosition))&&(ev.isMouse||ev.type==EventType.ScrollWheel);
        if(maskDebug)ev.type=EventType.Ignore;
        CodeWindow diagnosticOwner = codeWindows.Count > 0 ? codeWindows[activeWindow] : null;
        EventType diagnosticEvent = ev.type;
        bool maskDiagnostic = diagnosticOwner != null && diagnosticOwner.showDiagnostic && diagnosticOwner.diagnostic != null
            && DiagnosticRect().Contains(ev.mousePosition) && (ev.isMouse || ev.type == EventType.ScrollWheel);
        if (maskDiagnostic) ev.type = EventType.Ignore;
        Vector2 mouse = ev.mousePosition / workspaceZoom;
        workspaceMouse = mouse;
        dragControl = GUIUtility.GetControlID("WorkspaceDrag".GetHashCode(), FocusType.Passive);
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
        if (ev.type == EventType.MouseDown && (ev.button == 0 || ev.button == 1) && hit >= 0 && ev.mousePosition.y > 40)
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
        if (hit >= 0 && !overSales && ev.mousePosition.y > 40 && !panning && movingWindow < 0 && !movingSales)
        {
            Rect hovered = codeWindows[hit].rect; hovered.position += workspaceOffset;
            horizontalEdge |= mouse.y >= hovered.y + 4 && mouse.y <= hovered.yMax - 4
                && (Mathf.Abs(mouse.x - hovered.x) <= 4 || Mathf.Abs(mouse.x - hovered.xMax) <= 4);
        }
        SetHorizontalResizeCursor(horizontalEdge);
        if (renamingWindow >= 0 && ev.type == EventType.MouseDown)
        {
            Rect renameRect = codeWindows[renamingWindow].rect; renameRect.position += workspaceOffset;
            if (!NameRect(renameRect).Contains(mouse)) CommitName();
        }
        if (ev.type == EventType.ScrollWheel && !overWindow && ev.mousePosition.y > 40 && dragButton < 0)
        {
            Vector2 anchor = mouse - workspaceOffset;
            workspaceZoom = Mathf.Clamp(workspaceZoom * Mathf.Pow(1.05f, -ev.delta.y), .4f, 2f);
            workspaceOffset = ev.mousePosition / workspaceZoom - anchor;
            hover = ""; hoverSince = Time.realtimeSinceStartup;
            UpdateWorkspaceCamera(); ev.Use();
        }
        if (ev.type == EventType.MouseDown && (ev.button == 0 || ev.button == 1)
            && ev.mousePosition.y > 40 && GUIUtility.hotControl == 0)
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
                Rect testButton = new Rect(r.x + 40, r.y + 5, 28, 24);
                Rect runButton = new Rect(r.x + 8, r.y + 5, 28, 24);
                if (title.Contains(mouse) && !testButton.Contains(mouse) && !runButton.Contains(mouse) && mouse.x < r.xMax - 68
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
        GUI.Box(new Rect(0, 0, Screen.width, 40), "");
        GUI.Label(new Rect(12, 8, Mathf.Max(100, Screen.width - 330), 28), "자금 " + gold + "G · 철 " + iron + " · 완성품 " + products + " · 확대 " + Mathf.RoundToInt(workspaceZoom * 100) + "%");
        if (GUI.Button(new Rect(Screen.width - 440, 6, 120, 28), showSalesPanel ? "판매 창 숨기기" : "판매 창 열기")) showSalesPanel = !showSalesPanel;
        if (GUI.Button(new Rect(Screen.width - 310, 6, 150, 28), "+ 코드 창 생성"))
        {
            CommitName();
            int id = nextWindowId++;
            codeWindows.Add(new CodeWindow { id = id, name = "f" + (id + 1), rect = new Rect(new Vector2(70, 85) / workspaceZoom - workspaceOffset, new Vector2(360, 240)) });
            activeWindow = codeWindows.Count - 1; edge = -1; movingWindow = -1; GUI.FocusControl(null);
        }
        if (GUI.Button(new Rect(Screen.width - 150, 6, 140, 28), "작업 공간 원위치")) ResetWorkspaceView();
        if(GUI.Button(new Rect(Screen.width-540,6,90,28),"디버그 창"))showDebugWindow=!showDebugWindow;
        if(maskDebug)ev.type=debugUnderlyingEvent;
        DrawDebugWindow();
        if (maskDiagnostic) ev.type = diagnosticEvent;
        DrawCompileErrorWindow(diagnosticOwner);
        if (modalOpen) { Event.current.type = modalEvent; DrawDeleteDialog(); }
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
        GUI.Label(new Rect(panel.x + 10, panel.y + 7, panel.width - 48, 24), "컴파일 오류 · " + owner.name);
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
        if(GUI.Button(new Rect(window.x+40,window.y+5,28,24),new GUIContent("▷"), actionButton)) Execute(true);
        if(GUI.Button(new Rect(window.x+8,window.y+5,28,24),new GUIContent("▶"), actionButton)) Execute(false);
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
        source=GUI.TextArea(content,source,inputStyle);
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
        if(over>=2 || edge>=2) GUI.Box(new Rect(mouse.x+12,mouse.y+12,40,24),(edge>=0?edge:over)<2?"↔":"↕");
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
        GUI.enabled = selected != null && !busy;
        if (GUILayout.Button(selected == null ? "판매할 완성품을 선택하세요" : "선택 완성품 판매 · +" + ForgePricing.Quote(selected.Product).SalePrice + "G", GUILayout.Height(30)))
            SellProduct(selected.Id);
        GUI.enabled = true;
        GUILayout.EndArea();
        if (salesResizeEdge == 2)
            GUI.Box(new Rect(workspaceMouse.x + 12, workspaceMouse.y + 12, 40, 24), "↕");
        if (panel.Contains(workspaceMouse) && (Event.current.isMouse || Event.current.type == EventType.ScrollWheel)) Event.current.Use();
    }
}
