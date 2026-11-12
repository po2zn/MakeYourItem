using UnityEngine;
// One voice set per smith; normalized cues follow the same animation clock.
public sealed class ForgeActionAudio:MonoBehaviour {
 AudioSource effects,walking,cooling;ForgeMvp.Step step;float previous=-1;
 AudioSource Voice(){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.pitch=1;s.volume=ForgeAudioSettings.Sfx;return s;}
 void Awake(){effects=Voice();walking=Voice();cooling=Voice();walking.loop=cooling.loop=true;}
 void OnEnable(){ForgeAudioSettings.Changed+=Volume;}
 void OnDisable(){ForgeAudioSettings.Changed-=Volume;Stop();}
 void Volume(){if(effects)effects.volume=ForgeAudioSettings.Sfx;if(walking)walking.volume=.45f*ForgeAudioSettings.Sfx;if(cooling)cooling.volume=.7f*ForgeAudioSettings.Sfx;}
 public void Begin(ForgeMvp.Step value){walking.Stop();cooling.Stop();step=value;previous=-1;Volume();}
 void One(AudioClip clip){var lib=ForgeSfxLibrary.Instance;if(lib){clip=lib.AtSpeed(clip,step.workSpeed);if(clip)effects.PlayOneShot(clip,.75f);}}
 void Loop(AudioSource voice,AudioClip clip,bool active){if(!active){voice.Stop();return;}if(voice.isPlaying)return;var lib=ForgeSfxLibrary.Instance;if(!lib)return;voice.clip=lib.AtSpeed(clip,step.workSpeed);if(voice.clip)voice.Play();}
 public static bool Crossed(float previous,float current,float cue)=>previous<cue&&current>=cue;
 public static int HammerHits(float previous,float current)=>(Crossed(previous,current,.7f)?1:0)+(Crossed(previous,current,1f)?1:0);
 bool Cross(float t,float cue)=>Crossed(previous,t,cue);
 public void Frame(float t,bool moving){
  if(step==null)return;var lib=ForgeSfxLibrary.Instance;if(!lib)return;
  Loop(walking,lib.walk,moving);
  if(step.process==1&&Cross(t,.4f))One(lib.place);
  if(step.process==8&&Cross(t,.64f))One(lib.coal);
  if(step.process==3)for(int hit=0;hit<HammerHits(previous,t);hit++)One(lib.hammer);
  bool wet=step.consumed!=null&&(step.consumed.ContainsKey(3)||step.consumed.ContainsKey(8));
  Loop(cooling,wet?lib.water:lib.air,step.process==4&&t>=(wet?.58f:.4f)&&t<.9f);
  previous=t;
 }
 public void Finish(){Frame(1,false);walking.Stop();cooling.Stop();step=null;}
 public void AbortIfActive(){if(step!=null)Stop();}
 public void Stop(){if(effects)effects.Stop();if(walking)walking.Stop();if(cooling)cooling.Stop();step=null;}
}
