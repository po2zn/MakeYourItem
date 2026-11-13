using UnityEngine;
public partial class ForgeMvp {
 [SerializeField,HideInInspector] bool frostRevealSeen;
 public bool TryBeginFrostReveal(){if(frostRevealSeen)return false;frostRevealSeen=true;SaveProgress();return true;}
 void ResetFrostRevealProgress(){frostRevealSeen=false;foreach(var motion in smithMotions)if(motion)motion.ResetFrostStation();if(craftMotion&&!smithMotions.Contains(craftMotion))craftMotion.ResetFrostStation();}
}
