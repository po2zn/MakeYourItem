using UnityEngine;using UnityEngine.UI;
// Same installed-font selection as the original IMGUI frontend. Serialized fonts remain the portable fallback.
public sealed class ForgeLegacyUIFont:MonoBehaviour {
 static Font shared;
 public static void Apply(Transform root){if(!shared)shared=Font.CreateDynamicFontFromOSFont(new[]{"Consolas","Malgun Gothic"},18);if(!shared)return;foreach(var text in root.GetComponentsInChildren<Text>(true))text.font=shared;}
 void Awake(){Apply(transform);}
}
