using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// Temporary single-scene frontend; the evaluator owns no Unity or inventory state.
public class ForgeMvp : MonoBehaviour
{
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
        public bool compileError;
        public Vector2 scroll;
        public List<EditSnapshot> undo = new List<EditSnapshot>();
        public int id;
    }
    readonly List<CodeWindow> codeWindows = new List<CodeWindow>();
    int activeWindow, movingWindow = -1;
    int nextWindowId;
    int deleteWindowId = -1;
    bool windowsInitialized;
    int dragButton = -1, dragControl;
    Vector2 dragStartMouse, dragStartPosition;
    bool showSalesPanel = true;
    Rect salesWindow = new Rect(430, 55, 310, 360);
    bool movingSales;
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
        movingWindow = -1; movingSales = false; panning = false; dragButton = -1;
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
    int edge = -1, gold = 100, iron = 10, products;
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
    void OnDisable() { SetHorizontalResizeCursor(false); }
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
    public class Piece { public int heat; public bool shaped; }
    public class Step { public int line, cost, iron, heat; public bool shaped, finished; public string text; }
    public class Evaluation { public List<Step> steps = new List<Step>(); public string error; public int cost; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleScene" && FindAnyObjectByType<ForgeMvp>() == null) new GameObject("Forge MVP").AddComponent<ForgeMvp>(); }
    readonly Queue<int> productCosts = new Queue<int>();
    readonly Queue<string> productOwners = new Queue<string>();
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
    static object Value(string token, Dictionary<string, object> vars)
    {
        token = token.Trim();
        if (Regex.IsMatch(token, @"^[0-9]+$")) { if (int.TryParse(token, out int n)) return n; throw new Exception("정수 범위 초과"); }
        token = PythonIdentifier.Parse(token);
        if (!vars.TryGetValue(token, out object value)) throw new Exception("미선언 변수 " + token + " · Blade = 1(1)처럼 먼저 선언하세요.");
        return value;
    }
    // Structural/type validation runs over the entire source before simulation.
    public static Evaluation Evaluate(string source)
    {
        var result = new Evaluation();
        var types = new Dictionary<string, object>();
        string[] lines = source.Replace("\r", "").Split('\n');
        for (int pass = 0; pass < 2; pass++)
        {
            var vars = pass == 0 ? types : new Dictionary<string, object>();
            bool ended = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int comment = line.IndexOf('#');
                string s = (comment >= 0 ? line.Substring(0, comment) : line).Trim();
                if (s.Length == 0) continue;
                try
                {
                    if (ended) throw new Exception("완성품 반환 뒤에는 명령을 쓸 수 없습니다.");
                    var step = new Step { line = i + 1 };
                    if (s.StartsWith("!"))
                    {
                        if (!(Value(s.Substring(1), vars) is Piece p)) throw new Exception("완성품에는 작업물 변수가 필요합니다.");
                        if (pass == 1 && (!p.shaped || p.heat != 0)) throw new Exception("완성품 검사 실패: 타격과 공기 냉각이 필요합니다.");
                        step.finished = true; step.shaped = p.shaped; step.text = "철 검날 완성 · 판매가 28G"; ended = true;
                    }
                    else
                    {
                        string name = null;
                        int eq = s.IndexOf('=');
                        if (eq >= 0) { name = PythonIdentifier.ParseAssignment(s.Substring(0, eq).Trim()); s = s.Substring(eq + 1).Trim(); }
                        var call = Regex.Match(s, @"^([0-9]+)\((.*)\)$");
                        if (!call.Success) { if (name == null) throw new Exception("대입 또는 함수 호출이 필요합니다."); vars[name] = Value(s, vars); step.text = name + " 저장"; }
                        else
                        {
                            if (!int.TryParse(call.Groups[1].Value, out int f) || f < 1 || f > 4) throw new Exception("지원 함수는 1~4입니다.");
                            string[] args = call.Groups[2].Value.Split(',');
                            int count = f == 1 || f == 3 ? 1 : 2;
                            if (args.Length != count) throw new Exception("필요 인자 수: " + count);
                            object a = Value(args[0], vars);
                            object b = count == 2 ? Value(args[1], vars) : null;
                            if (f == 1)
                            {
                                if (!(a is int material) || material != 1) throw new Exception("MVP 작업대 재료는 철(1)입니다.");
                                if (name == null) throw new Exception("작업물 참조를 변수에 저장하세요.");
                                vars[name] = new Piece(); step.cost = 10; step.iron = 1; step.text = "철 올리기 · 10G";
                            }
                            else
                            {
                                if (name != null) throw new Exception("함수 2~4는 반환값이 없습니다.");
                                if (!(a is Piece p)) throw new Exception("첫 인자는 작업물이어야 합니다.");
                                if (count == 2 && !(b is int)) throw new Exception("두 번째 인자는 정수여야 합니다.");
                                if (f == 2) { int heat = (int)b; if (heat < 1 || heat > 9) throw new Exception("열 단계 범위: 1~9"); p.heat = heat; step.cost = 2; step.text = "가열 · 연료 2G"; }
                                if (f == 3) { if (pass == 1 && p.heat < 6) throw new Exception("실행 오류: 타격 전에 2(작업물, 6 이상)으로 가열해야 합니다."); p.shaped = true; step.text = "타격 · 검날 성형"; }
                                if (f == 4) { if ((int)b != 0) throw new Exception("MVP 냉각 재료는 공기(0)입니다."); p.heat = 0; step.text = "공기 냉각 · 0G"; }
                                step.heat = p.heat; step.shaped = p.shaped;
                            }
                        }
                    }
                    if (pass == 1) { result.steps.Add(step); result.cost += step.cost; }
                }
                catch (Exception ex) { result.error = (pass == 0 ? "컴파일 오류" : "공정 오류") + " · " + (i + 1) + "줄: " + ex.Message; return result; }
            }
            if (!ended) { result.error = "완성품 반환 !Blade가 필요합니다."; return result; }
        }
        return result;
    }
    void Execute(bool preview)
    {
        if (busy) return;
        resultOwner = codeWindows[activeWindow].name;
        var evaluation = Evaluate(source);
        codeWindows[activeWindow].compileError = evaluation.error != null && evaluation.steps.Count == 0;
        reportError = evaluation.error != null;
        if (reportError) showSalesPanel = true;
        report = "";
        if (preview) { foreach (var s in evaluation.steps) report += s.line + "줄: " + s.text + "\n"; report += "예상 비용 " + evaluation.cost + "G\n" + evaluation.error; return; }
        if (evaluation.error != null && evaluation.steps.Count == 0) { report = evaluation.error; return; }
        StartCoroutine(Craft(evaluation));
    }
    IEnumerator Craft(Evaluation e)
    {
        busy = true;
        foreach (var s in e.steps)
        {
            if (gold < s.cost || iron < s.iron) { reportError = true; report += s.line + "줄: 자금 또는 철 부족"; busy = false; yield break; }
            gold -= s.cost; iron -= s.iron;
            report += s.line + "줄: " + s.text + "\n";
            work.transform.localScale = s.shaped ? new Vector3(.25f, .15f, 2) : new Vector3(1, .4f, .5f);
            work.GetComponent<Renderer>().material.color = s.heat > 0 ? Color.red : Color.gray;
            if (s.finished) { products++; productCosts.Enqueue(e.cost); productOwners.Enqueue(resultOwner); }
            yield return new WaitForSeconds(.65f);
        }
        report += e.error ?? ("완성품: 철 검날 · 제작비 " + e.cost + "G · 판매가 28G · 이익 " + (28-e.cost) + "G");
        busy = false;
    }
    void OnGUI()
    {
        InitializeCodeStyle();
        if (!windowsInitialized) { windowsInitialized = true; nextWindowId = 1; codeWindows.Add(new CodeWindow { rect = workspaceWindow, id = 0, name = "f1" }); }
        EventType modalEvent = Event.current.type;
        bool modalOpen = deleteWindowId >= 0;
        if (modalOpen && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint) Event.current.type = EventType.Ignore;
        var ev = Event.current;
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
        bool overSales = showSalesPanel && salesScreen.Contains(mouse);
        if (overSales) { overWindow = true; hit = -1; }
        bool horizontalEdge = edge == 0 || edge == 1;
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
        if (ev.type == EventType.ScrollWheel && !overWindow && ev.mousePosition.y > 40)
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
            if (overSales && new Rect(salesScreen.x + 8, salesScreen.y + 4, salesScreen.width - 50, 30).Contains(mouse))
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
            if (movingWindow >= 0 || movingSales || panning)
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
                if (movingWindow >= 0 && draggedTitle) codeWindows[movingWindow].rect.position = position;
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
            Vector2 previewSize = codeStyle.CalcSize(new GUIContent(codeWindows[i].text + "\n "));
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
        if (modalOpen) { Event.current.type = modalEvent; DrawDeleteDialog(); }
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
        if (codeWindow.compileError)
        {
            GUI.color = new Color(1f, .25f, .25f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2), Texture2D.whiteTexture);
            GUI.color = previous;
        }
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
        foreach (var token in owner.coloredTokens)
        {
            Color color = Color.white;
            switch (token.kind)
            {
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
        Vector2 size=codeStyle.CalcSize(new GUIContent(source+"\n "));
        Rect content=new Rect(0,0,Mathf.Max(viewport.width,size.x),Mathf.Max(viewport.height,size.y));
        Vector2 oldScroll=scroll;
        scroll=GUI.BeginScrollView(viewport,scroll,content, false, false, GUIStyle.none, GUIStyle.none);
        string old=source;
        bool focused = GUI.GetNameOfFocusedControl() == controlName;
        var editor = focused ? (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) : null;
        var before = new EditSnapshot { text = source, cursor = editor != null ? editor.cursorIndex : 0,
            selection = editor != null ? editor.selectIndex : 0, scroll = scroll };
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
        GUI.SetNextControlName(controlName);
        // Keep native selection/caret and plain-text editing; only the glyph layer is colored.
        source=GUI.TextArea(content,source,inputStyle);
        DrawSyntax(content, source, codeWindows[activeWindow]);
        if (!undoKey && source != old)
        {
            undoHistory.Add(before);
            if (undoHistory.Count > 256) undoHistory.RemoveAt(0);
        }
        GUI.EndScrollView();
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
            foreach(Match m in Regex.Matches(source,@"\b[0-9]+\b"))
            {
                if(index<m.Index || index>m.Index+m.Length) continue;
                Vector2 start=codeStyle.GetCursorPixelPosition(content,new GUIContent(source),m.Index);
                Vector2 end=codeStyle.GetCursorPixelPosition(content,new GUIContent(source),m.Index+m.Length);
                if(new Rect(start.x,start.y,end.x-start.x,codeStyle.lineHeight).Contains(local)) hover=Describe(source,m);
            }
        }
        else hover="";
        if(over>=2 || edge>=2) GUI.Box(new Rect(mouse.x+12,mouse.y+12,40,24),(edge>=0?edge:over)<2?"↔":"↕");
        if(hover.Length>0) GUI.Box(new Rect(Mathf.Min(mouse.x+16,Screen.width-300),Mathf.Min(mouse.y+24,Screen.height-50),300,44),hover);
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
        GUI.Label(new Rect(panel.x + 10, panel.y + 6, panel.width - 50, 26), "공용 결과 / 판매");
        if (GUI.Button(new Rect(panel.xMax - 34, panel.y + 6, 24, 24), "×")) showSalesPanel = false;
        GUILayout.BeginArea(new Rect(panel.x + 10, panel.y + 38, panel.width - 20, panel.height - 48));
        GUILayout.Label("실행 도면: " + resultOwner + (busy ? " · 제작 중" : ""));
        logs=GUILayout.BeginScrollView(logs); GUILayout.Label(report, reportError ? errorLabel : GUI.skin.label); GUILayout.EndScrollView();
        GUI.enabled=products>0&&!busy;
        if(GUILayout.Button("철 검날 판매 · +28G"))
        {
            if (!busy && productCosts.Count > 0)
            {
                reportError = false;
                int cost = productCosts.Dequeue(); resultOwner = productOwners.Dequeue(); products--; gold+=28;
                report="판매 영수증 · 철 검날\n제작비 " + cost + "G · 판매가 28G · 순이익 " + (28-cost) + "G";
            }
        }
        GUI.enabled=true; GUILayout.EndArea();
        if (panel.Contains(workspaceMouse) && (Event.current.isMouse || Event.current.type == EventType.ScrollWheel)) Event.current.Use();
    }
    static string Describe(string text, Match token)
    {
        int lineStart=text.LastIndexOf('\n',Mathf.Max(0,token.Index-1))+1;
        string prefix=text.Substring(lineStart,token.Index-lineStart);
        if (prefix.IndexOf('#') >= 0) return "";
        int lineEnd = text.IndexOf('\n', token.Index);
        if (lineEnd < 0) lineEnd = text.Length;
        string after=text.Substring(token.Index+token.Length, lineEnd-token.Index-token.Length).TrimStart();
        string[] names={"","재료 올리기","가열 함수","타격 함수","냉각 함수"};
        if(after.StartsWith("(")) return int.TryParse(token.Value,out int f)&&f>=1&&f<=4?names[f]:"미지원 함수 "+token.Value;
        var call=Regex.Match(prefix,@"([1-4])\(([^()]*)$");
        if(call.Success)
        {
            int arg=call.Groups[2].Value.Split(',').Length;
            string f=call.Groups[1].Value;
            if(f=="2"&&arg==2) return "목표 열 단계 "+token.Value;
            if(f=="4"&&arg==2) return token.Value=="0"?"냉각 재료 · 공기 · 비용 0G":"냉각 재료 · MVP 미지원";
            if(f=="1"&&arg==1) return token.Value=="1"?"재료 · 철 · 1개 + 10G":"재료 · MVP 미지원";
            return "함수 "+f+" · "+arg+"번째 인자 · "+token.Value;
        }
        return "정수 값 "+token.Value;
    }
}
