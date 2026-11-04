using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.Services;

public static class InventoryService
{
    public static async Task UpdateStockForInvoiceAsync(TechtoonDbContext db, Invoice invoice, bool isEdit = false)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.IsActive);
        if (wh == null) return;
        
        foreach (var line in invoice.Lines)
        {
            var item = await db.Items.FindAsync(line.ItemId);
            if (item == null || item.IsKit) continue;
            
            var sl = await db.StockLevels.FirstOrDefaultAsync(s => s.WarehouseId == wh.Id && s.ItemId == line.ItemId);
            if (sl == null)
            {
                sl = new StockLevel { WarehouseId = wh.Id, ItemId = line.ItemId, Quantity = 0, LastUpdated = DateTime.UtcNow };
                db.StockLevels.Add(sl);
            }
            
            double qtyChange = invoice.Type == "sale" ? -line.Quantity : line.Quantity;
            sl.Quantity += qtyChange;
            sl.LastUpdated = DateTime.UtcNow;
            
            db.InventoryMovements.Add(new InventoryMovement 
            { 
                ItemId = line.ItemId, 
                Date = DateTime.UtcNow, 
                Type = invoice.Type == "sale" ? "sale_out" : "purchase_in", 
                Quantity = qtyChange, 
                ReferenceId = invoice.Id 
            });
        }
        await db.SaveChangesAsync();
    }
}