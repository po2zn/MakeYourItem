using UnityEngine;
public static class ForgeCodePreferences
{
 public const string SoundKey="MakeYourItem.Settings.CodeTypingSound",ForgeKey="MakeYourItem.Settings.ForgeCode";
 static bool loaded,sound=true,forge;
 public static event System.Action Changed;
 public static bool TypingSound {get{Ensure();return sound;}}
 public static bool ForgeCode {get{Ensure();return forge;}}
 static void Ensure(){if(!loaded)Load();}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){loaded=false;Changed=null;}
 public static void Load(){sound=PlayerPrefs.GetInt(SoundKey,1)!=0;forge=PlayerPrefs.GetInt(ForgeKey,0)!=0;loaded=true;Changed?.Invoke();}
 public static void SetTypingSound(bool value){Ensure();sound=value;PlayerPrefs.SetInt(SoundKey,value?1:0);PlayerPrefs.Save();Changed?.Invoke();}
 public static void SetForgeCode(bool value){Ensure();forge=value;PlayerPrefs.SetInt(ForgeKey,value?1:0);PlayerPrefs.Save();Changed?.Invoke();}
}
