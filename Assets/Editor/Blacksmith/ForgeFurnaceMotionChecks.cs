using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Run as a coroutine in Play Mode. Throws on motion/cleanup regressions.
public static class ForgeFurnaceMotionChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception("Furnace motion: "+message);}
    static IEnumerator Frames(IEnumerator action)
    {
        var stack=new Stack<IEnumerator>();stack.Push(action);
        try{
            while(stack.Count>0){
                var current=stack.Peek();
                if(!current.MoveNext()){(current as IDisposable)?.Dispose();stack.Pop();continue;}
                if(current.Current is IEnumerator nested){stack.Push(nested);continue;}
                yield return current.Current;
            }
        }finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();}
    }
    public static IEnumerator Run()
    {
        Require(Application.isPlaying,"Play Mode required");
        var root=new GameObject("Furnace motion validation");root.transform.position=new Vector3(30,0,0);
        var view=root.AddComponent<ForgeWorkshopVisuals>();
        try{
            view.Initialize(Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets"));
            Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
            foreach(float speed in new[]{1f,5f}){
                view.Clear();Vector3 start=view.SmithPosition;bool coal=false,poker=false;float traveled=0;
                var e=BlueprintRuntime.Evaluate("8(1000)",0,1,"",1,context:new ForgeRunContext{Speed=speed});
                var action=Frames(view.Play(e.steps.Single()));
                try{while(action.MoveNext()){
                    traveled=Mathf.Max(traveled,Vector3.Distance(start,view.SmithPosition));
                    coal|=Find("Furnace coal").gameObject.activeSelf;poker|=Find("Furnace poker").gameObject.activeSelf;
                    Require(view.PieceCount==0,"no phantom sword");
                    foreach(var bone in Find("Blacksmith").GetComponentsInChildren<Transform>())Require(float.IsFinite(bone.position.x)&&float.IsFinite(bone.position.y)&&float.IsFinite(bone.position.z),"finite bone pose");
                    yield return action.Current;
                }}finally{(action as IDisposable)?.Dispose();}
                Require((speed>1||traveled>.2f)&&coal&&poker,"approach, coal feed and poker phases");
                Require(!view.IsActing&&!Find("Furnace coal").gameObject.activeSelf&&!Find("Furnace poker").gameObject.activeSelf,"completed cleanup");
            }
            var cancelled=Frames(view.Play(new ForgeMvp.Step{process=8,heat=1100,duration=2.4f}));
            try{while(cancelled.MoveNext()){
                if(Find("Furnace coal").gameObject.activeSelf){view.Cancel();break;}
                yield return cancelled.Current;
            }}finally{(cancelled as IDisposable)?.Dispose();}
            Require(!view.IsActing&&!Find("Furnace coal").gameObject.activeSelf&&!Find("Furnace poker").gameObject.activeSelf,"cancel cleanup");
            Require(Find("HeldHammer").gameObject.activeSelf&&view.PieceCount==0,"tools restored");
            view.Clear();
            var submission=ForgeMvp.Evaluate("a = 1(1)\nb = 1(1)\n2(a)\n3(a)\n4(a, 0)\n!a",100,10);
            Require(submission.error==null,submission.error);
            foreach(var step in submission.steps){
                step.duration=0;
                if(step.finished){
                    var submitted=Find("Workpiece "+step.pieceId).gameObject;
                    Require(!view.Play(step).MoveNext(),"submission finishes synchronously");
                    Require(!submitted.activeSelf&&view.PieceCount==1,"submission immediately hides only the submitted sword");
                    view.Cancel();
                    Require(!submitted.activeSelf,"cancel cannot restore submitted sword");
                    Require(!view.Play(step).MoveNext(),"repeated submission finishes synchronously");
                    Require(view.PieceCount==1,"duplicate finish cannot spawn a phantom sword");
                }else yield return Frames(view.Play(step));
            }
            yield return Frames(view.Play(new ForgeMvp.Step{finished=true,pieceId=submission.steps[1].pieceId}));
            Require(view.PieceCount==0,"second submission removes remaining workpiece");
            Debug.Log("PASS submission cleanup: targeted, immediate, repeated finish, cancel, multiple workpieces");
            Debug.Log("PASS furnace motion: approach, coal, poker, 1x/5x, no phantom workpiece, cancellation cleanup");
        }finally{UnityEngine.Object.Destroy(root);}
    }
}
