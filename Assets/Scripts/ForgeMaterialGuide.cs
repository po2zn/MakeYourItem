using System;
using System.Collections.ObjectModel;

// GameDesign (2).pdf pp. 2–3 and 6. Planned materials are not runtime unlocks.
public static class ForgeMaterialGuide
{
    public sealed class Entry
    {
        public readonly int Number;
        public readonly string Name,Status,Description;
        public Entry(int number,string name,string status,string description)
        {Number=number;Name=name;Status=status;Description=description;}
        public string Heading => Number+" · "+Name;
    }
    public static readonly ReadOnlyCollection<Entry> Entries=Array.AsReadOnly(new[]{
        new Entry(0,"공기","현재 사용 가능","냉각에 사용하는 재료입니다. 작업물의 열을 0으로 내립니다.\n비용: 0G\n사용법: 4(Blade, 0)"),
        new Entry(1,"철","현재 사용 가능","검날의 바탕이 되는 재료입니다. 작업대에 올리면 새 작업물이 만들어집니다.\n소비: 철 1개 + 10G\n사용법: Blade = 1(1)"),
        new Entry(2,"나무","기획 단계 · 현재 사용 불가","손잡이 부품이나 화로의 연료로 사용하는 재료입니다."),
        new Entry(3,"물","기획 단계 · 현재 사용 불가","냉각용 재료입니다. 사용 직전 작업물의 상태에 따라 담금질 효과가 달라지는 방향입니다."),
        new Entry(4,"석탄","후속 후보 · 현재 사용 불가","화로 연료 후보입니다. 1개당 열 단계가 2 오르며, 목표보다 2단계 이상 높으면 과열로 표면 품질이 떨어지는 안입니다."),
        new Entry(5,"은","후속 후보 · 현재 사용 불가","식힌 검날을 연마한 뒤 도금하는 재료 후보입니다. 연마 전에 도금하면 은을 소비하고 얼룩이 남는 안입니다."),
        new Entry(6,"마력 수정","후속 후보 · 현재 사용 불가","식은 손잡이에 끼운 뒤 약한 마력을 3회 주입하는 재료 후보입니다. 강하게 한 번 주입하면 금이 가며, 충전하면 마력 전도 특성을 얻는 안입니다."),
        new Entry(7,"별철","후속 후보 · 현재 사용 불가","검날의 열 단계가 정확히 7일 때 냉각을 시작하면 별무늬가 생기는 재료 후보입니다. 6이나 8에서 냉각하면 일반 별철 검날이 되는 안입니다."),
        new Entry(8,"서리수","후속 후보 · 현재 사용 불가","검날과 서리수의 열 단계 차이가 3 이하이면 서리 문양을 얻는 냉각 재료 후보입니다. 차이가 크면 균열이 생기는 안입니다."),
        new Entry(9,"용뼈","후속 후보 · 현재 사용 불가","완벽하게 가공한 검날을 매개체로 100°C의 물에서 3초 동안 정제하는 재료 후보입니다. 정제 후 손잡이로 사용하고 마력 수정과 조합하는 안입니다.")
    });
}
