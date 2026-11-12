using UnityEngine;

// Shared channel levels. The legacy volume key remains the master control.
public static class ForgeAudioSettings
{
    public const string MasterKey="MakeYourItem.Settings.Volume";
    public const string MusicKey="MakeYourItem.Settings.BgmVolume";
    public const string SfxKey="MakeYourItem.Settings.SfxVolume";
    public static float Master { get; private set; }=1;
    public static float Music { get; private set; }=1;
    public static float Sfx { get; private set; }=1;
    public static event System.Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){Changed=null;Load();}
    public static void Load()
    {
        Master=Read(MasterKey);Music=Read(MusicKey);Sfx=Read(SfxKey);
        Apply();
    }
    static float Read(string key)=>Clamp(PlayerPrefs.GetFloat(key,1));
    static float Clamp(float value)=>float.IsNaN(value)?1:Mathf.Clamp01(value);
    public static void SetVolumes(float master,float music,float sfx)
    {
        Master=Clamp(master);Music=Clamp(music);Sfx=Clamp(sfx);
        PlayerPrefs.SetFloat(MasterKey,Master);
        PlayerPrefs.SetFloat(MusicKey,Music);
        PlayerPrefs.SetFloat(SfxKey,Sfx);
        Apply();Save();
    }
    static void Apply(){AudioListener.volume=Master;Changed?.Invoke();}
    public static void Save()=>PlayerPrefs.Save();
}
