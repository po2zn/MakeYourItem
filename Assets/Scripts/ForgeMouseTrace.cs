using System;
using System.IO;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

// Diagnostic only: never consumes an event, assigns hotControl, or installs OS hooks.
public sealed class ForgeMouseTrace : IDisposable
{
    [Serializable] sealed class Record
    {
        public long seq;public string utc,kind,detail;public int frame;public double seconds;
    }
    public static ForgeMouseTrace Current {get;private set;}
    StreamWriter writer;string folder,session;int part;long sequence,bytes;double flushAt;
    const long PartLimit=32L*1024*1024;
    public string PathName {get;private set;}
    public bool Active=>writer!=null;
    public static bool Enabled=>Current!=null&&Current.Active;
    public ForgeMouseTrace()
    {
        Current?.Dispose();Current=this;
        session=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6);
        try{
            folder=Application.isEditor?Path.Combine(Path.GetDirectoryName(Application.dataPath),"Logs","InputTrace"):Path.Combine(Application.persistentDataPath,"InputTrace");
            OpenPart();
#if ENABLE_INPUT_SYSTEM
            InputSystem.onEvent+=RawMouseEvent;
            InputSystem.onDeviceChange+=DeviceChanged;
#endif
            Write("SESSION_START","Unity="+Application.unityVersion+" resolution="+Screen.width+"x"+Screen.height+" screenOrigin=top-left; rawInputOrigin=bottom-left; F9 toggle; F10 marker",true);
            Debug.Log("[MouseTrace] 기록 시작: "+PathName+" | F10: 재현 표시 / F9: 기록 중지");
        }catch(Exception error){Fail(error);}
    }
    void OpenPart()
    {
        Directory.CreateDirectory(folder);
        PathName=Path.Combine(folder,"mouse-"+session+"-part"+(++part).ToString("000")+".jsonl");
        writer=new StreamWriter(new FileStream(PathName,FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(false),65536);
        bytes=0;
    }
    public static void Mark(string stage,string detail="",bool flush=false){if(Enabled)Current.Write(stage,detail,flush);}
    public void Write(string kind,string detail,bool flush=false)
    {
        if(writer==null)return;
        try{
            if(bytes>=PartLimit){writer.Flush();writer.Dispose();writer=null;OpenPart();}
            string line=JsonUtility.ToJson(new Record{seq=++sequence,utc=DateTime.UtcNow.ToString("O"),frame=Time.frameCount,seconds=Time.realtimeSinceStartupAsDouble,kind=kind,detail=detail});
            writer.WriteLine(line);bytes+=Encoding.UTF8.GetByteCount(line)+2;
            if(flush||Time.realtimeSinceStartupAsDouble>=flushAt){writer.Flush();flushAt=Time.realtimeSinceStartupAsDouble+.5;}
        }catch(Exception error){Fail(error);}
    }
    public void Tick(){if(writer!=null&&Time.realtimeSinceStartupAsDouble>=flushAt){try{writer.Flush();flushAt=Time.realtimeSinceStartupAsDouble+.5;}catch(Exception error){Fail(error);}}}
    void Fail(Exception error)
    {
        Unsubscribe();try{writer?.Dispose();}catch{}writer=null;
        Debug.LogWarning("[MouseTrace] 로그 기록 중지 (게임 입력은 유지): "+error.Message);
    }
    void Unsubscribe()
    {
#if ENABLE_INPUT_SYSTEM
        InputSystem.onEvent-=RawMouseEvent;InputSystem.onDeviceChange-=DeviceChanged;
#endif
    }
    public void Dispose()
    {
        Write("SESSION_END","recording stopped",true);Unsubscribe();
        try{writer?.Dispose();}catch{}writer=null;if(Current==this)Current=null;
    }
#if ENABLE_INPUT_SYSTEM
    static string Value<T>(UnityEngine.InputSystem.InputControl<T> control,InputEventPtr evt) where T:struct
    {return control.ReadValueFromEvent(evt,out T value)?value.ToString():"unchanged";}
    void RawMouseEvent(InputEventPtr evt,InputDevice device)
    {
        if(!(device is Mouse mouse))return;
        if(!evt.IsA<StateEvent>()&&!evt.IsA<DeltaStateEvent>()){Write("RAW_MOUSE_OTHER","device="+device.deviceId+" type="+evt.type+" event="+evt.id);return;}
        try{Write("RAW_MOUSE","device="+device.deviceId+" event="+evt.id+" inputTime="+evt.time.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)+" bytes="+evt.sizeInBytes+
            " position="+Value(mouse.position,evt)+" delta="+Value(mouse.delta,evt)+" wheel="+Value(mouse.scroll,evt)+
            " left="+Value(mouse.leftButton,evt)+" right="+Value(mouse.rightButton,evt)+" middle="+Value(mouse.middleButton,evt)+
            " back="+Value(mouse.backButton,evt)+" forward="+Value(mouse.forwardButton,evt)+" clicks="+Value(mouse.clickCount,evt));}catch(Exception error){Fail(error);}
    }
    void DeviceChanged(InputDevice device,InputDeviceChange change){if(device is Mouse)Write("MOUSE_DEVICE","device="+device.deviceId+" change="+change,true);}
#endif
}

public partial class ForgeMvp
{
    [SerializeField,Tooltip("마우스 진단 로그 자동 기록. F9 기록 전환, F10 재현 표시.")]
    bool traceMouseInput=true;
    ForgeMouseTrace mouseTrace;
    double nextMouseHeartbeat;
    bool tracingPointer;long pointerPass;int traceHotBefore;string traceStateBefore,lastTraceState;
    void OnEnable(){if(traceMouseInput)StartMouseTrace();}
    void StartMouseTrace(){mouseTrace?.Dispose();mouseTrace=new ForgeMouseTrace();}
    void StopMouseTrace(){mouseTrace?.Dispose();mouseTrace=null;}
    void Update()
    {
        bool toggle=false,marker=false;
#if ENABLE_INPUT_SYSTEM
        var keyboard=Keyboard.current;
        toggle=keyboard!=null&&keyboard.f9Key.wasPressedThisFrame;
        marker=keyboard!=null&&keyboard.f10Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        toggle=Input.GetKeyDown(KeyCode.F9);marker=Input.GetKeyDown(KeyCode.F10);
#endif
        if(toggle){if(mouseTrace!=null&&mouseTrace.Active){StopMouseTrace();Debug.Log("[MouseTrace] 기록 중지");}else StartMouseTrace();}
        if(marker){if(mouseTrace==null||!mouseTrace.Active)StartMouseTrace();ForgeMouseTrace.Mark("USER_MARKER","F10: 사용자 재현 시점",true);Debug.Log("[MouseTrace] F10 재현 표시 저장: "+mouseTrace?.PathName);}
        mouseTrace?.Tick();
        if(ForgeMouseTrace.Enabled&&Time.realtimeSinceStartupAsDouble>=nextMouseHeartbeat){
            nextMouseHeartbeat=Time.realtimeSinceStartupAsDouble+1;
            string sample="focused="+Application.isFocused+" resolution="+Screen.width+"x"+Screen.height;
#if ENABLE_INPUT_SYSTEM
            var mouse=Mouse.current;
            if(mouse!=null)sample+=" position="+mouse.position.ReadValue()+" left="+mouse.leftButton.isPressed+" right="+mouse.rightButton.isPressed+" middle="+mouse.middleButton.isPressed+" back="+mouse.backButton.isPressed+" forward="+mouse.forwardButton.isPressed;
#endif
            ForgeMouseTrace.Mark("HEARTBEAT",sample);
        }
    }
    string MouseTraceState()
    {
        var sb=new StringBuilder();
        sb.Append("hot=").Append(GUIUtility.hotControl).Append(" keyboard=").Append(GUIUtility.keyboardControl).Append(" focus=").Append(GUI.GetNameOfFocusedControl());
        sb.Append(" dragControl=").Append(dragControl).Append(" dragButton=").Append(dragButton).Append(" movingWindow=").Append(movingWindow).Append(" panning=").Append(panning).Append(" draggedTitle=").Append(draggedTitle);
        sb.Append(" codePointer=").Append(codePointerControl).Append(" nativePointer=").Append(codeWindowPointerControl).Append(" resizeEdge=").Append(edge).Append(" renaming=").Append(renamingWindow);
        sb.Append(" salesMove=").Append(movingSales).Append(" salesResize=").Append(salesResizeEdge).Append(" debugDrag=").Append(debugInteracting).Append(" debugResize=").Append(debugResizeEdge);
        sb.Append(" guideDrag=").Append(guideBook.Dragging).Append(" active=").Append(activeWindow).Append(" resetPending=").Append(pointerResetPending);
        sb.Append(" zoom=").Append(workspaceZoom).Append(" offset=").Append(workspaceOffset).Append(" resolution=").Append(Screen.width).Append('x').Append(Screen.height).Append(" appFocus=").Append(Application.isFocused);
        sb.Append(" screens=title:").Append(showTitle).Append(",menu:").Append(showGameMenu).Append(",tutorial:").Append(showTutorial).Append(",progress:").Append(showProgress).Append(",tech:").Append(showTechTree).Append(",smith:").Append(showSmithTree).Append(",guide:").Append(showGuideBook).Append(",sales:").Append(showSalesPanel).Append(",debug:").Append(showDebugWindow).Append(" modal=").Append(deleteWindowId);
        for(int i=0;i<codeWindows.Count;i++){var w=codeWindows[i];sb.Append(" | code[").Append(i).Append("] id=").Append(w.id).Append(" rect=").Append(w.rect).Append(" scroll=").Append(w.scroll).Append(" collapsed=").Append(w.collapsed);}
        sb.Append(" | retainedTree=").Append(hierarchyTree!=null&&hierarchyTree.gameObject.activeInHierarchy);
        sb.Append(" | salesRect=").Append(salesWindow).Append(" guideRect=").Append(guideBook.Rect).Append(" debugRect=").Append(debugWindow);
        return sb.ToString();
    }
    void BeginMouseTraceGUI()
    {
        if(!ForgeMouseTrace.Enabled)return;
        var ev=Event.current;
        tracingPointer=ev.isMouse||ev.rawType==EventType.MouseDown||ev.rawType==EventType.MouseUp||ev.rawType==EventType.MouseDrag||ev.rawType==EventType.ScrollWheel||ev.rawType==EventType.MouseEnterWindow||ev.rawType==EventType.MouseLeaveWindow;
        traceStateBefore=MouseTraceState();traceHotBefore=GUIUtility.hotControl;
        if(tracingPointer){pointerPass++;ForgeMouseTrace.Mark("GUI_BEGIN","pass="+pointerPass+" type="+ev.type+" raw="+ev.rawType+" button="+ev.button+" clicks="+ev.clickCount+" position="+ev.mousePosition+" delta="+ev.delta+" modifiers="+ev.modifiers+" | "+traceStateBefore);}
        else if(lastTraceState!=traceStateBefore)ForgeMouseTrace.Mark("STATE_CHANGE",traceStateBefore);
    }
    void EndMouseTraceGUI()
    {
        if(!ForgeMouseTrace.Enabled)return;
        var ev=Event.current;string after=MouseTraceState();
        if(tracingPointer||after!=traceStateBefore)ForgeMouseTrace.Mark("GUI_END","pass="+pointerPass+" type="+ev.type+" raw="+ev.rawType+" hotBefore="+traceHotBefore+" changed="+(after!=traceStateBefore)+" | "+after,ev.rawType==EventType.MouseUp);
        if(tracingPointer&&ev.rawType==EventType.MouseUp&&GUIUtility.hotControl!=0)ForgeMouseTrace.Mark("CAPTURE_REMAINS_AFTER_UP","pass="+pointerPass+" | "+after,true);
        lastTraceState=after;tracingPointer=false;
    }
    void TracePointer(string stage,string detail="")
    {
        if(!ForgeMouseTrace.Enabled||!tracingPointer)return;
        var ev=Event.current;
        ForgeMouseTrace.Mark(stage,"pass="+pointerPass+" type="+ev.type+" raw="+ev.rawType+" hot="+GUIUtility.hotControl+" keyboard="+GUIUtility.keyboardControl+" "+detail);
    }
}
