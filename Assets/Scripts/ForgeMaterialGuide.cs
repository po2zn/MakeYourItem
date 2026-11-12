using System;
using System.Collections.ObjectModel;

// Shared material registry. Workbench support does not automatically unlock stock or recipes.
public static class ForgeMaterialGuide
{
    public static bool IsHandleMaterial(int material) => material==1 || material==2 || material==5 || material==7;
    public const string HandleMaterials = "철(1), 나무(2), 은(5), 별철(7)";
    public static Entry Find(int number) => number>=0&&number<Entries.Count?Entries[number]:null;
    public sealed class Entry
    {
        public readonly int Number;
        public readonly string Name,Status,Description;
        public Entry(int number,string name,string status,string description)
        {Number=number;Name=name;Status=status;Description=description;}
        public string Heading => Number+" · "+Name;
    }
    public static readonly ReadOnlyCollection<Entry> Entries=Array.AsReadOnly(new[]{
        new Entry(0,"공기","현재 사용 가능","공기 자체를 작업대에 올릴 수 있으며 재고는 소비하지 않습니다. 1(0)은 0G입니다.\n냉각에 사용하면 작업물의 열을 0으로 내립니다.\n비용: 0G\n사용법: 4(Blade, 0)"),
        new Entry(1,"철","현재 사용 가능","검날의 바탕이 되는 재료입니다. 작업대에 올리면 새 작업물이 만들어집니다.\n소비: 철 1개 (골드 소비 없음)\n사용법: Blade = 1(1)\n손잡이 제작: 5(Item, 1) · 철 1개 추가 소비"),
        new Entry(2,"나무","기본 공급 · 재고 구매 필요","5(Item, 2)로 나무 1개를 사용해 손잡이를 만듭니다. 봉인 201(검날,3)의 재료로도 사용합니다."),
        new Entry(3,"물","기본 공급 · 재고 구매 필요","4(검날,3)에 사용하는 냉각용 재료입니다. 사용 직전 작업물의 상태에 따라 담금질 효과가 달라지는 규칙입니다."),
        new Entry(4,"석탄","8() 화로와 묶음 해금 · 재고 구매 필요","8(온도)로 화로를 100~1200°C로 설정할 때 석탄 1개를 사용합니다. 이후 2(작업물)로 검을 가열합니다. 201(작업물,1)의 열 봉인에도 사용합니다."),
        new Entry(5,"은","전용 가공 지원 · 해금 및 재료 필요","5(Item, 5)로 은 1개를 사용해 손잡이를 만들 수 있습니다. 식힌 검날을 연마한 뒤 도금하는 재료이기도 합니다. 연마 전에 도금하면 은을 소비하고 얼룩이 남는 규칙입니다."),
        new Entry(6,"마력 수정","전용 가공 지원 · 해금 및 재료 필요","손잡이가 있는 식은 검에 9(검,1)을 세 번 호출해 충전합니다. 수정은 첫 충전에 1개 소비되며 강한 세기 2·3은 균열을 만듭니다. 룬 각인·마력 단조·파괴 보호·거대한 룬에도 각각 필요한 수정 재고를 준비하세요."),
        new Entry(7,"별철","전용 가공 지원 · 해금 및 재료 필요","5(Item, 7)로 별철 1개를 사용해 손잡이를 만들 수 있습니다. 성형한 검날을 정확히 900°C에서 냉각하면 별무늬가 생깁니다. 다른 온도에서는 별무늬가 생기지 않습니다."),
        new Entry(8,"서리수","전용 가공 지원 · 해금 및 재료 필요","100~900°C의 검날을 식히면 서리 문양을 얻습니다. 이 범위 밖에서는 균열이 생깁니다. 물 담금질 보너스와는 별개입니다."),
        new Entry(9,"용뼈","전용 가공 지원 · 해금 및 재료 필요","b=1(9)로 용뼈를 올린 뒤 10(b,검날,100,13)으로 정제합니다. 식고 연마된 무균열 검날과 물 1개가 필요하며 3초가 걸립니다. 정제한 작업물을 5(검날,b)로 장착합니다. 재료 번호 9를 5에 직접 넣을 수는 없습니다.")
    });
}
