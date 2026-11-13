using UnityEngine;
using UnityEngine.Rendering;

// Cosmetic only: resources, expiry and effects are owned by saved rune state.
public sealed class ForgeGiantRuneArrival : MonoBehaviour
{
    public const float DropSeconds=1.5f,BounceSeconds=.3f;
    public bool Playing {get;private set;}
    Vector3 floor;float elapsed;GameObject shadow;Material shadowMaterial;
    public void Begin(Vector3 position)
    {
        floor=position;elapsed=0;Playing=true;
        if(!shadow){
            shadow=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shadow.name="거대한 룬 착지 그림자";shadow.transform.SetParent(transform.parent,false);
            var collider=shadow.GetComponent<Collider>();if(collider){collider.enabled=false;Release(collider);}
            shadowMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color"));
            shadowMaterial.SetFloat("_Surface",1);shadowMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);shadowMaterial.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);shadowMaterial.SetFloat("_ZWrite",0);
            shadowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");shadowMaterial.renderQueue=3000;
            var renderer=shadow.GetComponent<Renderer>();renderer.sharedMaterial=shadowMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        shadow.SetActive(true);Sample(0);
    }
    public static float HeightAt(float time)
    {
        if(time<DropSeconds){float t=Mathf.Clamp01(time/DropSeconds);return 7*(1-t*t);}
        return time<DropSeconds+BounceSeconds?.18f*Mathf.Sin((time-DropSeconds)/BounceSeconds*Mathf.PI):0;
    }
    public void Sample(float seconds)
    {
        elapsed=Mathf.Max(0,seconds);transform.localPosition=floor+Vector3.up*HeightAt(elapsed);
        if(shadow){float t=Mathf.Clamp01(elapsed/DropSeconds);shadow.transform.localPosition=floor-Vector3.up*.008f;float spread=Mathf.Lerp(1.5f,1,t);shadow.transform.localScale=new Vector3(2.25f*spread,.003f,2.25f*spread);
            var color=new Color(.025f,.02f,.04f,Mathf.Lerp(.08f,.5f,t));shadowMaterial.color=color;if(shadowMaterial.HasProperty("_BaseColor"))shadowMaterial.SetColor("_BaseColor",color);}
        if(elapsed>=DropSeconds+BounceSeconds){Playing=false;transform.localPosition=floor;if(shadow)shadow.SetActive(false);}
    }
    void Update(){if(Playing)Sample(elapsed+Time.deltaTime);}
    void OnDisable(){Playing=false;if(shadow)shadow.SetActive(false);}
    void OnDestroy(){Release(shadow);Release(shadowMaterial);}
    static void Release(Object value){if(!value)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
}
