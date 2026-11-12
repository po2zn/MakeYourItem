using UnityEngine;

// Route UI/gameplay one-shot effects through Play so they follow the SFX setting.
[RequireComponent(typeof(AudioSource))]
public sealed class ForgeSoundEffects : MonoBehaviour
{
    static ForgeSoundEffects instance;
    AudioSource source;
    AudioClip preview;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetInstance(){instance=null;}
    static ForgeSoundEffects Instance
    {
        get {
            if(!instance)new GameObject("ForgeSoundEffects").AddComponent<ForgeSoundEffects>();
            return instance;
        }
    }
    void Awake()
    {
        if(instance&&instance!=this){Destroy(gameObject);return;}
        instance=this;DontDestroyOnLoad(gameObject);
        source=GetComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0;
        ApplyVolume();
    }
    void OnEnable(){ForgeAudioSettings.Changed+=ApplyVolume;ApplyVolume();}
    void OnDisable(){ForgeAudioSettings.Changed-=ApplyVolume;}
    void ApplyVolume(){if(source)source.volume=ForgeAudioSettings.Sfx;}
    public static void Play(AudioClip clip,float volume=1)
    {
        if(clip)Instance.source.PlayOneShot(clip,Mathf.Clamp01(volume));
    }
    public static void Preview()
    {
        var player=Instance;
        if(!player.preview){
            const int rate=44100;
            var samples=new float[rate/5];
            for(int i=0;i<samples.Length;i++){
                float t=i/(float)rate;
                float envelope=Mathf.Min(1,t/.008f)*Mathf.Pow(1-i/(float)samples.Length,2);
                samples[i]=Mathf.Sin(2*Mathf.PI*660*t)*envelope*.25f;
            }
            player.preview=AudioClip.Create("SFX preview",samples.Length,1,rate,false);
            player.preview.SetData(samples,0);
        }
        player.source.Stop();
        Play(player.preview);
    }
    void OnDestroy(){if(instance==this)instance=null;if(preview)Destroy(preview);}
}
