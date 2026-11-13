using UnityEngine;
using UnityEngine.Rendering;

// Presentation only: the station remains usable while its model is assembled.
public sealed class ForgeFrostStationReveal : MonoBehaviour
{
 public const float BuildSeconds=1.4f;
 Transform model;Vector3 position,scale;Quaternion rotation;float elapsed;bool noticed,ready;
 Material outline;Renderer[] outlines;AudioSource sound;AudioClip hum;
 public bool Building=>ready&&elapsed<BuildSeconds;
 public bool Attention=>ready&&!noticed;
 public void Initialize(bool playReveal=true){
  if(ready)return;model=transform.Find("Imported model");if(!model)return;
  position=model.localPosition;scale=model.localScale;rotation=model.localRotation;
  if(!playReveal){elapsed=BuildSeconds;noticed=true;ready=true;Pose(1);return;}
  var shader=Resources.Load<Shader>("ForgeFrostOutline");
  if(shader){outline=new Material(shader);outline.SetColor("_Color",Color.yellow);var list=new System.Collections.Generic.List<Renderer>();
   foreach(var mesh in model.GetComponentsInChildren<MeshFilter>()){
    var source=mesh.GetComponent<MeshRenderer>();if(!source||!mesh.sharedMesh)continue;
    var obj=new GameObject("Yellow hover outline",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(mesh.transform,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
    var renderer=obj.GetComponent<MeshRenderer>();var materials=new Material[mesh.sharedMesh.subMeshCount];for(int i=0;i<materials.Length;i++)materials[i]=outline;renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;list.Add(renderer);
   }outlines=list.ToArray();
  }
  sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;sound.loop=false;ApplyVolume();ready=true;Pose(0);
  var samples=HumSamples(44100);hum=AudioClip.Create("Frost station arrival hum",samples.Length,1,44100,false);hum.SetData(samples,0);sound.clip=hum;sound.Play();
 }
 public void Acknowledge(){noticed=true;SetOutline(false);}
 void OnEnable(){ForgeAudioSettings.Changed+=ApplyVolume;}
 void ApplyVolume(){if(sound)sound.volume=ForgeAudioSettings.Sfx*.35f;}
 void Pose(float t){
  float grow=Mathf.SmoothStep(0,1,t);float bounce=Mathf.Abs(Mathf.Sin(t*Mathf.PI*3))*(1-t);
  model.localScale=Vector3.Scale(scale,new Vector3(.7f+.3f*grow,.12f+.88f*grow,.7f+.3f*grow));
  model.localPosition=position+Vector3.up*(bounce*.12f);
  model.localRotation=rotation*Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*6)*(1-t)*3);
 }
 void Update(){
  if(!ready)return;
  if(Building){elapsed=Mathf.Min(BuildSeconds,elapsed+Time.deltaTime);Pose(elapsed/BuildSeconds);

  }
  SetOutline(!noticed);
  if(outline)outline.SetColor("_Color",new Color(1,1,0,.35f+.65f*(.5f+.5f*Mathf.Sin(Time.unscaledTime*4))));
 }
 void SetOutline(bool visible){if(outlines!=null)foreach(var r in outlines)if(r)r.enabled=visible;}
 void OnDisable(){ForgeAudioSettings.Changed-=ApplyVolume;if(sound)sound.Stop();if(ready){elapsed=BuildSeconds;Pose(1);}SetOutline(false);}
 void OnDestroy(){if(outline)Destroy(outline);if(hum)Destroy(hum);}
 public static float[] HumSamples(int rate){
  if(rate<8000)throw new System.ArgumentOutOfRangeException(nameof(rate));
  var samples=new float[Mathf.RoundToInt(rate*BuildSeconds)];double phase=0;
  for(int i=0;i<samples.Length;i++){double t=(double)i/rate,u=t/BuildSeconds;phase+=2*System.Math.PI*(90+40*System.Math.Sin(u*System.Math.PI*.5))/rate;double fade=System.Math.Min(1,t/.18)*System.Math.Min(1,(samples.Length-1-i)/(rate*.3));fade=fade*fade*(3-2*fade);samples[i]=(float)((System.Math.Sin(phase)*.48+System.Math.Sin(phase*2.003)*.16+System.Math.Sin(phase*3.01)*.07)*fade*(.85+.15*System.Math.Sin(t*9)));}
  return samples;
 }
}
