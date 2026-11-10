using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

// Captures completed frames, not the number of IMGUI function calls.
// Exclusion deltas are marginal costs: batching means they are not additive.
[InitializeOnLoad]
public static class ForgeRenderBenchmark
{
    sealed class Case { public string Name; public bool Tree,Optimized; public ForgeRenderLayer Hidden; }
    sealed class Sample { public int Frame; public long Draws,Batches,SetPass; public double Milliseconds; }
    static readonly List<Case> cases=new List<Case>();
    static readonly List<Sample> samples=new List<Sample>();
    static readonly List<Action> restore=new List<Action>();
    static ProfilerRecorder draws,batches,setPass;
    static ForgeMvp game;
    static bool useUnityStats;
    static StreamWriter raw,summary;
    static int phase,frames,lastFrame;
    static double lastTick;
    static string directory;
    public static bool Running {get;private set;}
    public static string Status {get;private set;}="Idle";
    const int Warmup=30,Count=90;
    static ForgeRenderBenchmark(){AssemblyReloadEvents.beforeAssemblyReload+=Cancel;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)Cancel();};}
    static FieldInfo Field(object obj,string name)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)??throw new MissingFieldException(name);
    static void SetSaved(object obj,string name,object value){var f=Field(obj,name);var old=f.GetValue(obj);restore.Add(()=>f.SetValue(obj,old));f.SetValue(obj,value);}
    static void Add(string name,bool tree,bool optimized,ForgeRenderLayer hidden=ForgeRenderLayer.None)=>cases.Add(new Case{Name=name,Tree=tree,Optimized=optimized,Hidden=hidden});
    [MenuItem("Tools/Forge/Rendering/Measure Draw Calls (Play Mode)")]
    public static void Start()
    {
        if(Running)throw new InvalidOperationException("Benchmark already running.");
        if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new InvalidOperationException("Enter unpaused Play Mode first.");
        game=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();
        if(!game)throw new InvalidOperationException("ForgeMvp not found.");
        if((bool)Field(game,"busy").GetValue(game)||(bool)Field(game,"tutorialAnimating").GetValue(game)||Field(game,"executionSession").GetValue(game)!=null)
            throw new InvalidOperationException("Stop crafting/tutorial animation before measuring.");
        try{
            draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);
            setPass=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count",1);
            useUnityStats=!draws.Valid||!batches.Valid||!setPass.Valid;
            bool optimized=ForgeRenderDiagnostics.Optimized;var hidden=ForgeRenderDiagnostics.Hidden;
            restore.Add(()=>{ForgeRenderDiagnostics.Optimized=optimized;ForgeRenderDiagnostics.Hidden=hidden;});
            foreach(string flag in new[]{"showTitle","showTutorial","showGameMenu","showProgress","showTechTree"})SetSaved(game,flag,false);
            SetSaved(game,"endingSeen",true);
            SetSaved(game,"showGuideBook",true);
            SetSaved(game,"workspaceZoom",1f);SetSaved(game,"workspaceOffset",Vector2.zero);
            if(game.DayCycle){bool paused=game.DayCycle.Paused;game.DayCycle.Paused=true;restore.Add(()=>{if(game&&game.DayCycle)game.DayCycle.Paused=paused;});}
            var tree=Field(game,"techTree").GetValue(game);
            SetSaved(tree,"zoom",.35f);SetSaved(tree,"offset",new Vector2(16,20));SetSaved(tree,"focusRoot",false);SetSaved(tree,"tab",0);
            // An unexecuted fixture makes long-code comparisons repeatable, even on a new save.
            var windows=(System.Collections.IList)Field(game,"codeWindows").GetValue(game);
            if(windows.Count==0)throw new InvalidOperationException("Open gameplay once before measuring, so a code window exists.");
            var text=new System.Text.StringBuilder();for(int i=0;i<200;i++)text.Append("Material = 1  # benchmark line ").Append(i).Append('\n');
            SetSaved(windows[0],"text",text.ToString());SetSaved(windows[0],"scroll",Vector2.zero);
            cases.Clear();
            foreach(bool treeMode in new[]{false,true}){
                string prefix=treeMode?"tree":"game";
                Add(prefix+"_before",treeMode,false);
                Add(prefix+"_after",treeMode,true);
                Add(prefix+"_without_world",treeMode,true,ForgeRenderLayer.World);
                // With no camera and no GUI repaint, UnityStats retains the last rendered frame.
                if(!treeMode||!useUnityStats)Add(prefix+"_without_all_ui",treeMode,true,ForgeRenderLayer.AllUI);
                foreach(var layer in treeMode?new[]{ForgeRenderLayer.TreeEdges,ForgeRenderLayer.TreeCards}:new[]{ForgeRenderLayer.Syntax,ForgeRenderLayer.Guide,ForgeRenderLayer.Developer})
                    Add(prefix+"_without_"+layer,treeMode,true,layer);
            }
            directory=Path.Combine("Docs","Performance",DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(directory);
            raw=new StreamWriter(Path.Combine(directory,"frames.csv"));raw.WriteLine("scenario,frame,draw_calls,batches,setpass,frame_interval_ms");
            summary=new StreamWriter(Path.Combine(directory,"summary.csv"));summary.WriteLine("scenario,samples,draw_calls_mean,draw_calls_min,draw_calls_max,batches_mean,setpass_mean,frame_interval_mean_ms,frame_interval_p95_ms");
            File.WriteAllText(Path.Combine(directory,"conditions.txt"),"Unity "+Application.unityVersion+"; "+SystemInfo.graphicsDeviceType+"; "+SystemInfo.graphicsDeviceName+"\nGame view: "+Screen.width+"x"+Screen.height+"\nEditor Play Mode. 200-line unexecuted code fixture; guide open; tree zoom 35%.\n30 warmup + 90 distinct frames per scenario. Counter source: "+(useUnityStats?"UnityEditor.UnityStats (Game View); batches unavailable":"ProfilerRecorder previous completed frame")+".\nFrame intervals include Editor scheduling/VSync; they are not isolated CPU/GPU times.\nWithout-layer deltas are marginal, not additive. before/after cover optimization toggle only, same build.\nDo not interact with the game during capture. Inspect frames.csv for variance.\n");
            Running=true;phase=0;ApplyCase();EditorApplication.update+=Tick;
        }catch(Exception ex){Finish("Failed: "+ex.Message);throw;}
    }
    static void ApplyCase()
    {
        var c=cases[phase];Field(game,"showTechTree").SetValue(game,c.Tree);
        ForgeRenderDiagnostics.Optimized=c.Optimized;ForgeRenderDiagnostics.Hidden=c.Hidden;
        samples.Clear();frames=0;lastFrame=Time.frameCount;lastTick=EditorApplication.timeSinceStartup;Status=c.Name;
    }
    static void Tick()
    {
        if(!Running)return;
        if(!game||!EditorApplication.isPlaying||EditorApplication.isPaused){Cancel();return;}
        if(Time.frameCount==lastFrame){if(EditorApplication.timeSinceStartup-lastTick>30)Finish("Failed: frames stopped advancing.");return;}
        double now=EditorApplication.timeSinceStartup;
        double ms=Time.unscaledDeltaTime*1000.0;lastTick=now;lastFrame=Time.frameCount;
        if(++frames<=Warmup)return;
        if(!useUnityStats&&(draws.Count==0||batches.Count==0||setPass.Count==0)){Finish("Failed: rendering counters have no samples.");return;}
        var sample=new Sample{Frame=lastFrame-1,Draws=useUnityStats?UnityStats.drawCalls:draws.LastValue,Batches=useUnityStats?-1:batches.LastValue,SetPass=useUnityStats?UnityStats.setPassCalls:setPass.LastValue,Milliseconds=ms};samples.Add(sample);
        raw.WriteLine(string.Join(",",Status,sample.Frame,sample.Draws,useUnityStats?"":sample.Batches.ToString(),sample.SetPass,ms.ToString("F3",CultureInfo.InvariantCulture)));
        if(samples.Count<Count)return;
        var times=samples.Select(x=>x.Milliseconds).OrderBy(x=>x).ToArray();
        summary.WriteLine(string.Join(",",Status,samples.Count,F(samples.Average(x=>x.Draws)),samples.Min(x=>x.Draws),samples.Max(x=>x.Draws),useUnityStats?"":F(samples.Average(x=>x.Batches)),F(samples.Average(x=>x.SetPass)),F(times.Average()),F(times[(int)Math.Ceiling(times.Length*.95)-1])));summary.Flush();raw.Flush();
        if(++phase<cases.Count)ApplyCase();else Finish("Completed: "+Path.GetFullPath(directory));
    }
    static string F(double n)=>n.ToString("F3",CultureInfo.InvariantCulture);
    [MenuItem("Tools/Forge/Rendering/Cancel Measurement")]
    public static void Cancel(){if(Running)Finish("Cancelled (partial capture): "+directory);}
    static void Finish(string status)
    {
        EditorApplication.update-=Tick;Running=false;Status=status;
        raw?.Dispose();summary?.Dispose();raw=null;summary=null;
        draws.Dispose();batches.Dispose();setPass.Dispose();
        for(int i=restore.Count-1;i>=0;i--)try{restore[i]();}catch(Exception e){Debug.LogException(e);}
        restore.Clear();Debug.Log("Forge rendering benchmark: "+status);
    }
}
