using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class ItemEditViewModel : ViewModelBase
{
    private int _id;
    private string _name = "";
    private string _barcode = "";
    private double _costPrice;
    private double _sellPrice;
    private double _minStock = 10;
    private double _openingQuantity;
    private Category? _category;
    private ObservableCollection<Category> _categories = new();
    private string _statusMessage = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Barcode { get => _barcode; set => SetProperty(ref _barcode, value); }
    public double CostPrice { get => _costPrice; set => SetProperty(ref _costPrice, value); }
    public double SellPrice { get => _sellPrice; set => SetProperty(ref _sellPrice, value); }
    public double MinStock { get => _minStock; set => SetProperty(ref _minStock, value); }
    public double OpeningQuantity { get => _openingQuantity; set => SetProperty(ref _openingQuantity, value); }
    public Category? Category { get => _category; set => SetProperty(ref _category, value); }
    public ObservableCollection<Category> Categories { get => _categories; set => SetProperty(ref _categories, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "ØªØ¹Ø¯ÙŠÙ„ ØµÙ†Ù" : "ØµÙ†Ù Ø¬Ø¯ÙŠØ¯";
    public double ProfitMargin => CostPrice > 0 ? Math.Round((SellPrice - CostPrice) / CostPrice * 100, 2) : 0;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public ItemEditViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
    }

    public async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Categories = new ObservableCollection<Category>(await db.Categories.OrderBy(c => c.Name).ToListAsync());
            if (Id > 0)
            {
                var item = await db.Items.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == Id);
                if (item != null)
                {
                    Name = item.Name; Barcode = item.Barcode ?? ""; CostPrice = item.CostPrice; SellPrice = item.SellPrice;
                    MinStock = item.MinStock; Category = Categories.FirstOrDefault(c => c.Id == item.CategoryId);
                    var stock = await db.StockLevels.Where(s => s.ItemId == Id).SumAsync(s => (double?)s.Quantity) ?? 0;
                    OpeningQuantity = stock;
                }
            }
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name)) { StatusMessage = "Name required"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Item item;
            if (Id > 0) { item = await db.Items.FirstOrDefaultAsync(i => i.Id == Id); if (item == null) { StatusMessage = "Not found"; return; } }
            else { item = new Item(); db.Items.Add(item); }
            item.Name = Name; item.Barcode = string.IsNullOrEmpty(Barcode) ? null : Barcode;
            item.CostPrice = CostPrice; item.SellPrice = SellPrice; item.MinStock = MinStock; item.OpeningQuantity = OpeningQuantity;
            if (Category != null) { var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == Category.Id); item.CategoryId = cat?.Id; }
            await db.SaveChangesAsync();
            // Update stock
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.IsActive);
            if (wh != null)
            {
                var sl = await db.StockLevels.FirstOrDefaultAsync(s => s.WarehouseId == wh.Id && s.ItemId == item.Id);
                if (sl == null && OpeningQuantity > 0)
                {
                    db.StockLevels.Add(new StockLevel { WarehouseId = wh.Id, ItemId = item.Id, Quantity = OpeningQuantity, LastUpdated = DateTime.UtcNow });
                    db.InventoryMovements.Add(new InventoryMovement { ItemId = item.Id, Date = DateTime.UtcNow, Type = "opening", Quantity = OpeningQuantity });
                }
                else if (sl != null && Math.Abs(OpeningQuantity - sl.Quantity) > 0.001)
                {
                    var diff = OpeningQuantity - sl.Quantity; sl.Quantity = OpeningQuantity; sl.LastUpdated = DateTime.UtcNow;
                    db.InventoryMovements.Add(new InventoryMovement { ItemId = item.Id, Date = DateTime.UtcNow, Type = "adjustment", Quantity = diff });
                }
                await db.SaveChangesAsync();
            }
            StatusMessage = "Saved OK"; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
