using UnityEngine;
using UnityEngine.UI;
public sealed class ForgeSfxButton:MonoBehaviour {
 void Awake(){var button=GetComponent<Button>();if(button)button.onClick.AddListener(()=>{if(!GetComponent<ForgeTreeCard>())ForgeSoundEffects.Click();});}
 public static void Bind(Component root){foreach(var button in root.GetComponentsInChildren<Button>(true))if(!button.GetComponent<ForgeSfxButton>())button.gameObject.AddComponent<ForgeSfxButton>();}
}
