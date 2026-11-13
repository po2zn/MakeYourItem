using System;
using UnityEngine;
[Serializable] public sealed class ForgeDragonSettings {
 public int length=20; public float secondsPerUnit=.1f; public int bonusGold=200;
 public void Validate(){if(length<8||length>10000||float.IsNaN(secondsPerUnit)||float.IsInfinity(secondsPerUnit)||secondsPerUnit<0||bonusGold<0)throw new ArgumentException("용뼈 설정 오류: 길이 8~10000, 길이당 시간과 보너스는 0 이상이어야 합니다.");}
}
[Serializable] public sealed class ForgeDragonBone {
 [SerializeField] int length; [SerializeField] int[] positions; [SerializeField] bool[] cuts;
 [SerializeField] int cost,bonus; [SerializeField] float unitSeconds,seconds;
 [SerializeField] bool qualified,used;
 [NonSerialized] bool pending;
 public int Length=>length; public int Count=>positions.Length;public int Cost=>cost;public float Seconds=>seconds;public bool Qualified=>qualified;public bool Used=>used;public bool Pending=>pending; public int Bonus=>qualified?bonus:0;
 public int Completed {get{int n=0;foreach(bool c in cuts)if(c)n++;return n;}}
 public bool Complete=>Completed==Count;
 public string State=>used?"최종 아이템에 사용됨":qualified?"가열 조건 충족: 판매 시 용뼈 보너스 적용":Complete?"절단 완료: 1000도 이상으로 가열해주세요.":"필수 마디 절단 "+Completed+" / "+Count;
 public ForgeDragonBone(int total,int[] joints,float time=.1f,int gold=200){if(joints==null||joints.Length<3||joints.Length>7||total<joints.Length+1||time<0||float.IsNaN(time)||float.IsInfinity(time)||gold<0)throw new ArgumentException("용뼈 생성 설정이 올바르지 않습니다.");length=total;positions=(int[])joints.Clone();Array.Sort(positions);for(int i=0;i<positions.Length;i++)if(positions[i]<=0||positions[i]>=total||i>0&&positions[i]==positions[i-1])throw new ArgumentException("마디는 끝점을 제외한 서로 다른 정수 위치여야 합니다.");cuts=new bool[positions.Length];unitSeconds=time;bonus=gold;}
 public static ForgeDragonBone Create(uint seed,ForgeDragonSettings settings){settings.Validate();var r=new System.Random(unchecked((int)seed));int n=r.Next(3,8);var pool=new int[settings.length-1];for(int i=0;i<pool.Length;i++)pool[i]=i+1;for(int i=0;i<n;i++){int j=r.Next(i,pool.Length);int t=pool[i];pool[i]=pool[j];pool[j]=t;}var joints=new int[n];Array.Copy(pool,joints,n);return new ForgeDragonBone(settings.length,joints,settings.secondsPerUnit,settings.bonusGold);}
 // Unity inline serialization can materialize an absent optional object without its constructor.
 public ForgeDragonBone Clone(){if(positions==null||cuts==null||positions.Length<3||positions.Length>7||cuts.Length!=positions.Length||length<positions.Length+1)return null;for(int i=0;i<positions.Length;i++)if(positions[i]<=0||positions[i]>=length||i>0&&positions[i]<=positions[i-1])return null;var b=(ForgeDragonBone)MemberwiseClone();b.positions=(int[])positions.Clone();b.cuts=(bool[])cuts.Clone();b.pending=false;return b;}
 public int Position(int index){if(index<0||index>=Count)throw new ArgumentException("존재하지 않는 마디 번호. 유효 번호는 0~"+(Count-1)+"입니다.");return positions[index];}
 int Index(int position){int i=Array.BinarySearch(positions,position);if(i<0)throw new ArgumentException("필수 마디가 아닌 절단 위치입니다: "+position);return i;}
 public bool IsCut(int position)=>cuts[Index(position)];
 public int CutLength(int position,out int left,out int right){int i=Index(position);if(used||pending)throw new ArgumentException("소모되었거나 작업 중인 용뼈입니다.");if(cuts[i])throw new ArgumentException("이미 절단한 위치입니다: "+position);left=0;right=length;for(int j=0;j<Count;j++)if(cuts[j]){if(positions[j]<position)left=positions[j];else {right=positions[j];break;}}return right-left;}
 public float Duration(int length,float speed)=>length*unitSeconds/Math.Max(1,speed);
 public Action BeginCut(int position,float speed,out Action cancel){int left,right;int amount=CutLength(position,out left,out right);int i=Index(position);pending=true;bool active=true;cancel=()=>{if(!active)return;active=false;pending=false;};return ()=>{if(!active)return;active=false;pending=false;cuts[i]=true;cost+=amount;seconds+=Duration(amount,speed);};}
 public void Heated(int temperature){if(!used&&!pending&&Complete&&temperature>=1000)qualified=true;}
 public void Use(){if(pending||used)throw new ArgumentException("작업 중이거나 이미 사용한 용뼈입니다.");used=true;}
 public static bool IsFeature(BlueprintFeature f)=>f>=BlueprintFeature.BoneLength&&f<=BlueprintFeature.BoneCost;
}
