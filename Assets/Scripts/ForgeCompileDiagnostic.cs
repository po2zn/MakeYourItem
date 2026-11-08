using System;

// Original-source line positions; never recover a line number by parsing a localized message.
public sealed class ForgeCompileDiagnostic
{
    public string Source { get; }
    public int Line { get; }
    public string Message { get; }
    public ForgeCompileDiagnostic(string source, int line, string message)
    { Source = source ?? ""; Line = Math.Max(1, line); Message = message ?? "컴파일 오류"; }
    public void GetLineSpan(out int start, out int length)
    {
        start = 0;
        for (int line = 1; line < Line; line++)
        {
            int newline = Source.IndexOf('\n', start);
            if (newline < 0) break;
            start = newline + 1;
        }
        int end = Source.IndexOf('\n', start);
        if (end < 0) end = Source.Length;
        if (end > start && Source[end - 1] == '\r') end--;
        length = end - start;
    }
}
