using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Techtoon.UI.Services;

/// <summary>
/// تعبئة البيانات المرجعية عند أول تشغيل على قاعدة بيانات جديدة:
/// دليل الحسابات الأساسي، العملات، الخزائن. بدونها لا يستطيع المستخدم إنشاء أي فاتورة.
/// آمن للتشغيل المتكرر (لا يُكرّر البيانات).
/// </summary>
public static class SeedDataService
{
    private static readonly (string Code, string Name, string Type)[] ChartOfAccounts =
    {
        ("1000", "Cash", "ASSET"),
        ("1100", "Bank", "ASSET"),
        ("2100", "BankLoans", "LIABILITY"),
        ("2101", "LoansPayable", "LIABILITY"),
        ("4100", "Capital", "EQUITY"),
        ("5100", "Sales", "REVENUE"),
        ("6100", "Expenses", "EXPENSE"),
        ("6200", "InterestExpense", "EXPENSE"),
    };

    private static readonly (string Code, string Name, string Symbol, bool IsDefault)[] Currencies =
    {
        ("EGP", "EGP", "EGP", true),
        ("USD", "USD", "$", false),
        ("SAR", "SAR", "SAR", false),
    };

    /// <summary>نتيجة التعبئة.</summary>
    public class SeedResult
    {
        public bool Created { get; set; }
        public int Accounts { get; set; }
        public int Currencies { get; set; }
        public int CashBoxes { get; set; }
        public string Message => Created
            ? $"تمت تعبئة البيانات المرجعية (حسابات: {Accounts}، عملات: {Currencies}، خزائن: {CashBoxes})."
            : "البيانات المرجعية موجودة بالفعل.";
    }

    /// <summary>يملأ البيانات المرجعية إن كانت القاعدة فارغة.</summary>
    public static async Task<SeedResult> SeedIfEmptyAsync(string connectionString)
    {
        var result = new SeedResult();
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            // هل القاعدة فارغة من دليل الحسابات؟
            int existingAccounts;
            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM accounts", conn))
                existingAccounts = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            if (existingAccounts > 0) return result;

            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // 1) العملات
                foreach (var c in Currencies)
                {
                    // code ليس له قيد فريد في المخطط — نتحقق بالاستعلام قبل الإدراج.
                    await using var exists = new NpgsqlCommand(
                        "SELECT 1 FROM currencies WHERE code=@c LIMIT 1", conn, tx);
                    exists.Parameters.AddWithValue("@c", c.Code);
                    if (await exists.ExecuteScalarAsync() != null) continue;

                    await using var cmd = new NpgsqlCommand(
                        "INSERT INTO currencies (code, name, symbol, exchange_rate, is_default) " +
                        "VALUES (@c, @n, @s, 1.0, @d)", conn, tx);
                    cmd.Parameters.AddWithValue("@c", c.Code);
                    cmd.Parameters.AddWithValue("@n", c.Name);
                    cmd.Parameters.AddWithValue("@s", c.Symbol);
                    cmd.Parameters.AddWithValue("@d", c.IsDefault);
                    await cmd.ExecuteNonQueryAsync();
                    result.Currencies++;
                }

                // 2) دليل الحسابات
                var accountIds = new Dictionary<string, int>();
                foreach (var a in ChartOfAccounts)
                {
                    await using var cmd = new NpgsqlCommand(
                        "INSERT INTO accounts (code, name, parent_id, type) VALUES (@c, @n, NULL, @t) RETURNING id", conn, tx);
                    cmd.Parameters.AddWithValue("@c", a.Code);
                    cmd.Parameters.AddWithValue("@n", a.Name);
                    cmd.Parameters.AddWithValue("@t", a.Type);
                    accountIds[a.Code] = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    result.Accounts++;
                }

                // 3) الخزائن (نقدية + بنكية) مرتبطة بحسابي Cash / Bank
                if (accountIds.TryGetValue("1000", out var cashAcc))
                {
                    await using var cmd = new NpgsqlCommand(
                        "INSERT INTO cash_boxes (name, code, account_id, is_active, type, opening_balance, created_at) " +
                        "VALUES (@n, @c, @a, TRUE, 'CASH', 0, NOW())", conn, tx);
                    cmd.Parameters.AddWithValue("@n", "MainCash");
                    cmd.Parameters.AddWithValue("@c", "CB001");
                    cmd.Parameters.AddWithValue("@a", cashAcc);
                    await cmd.ExecuteNonQueryAsync();
                    result.CashBoxes++;
                }
                if (accountIds.TryGetValue("1100", out var bankAcc))
                {
                    await using var cmd = new NpgsqlCommand(
                        "INSERT INTO cash_boxes (name, code, account_id, is_active, type, opening_balance, created_at) " +
                        "VALUES (@n, @c, @a, TRUE, 'BANK', 0, NOW())", conn, tx);
                    cmd.Parameters.AddWithValue("@n", "BankAccount");
                    cmd.Parameters.AddWithValue("@c", "BANK01");
                    cmd.Parameters.AddWithValue("@a", bankAcc);
                    await cmd.ExecuteNonQueryAsync();
                    result.CashBoxes++;
                }

                await tx.CommitAsync();
                result.Created = true;
                App.Log("SEED: reference data created — " + result.Message);
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }
        catch (Exception ex)
        {
            App.Log("SEED failed: " + ex.Message);
        }
        return result;
    }
}
