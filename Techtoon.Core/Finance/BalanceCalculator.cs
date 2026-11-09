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
    /// رصيد الطرف: محاسبة ذمة — مشتريات (مهما) - مبيعات؟ لا: الذمة = مشتريات غير مدفوعة - مبيعات غير محصّلة + افتتاحي
    /// هنا نُبقي القاعدة الحالية (مشتريات فقط) صراحةً لتجنّب تغيير سلوك دون مراجعة.
    /// </summary>
    public static decimal PartyBalance(decimal opening, decimal purchaseUnpaid, decimal salesUnpaid)
        => opening + purchaseUnpaid - salesUnpaid;

    public static decimal InvoiceDue(decimal total, decimal paid)
        => Math.Max(0m, total - paid);

    public static bool IsOverdue(DateTime dueDate, DateTime today, bool isPaid)
        => !isPaid && dueDate < today;
}
