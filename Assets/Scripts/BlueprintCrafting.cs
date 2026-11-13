using System;
using System.Collections.Generic;

public sealed partial class BlueprintRuntime
{
    double Now => Math.Max(context.Seconds(), startedAt+productionSeconds);
    void EnsureLive(ForgeMvp.Piece p){if(p.data.dragonBone?.Pending==true)Fail("용뼈 절단 작업이 아직 완료되지 않았습니다.");if(p.data.destroyed||p.data.recovered)Fail("파괴되거나 회수된 작업물은 사용할 수 없습니다.");}
    void ColdBlade(ForgeMvp.Piece p){EnsureLive(p);if((p.material!=1&&p.material!=7)||!p.shaped||p.heat!=0)Fail("성형하고 식힌 철 또는 별철 검날이 필요합니다.");}
    void Consume(int material,int count=1)
    {
        if(material==0||count==0)return;
        if(material==1){if(iron<count)Fail("철 부족");if(!ledger.TrySpend(0,count,ref gold,ref iron))Fail("철 부족");}
        else if(stocks!=null){if(!stocks.TryGetValue(material,out long amount)||amount<count)Fail(ForgeMaterialGuide.Find(material).Name+" 재료 부족");stocks[material]-=count;}
        usedMaterials[material]+=count;
        pendingMaterials.TryGetValue(material,out int before);pendingMaterials[material]=before+count;
    }
    void Action(int id,ForgeMvp.Piece p,string text,float seconds=.8f)
    {
        Record(id,p,0,0,text,false);var step=result.steps[result.steps.Count-1];
        productionSeconds-=step.duration;step.duration=seconds/Math.Max(1,context.Speed);productionSeconds+=step.duration;step.productionSeconds=productionSeconds;
    }
    double StrengthAt(ForgeMvp.Piece p,double time)=>Math.Max(0,p.data.strength-Math.Max(0,time-p.data.lastStrike-1));
    double Strength(ForgeMvp.Piece p)=>StrengthAt(p,Now);
    void Cool(ForgeMvp.Piece p,int coolant)
    {
        if(coolant!=0&&coolant!=3&&coolant!=8)Fail("냉각 재료는 공기(0), 물(3), 서리수(8)입니다.");
        if(coolant==8){CoolingCall(BlueprintFeature.CoolingRun,new List<object>{p,8,CoolingGraph(p).DefaultPath()});return;}
        Consume(coolant);ApplyCooling(p,coolant);
    }
    void ApplyCooling(ForgeMvp.Piece p,int coolant)
    {
        int before=p.heat;
        if(p.material==7&&p.shaped&&before==ForgeBalance.DefaultFurnaceTemperature)p.data.star=true;
        if(coolant==3&&p.shaped&&before>=ForgeBalance.MinimumForgeTemperature&&p.data.lastHammerTemperature==before){
            p.data.hardness=7;
            p.data.perfectQuench=p.data.idealTemperature>0&&before==p.data.idealTemperature;
        }
        if(coolant==8){if(before>=100&&before<=900)p.data.frost=true;else p.data.cracked=true;}
        if(p.data.magic)p.data.magicGrade=(int)Strength(p);
        p.heat=0;if(coolant==0)p.traits|=ForgeTraits.AirCooled;else p.traits&=~ForgeTraits.AirCooled;
        Action(4,p,ForgeMaterialGuide.Find(coolant).Name+" 냉각 · "+before+"°C → 0 · 경도 "+p.data.hardness+(coolant==3&&p.data.hardness<7?" · 경고: 가열 후 850°C 이상에서 타격해야 담금질됩니다.":p.data.perfectQuench?" · 정밀 담금질 +100G":""));
    }
    int RuneNumber(object value){int n=Int(value);if(n<1||n>9)Fail("룬 번호는 1~9입니다.");return n;}
    object Extended(BlueprintFeature feature,List<object> args)
    {
        switch(feature){
        case BlueprintFeature.Append:
            if(!(args[0] is List<int>))Fail("목록이 필요합니다.");var list=(List<int>)args[0];int value=Int(args[1]);if(list.Count>=CollectionLimit)Fail("목록 크기 제한");Allocate(1);list.Add(value);return null;
        case BlueprintFeature.Pop:
            if(!(args[0] is List<int>))Fail("목록이 필요합니다.");var stack=(List<int>)args[0];if(stack.Count==0)Fail("빈 목록에서 꺼낼 수 없습니다.");int last=stack[stack.Count-1];stack.RemoveAt(stack.Count-1);return last;
        case BlueprintFeature.SetAdd:
            if(!(args[0] is HashSet<int>))Fail("집합이 필요합니다.");var set=(HashSet<int>)args[0];int item=Int(args[1]);if(!set.Contains(item)){if(set.Count>=CollectionLimit)Fail("집합 크기 제한");Allocate(1);set.Add(item);}return null;
        case BlueprintFeature.LuckyCode:return context.LuckyCode;
        case BlueprintFeature.RuneList:return new List<int>{1,2,3,4,5,6,7,8,9};
        case BlueprintFeature.RuneFamily:return (RuneNumber(args[0])-1)%3+1;
        case BlueprintFeature.RuneGrade:return (RuneNumber(args[0])-1)/3+1;
        case BlueprintFeature.Fuel:
            int temperature=Int(args[0]);if(temperature<100||temperature>1200)Fail("화로 온도는 100~1200°C의 정수입니다.");
            Consume(4);furnaceTemperature=temperature;
            float fuelDuration=2.4f/Math.Max(1,context.Speed);productionSeconds+=fuelDuration;
            result.steps.Add(new ForgeMvp.Step{line=currentLine,module=currentModule.IsMain?null:currentModule.Name,day=context.Day(),minute=(int)(clockHour()*60),process=8,heat=temperature,text="화로 설정 · "+temperature+"°C · 석탄 1개",consumed=new Dictionary<int,int>(pendingMaterials),duration=fuelDuration,workSpeed=context.Speed,productionSeconds=productionSeconds});
            pendingMaterials.Clear();return null;
        case BlueprintFeature.Wait:
            int wait=Int(args[0]);if(wait<0||wait>30)Fail("대기는 0~30초입니다.");float duration=wait/Math.Max(1,context.Speed);productionSeconds+=duration;
            result.steps.Add(new ForgeMvp.Step{line=currentLine,module=currentModule.IsMain?null:currentModule.Name,process=-1,duration=duration,productionSeconds=productionSeconds,text="대기 "+duration+"초"});return null;
        case BlueprintFeature.Speed:case BlueprintFeature.AutoSell:
            int setting=Int(args[0]);if(feature==BlueprintFeature.Speed?(setting<1||setting>5):(setting<0||setting>1))Fail("가속은 1~5, 자동판매는 0 또는 1입니다.");
            if(feature==BlueprintFeature.Speed&&moduleTechnology!=null&&setting>moduleTechnology.MaxSpeedLevel)
                Fail("테크 트리에서 스피드 "+setting+"단계를 먼저 구매하세요. 현재 해금: "+moduleTechnology.MaxSpeedLevel+"단계");
            if(feature==BlueprintFeature.Speed)context.Speed=ForgeTechnology.SpeedMultiplier(setting);
            result.steps.Add(new ForgeMvp.Step{line=currentLine,module=currentModule.IsMain?null:currentModule.Name,process=-2,setting=feature==BlueprintFeature.Speed?1:2,settingValue=setting,duration=0,text=feature==BlueprintFeature.Speed?"가속 "+ForgeTechnology.SpeedMultiplier(setting)+"배":"자동판매 "+(setting==1?"켜짐":"꺼짐")});return null;
        }
        int absorbed=0;
        var p=Piece(args[feature==BlueprintFeature.Lucky?1:0]);
        if(feature!=BlueprintFeature.Recover)EnsureLive(p);
        var d=p.data;
        switch(feature){
        case BlueprintFeature.Inspect:
            switch(Int(args[1])){case 0:return p.material;case 1:return d.hardness;case 2:return d.handle;case 3:return d.cracked?1:0;case 4:return d.polished?1:0;case 5:return d.charge;case 6:return d.activated?1:0;case 7:return d.magicGrade;default:Fail("상태 항목은 0~7입니다.");break;}break;
        case BlueprintFeature.Attach:
            ColdBlade(p);
            if(args[1] is int material){
                if(!ForgeMaterialGuide.IsHandleMaterial(material))Fail("손잡이 재료 번호는 "+ForgeMaterialGuide.HandleMaterials+"입니다. 용뼈는 정제한 작업물을 전달하세요.");
                if(d.handle!=0)Fail("이미 "+ForgeMaterialGuide.Find(d.handle).Name+" 손잡이가 있습니다.");
                Consume(material);d.handle=material;
            }
            else {if(!(args[1] is ForgeMvp.Piece))Fail("5(Item, Material)의 Material에는 정수 재료 번호를 넣으세요.");var part=Piece(args[1]);EnsureLive(part);if(ReferenceEquals(part,p))Fail("자기 자신을 장착할 수 없습니다.");
                if(part.material==9&&part.data.refined&&d.handle==0){d.handle=9;part.data.dragonBone?.Use();d.dragonBone=part.data.dragonBone?.Clone();}
                else if(part.material==6&&part.data.activated&&part.heat==0&&!d.activated)d.activated=true;
                else Fail("정제 용뼈 또는 식힌 공명 수정이 필요하며 같은 부품을 중복 장착할 수 없습니다.");part.data.recovered=true;absorbed=part.id;}
            Action(5,p,absorbed>0&&d.activated?"공명 수정 부품 장착":"손잡이 제작 · "+ForgeMaterialGuide.Find(d.handle).Name+" · 0G");result.steps[result.steps.Count-1].absorbedPiece=absorbed;break;
        case BlueprintFeature.Polish:ColdBlade(p);d.polished=true;Action(6,p,"검날 연마 완료");break;
        case BlueprintFeature.Plate:ColdBlade(p);if(d.plated)Fail("이미 도금되었습니다.");Consume(5);if(d.polished)d.plated=true;Action(7,p,d.polished?"은도금 완료":"경고: 연마하지 않아 은이 얼룩짐 · 보너스 없음");break;
        case BlueprintFeature.Charge:
            ColdBlade(p);if(d.handle==0)Fail("손잡이가 필요합니다.");int power=Int(args[1]);if(power<1||power>3)Fail("주입 세기는 1~3입니다.");if(d.charge>=3)Fail("이미 충전 완료되었습니다.");if(d.charge==0)Consume(6);if(power!=1)d.cracked=true;else d.charge++;Action(9,p,"마력 주입 · "+d.charge+"/3"+(power!=1?" · 강한 주입으로 균열":""));break;
        case BlueprintFeature.Refine:
            var blade=Piece(args[1]);ColdBlade(blade);if(p.material!=9||d.refined||!blade.data.polished||blade.data.cracked||Int(args[2])!=100||Int(args[3])!=6)Fail("미정제 용뼈, 연마된 무균열 검날, 수온 100, 설정값 6이 필요합니다.");// Validate both ingredients before consuming either one.
            if(stocks!=null){foreach(int ingredient in new[]{3,6})if(!stocks.TryGetValue(ingredient,out long amount)||amount<1)Fail("용뼈 정제에는 물 1개와 마력 수정 1개가 필요합니다. "+ForgeMaterialGuide.Find(ingredient).Name+" 재료 부족");}
            Consume(3);Consume(6);d.refined=true;Action(10,p,"용뼈 정제 완료",3);break;
        case BlueprintFeature.Lucky:
            ColdBlade(p);if(d.handle==0||d.cracked)Fail("손잡이가 있는 무균열 완성 검이 필요합니다.");if(Int(args[0])!=context.LuckyCode)Fail("오늘의 행운 코드가 아닙니다. 777?()로 다시 조회하세요.");
            if(d.luckyDay==0){d.luckyDay=context.Day();d.luckyCode=Int(args[0]);Action(777,p,"Day "+d.luckyDay+" 행운 적용 · 코드 "+d.luckyCode,1.6f);}return p;
        case BlueprintFeature.Protect:if(d.protectedItem)Fail("이미 보호 중입니다.");Consume(6);d.protectedItem=true;Action(667,p,"저주 파괴 보호 적용");break;
        case BlueprintFeature.Curse:
            if(d.cursed)Fail("저주는 한 번만 적용할 수 있습니다.");randomState=unchecked(randomState*1664525u+1013904223u);bool broken=randomState%4==0;d.cursed=true;
            if(broken){if(d.protectedItem)d.protectedItem=false;else d.destroyed=true;}Action(666,p,d.destroyed?"저주 실패 · 작업물 파괴":"저주 적용 · 판매가 "+ForgeBalance.CurseMultiplier+"배");break;
        case BlueprintFeature.Recover:
            if(d.recovered)Fail("이미 회수한 작업물입니다.");d.recovered=true;if(p.material>0)usedMaterials[p.material]=Math.Max(0,usedMaterials[p.material]-1);Action(404,p,"바탕 재료 회수 · 추가 부품과 소모 재료는 회수하지 않습니다.");result.steps[result.steps.Count-1].refund=p.material;
            if(p.material==1)iron++;else if(p.material>1&&stocks!=null){stocks.TryGetValue(p.material,out long amount);stocks[p.material]=amount+1;}break;
        case BlueprintFeature.Wrap:
            ColdBlade(p);int seal=Int(args[1]);if(seal<1||seal>3||d.seals.Count>=6)Fail("봉인 종류는 1~3, 최대 6층입니다.");Consume(seal==1?4:seal==2?3:2);d.seals.Add(seal);Action(201,p,"봉인 추가 · "+seal+" · 남은 층 "+d.seals.Count);break;
        case BlueprintFeature.Unwrap:
            int method=Int(args[1]);if(method<1||method>3||d.seals.Count==0)Fail("봉인 해제 종류는 1~3이며 남은 봉인이 필요합니다.");int top=d.seals[d.seals.Count-1];bool correct=Math.Abs(top)==method;
            if(correct){d.seals.RemoveAt(d.seals.Count-1);if(top>0)d.sealBonus=Math.Min(6,d.sealBonus+1);}else d.seals[d.seals.Count-1]=-Math.Abs(top);
            Action(method==1?2:method==2?4:3,p,correct?"봉인 해제 · 남은 층 "+d.seals.Count:"경고: 잘못된 해제 · 해당 층 보너스 손실");break;
        case BlueprintFeature.SealCount:return d.seals.Count;
        case BlueprintFeature.Rune:
            ColdBlade(p);int rune=RuneNumber(args[1]);if(d.runes.Count>=3)Fail("룬 슬롯이 가득 찼습니다.");foreach(int existing in d.runes)if((existing-1)%3==(rune-1)%3)Fail("같은 계열의 룬은 중복 각인할 수 없습니다.");Consume(6);d.runes.Add(rune);Action(211,p,"룬 "+rune+" 각인 · 남은 슬롯 "+(3-d.runes.Count));break;
        case BlueprintFeature.Slots:return 3-d.runes.Count;
        case BlueprintFeature.Magic:if(d.magic)Fail("이미 마력 단조가 활성화되었습니다.");Consume(6);d.magic=true;Action(401,p,"마력 단조 활성화");break;
        case BlueprintFeature.Strength:return (int)Strength(p);
        case BlueprintFeature.Resonate:
            if(p.material!=6)Fail("수정 작업물이 필요합니다.");if(d.resonanceTarget<0)d.resonanceTarget=((int)(((uint)context.Seed+(uint)p.id*17)%9)+1)*100;
            int comparison=p.heat.CompareTo(d.resonanceTarget);if(comparison==0)d.activated=true;Action(501,p,"공명 검사 · "+comparison);return comparison;
        default:Fail("확장 공정이 없습니다.");break;
        }
        return null;
    }
}
