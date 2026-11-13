using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Presentation reads recorded evaluations and current bindings; it never evaluates source.
public sealed class BlueprintObservation {
    public const int Capacity=80;
    public bool Enabled;
    readonly Queue<string> entries=new Queue<string>();
    IReadOnlyDictionary<string,object> local,global;
    public int Line {get;private set;}
    public string Module {get;private set;}
    public string Latest {get;private set;}="";
    public string History=>string.Join("\n",entries);
    public int RecordCount=>entries.Count;
    public void Scope(int line,string module,IReadOnlyDictionary<string,object> locals,IReadOnlyDictionary<string,object> globals){Line=line;Module=module;local=locals;global=globals;}
    public void Record(int line,string module,string message,bool important=false){if(!Enabled&&!important)return;Latest=(module==null?"":module+" · ")+line+"번째 줄: "+message;if(entries.Count>0&&entries.Last()==Latest)return;entries.Enqueue(Latest);while(entries.Count>Capacity)entries.Dequeue();}
    public IEnumerable<string> Names=> (local?.Keys??Enumerable.Empty<string>()).Concat(global?.Keys??Enumerable.Empty<string>()).Where(n=>!n.StartsWith("__")).Distinct().OrderBy(n=>n);
    public bool TryValue(string name,out object value){value=null;return name!=null&&(local!=null&&local.TryGetValue(name,out value)||global!=null&&global.TryGetValue(name,out value));}
    public string Inspect(string name){if(!TryValue(name,out var value))return "아직 기록하지 않은 변수 또는 현재 범위에 없는 변수";if(value is IList<int> array){var b=new StringBuilder(name+" · 길이 "+array.Count);for(int i=0;i<array.Count;i++)b.Append("\n[").Append(i).Append("]: ").Append(array[i]);return b.ToString();}return name+": "+Format(value);}
    public bool BindArray(string name,int count,out IList<int> array,out string reason){array=null;if(string.IsNullOrEmpty(name)){reason="연결 안 함";return false;}if(!TryValue(name,out var value)){reason=name+": 아직 기록하지 않았거나 현재 범위에 없습니다.";return false;}array=value as IList<int>;if(array==null){reason=name+": 정수 배열만 연결할 수 있습니다.";return false;}if(array.Count!=count){reason=name+": 배열 길이 "+array.Count+", 지점 수 "+count+"가 다릅니다.";array=null;return false;}reason="코드가 기록한 값 · 자동 정답 아님";return true;}
    public void Clear(){entries.Clear();Latest="";Line=0;Module=null;local=global=null;}
    public static string Format(object value){if(value==null)return "None";if(value is bool b)return b?"참":"거짓";if(value is ForgeMvp.Piece p)return "칼날/재료 #"+p.id;if(value is IList<int> array){return "["+string.Join(", ",array.Take(32))+(array.Count>32?", …":"")+"]";}return value.ToString();}
}
public sealed class BlueprintBreakpoints {
    readonly HashSet<string> points=new HashSet<string>();string resume;
    static string Key(string module,int line)=>(module??"")+":"+line;
    public bool Toggle(string module,int line){if(line<1)return false;string key=Key(module,line);if(points.Remove(key))return false;points.Add(key);return true;}
    public bool Contains(string module,int line)=>points.Contains(Key(module,line));
    public bool StopBefore(string module,int line,bool singleStep){string key=Key(module,line);if(singleStep){resume=null;return false;}if(resume==key){resume=null;return false;}if(!points.Contains(key))return false;resume=key;return true;}
    public IEnumerable<int> Lines(string module){string prefix=(module??"")+":";foreach(var key in points)if(key.StartsWith(prefix,StringComparison.Ordinal)&&int.TryParse(key.Substring(prefix.Length),out int line))yield return line;}
    public void Restart(){resume=null;}public void Clear(){points.Clear();resume=null;}
}
public sealed partial class BlueprintRuntime {
    bool Watching=>context.Observation!=null&&context.Observation.Enabled;
    void Trace(int line,string message)=>context.Observation?.Record(line,currentModule.IsMain?null:currentModule.Name,message);
    object ObservedCondition(object value,int line){if(Watching)Trace(line,"조건 평가: "+BlueprintObservation.Format(value));return value;}
    void ObservedScope(BlueprintToken token,Dictionary<string,object> env){context.Observation?.Scope(token.Line,currentModule.IsMain?null:currentModule.Name,env,currentModule.Globals);}
}
