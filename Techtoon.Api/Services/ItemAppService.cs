using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Common;
using Techtoon.Api.DTOs.Items;

namespace Techtoon.Api.Services;

public interface IItemAppService
{
    Task<PagedResultDto<ItemListDto>> GetItemsAsync(string? search, string? barcode, int? categoryId, int page, int pageSize);
    Task<ItemDetailDto?> GetItemAsync(int id);
    Task<ItemDetailDto> CreateItemAsync(ItemCreateDto dto, int userId);
    Task<ItemDetailDto?> UpdateItemAsync(int id, ItemUpdateDto dto, int userId);
    Task<bool> DeleteItemAsync(int id, int userId);
    Task<decimal> GetCurrentStockAsync(int itemId);
    Task<List<ItemListDto>> GetLowStockItemsAsync();
}

public class ItemAppService : IItemAppService
{
    private readonly string _connStr;

    public ItemAppService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("DefaultConnection") ?? "";
    }

    private static decimal SafeDecimal(object? v)
    {
        if (v == null || v == DBNull.Value) return 0m;
        return Convert.ToDecimal(v);
    }

    private static int SafeInt(object? v)
    {
        if (v == null || v == DBNull.Value) return 0;
        return Convert.ToInt32(v);
    }

    private static bool SafeBool(object? v)
    {
        if (v == null || v == DBNull.Value) return false;
        if (v is bool b) return b;
        if (v is int i) return i != 0;
        if (v is long l) return l != 0;
        return Convert.ToBoolean(v);
    }

    private static string SafeString(object? v)
    {
        if (v == null || v == DBNull.Value) return string.Empty;
        return v.ToString() ?? string.Empty;
    }

    public async Task<PagedResultDto<ItemListDto>> GetItemsAsync(string? search, string? barcode, int? categoryId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var result = new PagedResultDto<ItemListDto> { Page = page, PageSize = pageSize };
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var where = new List<string>();
        var parms = new List<NpgsqlParameter>();
        if (!string.IsNullOrEmpty(search))
        {
            where.Add("(i.name ILIKE @s OR i.barcode ILIKE @s)");
            parms.Add(new NpgsqlParameter("s", "%" + search + "%"));
        }
        if (!string.IsNullOrEmpty(barcode))
        {
            where.Add("i.barcode = @b");
            parms.Add(new NpgsqlParameter("b", barcode));
        }
        if (categoryId.HasValue)
        {
            where.Add("i.category_id = @c");
            parms.Add(new NpgsqlParameter("c", categoryId.Value));
        }
        var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        // Count
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM items i " + whereSql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            result.TotalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Items - cast everything to expected types
        var sql = "SELECT i.id::bigint, i.name, COALESCE(i.barcode,'') AS barcode, " +
                  "i.sell_price::numeric, i.cost_price::numeric, i.avg_cost_price::numeric, " +
                  "COALESCE(i.opening_quantity,0)::numeric AS opening_quantity, " +
                  "COALESCE(i.min_stock,0)::numeric AS min_stock, " +
                  "COALESCE(i.is_kit,false)::boolean AS is_kit, " +
                  "i.category_id, " +
                  "COALESCE(i.total_purchased_qty,0)::numeric AS total_purchased_qty, " +
                  "COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = i.id), 0)::numeric AS movement_sum " +
                  "FROM items i " + whereSql + " ORDER BY i.name LIMIT @limit OFFSET @offset";

        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            cmd.Parameters.AddWithValue("limit", pageSize);
            cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var opening = SafeDecimal(r["opening_quantity"]);
                var movement = SafeDecimal(r["movement_sum"]);
                result.Items.Add(new ItemListDto
                {
                    Id = SafeInt(r["id"]),
                    Name = SafeString(r["name"]),
                    Barcode = r["barcode"] == DBNull.Value ? null : SafeString(r["barcode"]),
                    SellPrice = SafeDecimal(r["sell_price"]),
                    CostPrice = SafeDecimal(r["cost_price"]),
                    AvgCostPrice = SafeDecimal(r["avg_cost_price"]),
                    OpeningQuantity = opening,
                    CurrentStock = opening + movement,
                    MinStock = SafeDecimal(r["min_stock"]),
                    IsKit = SafeBool(r["is_kit"]),
                    CategoryId = r["category_id"] == DBNull.Value ? null : SafeInt(r["category_id"])
                });
            }
        }
        return result;
    }

    public async Task<ItemDetailDto?> GetItemAsync(int id)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        var sql = "SELECT i.id::bigint, i.name, COALESCE(i.barcode,'') AS barcode, " +
                  "i.sell_price::numeric, i.cost_price::numeric, i.avg_cost_price::numeric, " +
                  "COALESCE(i.opening_quantity,0)::numeric AS opening_quantity, " +
                  "COALESCE(i.min_stock,0)::numeric AS min_stock, " +
                  "COALESCE(i.is_kit,false)::boolean AS is_kit, " +
                  "i.category_id, " +
                  "i.inventory_account_id, i.cogs_account_id, i.revenue_account_id, " +
                  "COALESCE(i.total_purchased_qty,0)::numeric AS total_purchased_qty, " +
                  "COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = i.id), 0)::numeric AS movement_sum " +
                  "FROM items i WHERE i.id = @id";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;

        var opening = SafeDecimal(r["opening_quantity"]);
        var movement = SafeDecimal(r["movement_sum"]);
        return new ItemDetailDto
        {
            Id = SafeInt(r["id"]),
            Name = SafeString(r["name"]),
            Barcode = r["barcode"] == DBNull.Value ? null : SafeString(r["barcode"]),
            SellPrice = SafeDecimal(r["sell_price"]),
            CostPrice = SafeDecimal(r["cost_price"]),
            AvgCostPrice = SafeDecimal(r["avg_cost_price"]),
            OpeningQuantity = opening,
            MinStock = SafeDecimal(r["min_stock"]),
            IsKit = SafeBool(r["is_kit"]),
            CategoryId = r["category_id"] == DBNull.Value ? null : SafeInt(r["category_id"]),
            InventoryAccountId = r["inventory_account_id"] == DBNull.Value ? null : SafeInt(r["inventory_account_id"]),
            CogsAccountId = r["cogs_account_id"] == DBNull.Value ? null : SafeInt(r["cogs_account_id"]),
            RevenueAccountId = r["revenue_account_id"] == DBNull.Value ? null : SafeInt(r["revenue_account_id"]),
            TotalPurchasedQty = SafeDecimal(r["total_purchased_qty"]),
            CurrentStock = opening + movement
        };
    }

    public async Task<ItemDetailDto> CreateItemAsync(ItemCreateDto dto, int userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Item name is required");

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        int newId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO items (name, barcode, sell_price, cost_price, avg_cost_price, opening_quantity, min_stock, is_kit, category_id, total_purchased_qty) " +
            "VALUES (@name, @barcode, @sell, @cost, @cost, @opening, @min, @kit, @cat, 0) RETURNING id", conn, tx))
        {
            cmd.Parameters.AddWithValue("name", dto.Name);
            cmd.Parameters.AddWithValue("barcode", (object?)dto.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sell", dto.SellPrice);
            cmd.Parameters.AddWithValue("cost", dto.CostPrice);
            cmd.Parameters.AddWithValue("opening", dto.OpeningQuantity);
            cmd.Parameters.AddWithValue("min", dto.MinStock);
            cmd.Parameters.AddWithValue("kit", dto.IsKit);
            cmd.Parameters.AddWithValue("cat", (object?)dto.CategoryId ?? DBNull.Value);
            newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        if (dto.OpeningQuantity != 0)
        {
            using var cmd = new NpgsqlCommand(
                "INSERT INTO inventory_movements (item_id, date, type, quantity, reference_type, created_at) " +
                "VALUES (@itemId, NOW(), 'opening', @qty, 'manual', NOW())", conn, tx);
            cmd.Parameters.AddWithValue("itemId", newId);
            cmd.Parameters.AddWithValue("qty", dto.OpeningQuantity);
            await cmd.ExecuteNonQueryAsync();
        }

        await LogAuditAsync(conn, tx, userId, "create_item", "item", newId, "Created item: " + dto.Name);
        await tx.CommitAsync();

        return (await GetItemAsync(newId))!;
    }

    public async Task<ItemDetailDto?> UpdateItemAsync(int id, ItemUpdateDto dto, int userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Item name is required");

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        using (var cmd = new NpgsqlCommand(
            "UPDATE items SET name=@name, barcode=@barcode, sell_price=@sell, cost_price=@cost, " +
            "min_stock=@min, category_id=@cat WHERE id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("name", dto.Name);
            cmd.Parameters.AddWithValue("barcode", (object?)dto.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sell", dto.SellPrice);
            cmd.Parameters.AddWithValue("cost", dto.CostPrice);
            cmd.Parameters.AddWithValue("min", dto.MinStock);
            cmd.Parameters.AddWithValue("cat", (object?)dto.CategoryId ?? DBNull.Value);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) { await tx.RollbackAsync(); return null; }
        }

        await LogAuditAsync(conn, tx, userId, "update_item", "item", id, "Updated item: " + dto.Name);
        await tx.CommitAsync();
        return await GetItemAsync(id);
    }

    public async Task<bool> DeleteItemAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM inventory_movements WHERE item_id=@id AND type NOT IN ('opening')", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (count > 0)
                throw new InvalidOperationException("Cannot delete item with existing transactions. Consider disabling it instead.");
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM inventory_movements WHERE item_id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM items WHERE id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) { await tx.RollbackAsync(); return false; }
        }

        await LogAuditAsync(conn, tx, userId, "delete_item", "item", id, "Deleted item id " + id);
        await tx.CommitAsync();
        return true;
    }

    public async Task<decimal> GetCurrentStockAsync(int itemId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT COALESCE(opening_quantity,0)::numeric + COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = @id), 0)::numeric " +
            "FROM items WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", itemId);
        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
    }

    public async Task<List<ItemListDto>> GetLowStockItemsAsync()
    {
        var list = new List<ItemListDto>();
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT i.id::bigint, i.name, COALESCE(i.barcode,'') AS barcode, " +
            "i.sell_price::numeric, i.cost_price::numeric, i.avg_cost_price::numeric, " +
            "COALESCE(i.opening_quantity,0)::numeric AS opening_quantity, " +
            "COALESCE(i.min_stock,0)::numeric AS min_stock, " +
            "COALESCE(i.is_kit,false)::boolean AS is_kit, " +
            "i.category_id, " +
            "COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = i.id), 0)::numeric AS movement_sum " +
            "FROM items i WHERE i.min_stock > 0 AND " +
            "(COALESCE(i.opening_quantity,0) + COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = i.id), 0)) <= i.min_stock " +
            "ORDER BY i.name", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var opening = SafeDecimal(r["opening_quantity"]);
            var movement = SafeDecimal(r["movement_sum"]);
            list.Add(new ItemListDto
            {
                Id = SafeInt(r["id"]),
                Name = SafeString(r["name"]),
                Barcode = r["barcode"] == DBNull.Value ? null : SafeString(r["barcode"]),
                SellPrice = SafeDecimal(r["sell_price"]),
                CostPrice = SafeDecimal(r["cost_price"]),
                AvgCostPrice = SafeDecimal(r["avg_cost_price"]),
                OpeningQuantity = opening,
                CurrentStock = opening + movement,
                MinStock = SafeDecimal(r["min_stock"]),
                IsKit = SafeBool(r["is_kit"]),
                CategoryId = r["category_id"] == DBNull.Value ? null : SafeInt(r["category_id"])
            });
        }
        return list;
    }

    private async Task LogAuditAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, string action, string entityType, int entityId, string details)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, @action, @etype, @eid, @details, NOW())",
            conn, tx);
        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("action", action);
        cmd.Parameters.AddWithValue("etype", entityType);
        cmd.Parameters.AddWithValue("eid", entityId);
        cmd.Parameters.AddWithValue("details", details);
        await cmd.ExecuteNonQueryAsync();
    }
}