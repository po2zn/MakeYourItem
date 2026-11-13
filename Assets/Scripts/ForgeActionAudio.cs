using UnityEngine;
// One voice set per smith; normalized cues follow the same animation clock.
public sealed class ForgeActionAudio:MonoBehaviour {
 AudioSource effects,walking,cooling;ForgeMvp.Step step;float previous=-1;
 AudioClip destructionClip;
 public void PlayDestruction(){Stop();if(!destructionClip){const int rate=44100;var data=new float[rate/2];uint seed=719;for(int i=0;i<data.Length;i++){float t=i/(float)rate;seed=seed*1664525u+1013904223u;float noise=(seed>>8)/(float)0xffffff*2-1;float attack=Mathf.Min(1,t/.0015f);data[i]=attack*(noise*.45f*Mathf.Exp(-t*28)+Mathf.Sin(2*Mathf.PI*730*t)*.2f*Mathf.Exp(-t*13)+Mathf.Sin(2*Mathf.PI*1730*t)*.13f*Mathf.Exp(-t*18))*Mathf.Clamp01((.5f-t)/.025f);}destructionClip=AudioClip.Create("Item shatter",data.Length,1,rate,false);destructionClip.SetData(data,0);}UpdatePosition();Volume();effects.PlayOneShot(destructionClip,.85f);}
 void OnDestroy(){if(destructionClip)Destroy(destructionClip);}
 Transform emitter,smith;
 public void Follow(Transform target){smith=target;UpdatePosition();}
 void UpdatePosition(){if(emitter)emitter.position=(smith?smith.position:transform.position)+Vector3.up;}
 void LateUpdate(){UpdatePosition();}
 AudioSource Voice(){if(!emitter){emitter=new GameObject("Smith sound source").transform;emitter.SetParent(transform,false);}var s=emitter.gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=1;s.rolloffMode=AudioRolloffMode.Linear;s.minDistance=3;s.maxDistance=40;s.dopplerLevel=0;s.spread=0;s.pitch=1;s.volume=ForgeAudioSettings.Sfx;return s;}
 void Awake(){effects=Voice();walking=Voice();cooling=Voice();walking.loop=cooling.loop=true;}
 void OnEnable(){ForgeAudioSettings.Changed+=Volume;}
 void OnDisable(){ForgeAudioSettings.Changed-=Volume;Stop();}
 void Volume(){if(effects)effects.volume=ForgeAudioSettings.Sfx;if(walking)walking.volume=.45f*ForgeAudioSettings.Sfx;if(cooling)cooling.volume=.7f*ForgeAudioSettings.Sfx;}
 public void Begin(ForgeMvp.Step value){walking.Stop();cooling.Stop();step=value;previous=-1;Volume();}
 void One(AudioClip clip){var lib=ForgeSfxLibrary.Instance;if(lib){clip=lib.AtSpeed(clip,step.workSpeed);if(clip)effects.PlayOneShot(clip,.75f);}}
 void Loop(AudioSource voice,AudioClip clip,bool active){if(!active){voice.Stop();return;}if(voice.isPlaying)return;var lib=ForgeSfxLibrary.Instance;if(!lib)return;voice.clip=lib.AtSpeed(clip,step.workSpeed);if(voice.clip)voice.Play();}
 public const float SealContactTime=.82f,FrostDropTime=.52f,HandleContactTime=.6f;
 public static bool IsFrost(ForgeMvp.Step receipt)=>receipt!=null&&receipt.process==4&&receipt.consumed!=null&&receipt.consumed.TryGetValue(8,out var amount)&&amount>0;
 public static bool Crossed(float previous,float current,float cue)=>previous<cue&&current>=cue;
 public static int HammerHits(float previous,float current)=>(Crossed(previous,current,.7f)?1:0)+(Crossed(previous,current,1f)?1:0);
 // Two back-and-forth polishing cycles produce four strokes from t=.4 to t=1.
 public static int PolishStrokes(float previous,float current)=>(Crossed(previous,current,.4f)?1:0)+(Crossed(previous,current,.55f)?1:0)+(Crossed(previous,current,.7f)?1:0)+(Crossed(previous,current,.85f)?1:0);
 bool Cross(float t,float cue)=>Crossed(previous,t,cue);
 public void Frame(float t,bool moving){
  UpdatePosition();
  if(step==null)return;var lib=ForgeSfxLibrary.Instance;if(!lib)return;
  Loop(walking,lib.walk,moving);
  if(step.process==1&&Cross(t,.4f))One(lib.place);
  if(step.process==8&&Cross(t,.64f))One(lib.coal);
  if(step.process==3)for(int hit=0;hit<HammerHits(previous,t);hit++)One(lib.hammer);
  if(step.process==5&&Cross(t,HandleContactTime))One(lib.handle);
  if(step.process==777&&Cross(t,.48f))One(lib.sparkle);
  if(step.process==201&&Cross(t,SealContactTime))One(lib.seal);
  if(step.process==6)for(int stroke=0;stroke<PolishStrokes(previous,t);stroke++)One(lib.polish);
  bool wet=step.consumed!=null&&(step.consumed.ContainsKey(3)||step.consumed.ContainsKey(8));
  bool frost=IsFrost(step);
  if(frost&&Cross(t,FrostDropTime))One(lib.frostDrop);
  Loop(cooling,frost&&lib.frostCooling?lib.frostCooling:wet?lib.water:lib.air,step.process==4&&t>=(wet?.58f:.4f)&&t<.9f);
  previous=t;
 }
 public void Finish(){Frame(1,false);walking.Stop();cooling.Stop();step=null;}
 public void AbortIfActive(){if(step!=null)Stop();}
 public void Stop(){if(effects)effects.Stop();if(walking)walking.Stop();if(cooling)cooling.Stop();step=null;}
}
