using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum ForgeProductKind { IronBlade, Unfinished }
[Flags]
public enum ForgeTraits { None = 0, AirCooled = 1 }

// Immutable completion snapshot; prices never depend on the source text or extra heating.
public sealed class ForgeProduct
{
    public ForgeProductKind Kind { get; }
    public ForgeTraits Traits { get; }
    public int FinalHeat { get; }
    public bool Shaped { get; }
    public int CashCost { get; }
    public int IronConsumed { get; }
    public string BlueprintName { get; }
    public int Material { get; }
    public string DisplayName => Kind == ForgeProductKind.IronBlade ? "철 검날" : ForgeMaterialGuide.Find(Material).Name + (Shaped ? " 미완성품" : " 원재료");
    public string StateText => (Shaped ? "성형 완료" : "미성형") + " · 열 " + FinalHeat + ((Traits & ForgeTraits.AirCooled) != 0 ? " · 공기 냉각" : "");

    public ForgeProduct(string blueprintName, int finalHeat, bool shaped, ForgeTraits traits, int cashCost, int ironConsumed, int material = 1)
    {
        if (ForgeMaterialGuide.Find(material) == null) throw new ArgumentException("등록되지 않은 재료입니다.");
        if (cashCost < 0 || ironConsumed < 0) throw new ArgumentOutOfRangeException(nameof(cashCost));
        Material = material;
        Kind = material == 1 && shaped && finalHeat == 0 ? ForgeProductKind.IronBlade : ForgeProductKind.Unfinished;
        BlueprintName = blueprintName ?? "";
        FinalHeat = finalHeat; Shaped = shaped; Traits = traits;
        CashCost = cashCost; IronConsumed = ironConsumed;
    }
}

// Used only after a successful resource check; preview owns a separate instance and balances.
public sealed class ForgeProductionLedger
{
    public int CashCost { get; private set; }
    public int IronConsumed { get; private set; }

    public bool TrySpend(int cost, int iron, ref int gold, ref int stock)
    {
        if (cost < 0 || iron < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        if (gold < cost || stock < iron) return false;
        int nextCost = checked(CashCost + cost);
        int nextIron = checked(IronConsumed + iron);
        gold -= cost; stock -= iron;
        CashCost = nextCost; IronConsumed = nextIron;
        return true;
    }

    public ForgeProduct Complete(string owner, int heat, bool shaped, ForgeTraits traits, int material = 1)
        => new ForgeProduct(owner, heat, shaped, traits, CashCost, IronConsumed, material);
}

public sealed class ForgeQuote
{
    public int BasePrice { get; }
    public int TraitBonus { get; }
    public int SalePrice => checked(BasePrice + TraitBonus);
    public int CashCost { get; }
    public int Profit => SalePrice - CashCost;
    internal ForgeQuote(int basePrice, int bonus, int cost)
    { BasePrice = basePrice; TraitBonus = bonus; CashCost = cost; }
}

public static class ForgePricing
{
    public static ForgeQuote Quote(ForgeProduct product)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));

        if ((product.Traits & ~ForgeTraits.AirCooled) != 0) throw new ArgumentException("미지원 완성품 특성");
        // Future quenching/handle bonuses belong here, keyed by traits earned by successful processes.
        // Air cooling has no bonus. Never infer a bonus from cost, heat calls or blueprint names.
        return new ForgeQuote(product.Kind == ForgeProductKind.IronBlade ? 28 : 0, 0, product.CashCost);
    }

    public static string Describe(ForgeProduct product, bool preview)
    {
        var quote = Quote(product);
        string prefix = preview ? "예상 " : "";
        return product.DisplayName + " · " + product.StateText
            + "\n기본가 " + quote.BasePrice + "G · 특성 보너스 +" + quote.TraitBonus + "G"
            + "\n" + prefix + "판매가 " + quote.SalePrice + "G"
            + "\n" + prefix + "제작비 " + quote.CashCost + "G · " + prefix + "순이익 " + quote.Profit + "G"
            + "\n" + (preview ? "소비 예정 재료" : "소비 재료") + " · 철 " + product.IronConsumed + "개";
    }
}

public sealed class ForgeShop
{
    public sealed class Entry
    {
        public int Id { get; }
        public ForgeProduct Product { get; }
        internal Entry(int id, ForgeProduct product) { Id = id; Product = product; }
    }
    readonly List<Entry> entries = new List<Entry>();
    readonly ReadOnlyCollection<Entry> view;
    int nextId = 1;
    public ForgeShop() { view = entries.AsReadOnly(); }
    public IReadOnlyList<Entry> Items => view;
    public int Count => entries.Count;
    public int Add(ForgeProduct product)
    {
        ForgePricing.Quote(product); // Reject incomplete/unpriceable records before listing.
        int id = nextId++;
        entries.Add(new Entry(id, product));
        return id;
    }
    public Entry Find(int id) => entries.Find(entry => entry.Id == id);

    public bool TrySell(int id, ref int gold, out ForgeProduct sold)
    {
        sold = null;
        int index = entries.FindIndex(entry => entry.Id == id);
        if (index < 0) return false;
        var product = entries[index].Product;
        int newGold = checked(gold + ForgePricing.Quote(product).SalePrice);
        entries.RemoveAt(index); // This ID cannot be paid again, including repeated UI events.
        gold = newGold;
        sold = product;
        return true;
    }
}
