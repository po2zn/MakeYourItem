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
        new Entry(1,"철","현재 사용 가능","검날의 바탕이 되는 재료입니다. 작업대에 올리면 새 작업물이 만들어집니다.\n소비: 철 1개 (골드 소비 없음)\n사용법: Blade = 1(1)\n손잡이 제작: 5(Item, 1) · 철 1개 추가 소비 · 판매가 +2G"),
        new Entry(2,"나무","기본 공급 · 재고 구매 필요","5(Item, 2)로 나무 1개를 사용해 손잡이를 만듭니다. 판매가 보너스는 +22G입니다. 봉인 201(검날,3)의 재료로도 사용합니다."),
        new Entry(3,"물","기본 공급 · 재고 구매 필요","4(검날,3)에 사용하는 냉각용 재료입니다. 사용 직전 작업물의 상태에 따라 담금질 효과가 달라지는 규칙입니다."),
        new Entry(4,"석탄","8() 화로와 묶음 해금 · 재고 구매 필요","8(온도)로 화로를 100~1200°C로 설정할 때 석탄 1개를 사용합니다. 이후 2(작업물)로 검을 가열합니다. 201(작업물,1)의 열 봉인에도 사용합니다."),
        new Entry(5,"은","전용 가공 지원 · 해금 및 재료 필요","5(Item, 5)로 은 1개를 사용해 손잡이를 만들 수 있습니다. 판매가 보너스는 +57G입니다. 식힌 검날을 연마한 뒤 도금하는 재료이기도 합니다. 연마 전에 도금하면 은을 소비하고 얼룩이 남는 규칙입니다."),
        new Entry(6,"마력 수정","전용 가공 지원 · 해금 및 재료 필요","마력 가공에 사용하는 재료입니다. 재료 상점에서 개당 30G로 구매합니다. 충전·룬 각인·마력 단조 등에 사용하며, 충전 방법과 이익은 9() 마력 충전 안내를 확인하세요."),
        new Entry(7,"별철","전용 가공 지원 · 해금 및 재료 필요","칼날에 사용하는 재료입니다. 숨은 별빛 5개(0~9)를 70~73으로 가공합니다.\n0을 제거하고, 처음 있던 나머지 숫자를 종류별로 하나씩 남겨 오름차순으로 정렬하면 판매가 +120G입니다. 유효 숫자 종류를 잃거나 빈 배열이 되면 보너스가 없습니다.\n교환·제거를 줄이면 가공 시간이 짧아집니다. 기존 별무늬 효과와 손잡이 사용은 유지됩니다."),
        new Entry(8,"서리수","전용 가공 지원 · 해금 및 재료 필요",ForgeCoolingGuide.Basic),
        new Entry(9,"용뼈","절단 · 가열 · 손잡이 · 판매 보너스",ForgeDragonGuide.Basic)
    });
}
