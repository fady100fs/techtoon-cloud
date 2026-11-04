# build_invoice_features.py
from pathlib import Path

ROOT = Path(r"D:\PROG\Accounting Prog\Techtoon Accounting C+")
VM = ROOT / "Techtoon.UI" / "ViewModels"
VIEWS = ROOT / "Views"

# ========================================================
# 1. تحديث InvoiceEditViewModel.cs (البحث الذكي + المدفوعات المتعددة)
# ========================================================

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
    private string _searchText = "";
    private ObservableCollection<Item> _filteredItems = new();

    public ObservableCollection<Item> AvailableItems { get; set; } = new();

    public ObservableCollection<Item> FilteredItems
    {
        get => _filteredItems;
        set => SetField(ref _filteredItems, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                UpdateFilteredItems();
            }
        }
    }

    public Item? Item
    {
        get => _item;
        set
        {
            if (SetField(ref _item, value))
            {
                if (_item != null)
                {
                    Price = _item.SellPrice;
                    SearchText = _item.SearchDisplay;
                }
                Recompute();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemName)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemId)));
            }
        }
    }

    public int ItemId => Item?.Id ?? 0;
    public string ItemName => Item?.Name ?? "";

    public double Quantity
    {
        get => _quantity;
        set { if (SetField(ref _quantity, value)) Recompute(); }
    }

    public double Price
    {
        get => _price;
        set { if (SetField(ref _price, value)) Recompute(); }
    }

    public double Total
    {
        get => _total;
        set => SetField(ref _total, value);
    }

    public void InitializeFilteredItems()
    {
        FilteredItems = new ObservableCollection<Item>(AvailableItems);
    }

    private void UpdateFilteredItems()
    {
        if (AvailableItems == null || AvailableItems.Count == 0)
        {
            FilteredItems = new ObservableCollection<Item>();
            return;
        }

        var matchedItem = AvailableItems.FirstOrDefault(i => i.SearchDisplay == SearchText);
        if (matchedItem != null) return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredItems = new ObservableCollection<Item>(AvailableItems);
            return;
        }

        var s = SearchText.Trim().ToLower();
        var filtered = AvailableItems.Where(i =>
            (i.Name?.ToLower().Contains(s) == true) ||
            (i.DisplayCode?.ToLower().Contains(s) == true) ||
            (i.Barcode?.ToLower().Contains(s) == true) ||
            (i.SearchDisplay?.ToLower().Contains(s) == true)).Take(50).ToList();
        FilteredItems = new ObservableCollection<Item>(filtered);
    }

    private void Recompute()
    {
        Total = Quantity * Price;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

public class InvoicePaymentRow : ViewModelBase
{
    private double _amount;
    private string _paymentMethod = "cash";
    private string _referenceNumber = "";

    public int Id { get; set; }
    public double Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public string PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }
    public string ReferenceNumber { get => _referenceNumber; set => SetProperty(ref _referenceNumber, value); }
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

    // Payments
    private ObservableCollection<InvoicePaymentRow> _invoicePayments = new();
    private double _newPayAmount;
    private string _newPayMethod = "cash";
    private string _newPayRef = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string InvoiceNumber { get => _invoiceNumber; set => SetProperty(ref _invoiceNumber, value); }
    public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
    public Party? Party { get => _party; set => SetProperty(ref _party, value); }
    public string Type { get => _type; set => SetProperty(ref _type, value); }
    public double DiscountPercentage
    {
        get => _discountPercentage;
        set { if (SetProperty(ref _discountPercentage, value)) RecomputeTotals(); }
    }
    public double TaxRate { get => _taxRate; set { if (SetProperty(ref _taxRate, value)) RecomputeTotals(); } }

    public ObservableCollection<Party> Parties { get => _parties; set => SetProperty(ref _parties, value); }
    public ObservableCollection<Item> Items { get => _items; set => SetProperty(ref _items, value); }
    public ObservableCollection<InvoiceLineItem> Lines
    {
        get => _lines;
        set
        {
            if (SetProperty(ref _lines, value))
            {
                foreach (var l in _lines)
                {
                    l.PropertyChanged += Line_PropertyChanged;
                    l.AvailableItems = Items;
                    l.InitializeFilteredItems();
                }
            }
        }
    }

    public double Subtotal { get => _subtotal; set => SetProperty(ref _subtotal, value); }
    public double TaxAmount { get => _taxAmount; set => SetProperty(ref _taxAmount, value); }
    public double NetAmount { get => _netAmount; set => SetProperty(ref _netAmount, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "تعديل الفاتورة" : "فاتورة جديدة";

    // Payments Properties
    public ObservableCollection<InvoicePaymentRow> InvoicePayments { get => _invoicePayments; set => SetProperty(ref _invoicePayments, value); }
    public double NewPayAmount { get => _newPayAmount; set => SetProperty(ref _newPayAmount, value); }
    public string NewPayMethod { get => _newPayMethod; set => SetProperty(ref _newPayMethod, value); }
    public string NewPayRef { get => _newPayRef; set => SetProperty(ref _newPayRef, value); }
    public bool ShowPaymentsSection => IsEdit;

    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddPaymentCommand { get; }
    public ICommand DeletePaymentCommand { get; }

    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public InvoiceEditViewModel()
    {
        AddLineCommand = new RelayCommand(AddLine);
        RemoveLineCommand = new RelayCommand<InvoiceLineItem>(RemoveLine);
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
        AddPaymentCommand = new RelayCommand(async () => await AddPaymentAsync());
        DeletePaymentCommand = new RelayCommand<InvoicePaymentRow>(async (p) => await DeletePaymentAsync(p));

        _lines.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
                foreach (InvoiceLineItem l in e.NewItems)
                {
                    l.PropertyChanged += Line_PropertyChanged;
                    l.AvailableItems = Items;
                    l.InitializeFilteredItems();
                }
            RecomputeTotals();
        };
    }

    public async override Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();

            Parties = new ObservableCollection<Party>(await db.Parties.OrderBy(p => p.Name).ToListAsync());
            Items = new ObservableCollection<Item>(await db.Items.OrderBy(i => i.Name).ToListAsync());

            foreach (var line in Lines)
            {
                line.AvailableItems = Items;
                line.InitializeFilteredItems();
            }

            if (Id == 0)
            {
                await GenerateInvoiceNumberAsync();
            }
            else
            {
                await LoadFromInvoiceAsync(Id);
            }
        }
        catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
    }

    private async Task GenerateInvoiceNumberAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var prefix = Type == "sale" ? "SAL-" : "PUR-";
            var year = DateTime.Now.Year;
            var count = await db.Invoices.CountAsync(i => i.InvoiceNumber.StartsWith(prefix + year));
            InvoiceNumber = prefix + year + "-" + (count + 1).ToString("D4");
        }
        catch { }
    }

    public async Task LoadFromInvoiceAsync(int invoiceId)
    {
        try
        {
            Id = invoiceId;
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();

            var invoice = await db.Invoices
                .Include(i => i.Party)
                .Include(i => i.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null) return;

            InvoiceNumber = invoice.InvoiceNumber;
            Date = invoice.Date;
            Type = invoice.Type;
            Party = invoice.Party;
            DiscountPercentage = invoice.DiscountPercentage;
            TaxRate = invoice.TaxRate;

            Lines.Clear();
            foreach (var line in invoice.Lines)
            {
                var l = new InvoiceLineItem
                {
                    Item = line.Item,
                    Quantity = line.Quantity,
                    Price = line.Price
                };
                l.AvailableItems = Items;
                l.InitializeFilteredItems();
                Lines.Add(l);
            }
            RecomputeTotals();
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(ShowPaymentsSection));

            // Load Payments
            var payments = await db.InvoicePayments.Where(p => p.InvoiceId == invoiceId).ToListAsync();
            InvoicePayments = new ObservableCollection<InvoicePaymentRow>(
                payments.Select(p => new InvoicePaymentRow { Id = p.Id, Amount = p.Amount, PaymentMethod = p.PaymentMethod, ReferenceNumber = p.ReferenceNumber ?? "" })
            );
        }
        catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
    }

    private void AddLine()
    {
        if (Items.Count == 0)
        {
            StatusMessage = "لا توجد أصناف. أضف أصنافاً أولاً من صفحة الأصناف.";
            return;
        }
        var line = new InvoiceLineItem { AvailableItems = Items };
        line.InitializeFilteredItems();
        Lines.Add(line);
    }

    private void RemoveLine(InvoiceLineItem? line)
    {
        if (line != null) Lines.Remove(line);
    }

    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InvoiceLineItem.Total))
            RecomputeTotals();
    }

    private void RecomputeTotals()
    {
        Subtotal = Lines.Sum(l => l.Total);
        if (DiscountPercentage > 0)
            DiscountAmount = Subtotal * DiscountPercentage / 100;
        var afterDiscount = Subtotal - DiscountAmount;
        TaxAmount = afterDiscount * TaxRate / 100;
        NetAmount = afterDiscount + TaxAmount;
    }

    private async Task SaveAsync()
    {
        try
        {
            if (Party == null) { StatusMessage = "اختر العميل/المورد"; return; }
            if (Lines.Count == 0) { StatusMessage = "أضف بنداً واحداً على الأقل"; return; }
            if (Lines.Any(l => l.Item == null)) { StatusMessage = "اختر صنفاً لكل بند"; return; }

            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();

            Invoice invoice;
            if (Id > 0)
            {
                invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == Id) ?? new Invoice();
                db.InvoiceLines.RemoveRange(invoice.Lines);
            }
            else
            {
                invoice = new Invoice();
                db.Invoices.Add(invoice);
            }

            invoice.InvoiceNumber = InvoiceNumber;
            invoice.Date = Date;
            invoice.PartyId = Party.Id;
            invoice.Type = Type;
            invoice.TotalAmount = Subtotal;
            invoice.DiscountAmount = Subtotal * DiscountPercentage / 100;
            invoice.DiscountPercentage = DiscountPercentage;
            invoice.TaxRate = TaxRate;
            invoice.TaxAmount = TaxAmount;
            invoice.NetAmount = NetAmount;
            invoice.Status = "pending";
            invoice.CurrencyId = 1;

            foreach (var line in Lines)
            {
                invoice.Lines.Add(new InvoiceLine
                {
                    ItemId = line.ItemId,
                    Quantity = line.Quantity,
                    Price = line.Price,
                    Total = line.Total,
                    CostPrice = line.Item?.CostPrice ?? 0
                });
            }

            await db.SaveChangesAsync();

            // Update inventory stock
            try
            {
                await Techtoon.UI.Services.InventoryService.UpdateStockForInvoiceAsync(db, invoice, Id > 0);
            }
            catch (Exception stockEx)
            {
                System.Diagnostics.Debug.WriteLine("Inventory update error: " + stockEx.Message);
            }

            StatusMessage = "✅ تم حفظ الفاتورة بنجاح - رقم: " + invoice.InvoiceNumber;
            DialogResult = true;
            RequestClose?.Invoke();
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException;
            var msg = "Error: " + ex.Message;
            while (inner != null) { msg += "\\n→ " + inner.Message; inner = inner.InnerException; }
            StatusMessage = msg;
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task AddPaymentAsync()
    {
        if (NewPayAmount <= 0) { StatusMessage = "المبلغ يجب أن يكون أكبر من صفر"; return; }

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();

            var payment = new InvoicePayment
            {
                InvoiceId = Id,
                Date = DateTime.UtcNow,
                Amount = NewPayAmount,
                PaymentMethod = NewPayMethod,
                ReferenceNumber = string.IsNullOrEmpty(NewPayRef) ? null : NewPayRef,
                CreatedAt = DateTime.UtcNow
            };

            db.InvoicePayments.Add(payment);
            await db.SaveChangesAsync();

            InvoicePayments.Add(new InvoicePaymentRow { Id = payment.Id, Amount = payment.Amount, PaymentMethod = payment.PaymentMethod, ReferenceNumber = payment.ReferenceNumber ?? "" });
            NewPayAmount = 0;
            NewPayRef = "";
            StatusMessage = "✅ تمت إضافة الدفعة بنجاح";
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task DeletePaymentAsync(InvoicePaymentRow? pay)
    {
        if (pay == null) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var p = await db.InvoicePayments.FirstOrDefaultAsync(x => x.Id == pay.Id);
            if (p != null)
            {
                db.InvoicePayments.Remove(p);
                await db.SaveChangesAsync();
                InvoicePayments.Remove(pay);
                StatusMessage = "✅ تم حذف الدفعة";
            }
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")
print("Created: InvoiceEditViewModel.cs")

# ========================================================
# 2. تحديث InvoiceEditWindow.xaml (الواجهة)
# ========================================================

(VIEWS / "InvoiceEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="800" Width="1100"
        WindowStartupLocation="CenterOwner"
        Background="{StaticResource BackgroundBrush}"
        FlowDirection="RightToLeft"
        Loaded="Window_Loaded">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <Grid Margin="20">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <TextBlock Grid.Row="0" Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}"/>

            <Border Grid.Row="1" Style="{StaticResource Card}" Margin="0,0,0,15">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>

                    <StackPanel Grid.Column="0" Margin="0,0,10,0">
                        <TextBlock Text="رقم الفاتورة:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding InvoiceNumber}" Padding="8,6" IsReadOnly="True"/>
                    </StackPanel>

                    <StackPanel Grid.Column="1" Margin="10,0">
                        <TextBlock Text="التاريخ:" Margin="0,0,0,5"/>
                        <DatePicker SelectedDate="{Binding Date}" Padding="8,6"/>
                    </StackPanel>

                    <StackPanel Grid.Column="2" Margin="10,0">
                        <TextBlock Text="النوع:" Margin="0,0,0,5"/>
                        <ComboBox SelectedValue="{Binding Type}" SelectedValuePath="Tag" Padding="8,6">
                            <ComboBoxItem Content="مبيعات" Tag="sale"/>
                            <ComboBoxItem Content="مشتريات" Tag="purchase"/>
                        </ComboBox>
                    </StackPanel>

                    <StackPanel Grid.Column="3" Margin="10,0,0,0">
                        <TextBlock Text="العميل/المورد:" Margin="0,0,0,5"/>
                        <ComboBox ItemsSource="{Binding Parties}" DisplayMemberPath="Name"
                                  SelectedItem="{Binding Party}" Padding="8,6"/>
                    </StackPanel>
                </Grid>
            </Border>

            <Border Grid.Row="2" Style="{StaticResource Card}" Padding="0" Margin="0,0,0,15">
                <DockPanel>
                    <DockPanel DockPanel.Dock="Top" Margin="15,15,15,10">
                        <TextBlock Text="بنود الفاتورة" Style="{StaticResource CardTitle}" DockPanel.Dock="Left"/>
                        <Button Content="+ إضافة بند" Style="{StaticResource PrimaryButton}" DockPanel.Dock="Right" Command="{Binding AddLineCommand}"/>
                    </DockPanel>

                    <DataGrid ItemsSource="{Binding Lines}" AutoGenerateColumns="False"
                              CanUserAddRows="False" CanUserDeleteRows="False"
                              HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                              HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="38"
                              RowHeight="34" BorderThickness="0" MaxHeight="200">
                        <DataGrid.Columns>
                            <DataGridTemplateColumn Header="الصنف (ابحث بالكود أو الاسم)" Width="*">
                                <DataGridTemplateColumn.CellTemplate>
                                    <DataTemplate>
                                        <ComboBox ItemsSource="{Binding FilteredItems}"
                                                  DisplayMemberPath="SearchDisplay"
                                                  SelectedItem="{Binding Item, UpdateSourceTrigger=PropertyChanged}"
                                                  Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged, Mode=TwoWay}"
                                                  IsEditable="True"
                                                  IsTextSearchEnabled="False"
                                                  StaysOpenOnEdit="True"
                                                  Margin="2"/>
                                    </DataTemplate>
                                </DataGridTemplateColumn.CellTemplate>
                            </DataGridTemplateColumn>
                            <DataGridTextColumn Header="الكمية" Binding="{Binding Quantity, UpdateSourceTrigger=LostFocus}" Width="100"/>
                            <DataGridTextColumn Header="السعر" Binding="{Binding Price, UpdateSourceTrigger=LostFocus}" Width="120"/>
                            <DataGridTextColumn Header="الإجمالي" Binding="{Binding Total, StringFormat=N2}" Width="120" IsReadOnly="True"/>
                            <DataGridTemplateColumn Header="حذف" Width="80">
                                <DataGridTemplateColumn.CellTemplate>
                                    <DataTemplate>
                                        <Button Content="🗑️" Margin="2" Padding="4"
                                                Command="{Binding DataContext.RemoveLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                                CommandParameter="{Binding}"/>
                                    </DataTemplate>
                                </DataGridTemplateColumn.CellTemplate>
                            </DataGridTemplateColumn>
                        </DataGrid.Columns>
                    </DataGrid>
                </DockPanel>
            </Border>

            <Border Grid.Row="3" Style="{StaticResource Card}" Margin="0,0,0,15" Padding="20">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>

                    <StackPanel Grid.Column="0" Margin="0,0,10,0">
                        <TextBlock Text="المجموع:" Style="{StaticResource StatLabel}"/>
                        <TextBlock Text="{Binding Subtotal, StringFormat=N2}" Style="{StaticResource StatValue}"/>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0">
                        <TextBlock Text="خصم %:" Style="{StaticResource StatLabel}"/>
                        <TextBox Text="{Binding DiscountPercentage, UpdateSourceTrigger=LostFocus}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Grid.Column="2" Margin="10,0">
                        <TextBlock Text="الضريبة (14%):" Style="{StaticResource StatLabel}"/>
                        <TextBlock Text="{Binding TaxAmount, StringFormat=N2}" Style="{StaticResource StatValue}"/>
                    </StackPanel>
                    <StackPanel Grid.Column="3" Margin="10,0,0,0">
                        <TextBlock Text="الإجمالي النهائي:" Style="{StaticResource StatLabel}"/>
                        <TextBlock Text="{Binding NetAmount, StringFormat=N2}" Style="{StaticResource StatValue}" FontSize="32"/>
                    </StackPanel>
                </Grid>
            </Border>

            <!-- المدفوعات المتعددة -->
            <Border Grid.Row="4" Style="{StaticResource Card}" Padding="0" Margin="0,0,0,15"
                    Visibility="{Binding ShowPaymentsSection, Converter={StaticResource BoolToVisConverter}}">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Top" Text="المدفوعات على هذه الفاتورة" Style="{StaticResource CardTitle}" Margin="15,15,15,10"/>
                    
                    <Grid Margin="15,0,15,15">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <StackPanel Grid.Column="0" Margin="0,0,10,0">
                            <TextBlock Text="المبلغ:" Margin="0,0,0,5"/>
                            <TextBox Text="{Binding NewPayAmount, UpdateSourceTrigger=LostFocus}" Padding="8,6"/>
                        </StackPanel>
                        <StackPanel Grid.Column="1" Margin="10,0">
                            <TextBlock Text="طريقة الدفع:" Margin="0,0,0,5"/>
                            <ComboBox SelectedValue="{Binding NewPayMethod}" SelectedValuePath="Tag" Padding="8,6">
                                <ComboBoxItem Content="نقدي" Tag="cash"/>
                                <ComboBoxItem Content="تحويل بنكي" Tag="bank_transfer"/>
                                <ComboBoxItem Content="شيك" Tag="cheque"/>
                            </ComboBox>
                        </StackPanel>
                        <StackPanel Grid.Column="2" Margin="10,0">
                            <TextBlock Text="المرجع:" Margin="0,0,0,5"/>
                            <TextBox Text="{Binding NewPayRef, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                        </StackPanel>
                        <Button Grid.Column="3" Content="+ دفعة" Style="{StaticResource PrimaryButton}" Command="{Binding AddPaymentCommand}" VerticalAlignment="Bottom"/>
                    </Grid>

                    <DataGrid ItemsSource="{Binding InvoicePayments}" AutoGenerateColumns="False"
                              CanUserAddRows="False" CanUserDeleteRows="False"
                              HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                              HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="38"
                              RowHeight="32" BorderThickness="0" MaxHeight="150" Margin="15,0,15,15">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="المبلغ" Binding="{Binding Amount, StringFormat=N2}" Width="*"/>
                            <DataGridTextColumn Header="الطريقة" Binding="{Binding PaymentMethod}" Width="*"/>
                            <DataGridTextColumn Header="المرجع" Binding="{Binding ReferenceNumber}" Width="*"/>
                            <DataGridTemplateColumn Header="حذف" Width="80">
                                <DataGridTemplateColumn.CellTemplate>
                                    <DataTemplate>
                                        <Button Content="🗑️" Margin="2" Padding="4"
                                                Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                                CommandParameter="{Binding}"/>
                                    </DataTemplate>
                                </DataGridTemplateColumn.CellTemplate>
                            </DataGridTemplateColumn>
                        </DataGrid.Columns>
                    </DataGrid>
                </DockPanel>
            </Border>

            <StackPanel Grid.Row="5" Orientation="Horizontal" HorizontalAlignment="Right">
                <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}"
                           VerticalAlignment="Center" Margin="0,0,20,0" TextWrapping="Wrap" MaxWidth="400"/>
                <Button Content="إلغاء" Style="{StaticResource PrimaryButton}"
                        Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
                <Button Content="💾 حفظ الفاتورة" Style="{StaticResource PrimaryButton}"
                        Command="{Binding SaveCommand}"/>
            </StackPanel>
        </Grid>
    </ScrollViewer>
</Window>
""", encoding="utf-8")
print("Created: InvoiceEditWindow.xaml")

print("\\n=== Invoice Features Updated ===")
print("  - InvoiceEditViewModel.cs (Smart Search + Multi-Payments)")
print("  - InvoiceEditWindow.xaml (Smart Search UI + Payments UI)")