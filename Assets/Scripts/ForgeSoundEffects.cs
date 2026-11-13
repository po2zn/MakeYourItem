using UnityEngine;

// Route UI/gameplay one-shot effects through Play so they follow the SFX setting.
[RequireComponent(typeof(AudioSource))]
public sealed class ForgeSoundEffects : MonoBehaviour
{
    static ForgeSoundEffects instance;
    AudioSource source;
    AudioClip preview,codeError,sale;
    double nextSaleSound;
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
    public static void Click(){var lib=ForgeSfxLibrary.Instance;if(lib)Play(lib.ui,.6f);}
    public static void PurchaseSucceeded(){var lib=ForgeSfxLibrary.Instance;if(lib)Play(lib.upgrade,.7f);}
    // Two short tones with soft edges avoid clicks and follow the shared SFX volume.
    public static void CodeError()
    {
        var player=Instance;
        if(!player.codeError){
            const int rate=44100,toneSamples=3528,gapSamples=1764;
            var samples=new float[toneSamples*2+gapSamples];
            for(int tone=0;tone<2;tone++){
                int start=tone*(toneSamples+gapSamples);
                float frequency=tone==0?880:740;
                for(int i=0;i<toneSamples;i++){
                    float t=i/(float)rate;
                    float envelope=Mathf.Min(1,Mathf.Min(i,toneSamples-1-i)/(rate*.006f));
                    samples[start+i]=Mathf.Sin(2*Mathf.PI*frequency*t)*envelope*.25f;
                }
            }
            player.codeError=AudioClip.Create("Code error double beep",samples.Length,1,rate,false);
            player.codeError.SetData(samples,0);
        }
        Play(player.codeError);
    }
    public static void ProductSold()
    {
        var player=Instance;
        // Batch sales and simultaneous smith completions share one coin cue.
        double now=Time.unscaledTimeAsDouble;
        if(now<player.nextSaleSound)return;
        player.nextSaleSound=now+.15;
        if(!player.sale){
            const int rate=44100;
            var samples=new float[rate*3/10];
            for(int i=0;i<samples.Length;i++){
                float t=i/(float)rate,sum=0;
                for(int note=0;note<2;note++){
                    float age=t-note*.065f;if(age<0)continue;
                    float frequency=note==0?1046.5f:1568f;
                    float envelope=Mathf.Min(1,age/.003f)*Mathf.Exp(-age*25);
                    sum+=(Mathf.Sin(2*Mathf.PI*frequency*age)+.22f*Mathf.Sin(2*Mathf.PI*frequency*2.76f*age))*envelope*.2f;
                }
                samples[i]=sum*Mathf.Clamp01((samples.Length-1-i)/(rate*.02f));
            }
            player.sale=AudioClip.Create("Sword sale coins",samples.Length,1,rate,false);
            player.sale.SetData(samples,0);
        }
        Play(player.sale);
    }
    public static void PurchaseClick()=>Click();
    public static bool Button(Rect rect,GUIContent content,GUIStyle style){bool clicked=GUI.Button(rect,content,style);if(clicked)Click();return clicked;}
    public static bool LayoutButton(string text,params GUILayoutOption[] options)=>LayoutButton(text,GUI.skin.button,options);
    public static bool LayoutButton(string text,GUIStyle style,params GUILayoutOption[] options){bool clicked=GUILayout.Button(text,style,options);if(clicked)Click();return clicked;}
    public static void Preview()=>PlayPreview(true);
    public static void MaterialPurchased()=>PlayPreview(false);
    static void PlayPreview(bool stopPrevious)
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
        if(stopPrevious)player.source.Stop();
        Play(player.preview);
    }
    void OnDestroy(){if(instance==this)instance=null;if(preview)Destroy(preview);if(codeError)Destroy(codeError);if(sale)Destroy(sale);}
}
