using System;
public static class ForgeDiagnosticChecks
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static string Run()
    {
        string text = "# comment\r\n\r\nMaterial = 1\r\nBlade = 1(Material)\r\n2(Blade)\r\n!Blade";
        var e = ForgeMvp.Evaluate(text, 100, 10);
        Require(e.compileError && e.errorLine == 5 && e.errorMessage.Contains("인자") && e.cost == 0 && e.product == null, "original line and compile isolation");
        var d = new ForgeCompileDiagnostic(text, e.errorLine, e.errorMessage);
        d.GetLineSpan(out int start, out int length);
        Require(text.Substring(start, length) == "2(Blade)", "CRLF source range");
        var missingFinish = ForgeMvp.Evaluate("# comment\nBlade = 1(1)\n# trailing\n", 100, 10);
        Require(missingFinish.compileError && missingFinish.errorLine == 2 && missingFinish.errorMessage.Contains("!Blade"), "missing completion location");
        var empty = ForgeMvp.Evaluate("", 100, 10);
        new ForgeCompileDiagnostic("", empty.errorLine, empty.errorMessage).GetLineSpan(out start, out length);
        Require(empty.compileError && empty.errorLine == 1 && start == 0 && length == 0, "empty source marker");
        var runtime = ForgeMvp.Evaluate("Blade = 1(1)\n3(Blade)\n4(Blade, 0)\n!Blade", 100, 10);
        Require(!runtime.compileError && runtime.errorLine == 2 && runtime.cost == 10, "runtime error stays distinct");
        return "PASS diagnostic checks: original line, CRLF span, missing completion, empty source, runtime distinction (5 cases)";
    }
    public static void Main() { Console.WriteLine(Run()); Console.WriteLine(ForgeEconomyChecks.Run()); }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Compile Diagnostics")]
    public static void ValidateInEditor() { UnityEngine.Debug.Log(Run()); }
#endif
}
