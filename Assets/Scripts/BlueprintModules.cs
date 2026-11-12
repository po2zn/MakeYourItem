using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// All sources are snapshots of open code windows. No filesystem or Python host execution.
public sealed partial class BlueprintRuntime
{
    sealed class Module
    {
        public string Name;
        public bool IsMain, Initializing;
        public Dictionary<string,object> Globals;
    }
    sealed class Function
    {
        public Module Module;
        public BlueprintStatement Definition;
        public HashSet<string> Locals = new HashSet<string>(StringComparer.Ordinal);
    }
    readonly Dictionary<string,string> moduleSources = new Dictionary<string,string>(StringComparer.Ordinal);
    readonly Dictionary<string,Module> moduleCache = new Dictionary<string,Module>(StringComparer.Ordinal);
    Module currentModule;
    HashSet<string> localNames;
    ForgeTechnology moduleTechnology;
    int importDepth;

    void InitializeModules(IReadOnlyDictionary<string,string> sources,ForgeTechnology technology,BlueprintProgram main)
    {
        moduleTechnology=technology;
        if(sources!=null)foreach(var pair in sources){
            string name;
            try{name=PythonIdentifier.Parse(pair.Key);}catch{continue;}
            // Null also represents ambiguous normalized code-window titles.
            if(moduleSources.ContainsKey(name))moduleSources[name]=null;
            else moduleSources.Add(name,pair.Value);
        }
        currentModule=new Module{Name="__main__",IsMain=true,Globals=result.values};
        BindFunctions(currentModule,main);
    }
    void BindFunctions(Module module,BlueprintProgram program)
    {
        module.Globals["__name__"]=module.Name;
        foreach(var pair in program.Functions){
            if(pair.Value.Error!=null)continue;
            var function=new Function{Module=module,Definition=pair.Value};
            foreach(var parameter in pair.Value.Parameters)function.Locals.Add(parameter);
            GatherLocals(pair.Value.Body,function.Locals);
            module.Globals[pair.Key]=function;
        }
    }
    static void GatherLocals(List<BlueprintStatement> body,HashSet<string> names)
    {
        foreach(var s in body){
            if(s.Kind==BlueprintStatementKind.Assign&&s.Target.Kind==BlueprintExprKind.Name)names.Add(s.Target.Name);
            if(s.Kind==BlueprintStatementKind.For)names.Add(s.Name);
            foreach(var item in s.Imports)if(item.Alias!=null)names.Add(item.Alias);
            GatherLocals(s.Body,names);GatherLocals(s.Other,names);
        }
    }
    object Lookup(string name,Dictionary<string,object> env)
    {
        if(env.TryGetValue(name,out var value))return value;
        if(localNames==null||!localNames.Contains(name))if(currentModule.Globals.TryGetValue(name,out value))return value;
        Fail("변수 '"+name+"'에 아직 값이 없습니다. 이 줄 실행 전에 값을 대입하세요.");return null;
    }
    object Member(object target,string name)
    {
        if(!(target is Module))Fail("점(.)으로 함수나 변수를 읽으려면 가져온 모듈이 필요합니다.");
        var module=(Module)target;
        if(!module.Globals.TryGetValue(name,out var value))Fail("모듈 '"+module.Name+"'에 '"+name+"'이 없습니다.");
        return value;
    }
    async Task<object> Invoke(object target,List<object> args)
    {
        if(!(target is Function))Fail("호출 대상은 함수여야 합니다.");
        var function=(Function)target;var definition=function.Definition;
        if(args.Count!=definition.Parameters.Count)Fail(definition.Name+"에 필요한 인자 수: "+definition.Parameters.Count);
        if(calls>=CallLimit)Fail("함수 호출 깊이 제한("+CallLimit+")에 도달했습니다. 재귀의 종료 조건을 확인하세요.");
        var previous=currentModule;var previousLocals=localNames;int previousLine=currentLine;
        currentModule=function.Module;localNames=function.Locals;calls++;
        try {
            var local=new Dictionary<string,object>(StringComparer.Ordinal);
            for(int i=0;i<args.Count;i++)local[definition.Parameters[i]]=Copy(args[i]);
            return (await Block(definition.Body,local)).Value;
        }catch(BlueprintException e){if(e.ModuleName==null&&!currentModule.IsMain)e.ModuleName=currentModule.Name;throw;}
        catch(OverflowException){Fail("정수 범위 초과");return null;}
        finally{calls--;currentModule=previous;localNames=previousLocals;currentLine=previousLine;}
    }
    async Task<Module> LoadModule(string name)
    {
        if(moduleCache.TryGetValue(name,out var cached)){
            if(cached.Initializing)Fail("순환 import는 지원하지 않습니다: "+name);
            return cached;
        }
        if(!moduleSources.TryGetValue(name,out var source))Fail("가져올 코드창을 찾을 수 없습니다: "+name+". 코드창 제목을 모듈 이름으로 사용하세요.");
        if(source==null)Fail("모듈 이름이 같은 코드창이 여러 개 있습니다: "+name+". 제목을 다르게 지정하세요.");
        if(importDepth>=32||moduleCache.Count>=64)Fail("모듈 가져오기 제한: 중첩 32단계, 한 실행에 64개입니다.");
        var module=new Module{Name=name,Initializing=true,Globals=new Dictionary<string,object>(StringComparer.Ordinal)};
        moduleCache.Add(name,module);
        var previous=currentModule;var previousLocals=localNames;int previousLine=currentLine;
        currentModule=module;localNames=null;importDepth++;
        try {
            bool deferErrors=session!=null&&session.DeferredErrors;
            var program=new BlueprintValidator(new BlueprintParser(source,deferErrors,moduleTechnology).Parse(),moduleTechnology,deferErrors).Validate();
            BindFunctions(module,program);
            await Block(program.Body,module.Globals);
            module.Initializing=false;
            return module;
        }catch(BlueprintException e){if(e.ModuleName==null)e.ModuleName=name;throw;}
        catch(OverflowException){Fail("정수 범위 초과");return null;}
        finally{importDepth--;currentModule=previous;localNames=previousLocals;currentLine=previousLine;}
    }
    async Task Import(BlueprintStatement statement,Dictionary<string,object> env)
    {
        if(statement.Kind==BlueprintStatementKind.Import){
            foreach(var item in statement.Imports){currentLine=statement.Token.Line;env[item.Alias]=await LoadModule(item.Name);}
            return;
        }
        var module=await LoadModule(statement.Name);
        foreach(var item in statement.Imports){
            currentLine=statement.Token.Line;
            if(item.Name=="*"){
                foreach(var pair in module.Globals)if(!pair.Key.StartsWith("_",StringComparison.Ordinal))env[pair.Key]=pair.Value;
            }else env[item.Alias]=Member(module,item.Name);
        }
    }
}
