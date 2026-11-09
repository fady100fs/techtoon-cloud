namespace Techtoon.Core.Finance;

/// <summary>
/// P0-4 Batch 1: مصدر وحيد لحساب أرصدة الصناديق والأطراف.
/// يوحّد الصيغ الثلاث المتضاربة (UI FinancialService / API CashBoxAppService / CashBoxesViewModel).
/// هذه الدوال pure (بدون DB) وقابلة للاختبار.
/// </summary>
public static class BalanceCalculator
{
    public static decimal CashBoxBalance(
        decimal opening,
        decimal receipts,
        decimal payments,
        decimal expenses,
        decimal transfersIn,
        decimal transfersOut)
        => opening + receipts - payments - expenses + transfersIn - transfersOut;

    /// <summary>
    /// P1.1: رصيد الطرف — يُطابق PartyAppService:
    /// opening + receipts - payments + sales - purchases
    /// (كل فاتورة/دفعة تتحرك واحدًا؛ بدون ازدواج).
    /// </summary>
    public static decimal PartyBalance(decimal opening, decimal receipts, decimal payments, decimal sales, decimal purchases)
        => opening + receipts - payments + sales - purchases;

    public static decimal InvoiceDue(decimal total, decimal paid)
        => Math.Max(0m, total - paid);

    public static bool IsOverdue(DateTime dueDate, DateTime today, bool isPaid)
        => !isPaid && dueDate < today;
}
