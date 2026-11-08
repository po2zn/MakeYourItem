using System.Collections.Generic;

// Bounded session output; user strings are never treated as rich text or code.
public sealed class ForgeDebugOutput
{
    const int MaxEntries=256, MaxCharacters=65536;
    readonly Queue<string> entries=new Queue<string>();
    int characters;
    public int Count => entries.Count;
    public string Text => string.Join("\n",entries);
    public void Clear(){entries.Clear();characters=0;}
    public void Append(string message)
    {
        message=message??"";
        if(message.Length>MaxCharacters-1)message=message.Substring(0,MaxCharacters-20)+"… [출력 잘림]";
        while(entries.Count>0&&(entries.Count>=MaxEntries||characters+message.Length+1>MaxCharacters))characters-=entries.Dequeue().Length+1;
        entries.Enqueue(message);characters+=message.Length+1;
    }
}
