using UnityEngine;
using UnityEngine.Rendering;

// Presentation only: the station remains usable while its model is assembled.
public sealed class ForgeFrostStationReveal : MonoBehaviour
{
 public const float BuildSeconds=1.4f;
 Transform model;Vector3 position,scale;Quaternion rotation;float elapsed;int knocks;bool noticed,ready;
 Material outline;Renderer[] outlines;AudioSource sound;
 public bool Building=>ready&&elapsed<BuildSeconds;
 public bool Attention=>ready&&!noticed;
 public void Initialize(){
  if(ready)return;model=transform.Find("Imported model");if(!model)return;
  position=model.localPosition;scale=model.localScale;rotation=model.localRotation;
  var shader=Resources.Load<Shader>("ForgeFrostOutline");
  if(shader){outline=new Material(shader);outline.SetColor("_Color",Color.yellow);var list=new System.Collections.Generic.List<Renderer>();
   foreach(var mesh in model.GetComponentsInChildren<MeshFilter>()){
    var source=mesh.GetComponent<MeshRenderer>();if(!source||!mesh.sharedMesh)continue;
    var obj=new GameObject("Yellow hover outline",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(mesh.transform,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
    var renderer=obj.GetComponent<MeshRenderer>();var materials=new Material[mesh.sharedMesh.subMeshCount];for(int i=0;i<materials.Length;i++)materials[i]=outline;renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;list.Add(renderer);
   }outlines=list.ToArray();
  }
  sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;sound.loop=false;ApplyVolume();ready=true;Pose(0);
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
   int next=Mathf.Min(3,Mathf.FloorToInt(elapsed/.38f));if(next>knocks){knocks=next;var lib=ForgeSfxLibrary.Instance;if(lib&&lib.hammer){sound.clip=lib.hammer;sound.Play();}}
  }
  SetOutline(!noticed);
  if(outline)outline.SetColor("_Color",new Color(1,1,0,.35f+.65f*(.5f+.5f*Mathf.Sin(Time.unscaledTime*4))));
 }
 void SetOutline(bool visible){if(outlines!=null)foreach(var r in outlines)if(r)r.enabled=visible;}
 void OnDisable(){ForgeAudioSettings.Changed-=ApplyVolume;if(sound)sound.Stop();if(ready){elapsed=BuildSeconds;Pose(1);}SetOutline(false);}
 void OnDestroy(){if(outline)Destroy(outline);}
}
