using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Common;
using Techtoon.Api.DTOs.Invoices;

namespace Techtoon.Api.Services;

public interface IInvoiceAppService
{
    Task<PagedResultDto<InvoiceListDto>> GetInvoicesAsync(string? type, string? status, int? partyId, DateTime? from, DateTime? to, string? search, int page, int pageSize);
    Task<InvoiceDetailDto?> GetInvoiceAsync(int id);
    Task<InvoiceDetailDto> CreateInvoiceAsync(InvoiceCreateDto dto, int userId);
    Task<bool> DeleteInvoiceAsync(int id, int userId);
    Task<bool> CancelInvoiceAsync(int id, int userId);
}

public class InvoiceAppService : IInvoiceAppService
{
    private readonly string _connStr;
    private readonly HashSet<string> _invCols;
    private readonly HashSet<string> _lineCols;
    private readonly HashSet<string> _movCols;
    private readonly bool _hasAvgCost;
    private readonly bool _hasCostPrice;
    private readonly bool _hasItemCost;

    public InvoiceAppService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("DefaultConnection") ?? "";
        using var conn = new NpgsqlConnection(_connStr);
        conn.Open();
        _invCols = LoadColumns(conn, "invoices");
        _lineCols = LoadColumns(conn, "invoice_lines");
        _movCols = LoadColumns(conn, "inventory_movements");
        var itemCols = LoadColumns(conn, "items");
        _hasAvgCost = itemCols.Contains("avg_cost_price");
        _hasCostPrice = itemCols.Contains("cost_price");
        _hasItemCost = _hasAvgCost || _hasCostPrice;
    }

    private static HashSet<string> LoadColumns(NpgsqlConnection conn, string table)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name=@t", conn);
        cmd.Parameters.AddWithValue("t", table);
        using var r = cmd.ExecuteReader();
        while (r.Read()) cols.Add(r.GetString(0));
        return cols;
    }

    private bool HasInv(string col) => _invCols.Contains(col);
    private bool HasLine(string col) => _lineCols.Contains(col);
    private bool HasMov(string col) => _movCols.Contains(col);

    private static decimal SafeDecimal(object? v) => v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
    private static int SafeInt(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    private static string SafeString(object? v) => v == null || v == DBNull.Value ? string.Empty : v.ToString() ?? string.Empty;

    public async Task<PagedResultDto<InvoiceListDto>> GetInvoicesAsync(string? type, string? status, int? partyId, DateTime? from, DateTime? to, string? search, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;
        var result = new PagedResultDto<InvoiceListDto> { Page = page, PageSize = pageSize };
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var where = new List<string>();
        var parms = new List<NpgsqlParameter>();
        if (!string.IsNullOrEmpty(type)) { where.Add("i.type = @t"); parms.Add(new NpgsqlParameter("t", type)); }
        if (!string.IsNullOrEmpty(status)) { where.Add("i.status = @s"); parms.Add(new NpgsqlParameter("s", status)); }
        if (partyId.HasValue) { where.Add("i.party_id = @pid"); parms.Add(new NpgsqlParameter("pid", partyId.Value)); }
        if (from.HasValue) { where.Add("i.date >= @from"); parms.Add(new NpgsqlParameter("from", from.Value)); }
        if (to.HasValue) { where.Add("i.date <= @to"); parms.Add(new NpgsqlParameter("to", to.Value)); }
        if (!string.IsNullOrEmpty(search)) {
            where.Add("(i.invoice_number ILIKE @q OR COALESCE(p.name,'') ILIKE @q)");
            parms.Add(new NpgsqlParameter("q", "%" + search + "%"));
        }
        var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM invoices i LEFT JOIN parties p ON i.party_id = p.id " + whereSql, conn))
        {
            foreach (var p in parms) cmd.Parameters.AddClones(parms);
            result.TotalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        var sql = "SELECT i.id::bigint, COALESCE(i.invoice_number,'') AS invoice_number, i.date, COALESCE(i.type,'') AS type, " +
                  "COALESCE(i.total,0)::numeric AS total, COALESCE(i.paid_amount,0)::numeric AS paid_amount, " +
                  "COALESCE(i.status,'') AS status, i.party_id, COALESCE(p.name,'') AS party_name, i.warehouse_id " +
                  "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id " + whereSql + " " +
                  "ORDER BY i.date DESC, i.id DESC LIMIT @limit OFFSET @offset";

        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            foreach (var p in parms) cmd.Parameters.AddClones(parms);
            cmd.Parameters.AddWithValue("limit", pageSize);
            cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                result.Items.Add(new InvoiceListDto
                {
                    Id = SafeInt(r["id"]),
                    InvoiceNumber = SafeString(r["invoice_number"]),
                    Date = r.GetDateTime(r.GetOrdinal("date")),
                    Type = SafeString(r["type"]),
                    Total = SafeDecimal(r["total"]),
                    PaidAmount = SafeDecimal(r["paid_amount"]),
                    Status = SafeString(r["status"]),
                    PartyId = r["party_id"] == DBNull.Value ? null : SafeInt(r["party_id"]),
                    PartyName = SafeString(r["party_name"]),
                    WarehouseId = r["warehouse_id"] == DBNull.Value ? null : SafeInt(r["warehouse_id"])
                });
            }
        }
        return result;
    }

    public async Task<InvoiceDetailDto?> GetInvoiceAsync(int id)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var sqlHdr = "SELECT i.id::bigint, COALESCE(i.invoice_number,'') AS invoice_number, i.date, " +
                     (HasInv("due_date") ? "i.due_date, " : "NULL::timestamp AS due_date, ") +
                     "COALESCE(i.type,'') AS type, i.party_id, COALESCE(p.name,'') AS party_name, " +
                     "i.warehouse_id, " + (HasInv("cash_box_id") ? "i.cash_box_id, " : "NULL::bigint AS cash_box_id, ") +
                     (HasInv("subtotal") ? "COALESCE(i.subtotal,0)::numeric AS subtotal, " : "0::numeric AS subtotal, ") +
                     (HasInv("discount_amount") ? "COALESCE(i.discount_amount,0)::numeric AS discount_amount, " : "0::numeric AS discount_amount, ") +
                     (HasInv("tax_rate") ? "COALESCE(i.tax_rate,0)::numeric AS tax_rate, " : "0::numeric AS tax_rate, ") +
                     (HasInv("tax_amount") ? "COALESCE(i.tax_amount,0)::numeric AS tax_amount, " : "0::numeric AS tax_amount, ") +
                     "COALESCE(i.total,0)::numeric AS total, COALESCE(i.paid_amount,0)::numeric AS paid_amount, " +
                     "COALESCE(i.status,'') AS status, " +
                     (HasInv("notes") ? "COALESCE(i.notes,'') AS notes, " : "'' AS notes, ") +
                     (HasInv("created_by") ? "COALESCE(i.created_by,0)::bigint AS created_by, " : "0::bigint AS created_by, ") +
                     (HasInv("created_at") ? "i.created_at " : "i.date AS created_at ") +
                     "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id WHERE i.id = @id";

        InvoiceDetailDto? inv = null;
        using (var cmd = new NpgsqlCommand(sqlHdr, conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            inv = new InvoiceDetailDto
            {
                Id = SafeInt(r["id"]),
                InvoiceNumber = SafeString(r["invoice_number"]),
                Date = r.GetDateTime(r.GetOrdinal("date")),
                DueDate = r["due_date"] == DBNull.Value ? null : r.GetDateTime(r.GetOrdinal("due_date")),
                Type = SafeString(r["type"]),
                PartyId = r["party_id"] == DBNull.Value ? null : SafeInt(r["party_id"]),
                PartyName = SafeString(r["party_name"]),
                WarehouseId = r["warehouse_id"] == DBNull.Value ? null : SafeInt(r["warehouse_id"]),
                CashBoxId = r["cash_box_id"] == DBNull.Value ? null : SafeInt(r["cash_box_id"]),
                Subtotal = SafeDecimal(r["subtotal"]),
                DiscountAmount = SafeDecimal(r["discount_amount"]),
                TaxRate = SafeDecimal(r["tax_rate"]),
                TaxAmount = SafeDecimal(r["tax_amount"]),
                Total = SafeDecimal(r["total"]),
                PaidAmount = SafeDecimal(r["paid_amount"]),
                Status = SafeString(r["status"]),
                Notes = r["notes"] == DBNull.Value ? null : SafeString(r["notes"]),
                CreatedBy = SafeInt(r["created_by"]),
                CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
            };
        }

        var sqlLines = "SELECT il.id::bigint, il.item_id, COALESCE(it.name,'') AS item_name, " +
                       "il.quantity::numeric, il.price::numeric, " +
                       (HasLine("discount") ? "COALESCE(il.discount,0)::numeric AS discount, " : "0::numeric AS discount, ") +
                       "il.total::numeric, " +
                       (HasLine("cost_price") ? "COALESCE(il.cost_price,0)::numeric AS cost_price " : "0::numeric AS cost_price ") +
                       "FROM invoice_lines il LEFT JOIN items it ON il.item_id = it.id WHERE il.invoice_id = @id";
        using (var cmd = new NpgsqlCommand(sqlLines, conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                inv.Lines.Add(new InvoiceLineDto
                {
                    Id = SafeInt(r["id"]),
                    ItemId = SafeInt(r["item_id"]),
                    ItemName = SafeString(r["item_name"]),
                    Quantity = SafeDecimal(r["quantity"]),
                    Price = SafeDecimal(r["price"]),
                    Discount = SafeDecimal(r["discount"]),
                    Total = SafeDecimal(r["total"]),
                    CostPrice = SafeDecimal(r["cost_price"])
                });
            }
        }

        var sqlPay = "SELECT id::bigint, COALESCE(method,'') AS method, amount::numeric, COALESCE(reference,'') AS reference, paid_at " +
                     "FROM invoice_payments WHERE invoice_id = @id ORDER BY paid_at";
        using (var cmd = new NpgsqlCommand(sqlPay, conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                inv.Payments.Add(new InvoicePaymentDto
                {
                    Id = SafeInt(r["id"]),
                    Method = SafeString(r["method"]),
                    Amount = SafeDecimal(r["amount"]),
                    Reference = r["reference"] == DBNull.Value ? null : SafeString(r["reference"]),
                    PaidAt = r.GetDateTime(r.GetOrdinal("paid_at"))
                });
            }
        }
        return inv;
    }

    public async Task<InvoiceDetailDto> CreateInvoiceAsync(InvoiceCreateDto dto, int userId)
    {
        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new ArgumentException("Invoice must have at least one line");
        if (string.IsNullOrEmpty(dto.Type)) dto.Type = "sales";

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            var invNumber = await GenerateInvoiceNumberAsync(conn, tx, dto.Type);

            decimal subtotal = 0;
            foreach (var line in dto.Lines)
            {
                var lineTotal = line.Quantity * line.Price - line.Discount;
                if (lineTotal < 0) lineTotal = 0;
                subtotal += lineTotal;
            }
            var discountAmount = dto.DiscountAmount;
            var taxableBase = subtotal - discountAmount;
            if (taxableBase < 0) taxableBase = 0;
            var taxAmount = taxableBase * (dto.TaxRate / 100m);
            var total = taxableBase + taxAmount;

            decimal paidAmount = 0;
            if (dto.Payments != null) foreach (var p in dto.Payments) paidAmount += p.Amount;
            var status = paidAmount >= total ? "paid" : (paidAmount > 0 ? "partial" : "unpaid");

            var cols = new List<string> { "invoice_number", "date", "type", "party_id", "total", "paid_amount", "status" };
            var vals = new List<string> { "@num", "@date", "@type", "@pid", "@total", "@paid", "@status" };
            var parms = new List<NpgsqlParameter> {
                new NpgsqlParameter("num", invNumber),
                new NpgsqlParameter("date", dto.Date),
                new NpgsqlParameter("type", dto.Type),
                new NpgsqlParameter("pid", (object?)dto.PartyId ?? DBNull.Value),
                new NpgsqlParameter("total", total),
                new NpgsqlParameter("paid", paidAmount),
                new NpgsqlParameter("status", status)
            };
            if (HasInv("due_date")) { cols.Add("due_date"); vals.Add("@due"); parms.Add(new NpgsqlParameter("due", (object?)dto.DueDate ?? DBNull.Value)); }
            if (HasInv("warehouse_id")) { cols.Add("warehouse_id"); vals.Add("@wh"); parms.Add(new NpgsqlParameter("wh", (object?)dto.WarehouseId ?? DBNull.Value)); }
            if (HasInv("cash_box_id")) { cols.Add("cash_box_id"); vals.Add("@cb"); parms.Add(new NpgsqlParameter("cb", (object?)dto.CashBoxId ?? DBNull.Value)); }
            if (HasInv("currency_id")) { cols.Add("currency_id"); vals.Add("@cur"); parms.Add(new NpgsqlParameter("cur", 1)); }
            if (HasInv("subtotal")) { cols.Add("subtotal"); vals.Add("@sub"); parms.Add(new NpgsqlParameter("sub", subtotal)); }
            if (HasInv("discount_amount")) { cols.Add("discount_amount"); vals.Add("@disc"); parms.Add(new NpgsqlParameter("disc", discountAmount)); }
            if (HasInv("tax_rate")) { cols.Add("tax_rate"); vals.Add("@taxrate"); parms.Add(new NpgsqlParameter("taxrate", dto.TaxRate)); }
            if (HasInv("tax_amount")) { cols.Add("tax_amount"); vals.Add("@tax"); parms.Add(new NpgsqlParameter("tax", taxAmount)); }
            if (HasInv("notes")) { cols.Add("notes"); vals.Add("@notes"); parms.Add(new NpgsqlParameter("notes", (object?)dto.Notes ?? DBNull.Value)); }
            if (HasInv("created_by")) { cols.Add("created_by"); vals.Add("@cb_by"); parms.Add(new NpgsqlParameter("cb_by", userId)); }
            if (HasInv("created_at")) { cols.Add("created_at"); vals.Add("NOW()"); }
            if (HasInv("updated_at")) { cols.Add("updated_at"); vals.Add("NOW()"); }

            int newId;
            using (var cmd = new NpgsqlCommand("INSERT INTO invoices (" + string.Join(",", cols) + ") VALUES (" + string.Join(",", vals) + ") RETURNING id", conn, tx))
            {
                foreach (var p in parms) cmd.Parameters.AddClones(parms);
                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // H10: capture the real item cost so invoice lines carry COGS instead of 0.
            var itemCosts = await LoadItemCostsAsync(conn, tx, dto.Lines);

            foreach (var line in dto.Lines)
            {
                var lineTotal = line.Quantity * line.Price - line.Discount;
                if (lineTotal < 0) lineTotal = 0;

                var lCols = new List<string> { "invoice_id", "item_id", "quantity", "price", "total" };
                var lVals = new List<string> { "@iid", "@item", "@qty", "@price", "@total" };
                var lParms = new List<NpgsqlParameter> {
                    new NpgsqlParameter("iid", newId),
                    new NpgsqlParameter("item", line.ItemId),
                    new NpgsqlParameter("qty", line.Quantity),
                    new NpgsqlParameter("price", line.Price),
                    new NpgsqlParameter("total", lineTotal)
                };
                if (HasLine("cost_price")) { lCols.Add("cost_price"); lVals.Add("@cost"); lParms.Add(new NpgsqlParameter("cost", NpgsqlTypes.NpgsqlDbType.Numeric) { Value = itemCosts.TryGetValue(line.ItemId, out var unitCost) ? unitCost : 0m }); }
                if (HasLine("discount")) { lCols.Add("discount"); lVals.Add("@disc"); lParms.Add(new NpgsqlParameter("disc", line.Discount)); }

                using (var cmd = new NpgsqlCommand("INSERT INTO invoice_lines (" + string.Join(",", lCols) + ") VALUES (" + string.Join(",", lVals) + ")", conn, tx))
                {
                    foreach (var p in lParms) cmd.Parameters.AddClones(parms);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (HasMov("item_id") && HasMov("quantity") && HasMov("type"))
                {
                    // C5: writers store positive magnitudes for in/out; the sign is carried by `type`.
                    var movType = dto.Type == "sales" ? "out" : "in";
                    var qtyMagnitude = Math.Abs(line.Quantity);
                    var mCols = new List<string> { "item_id", "type", "quantity" };
                    var mVals = new List<string> { "@item", "@mtype", "@qty" };
                    var mParms = new List<NpgsqlParameter> {
                        new NpgsqlParameter("item", line.ItemId),
                        new NpgsqlParameter("mtype", movType),
                        new NpgsqlParameter("qty", qtyMagnitude)
                    };
                    if (HasMov("date")) { mCols.Add("date"); mVals.Add("NOW()"); }
                    if (HasMov("reference_id")) { mCols.Add("reference_id"); mVals.Add("@refId"); mParms.Add(new NpgsqlParameter("refId", newId)); }
                    if (HasMov("reference_type")) { mCols.Add("reference_type"); mVals.Add("'invoice'"); }
                    if (HasMov("created_at")) { mCols.Add("created_at"); mVals.Add("NOW()"); }

                    using (var cmd = new NpgsqlCommand("INSERT INTO inventory_movements (" + string.Join(",", mCols) + ") VALUES (" + string.Join(",", mVals) + ")", conn, tx))
                    {
                        foreach (var p in mParms) cmd.Parameters.AddClones(parms);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            if (dto.Payments != null && dto.Payments.Count > 0)
            {
                foreach (var p in dto.Payments)
                {
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at, created_at) " +
                        "VALUES (@iid, @method, @amount, @ref, @paidAt, NOW())", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("iid", newId);
                        cmd.Parameters.AddWithValue("method", p.Method);
                        cmd.Parameters.AddWithValue("amount", p.Amount);
                        cmd.Parameters.AddWithValue("ref", (object?)p.Reference ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("paidAt", p.PaidAt ?? DateTime.Now);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            // P1.5: weighted-average cost on purchases (total_purchased_qty weighted)
            if (dto.Type == "purchase")
            {
                foreach (var line in dto.Lines)
                {
                    var oldAvg = itemCosts.TryGetValue(line.ItemId, out var c) ? c : 0m;
                    using (var up = new NpgsqlCommand(
                        "UPDATE items SET avg_cost_price = CASE WHEN COALESCE(total_purchased_qty,0) + @qty > 0 " +
                        "THEN (COALESCE(avg_cost_price,0) * COALESCE(total_purchased_qty,0) + @cost * @qty) / (COALESCE(total_purchased_qty,0) + @qty) " +
                        "ELSE @cost END, total_purchased_qty = COALESCE(total_purchased_qty,0) + @qty WHERE id=@iid", conn, tx))
                    {
                        up.Parameters.AddWithValue("iid", line.ItemId);
                        up.Parameters.AddWithValue("qty", Math.Abs(line.Quantity));
                        up.Parameters.AddWithValue("cost", line.Price);
                        await up.ExecuteNonQueryAsync();
                    }
                }
            }

            await LogAuditAsync(conn, tx, userId, "create_invoice", "invoice", newId, "Created " + dto.Type + " invoice " + invNumber + " total " + total);
            await tx.CommitAsync();
            return (await GetInvoiceAsync(newId))!;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteInvoiceAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            if (HasMov("reference_id") && HasMov("reference_type"))
            {
                using var cmd = new NpgsqlCommand("DELETE FROM inventory_movements WHERE reference_id = @id AND reference_type = 'invoice'", conn, tx);
                cmd.Parameters.AddWithValue("id", id);
                await cmd.ExecuteNonQueryAsync();
            }
            using (var cmd = new NpgsqlCommand("DELETE FROM invoices WHERE id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", id);
                var affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0) { await tx.RollbackAsync(); return false; }
            }
            await LogAuditAsync(conn, tx, userId, "delete_invoice", "invoice", id, "Deleted invoice id " + id);
            await tx.CommitAsync();
            return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<bool> CancelInvoiceAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        using (var cmd = new NpgsqlCommand("UPDATE invoices SET status = 'cancelled'" + (HasInv("updated_at") ? ", updated_at = NOW()" : "") + " WHERE id = @id AND status <> 'cancelled'", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) { await tx.RollbackAsync(); return false; }
        }
        if (HasMov("reference_id") && HasMov("reference_type"))
        {
            // C5: cancel by reversing the movement type, not by negating the quantity.
            // Also normalise legacy negative in/out rows to positive magnitudes.
            using var cmd = new NpgsqlCommand(
                "UPDATE inventory_movements SET " +
                "type = CASE WHEN type = 'out' THEN 'in' WHEN type = 'in' THEN 'out' ELSE type END, " +
                "quantity = ABS(quantity) " +
                "WHERE reference_id = @id AND reference_type = 'invoice'", conn, tx);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        await LogAuditAsync(conn, tx, userId, "cancel_invoice", "invoice", id, "Cancelled invoice id " + id);
        await tx.CommitAsync();
        return true;
    }

    private async Task<Dictionary<int, decimal>> LoadItemCostsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, List<InvoiceLineInputDto> lines)
    {
        var costs = new Dictionary<int, decimal>();
        if (!_hasItemCost || lines == null || lines.Count == 0) return costs;

        var ids = new HashSet<int>();
        foreach (var l in lines) if (l.ItemId > 0) ids.Add(l.ItemId);
        if (ids.Count == 0) return costs;

        var costExpr = _hasAvgCost && _hasCostPrice
            ? "COALESCE(NULLIF(avg_cost_price,0), NULLIF(cost_price,0), 0)"
            : _hasAvgCost
                ? "COALESCE(avg_cost_price,0)"
                : "COALESCE(cost_price,0)";
        using var cmd = new NpgsqlCommand("SELECT id, " + costExpr + "::numeric FROM items WHERE id = ANY(@ids)", conn, tx);
        cmd.Parameters.AddWithValue("ids", ids.ToArray());
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            costs[SafeInt(r[0])] = SafeDecimal(r[1]);
        return costs;
    }

    private async Task<string> GenerateInvoiceNumberAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string type)
    {
        var prefix = type == "sales" ? "SAL" : "PUR";
        var year = DateTime.Now.Year;

        // H8: COUNT(*)+1 produced duplicate numbers under concurrency (and after deletions).
        // Serialise numbering per type/year with a transaction-scoped advisory lock, then
        // take MAX(suffix)+1 so deleted invoices can never cause a reuse.
        var lockKey = prefix + "-" + year;
        using (var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtext(@key))", conn, tx))
        {
            lockCmd.Parameters.AddWithValue("key", lockKey);
            await lockCmd.ExecuteScalarAsync();
        }

        using var cmd = new NpgsqlCommand(
            "SELECT COALESCE(MAX(SUBSTRING(invoice_number FROM '([0-9]+)$')::int), 0) " +
            "FROM invoices WHERE type = @t AND date >= @start AND date < @end AND invoice_number LIKE @like", conn, tx);
        cmd.Parameters.AddWithValue("t", type);
        cmd.Parameters.AddWithValue("start", new DateTime(year, 1, 1));
        cmd.Parameters.AddWithValue("end", new DateTime(year + 1, 1, 1));
        cmd.Parameters.AddWithValue("like", prefix + "-" + year + "-%");
        var max = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return prefix + "-" + year + "-" + (max + 1).ToString("D6");
    }

    private async Task LogAuditAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, string action, string entityType, int entityId, string details)
    {
        using var cmd = new NpgsqlCommand("INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, @action, @etype, @eid, @details, NOW())", conn, tx);
        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("action", action);
        cmd.Parameters.AddWithValue("etype", entityType);
        cmd.Parameters.AddWithValue("eid", entityId);
        cmd.Parameters.AddWithValue("details", details);
        await cmd.ExecuteNonQueryAsync();
    }
}