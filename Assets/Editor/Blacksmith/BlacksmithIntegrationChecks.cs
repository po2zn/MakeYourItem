using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public static class BlacksmithIntegrationChecks
{
    public static async Task<string> CoolingRun()
    {
        Require(EditorApplication.isPlaying,"Enter Play Mode first");bool background=Application.runInBackground;Application.runInBackground=true;
        var root=new GameObject("Cooling regression check");root.transform.position=new Vector3(30,0,0);
        var template=new GameObject("Cooling check template");var motion=root.AddComponent<ForgeCraftMotion>();motion.Initialize(template);
        var camera=new GameObject("CoolingCheckCamera").AddComponent<Camera>();camera.transform.position=new Vector3(35,3.8f,-5);camera.transform.LookAt(new Vector3(32.1f,.9f,1));camera.orthographic=true;camera.orthographicSize=2.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.22f);
        var light=new GameObject("CoolingCheckLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-30,0);
        string report="";
        try{
            foreach(int coolant in new[]{0,3,8}){
                motion.BeginRun();await Task.Delay(40);
                var e=ForgeMvp.Evaluate("p = 1(1)\n2(p)\n3(p)\n4(p, "+coolant+")",100,10,materialStocks:new Dictionary<int,long>{{3,10},{8,10}});Require(e.error==null,e.error);
                foreach(var step in e.steps.Take(3)){step.duration=.08f;await Animate(motion,step);}
                var work=root.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("Workpiece "));
                var tip=work.Find("Tip");var water=root.GetComponentsInChildren<Transform>().First(t=>t.name=="QuenchTarget");
                var blade=work.Find("Blade").GetComponent<Renderer>();var hammer=root.GetComponentsInChildren<Transform>().First(t=>t.name=="HeldHammer");
                var stepCool=e.steps.Last();float duration=stepCool.duration;float minY=float.MaxValue,minZ=float.MaxValue,maxZ=float.MinValue;int particles=0;bool approach=false,immersed=false,withdraw=false;
                await Animate(motion,stepCool,elapsed=>{
                    Require(!hammer.gameObject.activeSelf,"Cooling should put hammer away");
                    if(elapsed>duration*.43f){minY=Mathf.Min(minY,tip.position.y);minZ=Mathf.Min(minZ,work.position.z);maxZ=Mathf.Max(maxZ,work.position.z);}
                    var ps=root.GetComponentInChildren<ParticleSystem>();if(ps)particles=Mathf.Max(particles,ps.particleCount);
                    if(elapsed>duration*.43f&&!approach){Capture(camera,"Cooling"+coolant+"_Approach");approach=true;}
                    if(elapsed>duration*.67f&&!immersed){Capture(camera,"Cooling"+coolant+"_Active");immersed=true;}
                    if(elapsed>duration*.91f&&!withdraw){Capture(camera,"Cooling"+coolant+"_Withdraw");withdraw=true;}
                });
                Require(blade.sharedMaterial.GetColor("_EmissionColor").maxColorComponent<.001f,"Blade did not cool");Require(particles>0,"No cooling vapor emitted");
                if(coolant==0){Require(maxZ-minZ>.2f,"Air cooling has no visible fanning motion");Require(work.position.y>root.transform.position.y+1,"Air cooling did not lift sword");}
                else{Require(minY<water.position.y-.1f,"Blade never immersed");Require(minY>root.transform.position.y+.04f,"Blade penetrated bucket floor");Require(tip.position.y>water.position.y+.1f,"Blade not withdrawn");}
                Require(hammer.gameObject.activeSelf,"Hammer not restored");Require(motion.CompletedActions==4,"Action count mismatch");
                report+="PASS coolant "+coolant+": visible motion, vapor="+particles+", cold blade, withdrawal/restoration, 4 receipts\n";
            }
            File.WriteAllText("Assets/Art/Blacksmith/Documentation/CoolingChecks.txt",report);return report;
        }finally{Application.runInBackground=background;UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(template);UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(light.gameObject);}
    }
    public static async Task<string> MainGameRun()
    {
        Require(EditorApplication.isPlaying,"Enter Play Mode first");
        var main=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();Require(main,"Game missing");
        const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(ForgeMvp);
        object Get(string n)=>type.GetField(n,flags).GetValue(main);
        void Set(string n,object v)=>type.GetField(n,flags).SetValue(main,v);
        void Call(string n,params object[] args)=>type.GetMethod(n,flags).Invoke(main,args);
        Require(!(bool)Get("progressLoaded")&&(int)Get("activeSaveSlot")==0,"Run only from title with no save loaded");
        bool background=Application.runInBackground;Application.runInBackground=true;
        try{
            Call("ResetProgressForDebug",false);Set("showTitle",false);
            // Deterministic ordinary success: exclude the game's daily 50% great-success refund from this stock assertion.
            var smiths=(ForgeBlacksmiths)Get("blacksmiths");smiths.Tick(main.DayCycle.Day,(main.DayCycle.Day-1)*24d+main.DayCycle.CurrentHour);smiths.burningUntil=0;smiths.randomState=123;
            var stocks=(ForgeMaterialStock[])Get("materialStocks");stocks.First(s=>s.material==3).count=2;
            Set("source","p = 1(1)\n2(p)\n3(p)\n4(p, 3)\n!p");
            Call("Execute",false);
            float start=Time.realtimeSinceStartup;bool observedHeat=false,observedStrike=false,observedQuench=false;
            while((bool)Get("busy")||Get("executionSession")!=null){
                int process=main.GetComponent<ForgeCraftMotion>().ActiveProcess;
                observedHeat|=process==2;observedStrike|=process==3;observedQuench|=process==4;
                Require(Time.realtimeSinceStartup-start<30,"Real game execution timed out");await Task.Delay(20);
            }
            Require(!(bool)Get("reportError"),(string)Get("report"));
            Require((int)Get("iron")==9,"Real game iron receipt mismatch");
            Require(stocks.First(s=>s.material==3).count==1,"Real game water receipt mismatch");
            Require(main.GetComponent<ForgeCraftMotion>().CompletedActions==4,"Real game animations incomplete");
            Require(observedHeat&&observedStrike&&observedQuench,"Real execution skipped a visual phase");
            var shop=(ForgeShop)Get("shop");Require(shop.Count==1,"Completed sword did not enter shop");
            Capture(Camera.main,"ActualGameCompleted");
            string report="PASS actual ForgeMvp.Execute -> RunExecution -> ForgeCraftMotion -> saved prefabs\nPASS real stock: iron 10 -> 9; water 2 -> 1\nPASS 4 completed physical actions, heat/strike/quench observed\nPASS 1 completed product in real shop\nNo save slot loaded or written. Test state reset to title.\n";
            File.WriteAllText("Assets/Art/Blacksmith/Documentation/ActualGameChecks.txt",report);return report;
        }finally{Call("StopExecution",false);Set("busy",false);Call("ResetProgressForDebug",false);Set("showTitle",true);Application.runInBackground=background;}
    }
    static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
    static async Task Animate(ForgeCraftMotion motion,ForgeMvp.Step step,Action<float> sample=null)
    {
        bool done=false;Exception failure=null;float began=Time.realtimeSinceStartup;
        IEnumerator Runner(){var stack=new Stack<IEnumerator>();stack.Push(motion.Play(step));
            try{while(stack.Count>0){var top=stack.Peek();bool more=false;try{more=top.MoveNext();}catch(Exception e){failure=e;break;}
                if(!more){(top as IDisposable)?.Dispose();stack.Pop();continue;}
                if(top.Current is IEnumerator nested){stack.Push(nested);continue;}yield return top.Current;
            }}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();done=true;}}
        var coroutine=motion.StartCoroutine(Runner());
        try{while(!done){sample?.Invoke(Time.realtimeSinceStartup-began);Require(Time.realtimeSinceStartup-began<10,"Animation timed out");await Task.Delay(20);}if(failure!=null)throw failure;}
        finally{if(!done)motion.StopCoroutine(coroutine);}
    }
    static void Capture(Camera camera,string name){
        var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;camera.targetTexture=rt;
        try{camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Assets/Art/Blacksmith/Documentation/Integration_"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
        finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.Destroy(rt);}
    }
    public static async Task<string> Run()
    {
        Require(EditorApplication.isPlaying,"Enter Play Mode first");
        bool background=Application.runInBackground;Application.runInBackground=true;
        var main=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();Require(main,"Real ForgeMvp boot missing");
        Require(main.GetComponent<ForgeCraftMotion>().UsesWorkshopAssets,"Real game did not select workshop assets");
        Require(GameObject.Find("Temporary Workbench")==null,"Legacy bench still present");
        string report=BlueprintFreeForgeChecks.Run()+"\nPASS real SampleScene game boot uses workshop assets\n";
        var root=new GameObject("Isolated integration check");root.transform.position=new Vector3(30,0,0);
        var template=new GameObject("Hidden check template");var motion=root.AddComponent<ForgeCraftMotion>();motion.Initialize(template);
        var camera=new GameObject("IntegrationCamera").AddComponent<Camera>();camera.transform.position=new Vector3(35,3.8f,-5);camera.transform.LookAt(new Vector3(32.1f,.8f,1));camera.orthographic=true;camera.orthographicSize=2.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.22f);
        var light=new GameObject("IntegrationLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-30,0);
        try{
            var evaluation=ForgeMvp.Evaluate("p = 1(1)\n2(p)\n3(p)\n4(p, 3)\n!p",100,10,materialStocks:new Dictionary<int,long>{{3,10}});
            Require(evaluation.error==null,evaluation.error);motion.BeginRun();int actions=0;
            foreach(var step in evaluation.steps){step.duration=.9f;bool captured=false;float armMotion=0;var hand=root.GetComponentsInChildren<Transform>().First(t=>t.name=="RightHandSocket");var first=hand.position;
                await Animate(motion,step,t=>{
                    armMotion=Mathf.Max(armMotion,Vector3.Distance(first,hand.position));
                    foreach(var bone in root.GetComponentsInChildren<Transform>())Require(float.IsFinite(bone.position.x)&&float.IsFinite(bone.position.y)&&float.IsFinite(bone.position.z),"Non-finite bone "+bone.name);
                    if(t>.68f&&!captured){captured=true;Capture(camera,"Process"+step.process);}
                });
                if(step.process>0)actions++;Require(motion.ActiveProcess==0,"Active process not cleared");Require(motion.CompletedActions==actions,"Receipt count mismatch");
                var blade=root.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="Blade");Require(blade,"Blade absent");
                Require(blade.sharedMaterial.GetColor("_EmissionColor").maxColorComponent>(step.heat>0?.01f:-1),"Heat not applied");
                if(step.heat==0)Require(blade.sharedMaterial.GetColor("_EmissionColor").maxColorComponent<.001f,"Cooling did not clear emission");
                if(step.process==3)Require(armMotion>.05f,"Hammer hand did not animate");
                report+="PASS receipt "+step.process+"; finite bones, heat and completion count\n";
            }
            Require(evaluation.product!=null&&evaluation.product.Shaped,"Interpreter product missing");
            // The second smith has its own station coordinates and piece identity.
            motion.BeginRun();await Task.Delay(40);Require(root.GetComponent<ForgeWorkshopVisuals>().PieceCount==0,"Clear retained pieces");
            var two=ForgeMvp.Evaluate("a = 1(1)\nb = 1(1)\n2(a)\n4(b, 0)");Require(two.error==null,two.error);
            foreach(var step in two.steps){step.duration=.08f;await Animate(motion,step);}
            Require(root.GetComponent<ForgeWorkshopVisuals>().PieceCount==2,"Piece identity was merged");report+="PASS independent offset station, two workpieces and air cooling\n";
            var cancelStep=new ForgeMvp.Step{pieceId=1,process=2,heat=7,duration=2};var enumerator=root.GetComponent<ForgeWorkshopVisuals>().Play(cancelStep);Require(enumerator.MoveNext(),"Cancellation setup");root.GetComponent<ForgeWorkshopVisuals>().Cancel();Require(!enumerator.MoveNext(),"Cancellation left animation running");report+="PASS cancellation\n";
            File.WriteAllText("Assets/Art/Blacksmith/Documentation/IntegrationChecks.txt",report);return report;
        }finally{Application.runInBackground=background;UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(template);UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(light.gameObject);}
    }
}
