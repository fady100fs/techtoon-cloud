# build_core_pages.py
from pathlib import Path

ROOT = Path(r"D:\PROG\Accounting Prog\Techtoon Accounting C+")
UI = ROOT / "Techtoon.UI"
VM = UI / "ViewModels"
VIEWS = UI / "Views"
SERVICES = UI / "Services"

for d in [VM, VIEWS, SERVICES]:
    d.mkdir(parents=True, exist_ok=True)

# ========================================================
# 1. PartiesViewModel + PartyEditViewModel + Views
# ========================================================

(VM / "PartiesViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class PartiesViewModel : ViewModelBase
{
    private ObservableCollection<Party> _parties = new();
    private string _searchText = "";
    private Party? _selectedParty;

    public ObservableCollection<Party> Parties { get => _parties; set => SetProperty(ref _parties, value); }
    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) ApplyFilter(); } }
    public Party? SelectedParty { get => _selectedParty; set => SetProperty(ref _selectedParty, value); }

    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public PartiesViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedParty), () => SelectedParty != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedParty != null);
    }

    public override async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var list = await db.Parties.Include(p => p.Account).OrderBy(p => p.Name).Take(500).ToListAsync();
            Parties = new ObservableCollection<Party>(list);
            ApplyFilter();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        var s = SearchText.Trim().ToLower();
        var filtered = Parties.Where(p => (p.Name?.ToLower().Contains(s) == true) || (p.Phone?.ToLower().Contains(s) == true)).ToList();
        Parties = new ObservableCollection<Party>(filtered);
    }

    private void OpenEdit(Party? party)
    {
        var window = new PartyEditWindow(party) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedParty == null) return;
        var confirm = System.Windows.MessageBox.Show("Delete: " + SelectedParty.Name + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var p = await db.Parties.FirstOrDefaultAsync(x => x.Id == SelectedParty.Id);
            if (p != null) { db.Parties.Remove(p); await db.SaveChangesAsync(); await LoadAsync(); }
        }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
""", encoding="utf-8")

(VM / "PartyEditViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class PartyEditViewModel : ViewModelBase
{
    private int _id;
    private string _name = "";
    private string _type = "customer";
    private string _phone = "";
    private string _address = "";
    private double _creditLimit;
    private double _openingBalance;
    private Account? _account;
    private ObservableCollection<Account> _accounts = new();
    private string _statusMessage = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Type { get => _type; set => SetProperty(ref _type, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    public double CreditLimit { get => _creditLimit; set => SetProperty(ref _creditLimit, value); }
    public double OpeningBalance { get => _openingBalance; set => SetProperty(ref _openingBalance, value); }
    public Account? Account { get => _account; set => SetProperty(ref _account, value); }
    public ObservableCollection<Account> Accounts { get => _accounts; set => SetProperty(ref _accounts, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "تعديل عميل/مورد" : "عميل/مورد جديد";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public PartyEditViewModel()
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
            Accounts = new ObservableCollection<Account>(await db.Accounts.OrderBy(a => a.Code).ToListAsync());
            if (Id > 0)
            {
                var p = await db.Parties.Include(x => x.Account).FirstOrDefaultAsync(x => x.Id == Id);
                if (p != null)
                {
                    Name = p.Name; Type = p.Type; Phone = p.Phone ?? ""; Address = p.Address ?? "";
                    CreditLimit = p.CreditLimit; OpeningBalance = p.OpeningBalance;
                    Account = Accounts.FirstOrDefault(a => a.Id == p.AccountId);
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
            Party p;
            if (Id > 0) { p = await db.Parties.FirstOrDefaultAsync(x => x.Id == Id); if (p == null) { StatusMessage = "Not found"; return; } }
            else { p = new Party(); db.Parties.Add(p); }
            p.Name = Name; p.Type = Type; p.Phone = string.IsNullOrEmpty(Phone) ? null : Phone;
            p.Address = string.IsNullOrEmpty(Address) ? null : Address; p.CreditLimit = CreditLimit; p.OpeningBalance = OpeningBalance;
            if (Account != null) { var acc = await db.Accounts.FirstOrDefaultAsync(a => a.Id == Account.Id); if (acc != null) p.AccountId = acc.Id; }
            await db.SaveChangesAsync();
            StatusMessage = "Saved OK"; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "PartyEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.PartyEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="560" Width="600"
        WindowStartupLocation="CenterOwner" Background="{StaticResource BackgroundBrush}" FlowDirection="RightToLeft">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <TextBlock Grid.Row="0" Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}"/>
        <Border Grid.Row="1" Style="{StaticResource Card}">
            <StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15">
                        <TextBlock Text="الاسم:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0,0,15">
                        <TextBlock Text="النوع:" Margin="0,0,0,5"/>
                        <ComboBox SelectedValue="{Binding Type}" SelectedValuePath="Tag" Padding="8,6">
                            <ComboBoxItem Content="عميل" Tag="customer"/>
                            <ComboBoxItem Content="مورد" Tag="supplier"/>
                            <ComboBoxItem Content="عميل ومورد" Tag="both"/>
                        </ComboBox>
                    </StackPanel>
                </Grid>
                <StackPanel Margin="0,0,0,15">
                    <TextBlock Text="الهاتف:" Margin="0,0,0,5"/>
                    <TextBox Text="{Binding Phone, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                </StackPanel>
                <StackPanel Margin="0,0,0,15">
                    <TextBlock Text="العنوان:" Margin="0,0,0,5"/>
                    <TextBox Text="{Binding Address, UpdateSourceTrigger=PropertyChanged}" Padding="8,6" AcceptsReturn="True" TextWrapping="Wrap" Height="60"/>
                </StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15">
                        <TextBlock Text="حد الائتمان:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding CreditLimit, UpdateSourceTrigger=LostFocus}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0,0,15">
                        <TextBlock Text="الرصيد الافتتاحي:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding OpeningBalance, UpdateSourceTrigger=LostFocus}" Padding="8,6"/>
                    </StackPanel>
                </Grid>
                <StackPanel Margin="0,0,0,15">
                    <TextBlock Text="الحساب المرتبط:" Margin="0,0,0,5"/>
                    <ComboBox ItemsSource="{Binding Accounts}" DisplayMemberPath="Name" SelectedItem="{Binding Account}" Padding="8,6"/>
                </StackPanel>
            </StackPanel>
        </Border>
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center" Margin="0,0,20,0" TextWrapping="Wrap" MaxWidth="350"/>
            <Button Content="إلغاء" Style="{StaticResource PrimaryButton}" Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
            <Button Content="💾 حفظ" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</Window>
""", encoding="utf-8")

(VIEWS / "PartyEditWindow.xaml.cs").write_text("""using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class PartyEditWindow : Window
{
    private readonly PartyEditViewModel _vm;
    public PartyEditWindow(Party? party = null)
    {
        InitializeComponent();
        _vm = new PartyEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (party != null) _vm.Id = party.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
""", encoding="utf-8")

(VIEWS / "PartiesView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.PartiesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="PartiesView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0" LastChildFill="False">
            <TextBlock Text="العملاء والموردون" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" Margin="0,0,0,20">
                <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Margin="0,0,10,0"/>
                <Button Content="+ عميل/مورد جديد" Style="{StaticResource PrimaryButton}" Command="{Binding NewCommand}"/>
            </StackPanel>
        </DockPanel>
        <Border Grid.Row="1" Style="{StaticResource Card}" Margin="0,0,0,15" Padding="15">
            <DockPanel>
                <TextBlock Text="🔍" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" Width="400" Padding="8,6" VerticalAlignment="Center"/>
            </DockPanel>
        </Border>
        <Border Grid.Row="2" Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding Parties}" SelectedItem="{Binding SelectedParty}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      SelectionMode="Single" HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="الاسم" Binding="{Binding Name}" Width="*"/>
                    <DataGridTextColumn Header="النوع" Binding="{Binding Type}" Width="100"/>
                    <DataGridTextColumn Header="الهاتف" Binding="{Binding Phone}" Width="130"/>
                    <DataGridTextColumn Header="حد الائتمان" Binding="{Binding CreditLimit, StringFormat=N2}" Width="120"/>
                    <DataGridTextColumn Header="الرصيد الافتتاحي" Binding="{Binding OpeningBalance, StringFormat=N2}" Width="120"/>
                    <DataGridTextColumn Header="الحساب" Binding="{Binding Account.Name}" Width="150"/>
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

(VIEWS / "PartiesView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class PartiesView : UserControl
{
    public PartiesView() { InitializeComponent(); }
    private async void PartiesView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PartiesViewModel vm) await vm.LoadAsync();
    }
}
""", encoding="utf-8")

# ========================================================
# 2. ItemsViewModel + ItemEditViewModel + Views
# ========================================================

(VM / "ItemsViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class ItemsViewModel : ViewModelBase
{
    private ObservableCollection<Item> _items = new();
    private string _searchText = "";
    private Item? _selectedItem;

    public ObservableCollection<Item> Items { get => _items; set => SetProperty(ref _items, value); }
    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) ApplyFilter(); } }
    public Item? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public ItemsViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedItem), () => SelectedItem != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedItem != null);
    }

    public override async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var list = await db.Items.Include(i => i.Category).OrderBy(i => i.Name).Take(500).ToListAsync();
            var stocks = await db.StockLevels.GroupBy(s => s.ItemId).Select(g => new { ItemId = g.Key, Qty = g.Sum(s => s.Quantity) }).ToListAsync();
            foreach (var item in list) { var st = stocks.FirstOrDefault(s => s.ItemId == item.Id); item.CurrentStock = st?.Qty ?? 0; }
            Items = new ObservableCollection<Item>(list);
            ApplyFilter();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        var s = SearchText.Trim().ToLower();
        var filtered = Items.Where(i => (i.Name?.ToLower().Contains(s) == true) || (i.Barcode?.ToLower().Contains(s) == true) || (i.DisplayCode?.ToLower().Contains(s) == true)).ToList();
        Items = new ObservableCollection<Item>(filtered);
    }

    private void OpenEdit(Item? item)
    {
        var window = new ItemEditWindow(item) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem == null) return;
        var confirm = System.Windows.MessageBox.Show("Delete: " + SelectedItem.Name + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var item = await db.Items.FirstOrDefaultAsync(x => x.Id == SelectedItem.Id);
            if (item != null) { db.Items.Remove(item); await db.SaveChangesAsync(); await LoadAsync(); }
        }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
""", encoding="utf-8")

(VM / "ItemEditViewModel.cs").write_text("""using System.Collections.ObjectModel;
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
    public string WindowTitle => IsEdit ? "تعديل صنف" : "صنف جديد";
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

    public async override Task LoadAsync()
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
            Item item; bool isNew = false;
            if (Id > 0) { item = await db.Items.FirstOrDefaultAsync(i => i.Id == Id); if (item == null) { StatusMessage = "Not found"; return; } }
            else { item = new Item(); db.Items.Add(item); isNew = true; }
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
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "ItemEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.ItemEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="560" Width="600"
        WindowStartupLocation="CenterOwner" Background="{StaticResource BackgroundBrush}" FlowDirection="RightToLeft">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <TextBlock Grid.Row="0" Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}"/>
        <Border Grid.Row="1" Style="{StaticResource Card}">
            <StackPanel>
                <StackPanel Margin="0,0,0,15">
                    <TextBlock Text="اسم الصنف:" Margin="0,0,0,5"/>
                    <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                </StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15">
                        <TextBlock Text="الباركود:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding Barcode, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0,0,15">
                        <TextBlock Text="التصنيف:" Margin="0,0,0,5"/>
                        <ComboBox ItemsSource="{Binding Categories}" DisplayMemberPath="Name" SelectedItem="{Binding Category}" Padding="8,6"/>
                    </StackPanel>
                </Grid>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15">
                        <TextBlock Text="سعر التكلفة:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding CostPrice, UpdateSourceTrigger=LostFocus}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0">
                        <TextBlock Text="سعر البيع:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding SellPrice, UpdateSourceTrigger=LostFocus}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Grid.Column="2" Margin="10,0,0,15">
                        <TextBlock Text="هامش الربح %:" Margin="0,0,0,5"/>
                        <TextBlock Text="{Binding ProfitMargin}" Padding="8,6" Foreground="{StaticResource PrimaryBrush}" FontWeight="Bold"/>
                    </StackPanel>
                </Grid>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15">
                        <TextBlock Text="الحد الأدنى:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding MinStock, UpdateSourceTrigger=LostFocus}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0,0,15">
                        <TextBlock Text="الرصيد الحالي:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding OpeningQuantity, UpdateSourceTrigger=LostFocus}" Padding="8,6"/>
                    </StackPanel>
                </Grid>
            </StackPanel>
        </Border>
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center" Margin="0,0,20,0" TextWrapping="Wrap" MaxWidth="350"/>
            <Button Content="إلغاء" Style="{StaticResource PrimaryButton}" Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
            <Button Content="💾 حفظ" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</Window>
""", encoding="utf-8")

(VIEWS / "ItemEditWindow.xaml.cs").write_text("""using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class ItemEditWindow : Window
{
    private readonly ItemEditViewModel _vm;
    public ItemEditWindow(Item? item = null)
    {
        InitializeComponent();
        _vm = new ItemEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (item != null) _vm.Id = item.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
""", encoding="utf-8")

(VIEWS / "ItemsView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.ItemsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="ItemsView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0" LastChildFill="False">
            <TextBlock Text="الأصناف" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" Margin="0,0,0,20">
                <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Margin="0,0,10,0"/>
                <Button Content="+ صنف جديد" Style="{StaticResource PrimaryButton}" Command="{Binding NewCommand}"/>
            </StackPanel>
        </DockPanel>
        <Border Grid.Row="1" Style="{StaticResource Card}" Margin="0,0,0,15" Padding="15">
            <DockPanel>
                <TextBlock Text="🔍" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" Width="400" Padding="8,6" VerticalAlignment="Center"/>
            </DockPanel>
        </Border>
        <Border Grid.Row="2" Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      SelectionMode="Single" HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="الكود" Binding="{Binding DisplayCode}" Width="120"/>
                    <DataGridTextColumn Header="الاسم" Binding="{Binding Name}" Width="*"/>
                    <DataGridTextColumn Header="الباركود" Binding="{Binding Barcode}" Width="120"/>
                    <DataGridTextColumn Header="التكلفة" Binding="{Binding CostPrice, StringFormat=N2}" Width="90"/>
                    <DataGridTextColumn Header="البيع" Binding="{Binding SellPrice, StringFormat=N2}" Width="90"/>
                    <DataGridTextColumn Header="الرصيد" Binding="{Binding CurrentStock, StringFormat=N2}" Width="90">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Center"/><Setter Property="FontWeight" Value="Bold"/><Setter Property="Foreground" Value="{StaticResource PrimaryBrush}"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
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

(VIEWS / "ItemsView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class ItemsView : UserControl
{
    public ItemsView() { InitializeComponent(); }
    private async void ItemsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ItemsViewModel vm) await vm.LoadAsync();
    }
}
""", encoding="utf-8")

# ========================================================
# 3. InvoicesViewModel + InvoiceEditViewModel + Views
# ========================================================

(VM / "InvoicesViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class InvoicesViewModel : ViewModelBase
{
    private ObservableCollection<Invoice> _invoices = new();
    private string _searchText = "";
    private Invoice? _selectedInvoice;

    public ObservableCollection<Invoice> Invoices { get => _invoices; set => SetProperty(ref _invoices, value); }
    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) ApplyFilter(); } }
    public Invoice? SelectedInvoice { get => _selectedInvoice; set => SetProperty(ref _selectedInvoice, value); }

    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public InvoicesViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedInvoice), () => SelectedInvoice != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedInvoice != null);
    }

    public override async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var list = await db.Invoices.Include(i => i.Party).OrderByDescending(i => i.Date).Take(200).ToListAsync();
            Invoices = new ObservableCollection<Invoice>(list);
            ApplyFilter();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        var s = SearchText.Trim().ToLower();
        var filtered = Invoices.Where(i => (i.InvoiceNumber?.ToLower().Contains(s) == true) || (i.Party?.Name?.ToLower().Contains(s) == true)).ToList();
        Invoices = new ObservableCollection<Invoice>(filtered);
    }

    private void OpenEdit(Invoice? invoice)
    {
        var window = new InvoiceEditWindow(invoice) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedInvoice == null) return;
        var confirm = System.Windows.MessageBox.Show("Delete: " + SelectedInvoice.InvoiceNumber + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var inv = await db.Invoices.FirstOrDefaultAsync(x => x.Id == SelectedInvoice.Id);
            if (inv != null) { db.Invoices.Remove(inv); await db.SaveChangesAsync(); await LoadAsync(); }
        }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
""", encoding="utf-8")

(VM / "InvoiceEditViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class InvoiceLineItem : INotifyPropertyChanged
{
    private Item? _item;
    private double _quantity = 1;
    private double _price;
    private double _total;
    public ObservableCollection<Item> AvailableItems { get; set; } = new();
    public Item? Item { get => _item; set { if (SetField(ref _item, value)) { if (_item != null) Price = _item.SellPrice; Recompute(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemName))); } } }
    public int ItemId => Item?.Id ?? 0;
    public string ItemName => Item?.Name ?? "";
    public double Quantity { get => _quantity; set { if (SetField(ref _quantity, value)) Recompute(); } }
    public double Price { get => _price; set { if (SetField(ref _price, value)) Recompute(); } }
    public double Total { get => _total; set => SetField(ref _total, value); }
    private void Recompute() { Total = Quantity * Price; }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); return true; }
}

public class InvoiceEditViewModel : ViewModelBase
{
    private int _id;
    private string _invoiceNumber = "";
    private DateTime _date = DateTime.Today;
    private Party? _party;
    private string _type = "sale";
    private double _discountPercentage;
    private double _taxRate = 14;
    private ObservableCollection<Party> _parties = new();
    private ObservableCollection<Item> _items = new();
    private ObservableCollection<InvoiceLineItem> _lines = new();
    private string _statusMessage = "";
    private double _subtotal;
    private double _taxAmount;
    private double _netAmount;

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string InvoiceNumber { get => _invoiceNumber; set => SetProperty(ref _invoiceNumber, value); }
    public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
    public Party? Party { get => _party; set => SetProperty(ref _party, value); }
    public string Type { get => _type; set => SetProperty(ref _type, value); }
    public double DiscountPercentage { get => _discountPercentage; set { if (SetProperty(ref _discountPercentage, value)) RecomputeTotals(); } }
    public double TaxRate { get => _taxRate; set { if (SetProperty(ref _taxRate, value)) RecomputeTotals(); } }
    public ObservableCollection<Party> Parties { get => _parties; set => SetProperty(ref _parties, value); }
    public ObservableCollection<Item> Items { get => _items; set => SetProperty(ref _items, value); }
    public ObservableCollection<InvoiceLineItem> Lines { get => _lines; set => SetProperty(ref _lines, value); }
    public double Subtotal { get => _subtotal; set => SetProperty(ref _subtotal, value); }
    public double TaxAmount { get => _taxAmount; set => SetProperty(ref _taxAmount, value); }
    public double NetAmount { get => _netAmount; set => SetProperty(ref _netAmount, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "تعديل فاتورة" : "فاتورة جديدة";

    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public InvoiceEditViewModel()
    {
        AddLineCommand = new RelayCommand(AddLine);
        RemoveLineCommand = new RelayCommand<InvoiceLineItem>(RemoveLine);
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
        _lines.CollectionChanged += (s, e) => { if (e.NewItems != null) foreach (InvoiceLineItem l in e.NewItems) { l.PropertyChanged += Line_PropertyChanged; l.AvailableItems = Items; } RecomputeTotals(); };
    }

    public async override Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Parties = new ObservableCollection<Party>(await db.Parties.OrderBy(p => p.Name).ToListAsync());
            Items = new ObservableCollection<Item>(await db.Items.OrderBy(i => i.Name).ToListAsync());
            foreach (var line in Lines) line.AvailableItems = Items;
            if (Id == 0)
            {
                var prefix = Type == "sale" ? "SAL-" : "PUR-";
                var year = DateTime.Now.Year;
                var count = await db.Invoices.CountAsync(i => i.InvoiceNumber.StartsWith(prefix + year));
                InvoiceNumber = prefix + year + "-" + (count + 1).ToString("D4");
            }
            else
            {
                var inv = await db.Invoices.Include(i => i.Party).Include(i => i.Lines).ThenInclude(l => l.Item).FirstOrDefaultAsync(i => i.Id == Id);
                if (inv != null)
                {
                    InvoiceNumber = inv.InvoiceNumber; Date = inv.Date; Type = inv.Type; Party = Parties.FirstOrDefault(p => p.Id == inv.PartyId);
                    DiscountPercentage = inv.DiscountPercentage; TaxRate = inv.TaxRate;
                    Lines.Clear();
                    foreach (var line in inv.Lines)
                    {
                        var l = new InvoiceLineItem { Item = Items.FirstOrDefault(i => i.Id == line.ItemId), Quantity = line.Quantity, Price = line.Price };
                        l.AvailableItems = Items; Lines.Add(l);
                    }
                    RecomputeTotals();
                }
            }
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private void AddLine() { if (Items.Count == 0) { StatusMessage = "No items"; return; } var line = new InvoiceLineItem { AvailableItems = Items }; Lines.Add(line); }
    private void RemoveLine(InvoiceLineItem? line) { if (line != null) Lines.Remove(line); }
    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(InvoiceLineItem.Total)) RecomputeTotals(); }
    private void RecomputeTotals() { Subtotal = Lines.Sum(l => l.Total); var afterDiscount = Subtotal * (1 - DiscountPercentage / 100); TaxAmount = afterDiscount * TaxRate / 100; NetAmount = afterDiscount + TaxAmount; }

    private async Task SaveAsync()
    {
        try
        {
            if (Party == null) { StatusMessage = "Select party"; return; }
            if (Lines.Count == 0) { StatusMessage = "Add at least one line"; return; }
            if (Lines.Any(l => l.Item == null)) { StatusMessage = "Select item for all lines"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Invoice invoice;
            if (Id > 0)
            {
                invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == Id) ?? new Invoice();
                db.InvoiceLines.RemoveRange(invoice.Lines);
            }
            else { invoice = new Invoice(); db.Invoices.Add(invoice); }
            invoice.InvoiceNumber = InvoiceNumber; invoice.Date = Date; invoice.PartyId = Party.Id; invoice.Type = Type;
            invoice.TotalAmount = Subtotal; invoice.DiscountAmount = Subtotal * DiscountPercentage / 100; invoice.DiscountPercentage = DiscountPercentage;
            invoice.TaxRate = TaxRate; invoice.TaxAmount = TaxAmount; invoice.NetAmount = NetAmount; invoice.Status = "pending"; invoice.CurrencyId = 1;
            foreach (var line in Lines) invoice.Lines.Add(new InvoiceLine { ItemId = line.ItemId, Quantity = line.Quantity, Price = line.Price, Total = line.Total, CostPrice = line.Item?.CostPrice ?? 0 });
            await db.SaveChangesAsync();
            StatusMessage = "Saved OK - " + invoice.InvoiceNumber; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "InvoiceEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="750" Width="1100"
        WindowStartupLocation="CenterOwner" Background="{StaticResource BackgroundBrush}" FlowDirection="RightToLeft">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <TextBlock Grid.Row="0" Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}"/>
        <Border Grid.Row="1" Style="{StaticResource Card}" Margin="0,0,0,15">
            <Grid>
                <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Margin="0,0,10,0"><TextBlock Text="رقم الفاتورة:" Margin="0,0,0,5"/><TextBox Text="{Binding InvoiceNumber}" Padding="8,6" IsReadOnly="True"/></StackPanel>
                <StackPanel Grid.Column="1" Margin="10,0"><TextBlock Text="التاريخ:" Margin="0,0,0,5"/><DatePicker SelectedDate="{Binding Date}" Padding="8,6"/></StackPanel>
                <StackPanel Grid.Column="2" Margin="10,0"><TextBlock Text="النوع:" Margin="0,0,0,5"/><ComboBox SelectedValue="{Binding Type}" SelectedValuePath="Tag" Padding="8,6"><ComboBoxItem Content="مبيعات" Tag="sale"/><ComboBoxItem Content="مشتريات" Tag="purchase"/></ComboBox></StackPanel>
                <StackPanel Grid.Column="3" Margin="10,0,0,0"><TextBlock Text="العميل/المورد:" Margin="0,0,0,5"/><ComboBox ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedItem="{Binding Party}" Padding="8,6"/></StackPanel>
            </Grid>
        </Border>
        <Border Grid.Row="2" Style="{StaticResource Card}" Padding="0">
            <DockPanel>
                <DockPanel DockPanel.Dock="Top" Margin="15,15,15,10">
                    <TextBlock Text="بنود الفاتورة" Style="{StaticResource CardTitle}" DockPanel.Dock="Left"/>
                    <Button Content="+ إضافة بند" Style="{StaticResource PrimaryButton}" DockPanel.Dock="Right" Command="{Binding AddLineCommand}"/>
                </DockPanel>
                <DataGrid ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" CanUserDeleteRows="False"
                          HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#E0E0E0"
                          ColumnHeaderHeight="38" RowHeight="34" BorderThickness="0">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn Header="الصنف" Width="*">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox ItemsSource="{Binding AvailableItems}" DisplayMemberPath="SearchDisplay" SelectedItem="{Binding Item, UpdateSourceTrigger=PropertyChanged}" IsEditable="True" StaysOpenOnEdit="True" Margin="2"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="الكمية" Binding="{Binding Quantity, UpdateSourceTrigger=LostFocus}" Width="100"/>
                        <DataGridTextColumn Header="السعر" Binding="{Binding Price, UpdateSourceTrigger=LostFocus}" Width="120"/>
                        <DataGridTextColumn Header="الإجمالي" Binding="{Binding Total, StringFormat=N2}" Width="120" IsReadOnly="True"/>
                        <DataGridTemplateColumn Header="حذف" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="🗑️" Margin="2" Padding="4" Command="{Binding DataContext.RemoveLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>
        <Border Grid.Row="3" Style="{StaticResource Card}" Margin="0,15,0,15" Padding="20">
            <Grid>
                <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Margin="0,0,10,0"><TextBlock Text="المجموع:" Style="{StaticResource StatLabel}"/><TextBlock Text="{Binding Subtotal, StringFormat=N2}" Style="{StaticResource StatValue}"/></StackPanel>
                <StackPanel Grid.Column="1" Margin="10,0"><TextBlock Text="خصم %:" Style="{StaticResource StatLabel}"/><TextBox Text="{Binding DiscountPercentage, UpdateSourceTrigger=LostFocus}" Padding="8,6"/></StackPanel>
                <StackPanel Grid.Column="2" Margin="10,0"><TextBlock Text="الضريبة:" Style="{StaticResource StatLabel}"/><TextBlock Text="{Binding TaxAmount, StringFormat=N2}" Style="{StaticResource StatValue}"/></StackPanel>
                <StackPanel Grid.Column="3" Margin="10,0,0,0"><TextBlock Text="الإجمالي:" Style="{StaticResource StatLabel}"/><TextBlock Text="{Binding NetAmount, StringFormat=N2}" Style="{StaticResource StatValue}" FontSize="32"/></StackPanel>
            </Grid>
        </Border>
        <StackPanel Grid.Row="4" Orientation="Horizontal" HorizontalAlignment="Right">
            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center" Margin="0,0,20,0" TextWrapping="Wrap" MaxWidth="400"/>
            <Button Content="إلغاء" Style="{StaticResource PrimaryButton}" Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
            <Button Content="💾 حفظ" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</Window>
""", encoding="utf-8")

(VIEWS / "InvoiceEditWindow.xaml.cs").write_text("""using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class InvoiceEditWindow : Window
{
    private readonly InvoiceEditViewModel _vm;
    public InvoiceEditWindow(Invoice? invoice = null)
    {
        InitializeComponent();
        _vm = new InvoiceEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (invoice != null) _vm.Id = invoice.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
""", encoding="utf-8")

(VIEWS / "InvoicesView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.InvoicesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="InvoicesView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0} LastChildFill="False">
            <TextBlock Text="الفواتير" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" Margin="0,0,0,20">
                <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Margin="0,0,10,0"/>
                <Button Content="+ فاتورة جديدة" Style="{StaticResource PrimaryButton}" Command="{Binding NewCommand}"/>
            </StackPanel>
        </DockPanel>
        <Border Grid.Row="1" Style="{StaticResource Card}" Margin="0,0,0,15" Padding="15">
            <DockPanel>
                <TextBlock Text="🔍" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" Width="400" Padding="8,6" VerticalAlignment="Center"/>
            </DockPanel>
        </Border>
        <Border Grid.Row="2" Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding Invoices}" SelectedItem="{Binding SelectedInvoice}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      SelectionMode="Single" HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="رقم الفاتورة" Binding="{Binding InvoiceNumber}" Width="140"/>
                    <DataGridTextColumn Header="التاريخ" Binding="{Binding Date, StringFormat=yyyy-MM-dd}" Width="120"/>
                    <DataGridTextColumn Header="العميل/المورد" Binding="{Binding Party.Name}" Width="*"/>
                    <DataGridTextColumn Header="النوع" Binding="{Binding Type}" Width="80"/>
                    <DataGridTextColumn Header="الإجمالي" Binding="{Binding NetAmount, StringFormat=N2}" Width="120"/>
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

# Fix XAML typo
xaml_path = VIEWS / "InvoicesView.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "InvoicesView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class InvoicesView : UserControl
{
    public InvoicesView() { InitializeComponent(); }
    private async void InvoicesView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InvoicesViewModel vm) await vm.LoadAsync();
    }
}
""", encoding="utf-8")

# ========================================================
# 4. Update MainViewModel + MainWindow to add navigation
# ========================================================

(VM / "MainViewModel.cs").write_text("""using System.Windows.Input;

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

# Update MainWindow.xaml to add sidebar buttons
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
                        </StackPanel>
                    </ScrollViewer>
                </DockPanel>
            </Border>
            <ContentControl Grid.Column="1" Content="{Binding CurrentView}"/>
        </Grid>
    </Grid>
</Window>
""", encoding="utf-8")

print("=== Core Pages Built ===")
print("  - Parties (ViewModel + EditWindow + View)")
print("  - Items (ViewModel + EditWindow + View)")
print("  - Invoices (ViewModel + EditWindow + View)")
print("  - MainViewModel + MainWindow updated with navigation")