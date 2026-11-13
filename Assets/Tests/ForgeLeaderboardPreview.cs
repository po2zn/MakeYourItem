#if UNITY_EDITOR
using UnityEngine;
public sealed class ForgeLeaderboardPreview : MonoBehaviour {public System.Action Draw;void OnGUI(){Draw?.Invoke();}}
#endif
