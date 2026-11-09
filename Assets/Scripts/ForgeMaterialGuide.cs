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
        new Entry(2,"나무","작업대 배치 지원 · 해금 및 보유 필요","5(Item, 2)로 나무 1개를 사용해 손잡이를 만듭니다. 봉인 201(검날,3)의 재료로도 사용합니다."),
        new Entry(3,"물","작업대 배치 지원 · 해금 및 보유 필요","4(검날,3)에 사용하는 냉각용 재료입니다. 사용 직전 작업물의 상태에 따라 담금질 효과가 달라지는 규칙입니다."),
        new Entry(4,"석탄","전용 가공 지원 · 해금 및 재료 필요","화로 연료입니다. 1개당 열 단계가 2 오르며, 목표보다 2단계 이상 높으면 과열로 표면 품질이 떨어지는 규칙입니다."),
        new Entry(5,"은","전용 가공 지원 · 해금 및 재료 필요","5(Item, 5)로 은 1개를 사용해 손잡이를 만들 수 있습니다. 식힌 검날을 연마한 뒤 도금하는 재료이기도 합니다. 연마 전에 도금하면 은을 소비하고 얼룩이 남는 규칙입니다."),
        new Entry(6,"마력 수정","전용 가공 지원 · 해금 및 재료 필요","식은 손잡이에 끼운 뒤 약한 마력을 3회 주입하는 재료입니다. 강하게 한 번 주입하면 금이 가며, 충전하면 마력 전도 특성을 얻는 규칙입니다."),
        new Entry(7,"별철","전용 가공 지원 · 해금 및 재료 필요","5(Item, 7)로 별철 1개를 사용해 손잡이를 만들 수 있습니다. 검날의 열 단계가 정확히 7일 때 냉각을 시작하면 별무늬가 생기는 재료입니다. 6이나 8에서 냉각하면 일반 별철 검날이 되는 규칙입니다."),
        new Entry(8,"서리수","전용 가공 지원 · 해금 및 재료 필요","검날과 서리수의 열 단계 차이가 3 이하이면 서리 문양을 얻는 냉각 재료입니다. 차이가 크면 균열이 생기는 규칙입니다."),
        new Entry(9,"용뼈","전용 가공 지원 · 해금 및 재료 필요","완벽하게 가공한 검날을 매개체로 100°C의 물에서 3초 동안 정제하는 재료입니다. 정제 후 손잡이로 사용하고 마력 수정과 조합하는 규칙입니다.")
    });
}
