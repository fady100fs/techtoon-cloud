# build_remaining_pages.py
from pathlib import Path

ROOT = Path(r"D:\PROG\Accounting Prog\Techtoon Accounting C+")
UI = ROOT / "Techtoon.UI"
VM = UI / "ViewModels"
VIEWS = UI / "Views"
SERVICES = UI / "Services"

for d in [VM, VIEWS, SERVICES]:
    d.mkdir(parents=True, exist_ok=True)

# ========================================================
# 1. SERVICES (Inventory + ImportExport)
# ========================================================

(SERVICES / "InventoryService.cs").write_text("""using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.Services;

public static class InventoryService
{
    public static async Task UpdateStockForInvoiceAsync(TechtoonDbContext db, Invoice invoice)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.IsActive);
        if (wh == null) return;
        foreach (var line in invoice.Lines)
        {
            var item = await db.Items.FindAsync(line.ItemId);
            if (item == null || item.IsKit) continue;
            var sl = await db.StockLevels.FirstOrDefaultAsync(s => s.WarehouseId == wh.Id && s.ItemId == line.ItemId);
            if (sl == null) { sl = new StockLevel { WarehouseId = wh.Id, ItemId = line.ItemId, Quantity = 0, LastUpdated = DateTime.UtcNow }; db.StockLevels.Add(sl); }
            double qtyChange = invoice.Type == "sale" ? -line.Quantity : line.Quantity;
            sl.Quantity += qtyChange; sl.LastUpdated = DateTime.UtcNow;
            db.InventoryMovements.Add(new InventoryMovement { ItemId = line.ItemId, Date = DateTime.UtcNow, Type = invoice.Type == "sale" ? "sale_out" : "purchase_in", Quantity = qtyChange, ReferenceId = invoice.Id });
        }
        await db.SaveChangesAsync();
    }
}
""", encoding="utf-8")

(SERVICES / "ImportExportService.cs").write_text("""using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.Services;

public static class ImportExportService
{
    public static async Task ExportPartiesAsync()
    {
        var dialog = new SaveFileDialog { Filter = "CSV File|*.csv", FileName = "parties.csv" };
        if (dialog.ShowDialog() != true) return;
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
        var list = await db.Parties.Include(p => p.Account).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Name,Type,Phone,Address,CreditLimit,OpeningBalance,AccountName");
        foreach (var p in list) sb.AppendLine($""{Escape(p.Name)}",{p.Type},{Escape(p.Phone ?? "")},{Escape(p.Address ?? "")},{p.CreditLimit},{p.OpeningBalance},{Escape(p.Account?.Name ?? "")}"");
        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show($"Exported {list.Count} parties", "Done");
    }

    public static async Task ExportItemsAsync()
    {
        var dialog = new SaveFileDialog { Filter = "CSV File|*.csv", FileName = "items.csv" };
        if (dialog.ShowDialog() != true) return;
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
        var list = await db.Items.Include(i => i.Category).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Name,Barcode,CostPrice,SellPrice,MinStock,Category,IsKit");
        foreach (var i in list) sb.AppendLine($""{Escape(i.Name)}",{Escape(i.Barcode ?? "")},{i.CostPrice},{i.SellPrice},{i.MinStock},{Escape(i.Category?.Name ?? "")},{i.IsKit}"");
        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show($"Exported {list.Count} items", "Done");
    }

    public static async Task ImportPartiesAsync()
    {
        var dialog = new OpenFileDialog { Filter = "CSV File|*.csv" };
        if (dialog.ShowDialog() != true) return;
        var lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
        if (lines.Length < 2) { MessageBox.Show("Empty file"); return; }
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
        var accounts = await db.Accounts.ToListAsync();
        int imported = 0, skipped = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            var f = ParseCsvLine(lines[i]);
            if (f.Length < 6) { skipped++; continue; }
            var name = f[0].Trim();
            if (string.IsNullOrWhiteSpace(name) || await db.Parties.AnyAsync(p => p.Name == name)) { skipped++; continue; }
            var accName = f.Length > 6 ? f[6].Trim() : "";
            var acc = !string.IsNullOrEmpty(accName) ? accounts.FirstOrDefault(a => a.Name == accName) : accounts.FirstOrDefault();
            db.Parties.Add(new Party { Name = name, Type = f[1].Trim(), Phone = f[2].Trim(), Address = f[3].Trim(), CreditLimit = double.TryParse(f[4], out var cl) ? cl : 0, OpeningBalance = double.TryParse(f[5], out var ob) ? ob : 0, AccountId = acc?.Id ?? 0 });
            imported++;
        }
        await db.SaveChangesAsync();
        MessageBox.Show($"Imported: {imported}, Skipped: {skipped}", "Done");
    }

    public static async Task ImportItemsAsync()
    {
        var dialog = new OpenFileDialog { Filter = "CSV File|*.csv" };
        if (dialog.ShowDialog() != true) return;
        var lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
        if (lines.Length < 2) { MessageBox.Show("Empty file"); return; }
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
        var cats = await db.Categories.ToListAsync();
        int imported = 0, skipped = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            var f = ParseCsvLine(lines[i]);
            if (f.Length < 5) { skipped++; continue; }
            var name = f[0].Trim();
            if (string.IsNullOrWhiteSpace(name) || await db.Items.AnyAsync(x => x.Name == name)) { skipped++; continue; }
            var catName = f.Length > 5 ? f[5].Trim() : "";
            var cat = !string.IsNullOrEmpty(catName) ? cats.FirstOrDefault(c => c.Name == catName) : null;
            db.Items.Add(new Item { Name = name, Barcode = f[1].Trim(), CostPrice = double.TryParse(f[2], out var cp) ? cp : 0, SellPrice = double.TryParse(f[3], out var sp) ? sp : 0, MinStock = double.TryParse(f[4], out var ms) ? ms : 10, CategoryId = cat?.Id, IsKit = f.Length > 6 && bool.TryParse(f[6], out var ik) ? ik : false });
            imported++;
        }
        await db.SaveChangesAsync();
        MessageBox.Show($"Imported: {imported}, Skipped: {skipped}", "Done");
    }

    private static string Escape(string s) => (s.Contains(",") || s.Contains(""")) ? $"""{s.Replace(""", """)}""" : s ?? "";
    private static string[] ParseCsvLine(string line) { var r = new List<string>(); var c = new StringBuilder(); bool q = false; for (int i = 0; i < line.Length; i++) { if (line[i] == '"' ) { if (q && i + 1 < line.Length && line[i + 1] == '"') { c.Append('"'); i++; } else q = !q; } else if (line[i] == ',' && !q) { r.Add(c.ToString()); c.Clear(); } else c.Append(line[i]); } r.Add(c.ToString()); return r.ToArray(); }
}
""", encoding="utf-8")

# ========================================================
# 2. WAREHOUSES PAGE
# ========================================================

(VM / "WarehousesViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class WarehousesViewModel : ViewModelBase
{
    private ObservableCollection<Warehouse> _warehouses = new();
    private Warehouse? _selectedWarehouse;
    public ObservableCollection<Warehouse> Warehouses { get => _warehouses; set => SetProperty(ref _warehouses, value); }
    public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set => SetProperty(ref _selectedWarehouse, value); }
    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public WarehousesViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedWarehouse), () => SelectedWarehouse != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedWarehouse != null);
    }

    public override async Task LoadAsync()
    {
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); Warehouses = new ObservableCollection<Warehouse>(await db.Warehouses.OrderBy(w => w.Name).ToListAsync()); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void OpenEdit(Warehouse? wh)
    {
        var window = new WarehouseEditWindow(wh) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedWarehouse == null) return;
        var c = System.Windows.MessageBox.Show("Delete: " + SelectedWarehouse.Name + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (c != System.Windows.MessageBoxResult.Yes) return;
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var w = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == SelectedWarehouse.Id); if (w != null) { db.Warehouses.Remove(w); await db.SaveChangesAsync(); await LoadAsync(); } }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
""", encoding="utf-8")

(VM / "WarehouseEditViewModel.cs").write_text("""using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class WarehouseEditViewModel : ViewModelBase
{
    private int _id;
    private string _name = "";
    private string _code = "";
    private string _location = "";
    private bool _isActive = true;
    private string _statusMessage = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string Location { get => _location; set => SetProperty(ref _location, value); }
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "تعديل مخزن" : "مخزن جديد";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public WarehouseEditViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
    }

    public async override Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            if (Id == 0) { var count = await db.Warehouses.CountAsync(); Code = "WH-" + (count + 1).ToString("D3"); }
            else { var w = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == Id); if (w != null) { Name = w.Name; Code = w.Code; Location = w.Location ?? ""; IsActive = w.IsActive; } }
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name)) { StatusMessage = "Name required"; return; }
            if (string.IsNullOrWhiteSpace(Code)) { StatusMessage = "Code required"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Warehouse w;
            if (Id > 0) { w = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == Id); if (w == null) { StatusMessage = "Not found"; return; } }
            else { w = new Warehouse { CreatedAt = DateTime.UtcNow }; db.Warehouses.Add(w); }
            w.Name = Name; w.Code = Code; w.Location = string.IsNullOrEmpty(Location) ? null : Location; w.IsActive = IsActive;
            await db.SaveChangesAsync();
            StatusMessage = "Saved OK"; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "WarehouseEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.WarehouseEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="450" Width="500"
        WindowStartupLocation="CenterOwner" Background="{StaticResource BackgroundBrush}" FlowDirection="RightToLeft">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <TextBlock Grid.Row="0" Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}"/>
        <Border Grid.Row="1" Style="{StaticResource Card}">
            <StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15"><TextBlock Text="الاسم:" Margin="0,0,0,5"/><TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/></StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0,0,15"><TextBlock Text="الكود:" Margin="0,0,0,5"/><TextBox Text="{Binding Code, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/></StackPanel>
                </Grid>
                <StackPanel Margin="0,0,0,15"><TextBlock Text="الموقع:" Margin="0,0,0,5"/><TextBox Text="{Binding Location, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/></StackPanel>
                <CheckBox Content="نشط" IsChecked="{Binding IsActive}" Margin="0,0,0,15"/>
            </StackPanel>
        </Border>
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center" Margin="0,0,20,0"/>
            <Button Content="إلغاء" Style="{StaticResource PrimaryButton}" Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
            <Button Content="💾 حفظ" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</Window>
""", encoding="utf-8")

(VIEWS / "WarehouseEditWindow.xaml.cs").write_text("""using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class WarehouseEditWindow : Window
{
    private readonly WarehouseEditViewModel _vm;
    public WarehouseEditWindow(Warehouse? wh = null)
    {
        InitializeComponent();
        _vm = new WarehouseEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (wh != null) _vm.Id = wh.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
""", encoding="utf-8")

(VIEWS / "WarehousesView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.WarehousesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="WarehousesView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0" LastChildFill="False">
            <TextBlock Text="المخازن" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" Margin="0,0,0,20">
                <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Margin="0,0,10,0"/>
                <Button Content="+ مخزن جديد" Style="{StaticResource PrimaryButton}" Command="{Binding NewCommand}"/>
            </StackPanel>
        </DockPanel>
        <Border Grid.Row="1" Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding Warehouses}" SelectedItem="{Binding SelectedWarehouse}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      SelectionMode="Single" HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="الكود" Binding="{Binding Code}" Width="120"/>
                    <DataGridTextColumn Header="الاسم" Binding="{Binding Name}" Width="*"/>
                    <DataGridTextColumn Header="الموقع" Binding="{Binding Location}" Width="200"/>
                    <DataGridCheckBoxColumn Header="نشط" Binding="{Binding IsActive}" Width="60"/>
                    <DataGridTemplateColumn Header="إجراءات" Width="100">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                    <Button Content="✏️" Margin="2" Padding="6,2" Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                    <Button Content="🗑️" Margin="2" Padding="6,2" Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
""", encoding="utf-8")

(VIEWS / "WarehousesView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class WarehousesView : UserControl
{
    public WarehousesView() { InitializeComponent(); }
    private async void WarehousesView_Loaded(object sender, RoutedEventArgs e) { if (DataContext is WarehousesViewModel vm) await vm.LoadAsync(); }
}
""", encoding="utf-8")

# ========================================================
# 3. STOCK LEVELS PAGE
# ========================================================

(VM / "StockLevelsViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class StockLevelRow : ViewModelBase
{
    public string WarehouseName { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public double Quantity { get; set; }
    public double MinStock { get; set; }
    public bool IsLow => Quantity < MinStock;
}

public class StockLevelsViewModel : ViewModelBase
{
    private ObservableCollection<StockLevelRow> _stockLevels = new();
    public ObservableCollection<StockLevelRow> StockLevels { get => _stockLevels; set => SetProperty(ref _stockLevels, value); }
    public ICommand RefreshCommand { get; }

    public StockLevelsViewModel() { RefreshCommand = new RelayCommand(async () => await LoadAsync()); }

    public override async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var list = await db.StockLevels.Include(s => s.Warehouse).Include(s => s.Item).OrderBy(s => s.Item.Name).ToListAsync();
            StockLevels = new ObservableCollection<StockLevelRow>(list.Select(s => new StockLevelRow { WarehouseName = s.Warehouse?.Name ?? "", ItemCode = s.Item?.DisplayCode ?? "", ItemName = s.Item?.Name ?? "", Quantity = s.Quantity, MinStock = s.Item?.MinStock ?? 0 }));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }
}
""", encoding="utf-8")

(VIEWS / "StockLevelsView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.StockLevelsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="StockLevelsView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0} LastChildFill="False">
            <TextBlock Text="أرصدة المخزون" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" DockPanel.Dock="Right" Margin="0,0,0,20"/>
        </DockPanel>
        <Border Grid.Row="1" Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding StockLevels}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#E0E0E0"
                      ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="المخزن" Binding="{Binding WarehouseName}" Width="150"/>
                    <DataGridTextColumn Header="الكود" Binding="{Binding ItemCode}" Width="120"/>
                    <DataGridTextColumn Header="الصنف" Binding="{Binding ItemName}" Width="*"/>
                    <DataGridTextColumn Header="الرصيد" Binding="{Binding Quantity, StringFormat=N2}" Width="120"/>
                    <DataGridTextColumn Header="الحد الأدنى" Binding="{Binding MinStock, StringFormat=N2}" Width="120"/>
                    <DataGridCheckBoxColumn Header="منخفض" Binding="{Binding IsLow, Mode=OneWay}" Width="80" IsReadOnly="True"/>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
""", encoding="utf-8")

xaml_path = VIEWS / "StockLevelsView.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "StockLevelsView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class StockLevelsView : UserControl
{
    public StockLevelsView() { InitializeComponent(); }
    private async void StockLevelsView_Loaded(object sender, RoutedEventArgs e) { if (DataContext is StockLevelsViewModel vm) await vm.LoadAsync(); }
}
""", encoding="utf-8")

# ========================================================
# 4. LOANS PAGE
# ========================================================

(VM / "LoansViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class LoansViewModel : ViewModelBase
{
    private ObservableCollection<Loan> _loans = new();
    private Loan? _selectedLoan;
    public ObservableCollection<Loan> Loans { get => _loans; set => SetProperty(ref _loans, value); }
    public Loan? SelectedLoan { get => _selectedLoan; set => SetProperty(ref _selectedLoan, value); }
    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public LoansViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedLoan), () => SelectedLoan != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedLoan != null);
    }

    public override async Task LoadAsync()
    {
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); Loans = new ObservableCollection<Loan>(await db.Loans.Include(l => l.Installments).OrderByDescending(l => l.LoanDate).Take(200).ToListAsync()); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void OpenEdit(Loan? loan)
    {
        var window = new LoanEditWindow(loan) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedLoan == null) return;
        var c = System.Windows.MessageBox.Show("Delete: " + SelectedLoan.LoanNumber + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (c != System.Windows.MessageBoxResult.Yes) return;
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var l = await db.Loans.Include(x => x.Installments).FirstOrDefaultAsync(x => x.Id == SelectedLoan.Id); if (l != null) { db.LoanInstallments.RemoveRange(l.Installments); db.Loans.Remove(l); await db.SaveChangesAsync(); await LoadAsync(); } }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
""", encoding="utf-8")

(VM / "LoanEditViewModel.cs").write_text("""using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class LoanEditViewModel : ViewModelBase
{
    private int _id;
    private string _loanNumber = "";
    private string _loanType = "RECEIVED";
    private string _borrowerName = "";
    private double _principalAmount;
    private double _interestRate;
    private DateTime _loanDate = DateTime.Today;
    private int _termMonths = 12;
    private string _statusMessage = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string LoanNumber { get => _loanNumber; set => SetProperty(ref _loanNumber, value); }
    public string LoanType { get => _loanType; set => SetProperty(ref _loanType, value); }
    public string BorrowerName { get => _borrowerName; set => SetProperty(ref _borrowerName, value); }
    public double PrincipalAmount { get => _principalAmount; set => SetProperty(ref _principalAmount, value); }
    public double InterestRate { get => _interestRate; set => SetProperty(ref _interestRate, value); }
    public DateTime LoanDate { get => _loanDate; set => SetProperty(ref _loanDate, value); }
    public int TermMonths { get => _termMonths; set => SetProperty(ref _termMonths, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "تعديل قرض" : "قرض جديد";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public LoanEditViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
    }

    public async override Task LoadAsync()
    {
        if (Id == 0)
        {
            try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var year = DateTime.Now.Year; var count = await db.Loans.CountAsync(l => l.LoanNumber.StartsWith("LOAN-" + year)); LoanNumber = "LOAN-" + year + "-" + (count + 1).ToString("D4"); }
            catch { }
        }
        else
        {
            try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var l = await db.Loans.FirstOrDefaultAsync(x => x.Id == Id); if (l != null) { LoanNumber = l.LoanNumber; LoanType = l.LoanType; BorrowerName = l.BorrowerName; PrincipalAmount = l.PrincipalAmount; InterestRate = l.InterestRate; LoanDate = l.LoanDate; TermMonths = l.TermMonths; } }
            catch { }
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BorrowerName)) { StatusMessage = "Borrower required"; return; }
            if (PrincipalAmount <= 0) { StatusMessage = "Amount > 0"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Loan loan; bool isNew = false;
            if (Id > 0) { loan = await db.Loans.FirstOrDefaultAsync(x => x.Id == Id); if (loan == null) { StatusMessage = "Not found"; return; } }
            else { loan = new Loan(); db.Loans.Add(loan); isNew = true; }
            loan.LoanNumber = LoanNumber; loan.LoanType = LoanType; loan.BorrowerName = BorrowerName;
            loan.PrincipalAmount = PrincipalAmount; loan.InterestRate = InterestRate; loan.LoanDate = LoanDate;
            loan.MaturityDate = LoanDate.AddMonths(TermMonths); loan.TermMonths = TermMonths;
            loan.TotalInterest = PrincipalAmount * InterestRate / 100 * TermMonths / 12;
            loan.TotalAmount = PrincipalAmount + loan.TotalInterest;
            loan.InstallmentAmount = loan.TotalAmount / TermMonths;
            loan.RemainingAmount = loan.TotalAmount - loan.PaidAmount;
            loan.Status = "ACTIVE";
            await db.SaveChangesAsync();
            if (isNew) { for (int i = 1; i <= TermMonths; i++) db.LoanInstallments.Add(new LoanInstallment { LoanId = loan.Id, InstallmentNumber = i, DueDate = LoanDate.AddMonths(i), PrincipalAmount = PrincipalAmount / TermMonths, InterestAmount = loan.TotalInterest / TermMonths, TotalAmount = loan.InstallmentAmount, Status = "pending" }); await db.SaveChangesAsync(); }
            StatusMessage = "Saved OK"; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "LoanEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.LoanEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="550" Width="600"
        WindowStartupLocation="CenterOwner" Background="{StaticResource BackgroundBrush}" FlowDirection="RightToLeft">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <TextBlock Grid.Row="0} Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}"/>
        <Border Grid.Row="1" Style="{StaticResource Card}">
            <StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15"><TextBlock Text="رقم القرض:" Margin="0,0,0,5"/><TextBox Text="{Binding LoanNumber}" Padding="8,6" IsReadOnly="True"/></StackPanel>
                    <StackPanel Grid.Column="1} Margin="10,0,0,15"><TextBlock Text="النوع:" Margin="0,0,0,5"/><ComboBox SelectedValue="{Binding LoanType}" SelectedValuePath="Tag" Padding="8,6"><ComboBoxItem Content="مستلم" Tag="RECEIVED"/><ComboBoxItem Content="ممنوح" Tag="GIVEN"/></ComboBox></StackPanel>
                </Grid>
                <StackPanel Margin="0,0,0,15"><TextBlock Text="اسم المقترض:" Margin="0,0,0,5"/><TextBox Text="{Binding BorrowerName, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/></StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0} Margin="0,0,10,15"><TextBlock Text="المبلغ:" Margin="0,0,0,5"/><TextBox Text="{Binding PrincipalAmount, UpdateSourceTrigger=LostFocus}" Padding="8,6"/></StackPanel>
                    <StackPanel Grid.Column="1} Margin="10,0"><TextBlock Text="الفائدة %:" Margin="0,0,0,5"/><TextBox Text="{Binding InterestRate, UpdateSourceTrigger=LostFocus}" Padding="8,6"/></StackPanel>
                    <StackPanel Grid.Column="2} Margin="10,0,0,15"><TextBlock Text="المدة (شهر):" Margin="0,0,0,5"/><TextBox Text="{Binding TermMonths, UpdateSourceTrigger=LostFocus}" Padding="8,6"/></StackPanel>
                </Grid>
                <StackPanel Margin="0,0,0,15"><TextBlock Text="تاريخ القرض:" Margin="0,0,0,5"/><DatePicker SelectedDate="{Binding LoanDate}" Padding="8,6"/></StackPanel>
            </StackPanel>
        </Border>
        <StackPanel Grid.Row="2} Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center" Margin="0,0,20,0"/>
            <Button Content="إلغاء" Style="{StaticResource PrimaryButton}" Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
            <Button Content="💾 حفظ" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</Window>
""", encoding="utf-8")

xaml_path = VIEWS / "LoanEditWindow.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"').replace('Grid.Column="0}', 'Grid.Column="0"').replace('Grid.Column="1}', 'Grid.Column="1"').replace('Grid.Column="2}', 'Grid.Column="2"').replace('Grid.Row="2}', 'Grid.Row="2"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "LoanEditWindow.xaml.cs").write_text("""using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class LoanEditWindow : Window
{
    private readonly LoanEditViewModel _vm;
    public LoanEditWindow(Loan? loan = null)
    {
        InitializeComponent();
        _vm = new LoanEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (loan != null) _vm.Id = loan.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
""", encoding="utf-8")

(VIEWS / "LoansView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.LoansView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="LoansView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0} LastChildFill="False">
            <TextBlock Text="القروض" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" Margin="0,0,0,20">
                <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Margin="0,0,10,0"/>
                <Button Content="+ قرض جديد" Style="{StaticResource PrimaryButton}" Command="{Binding NewCommand}"/>
            </StackPanel>
        </DockPanel>
        <Border Grid.Row="1} Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding Loans}" SelectedItem="{Binding SelectedLoan}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      SelectionMode="Single" HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="رقم القرض" Binding="{Binding LoanNumber}" Width="140"/>
                    <DataGridTextColumn Header="المقترض" Binding="{Binding BorrowerName}" Width="*"/>
                    <DataGridTextColumn Header="المبلغ" Binding="{Binding PrincipalAmount, StringFormat=N2}" Width="120"/>
                    <DataGridTextColumn Header="الإجمالي" Binding="{Binding TotalAmount, StringFormat=N2}" Width="120"/>
                    <DataGridTextColumn Header="المتبقي" Binding="{Binding RemainingAmount, StringFormat=N2}" Width="120"/>
                    <DataGridTextColumn Header="الحالة" Binding="{Binding Status}" Width="80"/>
                    <DataGridTemplateColumn Header="إجراءات" Width="100">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                    <Button Content="✏️" Margin="2" Padding="6,2" Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                    <Button Content="🗑️" Margin="2" Padding="6,2" Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
""", encoding="utf-8")

xaml_path = VIEWS / "LoansView.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"').replace('Grid.Row="1}', 'Grid.Row="1"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "LoansView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class LoansView : UserControl
{
    public LoansView() { InitializeComponent(); }
    private async void LoansView_Loaded(object sender, RoutedEventArgs e) { if (DataContext is LoansViewModel vm) await vm.LoadAsync(); }
}
""", encoding="utf-8")

# ========================================================
# 5. EMPLOYEES PAGE (Simple CRUD)
# ========================================================

(VM / "EmployeesViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class EmployeesViewModel : ViewModelBase
{
    private ObservableCollection<Employee> _employees = new();
    private Employee? _selectedEmployee;
    public ObservableCollection<Employee> Employees { get => _employees; set => SetProperty(ref _employees, value); }
    public Employee? SelectedEmployee { get => _selectedEmployee; set => SetProperty(ref _selectedEmployee, value); }
    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public EmployeesViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedEmployee), () => SelectedEmployee != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedEmployee != null);
    }

    public override async Task LoadAsync()
    {
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); Employees = new ObservableCollection<Employee>(await db.Employees.OrderBy(e => e.FullName).ToListAsync()); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void OpenEdit(Employee? emp)
    {
        var window = new EmployeeEditWindow(emp) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedEmployee == null) return;
        var c = System.Windows.MessageBox.Show("Delete: " + SelectedEmployee.FullName + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (c != System.Windows.MessageBoxResult.Yes) return;
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var e = await db.Employees.FirstOrDefaultAsync(x => x.Id == SelectedEmployee.Id); if (e != null) { db.Employees.Remove(e); await db.SaveChangesAsync(); await LoadAsync(); } }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
""", encoding="utf-8")

(VM / "EmployeeEditViewModel.cs").write_text("""using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class EmployeeEditViewModel : ViewModelBase
{
    private int _id;
    private string _fullName = "";
    private string _phone = "";
    private string _jobTitle = "";
    private DateTime _hireDate = DateTime.Today;
    private double _basicSalary;
    private double _allowances;
    private string _statusMessage = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string JobTitle { get => _jobTitle; set => SetProperty(ref _jobTitle, value); }
    public DateTime HireDate { get => _hireDate; set => SetProperty(ref _hireDate, value); }
    public double BasicSalary { get => _basicSalary; set => SetProperty(ref _basicSalary, value); }
    public double Allowances { get => _allowances; set => SetProperty(ref _allowances, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "تعديل موظف" : "موظف جديد";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public EmployeeEditViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
    }

    public async override Task LoadAsync()
    {
        if (Id > 0) { try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var e = await db.Employees.FirstOrDefaultAsync(x => x.Id == Id); if (e != null) { FullName = e.FullName; Phone = e.Phone ?? ""; JobTitle = e.JobTitle; HireDate = e.HireDate; BasicSalary = e.BasicSalary; Allowances = e.Allowances; } } catch { } }
        else { try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var count = await db.Employees.CountAsync(); } catch { } }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(FullName)) { StatusMessage = "Name required"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Employee e;
            if (Id > 0) { e = await db.Employees.FirstOrDefaultAsync(x => x.Id == Id); if (e == null) { StatusMessage = "Not found"; return; } }
            else { e = new Employee { CreatedAt = DateTime.UtcNow }; db.Employees.Add(e); }
            e.FullName = FullName; e.Phone = string.IsNullOrEmpty(Phone) ? null : Phone; e.JobTitle = JobTitle; e.HireDate = HireDate; e.BasicSalary = BasicSalary; e.Allowances = Allowances;
            await db.SaveChangesAsync();
            StatusMessage = "Saved OK"; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "EmployeeEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.EmployeeEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="500" Width="550"
        WindowStartupLocation="CenterOwner" Background="{StaticResource BackgroundBrush}" FlowDirection="RightToLeft">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <TextBlock Grid.Row="0} Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}"/>
        <Border Grid.Row="1} Style="{StaticResource Card}">
            <StackPanel>
                <StackPanel Margin="0,0,0,15"><TextBlock Text="الاسم:" Margin="0,0,0,5"/><TextBox Text="{Binding FullName, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/></StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0} Margin="0,0,10,15"><TextBlock Text="الهاتف:" Margin="0,0,0,5"/><TextBox Text="{Binding Phone, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/></StackPanel>
                    <StackPanel Grid.Column="1} Margin="10,0,0,15"><TextBlock Text="المسمى:" Margin="0,0,0,5"/><TextBox Text="{Binding JobTitle, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/></StackPanel>
                </Grid>
                <StackPanel Margin="0,0,0,15"><TextBlock Text="تاريخ التعيين:" Margin="0,0,0,5"/><DatePicker SelectedDate="{Binding HireDate}" Padding="8,6"/></StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0} Margin="0,0,10,15"><TextBlock Text="الراتب:" Margin="0,0,0,5"/><TextBox Text="{Binding BasicSalary, UpdateSourceTrigger=LostFocus}" Padding="8,6"/></StackPanel>
                    <StackPanel Grid.Column="1} Margin="10,0,0,15"><TextBlock Text="البدلات:" Margin="0,0,0,5"/><TextBox Text="{Binding Allowances, UpdateSourceTrigger=LostFocus}" Padding="8,6"/></StackPanel>
                </Grid>
            </StackPanel>
        </Border>
        <StackPanel Grid.Row="2} Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center" Margin="0,0,20,0"/>
            <Button Content="إلغاء" Style="{StaticResource PrimaryButton}" Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
            <Button Content="💾 حفظ" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</Window>
""", encoding="utf-8")

xaml_path = VIEWS / "EmployeeEditWindow.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"').replace('Grid.Row="1}', 'Grid.Row="1"').replace('Grid.Row="2}', 'Grid.Row="2"').replace('Grid.Column="0}', 'Grid.Column="0"').replace('Grid.Column="1}', 'Grid.Column="1"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "EmployeeEditWindow.xaml.cs").write_text("""using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class EmployeeEditWindow : Window
{
    private readonly EmployeeEditViewModel _vm;
    public EmployeeEditWindow(Employee? emp = null)
    {
        InitializeComponent();
        _vm = new EmployeeEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (emp != null) _vm.Id = emp.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
""", encoding="utf-8")

(VIEWS / "EmployeesView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.EmployeesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="EmployeesView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0} LastChildFill="False">
            <TextBlock Text="الموظفون" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" Margin="0,0,0,20">
                <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Margin="0,0,10,0"/>
                <Button Content="+ موظف جديد" Style="{StaticResource PrimaryButton}" Command="{Binding NewCommand}"/>
            </StackPanel>
        </DockPanel>
        <Border Grid.Row="1} Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding Employees}" SelectedItem="{Binding SelectedEmployee}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      SelectionMode="Single" HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="الاسم" Binding="{Binding FullName}" Width="*"/>
                    <DataGridTextColumn Header="المسمى" Binding="{Binding JobTitle}" Width="150"/>
                    <DataGridTextColumn Header="الهاتف" Binding="{Binding Phone}" Width="130"/>
                    <DataGridTextColumn Header="الراتب" Binding="{Binding BasicSalary, StringFormat=N2}" Width="120"/>
                    <DataGridTemplateColumn Header="إجراءات" Width="100">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                    <Button Content="✏️" Margin="2" Padding="6,2" Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                    <Button Content="🗑️" Margin="2" Padding="6,2" Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
""", encoding="utf-8")

xaml_path = VIEWS / "EmployeesView.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"').replace('Grid.Row="1}', 'Grid.Row="1"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "EmployeesView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class EmployeesView : UserControl
{
    public EmployeesView() { InitializeComponent(); }
    private async void EmployeesView_Loaded(object sender, RoutedEventArgs e) { if (DataContext is EmployeesViewModel vm) await vm.LoadAsync(); }
}
""", encoding="utf-8")

# ========================================================
# 6. COST CENTERS PAGE (Simple CRUD)
# ========================================================

(VM / "CostCentersViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class CostCentersViewModel : ViewModelBase
{
    private ObservableCollection<CostCenter> _costCenters = new();
    private CostCenter? _selectedCostCenter;
    public ObservableCollection<CostCenter> CostCenters { get => _costCenters; set => SetProperty(ref _costCenters, value); }
    public CostCenter? SelectedCostCenter { get => _selectedCostCenter; set => SetProperty(ref _selectedCostCenter, value); }
    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public CostCentersViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedCostCenter), () => SelectedCostCenter != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedCostCenter != null);
    }

    public override async Task LoadAsync()
    {
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); CostCenters = new ObservableCollection<CostCenter>(await db.CostCenters.OrderBy(c => c.Code).ToListAsync()); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void OpenEdit(CostCenter? cc)
    {
        var window = new CostCenterEditWindow(cc) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedCostCenter == null) return;
        var c = System.Windows.MessageBox.Show("Delete: " + SelectedCostCenter.Name + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (c != System.Windows.MessageBoxResult.Yes) return;
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var cc = await db.CostCenters.FirstOrDefaultAsync(x => x.Id == SelectedCostCenter.Id); if (cc != null) { db.CostCenters.Remove(cc); await db.SaveChangesAsync(); await LoadAsync(); } }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
""", encoding="utf-8")

(VM / "CostCenterEditViewModel.cs").write_text("""using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class CostCenterEditViewModel : ViewModelBase
{
    private int _id;
    private string _code = "";
    private string _name = "";
    private string _statusMessage = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "تعديل مركز تكلفة" : "مركز تكلفة جديد";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public CostCenterEditViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
    }

    public async override Task LoadAsync()
    {
        if (Id > 0) { try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var c = await db.CostCenters.FirstOrDefaultAsync(x => x.Id == Id); if (c != null) { Code = c.Code; Name = c.Name; } } catch { } }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name)) { StatusMessage = "Name required"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            CostCenter c;
            if (Id > 0) { c = await db.CostCenters.FirstOrDefaultAsync(x => x.Id == Id); if (c == null) { StatusMessage = "Not found"; return; } }
            else { c = new CostCenter { CreatedAt = DateTime.UtcNow }; db.CostCenters.Add(c); }
            c.Code = Code; c.Name = Name; c.IsActive = true;
            await db.SaveChangesAsync();
            StatusMessage = "Saved OK"; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "CostCenterEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.CostCenterEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="350" Width="450"
        WindowStartupLocation="CenterOwner" Background="{StaticResource BackgroundBrush}" FlowDirection="RightToLeft">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <TextBlock Grid.Row="0} Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}"/>
        <Border Grid.Row="1} Style="{StaticResource Card}">
            <StackPanel>
                <StackPanel Margin="0,0,0,15"><TextBlock Text="الكود:" Margin="0,0,0,5"/><TextBox Text="{Binding Code, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/></StackPanel>
                <StackPanel Margin="0,0,0,15"><TextBlock Text="الاسم:" Margin="0,0,0,5"/><TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/></StackPanel>
            </StackPanel>
        </Border>
        <StackPanel Grid.Row="2} Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center" Margin="0,0,20,0"/>
            <Button Content="إلغاء" Style="{StaticResource PrimaryButton}" Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
            <Button Content="💾 حفظ" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</Window>
""", encoding="utf-8")

xaml_path = VIEWS / "CostCenterEditWindow.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"').replace('Grid.Row="1}', 'Grid.Row="1"').replace('Grid.Row="2}', 'Grid.Row="2"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "CostCenterEditWindow.xaml.cs").write_text("""using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class CostCenterEditWindow : Window
{
    private readonly CostCenterEditViewModel _vm;
    public CostCenterEditWindow(CostCenter? cc = null)
    {
        InitializeComponent();
        _vm = new CostCenterEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (cc != null) _vm.Id = cc.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
""", encoding="utf-8")

(VIEWS / "CostCentersView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.CostCentersView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="CostCentersView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0} LastChildFill="False">
            <TextBlock Text="مراكز التكلفة" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" Margin="0,0,0,20">
                <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Margin="0,0,10,0"/>
                <Button Content="+ مركز جديد" Style="{StaticResource PrimaryButton}" Command="{Binding NewCommand}"/>
            </StackPanel>
        </DockPanel>
        <Border Grid.Row="1} Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding CostCenters}" SelectedItem="{Binding SelectedCostCenter}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      SelectionMode="Single" HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="الكود" Binding="{Binding Code}" Width="120"/>
                    <DataGridTextColumn Header="الاسم" Binding="{Binding Name}" Width="*"/>
                    <DataGridTemplateColumn Header="إجراءات" Width="100">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                    <Button Content="✏️" Margin="2" Padding="6,2" Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                    <Button Content="🗑️" Margin="2" Padding="6,2" Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
""", encoding="utf-8")

xaml_path = VIEWS / "CostCentersView.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"').replace('Grid.Row="1}', 'Grid.Row="1"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "CostCentersView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class CostCentersView : UserControl
{
    public CostCentersView() { InitializeComponent(); }
    private async void CostCentersView_Loaded(object sender, RoutedEventArgs e) { if (DataContext is CostCentersViewModel vm) await vm.LoadAsync(); }
}
""", encoding="utf-8")

# ========================================================
# 7. RECURRING INVOICES PAGE (Simple list)
# ========================================================

(VM / "RecurringInvoicesViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class RecurringInvoicesViewModel : ViewModelBase
{
    private ObservableCollection<RecurringInvoiceTemplate> _templates = new();
    public ObservableCollection<RecurringInvoiceTemplate> Templates { get => _templates; set => SetProperty(ref _templates, value); }
    public ICommand RefreshCommand { get; }

    public RecurringInvoicesViewModel() { RefreshCommand = new RelayCommand(async () => await LoadAsync()); }

    public override async Task LoadAsync()
    {
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); Templates = new ObservableCollection<RecurringInvoiceTemplate>(await db.RecurringInvoiceTemplates.Include(t => t.Party).OrderByDescending(t => t.NextRunDate).ToListAsync()); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }
}
""", encoding="utf-8")

(VIEWS / "RecurringInvoicesView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.RecurringInvoicesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="RecurringInvoicesView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0} LastChildFill="False">
            <TextBlock Text="الفواتير المتكررة" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" DockPanel.Dock="Right" Margin="0,0,0,20"/>
        </DockPanel>
        <Border Grid.Row="1} Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding Templates}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#E0E0E0"
                      ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="الاسم" Binding="{Binding Name}" Width="*"/>
                    <DataGridTextColumn Header="العميل" Binding="{Binding Party.Name}" Width="150"/>
                    <DataGridTextColumn Header="التكرار" Binding="{Binding Frequency}" Width="100"/>
                    <DataGridTextColumn Header="التشغيل التالي" Binding="{Binding NextRunDate, StringFormat=yyyy-MM-dd}" Width="130"/>
                    <DataGridCheckBoxColumn Header="نشط" Binding="{Binding IsActive}" Width="60"/>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
""", encoding="utf-8")

xaml_path = VIEWS / "RecurringInvoicesView.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"').replace('Grid.Row="1}', 'Grid.Row="1"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "RecurringInvoicesView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class RecurringInvoicesView : UserControl
{
    public RecurringInvoicesView() { InitializeComponent(); }
    private async void RecurringInvoicesView_Loaded(object sender, RoutedEventArgs e) { if (DataContext is RecurringInvoicesViewModel vm) await vm.LoadAsync(); }
}
""", encoding="utf-8")

# ========================================================
# 8. UPDATE MainViewModel + MainWindow (Add all 13 pages)
# ========================================================

(VM / "MainViewModel.cs").write_text("""using System.Windows.Input;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentView;
    private string _activeButton = "Dashboard";
    private string _currentUserDisplay = "";
    private bool _isLoggedIn = false;
    private string _username = "";
    private string _password = "";
    private string _errorMessage = "";

    public ViewModelBase CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }
    public string ActiveButton { get => _activeButton; set => SetProperty(ref _activeButton, value); }
    public string CurrentUserDisplay { get => _currentUserDisplay; set => SetProperty(ref _currentUserDisplay, value); }
    public bool IsLoggedIn { get => _isLoggedIn; set { if (SetProperty(ref _isLoggedIn, value)) { OnPropertyChanged(nameof(IsLoginScreen)); OnPropertyChanged(nameof(IsAppVisible)); } } }
    public bool IsLoginScreen => !IsLoggedIn;
    public bool IsAppVisible => IsLoggedIn;
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public ICommand LoginCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    public MainViewModel()
    {
        _currentView = new ViewModelBase();
        LoginCommand = new RelayCommand(async () => await LoginAsync());
        NavigateCommand = new RelayCommand<string>(Navigate);
        LogoutCommand = new RelayCommand(Logout);
    }

    private async Task LoginAsync()
    {
        ErrorMessage = "";
        var result = await AuthService.LoginAsync(Username ?? "", Password ?? "");
        if (result.Success)
        {
            if (AuthService.CurrentUser != null) CurrentUserDisplay = AuthService.CurrentUser.FullName + " (" + AuthService.CurrentUser.Role + ")";
            IsLoggedIn = true; Navigate("Dashboard");
        }
        else ErrorMessage = result.Message;
    }

    private void Navigate(string? view)
    {
        if (string.IsNullOrEmpty(view) || !IsLoggedIn) return;
        ActiveButton = view;
        CurrentView = view switch
        {
            "Dashboard" => new DashboardViewModel(),
            "Invoices" => new InvoicesViewModel(),
            "Parties" => new PartiesViewModel(),
            "Items" => new ItemsViewModel(),
            "Accounts" => new AccountsViewModel(),
            "Payments" => new PaymentsViewModel(),
            "Reports" => new ReportsViewModel(),
            "Settings" => new SettingsViewModel(),
            "Warehouses" => new WarehousesViewModel(),
            "StockLevels" => new StockLevelsViewModel(),
            "Loans" => new LoansViewModel(),
            "Employees" => new EmployeesViewModel(),
            "CostCenters" => new CostCentersViewModel(),
            "RecurringInvoices" => new RecurringInvoicesViewModel(),
            _ => new ViewModelBase()
        };
    }

    private void Logout()
    {
        AuthService.Logout();
        IsLoggedIn = false; Username = ""; Password = ""; ErrorMessage = ""; CurrentUserDisplay = "";
    }
}
""", encoding="utf-8")

(UI / "MainWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:Techtoon.UI.ViewModels"
        xmlns:views="clr-namespace:Techtoon.UI.Views"
        Title="Techtoon Accounting" Height="750" Width="1280"
        WindowStartupLocation="CenterScreen" Background="{StaticResource BackgroundBrush}"
        FlowDirection="RightToLeft" UseLayoutRounding="True" TextOptions.TextFormattingMode="Display">

    <Window.DataContext>
        <vm:MainViewModel/>
    </Window.DataContext>

    <Window.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
        <DataTemplate DataType="{x:Type vm:DashboardViewModel}"><views:DashboardView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:InvoicesViewModel}"><views:InvoicesView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:PartiesViewModel}"><views:PartiesView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:ItemsViewModel}"><views:ItemsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:AccountsViewModel}"><views:AccountsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:PaymentsViewModel}"><views:PaymentsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:ReportsViewModel}"><views:ReportsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:SettingsViewModel}"><views:SettingsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:WarehousesViewModel}"><views:WarehousesView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:StockLevelsViewModel}"><views:StockLevelsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:LoansViewModel}"><views:LoansView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:EmployeesViewModel}"><views:EmployeesView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:CostCentersViewModel}"><views:CostCentersView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:RecurringInvoicesViewModel}"><views:RecurringInvoicesView/></DataTemplate>
    </Window.Resources>

    <Grid>
        <Grid Visibility="{Binding IsLoginScreen, Converter={StaticResource BoolToVis}}">
            <Grid.Background>
                <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                    <GradientStop Color="#263238" Offset="0"/>
                    <GradientStop Color="#00695C" Offset="1"/>
                </LinearGradientBrush>
            </Grid.Background>
            <Border HorizontalAlignment="Center" VerticalAlignment="Center" Background="White" CornerRadius="8" Padding="40" Width="400">
                <StackPanel>
                    <TextBlock Text="Techtoon" FontSize="36" FontWeight="Bold" Foreground="{StaticResource PrimaryBrush}" HorizontalAlignment="Center"/>
                    <TextBlock Text="Accounting System" FontSize="14" Foreground="{StaticResource TextSecondaryBrush}" HorizontalAlignment="Center" Margin="0,5,0,25"/>
                    <TextBlock Text="تسجيل الدخول" FontSize="20" FontWeight="Bold" Margin="0,0,0,20" HorizontalAlignment="Center"/>
                    <TextBlock Text="اسم المستخدم:" Margin="0,0,0,5"/>
                    <TextBox x:Name="UsernameBox" Padding="10,8" FontSize="14" Margin="0,0,0,15" Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" KeyDown="UsernameBox_KeyDown"/>
                    <TextBlock Text="كلمة المرور:" Margin="0,0,0,5"/>
                    <PasswordBox x:Name="PasswordBox" Padding="10,8" FontSize="14" Margin="0,0,0,15" KeyDown="PasswordBox_KeyDown"/>
                    <TextBlock Text="{Binding ErrorMessage}" Foreground="{StaticResource ErrorBrush}" Margin="0,0,0,15" TextWrapping="Wrap"/>
                    <Button Content="🔐 تسجيل الدخول" Style="{StaticResource PrimaryButton}" Click="LoginButton_Click" Padding="0,12" FontSize="15" Margin="0,5,0,15"/>
                    <Border Background="#E3F2FD" CornerRadius="4" Padding="15,10">
                        <StackPanel>
                            <TextBlock Text="بيانات الدخول الافتراضية:" FontWeight="SemiBold" Foreground="#1565C0" Margin="0,0,0,5"/>
                            <TextBlock Text="admin / admin123" Foreground="#1565C0" FontSize="13"/>
                        </StackPanel>
                    </Border>
                </StackPanel>
            </Border>
        </Grid>

        <Grid Visibility="{Binding IsAppVisible, Converter={StaticResource BoolToVis}}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="240"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Border Grid.Column="0" Background="{StaticResource SidebarBrush}">
                <DockPanel>
                    <Border DockPanel.Dock="Top" Padding="20,25,20,25" Background="{StaticResource PrimaryDarkBrush}">
                        <StackPanel>
                            <TextBlock Text="Techtoon" Foreground="White" FontSize="22" FontWeight="Bold"/>
                            <TextBlock Text="Accounting System" Foreground="#B2DFDB" FontSize="11"/>
                        </StackPanel>
                    </Border>
                    <Border DockPanel.Dock="Bottom" Background="#1A232A" Padding="15">
                        <StackPanel>
                            <TextBlock Text="المستخدم:" Foreground="#78909C" FontSize="10" Margin="0,0,0,3"/>
                            <TextBlock Text="{Binding CurrentUserDisplay}" Foreground="White" FontSize="12" FontWeight="SemiBold" TextWrapping="Wrap" Margin="0,0,0,10"/>
                            <Button Content="🚪 تسجيل الخروج" Style="{StaticResource SidebarButton}" Command="{Binding LogoutCommand}" HorizontalContentAlignment="Center" Background="#D32F2F" Foreground="White"/>
                            <TextBlock Text="v2.0.0" Foreground="#546E7A" FontSize="10" Margin="0,10,0,0" HorizontalAlignment="Center"/>
                        </StackPanel>
                    </Border>
                    <ScrollViewer VerticalScrollBarVisibility="Auto">
                        <StackPanel Margin="0,10">
                            <Button Content="  🏠 لوحة التحكم" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Dashboard"/>
                            <Button Content="  🛒 الفواتير" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Invoices"/>
                            <Button Content="  👥 العملاء والموردون" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Parties"/>
                            <Button Content="  📦 الأصناف" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Items"/>
                            <Button Content="  🌳 شجرة الحسابات" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Accounts"/>
                            <Button Content="  💰 المدفوعات" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Payments"/>
                            <Button Content="  📊 التقارير" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Reports"/>
                            <Button Content="  ⚙️ الإعدادات" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Settings"/>
                            <Button Content="  🏪 المخازن" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Warehouses"/>
                            <Button Content="  📊 أرصدة المخزون" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="StockLevels"/>
                            <Button Content="  🏦 القروض" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Loans"/>
                            <Button Content="  👨‍💼 الموظفون" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Employees"/>
                            <Button Content="  🏢 مراكز التكلفة" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="CostCenters"/>
                            <Button Content="  🔄 الفواتير المتكررة" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="RecurringInvoices"/>
                        </StackPanel>
                    </ScrollViewer>
                </DockPanel>
            </Border>
            <ContentControl Grid.Column="1" Content="{Binding CurrentView}"/>
        </Grid>
    </Grid>
</Window>
""", encoding="utf-8")

print("=== All Remaining Pages Built ===")
print("  - Services: InventoryService + ImportExportService")
print("  - Warehouses (ViewModel + EditWindow + View)")
print("  - StockLevels (ViewModel + View)")
print("  - Loans (ViewModel + EditWindow + View)")
print("  - Employees (ViewModel + EditWindow + View)")
print("  - CostCenters (ViewModel + EditWindow + View)")
print("  - RecurringInvoices (ViewModel + View)")
print("  - MainViewModel + MainWindow updated with 14 buttons")