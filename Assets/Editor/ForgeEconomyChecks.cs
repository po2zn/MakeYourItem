using System;
using System.Text;

public static class ForgeEconomyChecks
{
    public const string Basic = "Material = 1\nBlade = 1(Material)\n2(Blade, 7)\n3(Blade)\n4(Blade, 0)\n!Blade";
    public const string ExtraHeat = "Material = 1\nBlade = 1(Material)\n2(Blade, 7)\n2(Blade, 7)\n3(Blade)\n4(Blade, 0)\n!Blade";
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static string Run()
    {
        var log = new StringBuilder();
        int passed = 0;
        Action<string, Action> check = (name, test) => { test(); passed++; log.AppendLine("PASS " + name); };
        var baseline = ForgeMvp.Evaluate(Basic, 100, 10, "basic");
        var extra = ForgeMvp.Evaluate(ExtraHeat, 100, 10, "extra");
        check("basic: iron 1 / cost 12 / base 28 / bonus 0 / profit 16", () => {
            Require(baseline.error == null && baseline.product != null, "basic completion");
            var q = ForgePricing.Quote(baseline.product);
            Require(baseline.product.IronConsumed == 1 && q.CashCost == 12 && q.BasePrice == 28 && q.TraitBonus == 0 && q.SalePrice == 28 && q.Profit == 16, "basic quote");
            Require(baseline.product.FinalHeat == 0 && baseline.product.Shaped && baseline.product.Traits == ForgeTraits.AirCooled, "final state");
        });
        check("extra successful heat: cost 14 / same state / sale 28 / profit 14", () => {
            var q = ForgePricing.Quote(extra.product);
            Require(q.CashCost == 14 && q.SalePrice == 28 && q.Profit == 14 && extra.product.Traits == baseline.product.Traits, "extra heat");
        });
        check("preview versus committed ledger and balances", () => {
            foreach (var e in new[] { baseline, extra }) {
                int gold = 100, iron = 10;
                var ledger = new ForgeProductionLedger(); ForgeProduct completed = null;
                foreach (var step in e.steps) {
                    Require(ledger.TrySpend(step.cost, step.iron, ref gold, ref iron), "commit");
                    if (step.finished) completed = ledger.Complete("actual", step.heat, step.shaped, step.traits);
                }
                Require(completed != null && gold == 100 - e.cost && iron == 9, "balances");
                Require(ForgePricing.Quote(completed).Profit == ForgePricing.Quote(e.product).Profit && completed.CashCost == e.product.CashCost && completed.IronConsumed == e.product.IronConsumed, "preview parity");
            }
        });
        check("preview is isolated from existing shop and caller balances", () => {
            var shop = new ForgeShop(); shop.Add(baseline.product);
            int gold = 100, iron = 10;
            var a = ForgeMvp.Evaluate(ExtraHeat, gold, iron);
            var b = ForgeMvp.Evaluate(Basic, gold, iron);
            Require(gold == 100 && iron == 10 && shop.Count == 1 && a.product.CashCost == 14 && b.product.CashCost == 12, "isolation");
        });
        check("completion fails while hot: no product, previous cost remains 12", () => {
            var e = ForgeMvp.Evaluate(Basic.Replace("4(Blade, 0)\n", ""), 100, 10);
            Require(e.error != null && e.product == null && e.cost == 12 && e.ironConsumed == 1, "hot finish");
        });
        check("completion fails unshaped: no product", () => {
            var e = ForgeMvp.Evaluate("Blade = 1(1)\n4(Blade, 0)\n!Blade", 100, 10);
            Require(e.error != null && e.product == null && e.cost == 10, "unshaped finish");
        });
        check("failed strike does not add a step or completion", () => {
            var e = ForgeMvp.Evaluate(Basic.Replace("2(Blade, 7)", "2(Blade, 5)"), 100, 10);
            Require(e.product == null && e.error != null && e.cost == 12 && !e.steps.Exists(s => s.process == 3), "strike failure");
        });
        check("insufficient funds: only earlier successful cost 10", () => {
            var e = ForgeMvp.Evaluate(Basic, 11, 10);
            Require(e.product == null && e.error != null && e.cost == 10 && e.ironConsumed == 1 && !e.compileError, "funds");
        });
        check("no iron: no money or material consumed", () => {
            var e = ForgeMvp.Evaluate(Basic, 100, 0);
            Require(e.product == null && e.error != null && e.cost == 0 && e.ironConsumed == 0, "stock");
        });
        check("compile failure: whole run has zero cost", () => {
            var e = ForgeMvp.Evaluate(Basic.Replace("3(Blade)", "3(Blade, 1)"), 100, 10);
            Require(e.compileError && e.steps.Count == 0 && e.cost == 0 && e.product == null, "compile failure");
        });
        check("sell same ID repeatedly: pay exactly 28 once", () => {
            var shop = new ForgeShop(); int id = shop.Add(baseline.product); int gold = 88;
            Require(shop.TrySell(id, ref gold, out var sold), "first sale");
            for (int i = 0; i < 10; i++) Require(!shop.TrySell(id, ref gold, out sold), "duplicate sale");
            Require(gold == 116 && shop.Count == 0, "sale balances");
        });
        check("select second product: retain first cost and identity", () => {
            var shop = new ForgeShop(); int first = shop.Add(baseline.product), second = shop.Add(extra.product); int gold = 74;
            Require(shop.TrySell(second, ref gold, out var sold) && sold.CashCost == 14 && gold == 102, "second sale");
            Require(shop.Count == 1 && shop.Find(first).Product.CashCost == 12, "first retained");
        });
        check("failed spend changes neither ledger nor balances", () => {
            int gold = 1, iron = 10; var ledger = new ForgeProductionLedger();
            Require(!ledger.TrySpend(2, 0, ref gold, ref iron) && gold == 1 && iron == 10 && ledger.CashCost == 0 && ledger.IronConsumed == 0, "atomic spend");
        });
        check("immutable record is unaffected by later ledger spending", () => {
            int gold = 100, iron = 10; var ledger = new ForgeProductionLedger();
            ledger.TrySpend(12, 1, ref gold, ref iron);
            var item = ledger.Complete("snapshot", 0, true, ForgeTraits.AirCooled);
            ledger.TrySpend(2, 0, ref gold, ref iron);
            Require(item.CashCost == 12 && ForgePricing.Quote(item).Profit == 16, "snapshot");
        });
        log.AppendLine("TOTAL " + passed + " passed");
        return log.ToString();
    }
    public static void Main() { Console.WriteLine(Run()); }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Shop Economy")]
    public static void ValidateInEditor()
    {
        try {
            UnityEngine.Debug.Log(Run());
        } catch (Exception ex) { UnityEngine.Debug.LogException(ex); throw; }
    }
#endif
}
