using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed partial class BlueprintRuntime
{
    Session session;
    Task BeforeStatement(BlueprintToken token) => session==null ? Task.CompletedTask : session.Pause(token);

    // No worker thread and no speculative replay: the async call stack stays suspended.
    public sealed class Session : IDisposable
    {
        readonly BlueprintRuntime runtime;
        readonly ForgeMvp.Evaluation evaluation;
        TaskCompletionSource<bool> gate, arrival;
        Task execution;
        readonly Func<Task> entry;
        public ForgeMvp.Evaluation Evaluation => evaluation;
        public bool DeferredErrors { get; }
        public bool Completed { get; private set; }
        public bool Cancelled { get; private set; }
        public int NextLine { get; private set; }
        public string NextModule { get; private set; }
        public int LastLine { get; private set; }
        public string LastModule { get; private set; }
        public bool IsPaused => gate!=null&&!Completed;

        internal Session(ForgeMvp.Evaluation failure){evaluation=failure;Completed=true;}
        internal Session(BlueprintRuntime runtime,bool deferredErrors,Func<Task> entry=null)
        {
            this.runtime=runtime;evaluation=runtime.result;runtime.session=this;DeferredErrors=deferredErrors;this.entry=entry;
            execution=Run();
        }
        internal Task Pause(BlueprintToken token)
        {
            if(Cancelled)throw new OperationCanceledException();
            NextLine=token.Line;NextModule=runtime.currentModule.IsMain?null:runtime.currentModule.Name;
            gate=new TaskCompletionSource<bool>();var paused=gate;
            var reached=arrival;arrival=null;reached?.TrySetResult(true);
            return paused.Task;
        }
        async Task Run()
        {
            try {if(entry!=null)await entry();else await runtime.Block(runtime.program.Body,evaluation.values);}
            catch(OperationCanceledException){Cancelled=true;}
            catch(BlueprintException e){Failure(evaluation,e,false);}
            catch(OverflowException){Failure(evaluation,new BlueprintException(runtime.currentLine,"정수 범위 초과"){ModuleName=runtime.currentModule.IsMain?null:runtime.currentModule.Name},false);}
            finally {
                Completed=true;NextLine=0;NextModule=null;gate=null;
                evaluation.skipped=evaluation.steps.Count==0;
                evaluation.debugOnly=evaluation.product==null&&evaluation.steps.Count>0&&evaluation.steps.TrueForAll(step=>step.debugOutput);
                var reached=arrival;arrival=null;reached?.TrySetResult(true);
            }
        }
        public Task Advance()
        {
            if(Completed||Cancelled)return Task.CompletedTask;
            runtime.context.RefreshResources?.Invoke(this);
            if(gate==null)return arrival?.Task??Task.CompletedTask;
            LastLine=NextLine;LastModule=NextModule;
            var reached=new TaskCompletionSource<bool>();arrival=reached;
            var resume=gate;gate=null;resume.TrySetResult(true);
            return reached.Task;
        }
        public void Dispose()
        {
            if(Completed)return;
            Cancelled=true;
            var resume=gate;gate=null;resume?.TrySetException(new OperationCanceledException());
        }
        public void SetResources(int gold,int iron,IReadOnlyDictionary<int,long> materials)
        {
            if(runtime==null)return;runtime.gold=gold;runtime.iron=iron;
            if(runtime.stocks!=null){runtime.stocks.Clear();foreach(var pair in materials)runtime.stocks[pair.Key]=pair.Value;}
        }
    }
    public static Session StartSession(string source,int gold,int iron,string owner="",uint seed=1,IReadOnlyDictionary<int,long> materialStocks=null,ForgeTechnology technology=null,IReadOnlyDictionary<string,string> modules=null,bool deferErrors=false,Func<double> clockHour=null,ForgeRunContext context=null)
    {
        BlueprintProgram program;
        try {program=new BlueprintValidator(new BlueprintParser(source,deferErrors,technology).Parse(),technology,deferErrors).Validate();}
        catch(BlueprintException e){return new Session(Failure(new ForgeMvp.Evaluation(),e,true));}
        return new Session(new BlueprintRuntime(program,gold,iron,owner,seed,materialStocks,modules,technology,clockHour,context),deferErrors);
    }
}
