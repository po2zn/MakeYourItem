using System;
using System.Collections.Generic;
using System.Reflection;
public static class BlueprintCommandLimitChecks
{
    public static void SetRemaining(BlueprintRuntime.Session session,int remaining)
    {
        var runtime=typeof(BlueprintRuntime.Session).GetField("runtime",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(session);
        typeof(BlueprintRuntime).GetField("commands",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(runtime,BlueprintRuntime.CommandLimit-remaining);
    }
    public static ForgeMvp.Evaluation NearLimit(string source,IReadOnlyDictionary<string,string> modules=null)
    {
        using(var session=BlueprintRuntime.StartSession(source,100,10,modules:modules)){
            if(session.Completed)throw new Exception(session.Evaluation.error??"No running session");
            SetRemaining(session,40);
            for(int i=0;!session.Completed&&i<100;i++)session.Advance().GetAwaiter().GetResult();
            if(!session.Completed)throw new Exception("Command limit did not stop execution");
            return session.Evaluation;
        }
    }
    public static string Run()
    {
        if(BlueprintRuntime.CommandLimit!=2100000000)throw new Exception("Expected 2.1 billion commands");
        var longRun=BlueprintRuntime.Evaluate("n = 0\n888(n < 3000):\n    n = n + 1",0,0,"limit",1);
        if(longRun.error!=null||!Equals(longRun.values["n"],3000))throw new Exception("Old 10000 limit still applied: "+longRun.error);
        using(var session=BlueprintRuntime.StartSession("x = 1\ny = 2",0,0)){
            SetRemaining(session,2);session.Advance().GetAwaiter().GetResult();
            if(session.Evaluation.error!=null||!Equals(session.Evaluation.values["x"],1))throw new Exception("Final allowed command rejected");
            session.Advance().GetAwaiter().GetResult();
            if(!session.Completed||session.Evaluation.errorLine!=2||session.Evaluation.values.ContainsKey("y")||!session.Evaluation.errorMessage.Contains("21억"))throw new Exception("Boundary command allowed or wrong diagnostic");
        }
        return "PASS command limit: 2.1 billion, execution beyond old limit, exact boundary and source line";
    }
}
