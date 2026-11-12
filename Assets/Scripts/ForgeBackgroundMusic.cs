using System.Collections;
using UnityEngine;

// A single persistent 2D music source, shared by the title and workshop.
[RequireComponent(typeof(AudioSource))]
public sealed class ForgeBackgroundMusic : MonoBehaviour
{
    static ForgeBackgroundMusic instance;
    [SerializeField] AudioClip[] playlist;
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
        var source=GetComponent<AudioSource>();
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
            yield return null;
            while(source.isPlaying||AudioListener.pause)yield return null;
        }
    }

    void OnDestroy(){if(instance==this)instance=null;}
}
