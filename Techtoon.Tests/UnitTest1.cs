using Techtoon.Core.Finance;
using Techtoon.Core.Inventory;

namespace Techtoon.Tests;

public class BalanceCalculatorTests
{
    [Fact]
    public void CashBoxBalance_opening_plus_in_minus_out()
    {
        var b = BalanceCalculator.CashBoxBalance(
            opening: 1000m, receipts: 200m, payments: 50m,
            expenses: 30m, transfersIn: 100m, transfersOut: 20m);
        Assert.Equal(1200m, b); // 1000+200-50-30+100-20
    }

    [Fact]
    public void PartyBalance_matches_api_formula()
    {
        var b = BalanceCalculator.PartyBalance(
            opening: 100m, receipts: 10m, payments: 5m, sales: 200m, purchases: 50m);
        Assert.Equal(255m, b); // 100+10-5+200-50
    }

    [Fact]
    public void InvoiceDue_never_negative()
    {
        Assert.Equal(0m, BalanceCalculator.InvoiceDue(100m, 150m));
        Assert.Equal(30m, BalanceCalculator.InvoiceDue(100m, 70m));
    }

    [Fact]
    public void IsOverdue_only_when_unpaid_and_past_due()
    {
        Assert.True(BalanceCalculator.IsOverdue(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), isPaid: false));
        Assert.False(BalanceCalculator.IsOverdue(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), isPaid: true));
    }
}

public class InventorySignConventionTests
{
    [Theory]
    [InlineData("in", 5, 5)]
    [InlineData("opening", -7, 7)]
    [InlineData("out", 5, -5)]
    [InlineData("transfer_out", -3, -3)]
    [InlineData("adjustment", -4, -4)]
    [InlineData("adjustment", 4, 4)]
    [InlineData("sale_out", 5, 0)] // legacy unknown type -> 0 (P1.4 fixed writer to use out)
    [InlineData("purchase_in", 5, 0)]
    public void SignedQuantity_cases(string type, decimal qty, decimal expected)
    {
        Assert.Equal(expected, InventorySignConvention.SignedQuantity(type, qty));
    }

    [Fact]
    public void SignedQuantitySql_contains_cases()
    {
        var sql = InventorySignConvention.SignedQuantitySql("im");
        Assert.Contains("WHEN im.type IN ('in','opening','transfer_in')", sql);
        Assert.Contains("WHEN im.type IN ('out','transfer_out')", sql);
    }
}
