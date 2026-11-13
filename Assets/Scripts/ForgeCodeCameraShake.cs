using UnityEngine;
using UnityEngine.Rendering;
// Apply a sub-pixel-scale camera nudge only during rendering, then restore the exact pose.
public sealed class ForgeCodeCameraShake:MonoBehaviour
{
 Camera target;float began=-10;Vector3 savedPosition;bool displaced;
 public static void Pulse(Camera camera){if(!camera)return;var effect=camera.GetComponent<ForgeCodeCameraShake>()??camera.gameObject.AddComponent<ForgeCodeCameraShake>();effect.target=camera;effect.began=Time.unscaledTime;}
 public static Vector2 PixelOffset(float age)=>age>=0&&age<.14f?new Vector2(Mathf.Sin(age*105),Mathf.Cos(age*85))*(1.1f*Mathf.Exp(-age*24)):Vector2.zero;
 void OnEnable(){RenderPipelineManager.beginCameraRendering+=Begin;RenderPipelineManager.endCameraRendering+=End;ForgeCodePreferences.Changed+=SettingChanged;}
 void Begin(ScriptableRenderContext context,Camera camera){if(camera!=target||!ForgeCodePreferences.ForgeCode||displaced)return;var offset=PixelOffset(Time.unscaledTime-began);if(offset==Vector2.zero)return;float unit=camera.orthographic?2*camera.orthographicSize/Mathf.Max(1,camera.pixelHeight):.002f;savedPosition=camera.transform.position;camera.transform.position+=unit*(camera.transform.right*offset.x+camera.transform.up*offset.y);displaced=true;}
 void End(ScriptableRenderContext context,Camera camera){if(camera==target)Restore();}
 void Restore(){if(displaced&&target)target.transform.position=savedPosition;displaced=false;}
 void SettingChanged(){if(!ForgeCodePreferences.ForgeCode){began=-10;Restore();}}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.endCameraRendering-=End;ForgeCodePreferences.Changed-=SettingChanged;Restore();}
}
