using UnityEngine;

// One continuous backing floor, below the existing workshop tiles to avoid z-fighting.
public sealed class ForgeRuneGround : MonoBehaviour {
 public const float ExpandSeconds=.45f;
 Vector4 current,from,target;float elapsed;
 Renderer surface;MaterialPropertyBlock properties;
 public Vector4 Extents=>current;
 public void Initialize(Material material,Vector4 bounds){
  surface=GetComponent<Renderer>();surface.sharedMaterial=material;properties=new MaterialPropertyBlock();current=from=target=bounds;Apply();
 }
 public void Fit(Vector4 bounds,bool immediate){
  if(immediate){current=from=target=bounds;elapsed=ExpandSeconds;Apply();return;}
  if((target-bounds).sqrMagnitude<.000001f)return;
  from=current;target=bounds;elapsed=0;
 }
 public void Advance(float seconds){elapsed=Mathf.Min(ExpandSeconds,elapsed+seconds);current=Vector4.Lerp(from,target,Mathf.SmoothStep(0,1,elapsed/ExpandSeconds));Apply();}
 void Update(){if(surface&&elapsed<ExpandSeconds)Advance(Time.deltaTime);}
 void Apply(){float width=Mathf.Max(.01f,current.z-current.x),depth=Mathf.Max(.01f,current.w-current.y);transform.localPosition=new Vector3((current.x+current.z)*.5f,-.075f,(current.y+current.w)*.5f);transform.localScale=new Vector3(width,.12f,depth);
  properties.SetVector("_BaseMap_ST",new Vector4(width/4.35f,depth/4.35f,current.x/4.35f,current.y/4.35f));surface.SetPropertyBlock(properties);
 }
}
public partial class ForgeMvp {
 ForgeRuneGround runeGround;
 void UpdateGiantRuneGround(){
  if(giantRuneViews.Count==0){if(runeGround){ReleaseRuneObject(runeGround.gameObject);runeGround=null;}return;}
  if(!runeWorkshopAssets)runeWorkshopAssets=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");
  if(!runeWorkshopAssets||!runeWorkshopAssets.floor)return;
  var floorRenderer=runeWorkshopAssets.floor.GetComponentInChildren<Renderer>();if(!floorRenderer)return;
  var bounds=new Vector4(.225f,-1.075f,4.575f,3.275f);
  for(int i=1;i<blacksmiths.smiths.Count;i++){float x=ForgeBlacksmiths.X(i)*4.5f,z=ForgeBlacksmiths.Y(i)*4.5f;bounds.z=Mathf.Max(bounds.z,x+4.575f);bounds.w=Mathf.Max(bounds.w,z+3.275f);}
  if(!runeGround){var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name="거대한 룬 확장 바닥";obj.transform.SetParent(transform,false);var collider=obj.GetComponent<Collider>();collider.enabled=false;ReleaseRuneObject(collider);runeGround=obj.AddComponent<ForgeRuneGround>();runeGround.Initialize(floorRenderer.sharedMaterial,bounds);}
  bool arriving=false;
  foreach(var view in giantRuneViews){var arrival=view.GetComponent<ForgeGiantRuneArrival>();arriving|=arrival&&arrival.Playing;
   // Include both the effect area and the imported model's actual silhouette.
   var point=view.transform.localPosition;bounds.x=Mathf.Min(bounds.x,point.x-1.3f);bounds.y=Mathf.Min(bounds.y,point.z-1.3f);bounds.z=Mathf.Max(bounds.z,point.x+1.3f);bounds.w=Mathf.Max(bounds.w,point.z+1.3f);
   foreach(var renderer in view.GetComponentsInChildren<Renderer>()){if(!renderer.enabled)continue;var b=renderer.bounds;var lo=transform.InverseTransformPoint(b.min);var hi=transform.InverseTransformPoint(b.max);bounds.x=Mathf.Min(bounds.x,lo.x-.15f);bounds.y=Mathf.Min(bounds.y,lo.z-.15f);bounds.z=Mathf.Max(bounds.z,hi.x+.15f);bounds.w=Mathf.Max(bounds.w,hi.z+.15f);}
  }
  runeGround.Fit(bounds,!arriving);
 }
}
