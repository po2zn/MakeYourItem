using System.Collections;
using UnityEngine;

// A single persistent 2D music source, shared by the title and workshop.
[RequireComponent(typeof(AudioSource))]
public sealed class ForgeBackgroundMusic : MonoBehaviour
{
    static ForgeBackgroundMusic instance;
    [SerializeField] AudioClip[] playlist;
    AudioSource musicSource;
    float authoredVolume;
    bool applicationPaused;
    double pauseStarted,suspendedDspTime;
    double PlaybackClock=>AudioSettings.dspTime-suspendedDspTime-(applicationPaused?AudioSettings.dspTime-pauseStarted:0);
    void OnApplicationPause(bool paused){
        if(paused==applicationPaused)return;
        if(paused)pauseStarted=AudioSettings.dspTime;
        else suspendedDspTime+=System.Math.Max(0,AudioSettings.dspTime-pauseStarted);
        applicationPaused=paused;
    }
    void OnEnable(){ForgeAudioSettings.Changed+=ApplyVolume;ApplyVolume();}
    void OnDisable(){ForgeAudioSettings.Changed-=ApplyVolume;}
    void ApplyVolume(){if(musicSource)musicSource.volume=authoredVolume*ForgeAudioSettings.Music;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetInstance(){instance=null;}

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if(instance)return;
        var existing=FindAnyObjectByType<ForgeBackgroundMusic>();
        if(existing){instance=existing;return;}
        var prefab=Resources.Load<GameObject>("ForgeBackgroundMusic");
        if(prefab)Instantiate(prefab);
    }

    void Awake()
    {
        if(instance&&instance!=this){Destroy(gameObject);return;}
        instance=this;DontDestroyOnLoad(gameObject);
        Application.runInBackground=true;
        var source=GetComponent<AudioSource>();
        musicSource=source;authoredVolume=source.volume;ApplyVolume();
        source.playOnAwake=false;source.loop=false;source.spatialBlend=0;
    }

    IEnumerator Start()
    {
        var source=GetComponent<AudioSource>();
        if(playlist==null||playlist.Length==0)yield break;
        int index=0;
        while(true){
            var clip=playlist[index];
            index=(index+1)%playlist.Length;
            if(!clip){yield return new WaitForSecondsRealtime(1);continue;}
            if(clip.loadState==AudioDataLoadState.Unloaded)clip.LoadAudioData();
            while(clip.loadState==AudioDataLoadState.Loading)yield return null;
            if(clip.loadState==AudioDataLoadState.Failed){
                Debug.LogWarning("Background music failed to load: "+clip.name,this);
                yield return new WaitForSecondsRealtime(1);continue;
            }
            source.clip=clip;source.Play();
            int resumeSample=0;
            double expectedEnd=PlaybackClock+clip.length;
            yield return null;
            while(true){
                if(applicationPaused||AudioListener.pause){yield return null;continue;}
                if(source.isPlaying){
                    resumeSample=source.timeSamples;
                    expectedEnd=PlaybackClock+(clip.samples-resumeSample)/(double)clip.frequency;
                    yield return null;continue;
                }
                // A temporary audio interruption is not the end of the track.
                if(PlaybackClock>=expectedEnd-.02)break;
                source.Play();source.timeSamples=Mathf.Clamp(resumeSample,0,Mathf.Max(0,clip.samples-1));
                yield return new WaitForSecondsRealtime(.1f);
            }
        }
    }

    void OnDestroy(){if(instance==this)instance=null;}
}
