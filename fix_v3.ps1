# fix_v3.ps1 - إصلاح اختيار الصنف + إنشاء صفحة فواتير كاملة
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
 $ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'
 $VMsDir = Join-Path $ProjectPath 'Techtoon.UI\ViewModels'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Fix v3" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ============================================================
# 1) إصلاح اختيار الصنف في InvoiceEditWindow.xaml.cs
# ============================================================
Write-Host "`n[1/4] Fixing item selection..." -ForegroundColor Yellow
 $csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
if (Test-Path $csPath) {
    Copy-Item $csPath ($csPath + '.bak_v3') -Force
    $content = Get-Content $csPath -Raw -Encoding UTF8
    $oldMethod = '(?s)private void ItemSearchCombo_SelectionChanged\(.*?\n        \}'
    $newMethod = @'
private void ItemSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is ItemSearchResult item)
                {
                    if (cb.DataContext is InvoiceLineItem line)
                    {
                        line.SelectItem(item);
                        cb.Text = item.Name;
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                                LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                                if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                                {
                                    var qtyTb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                                    qtyTb?.Focus();
                                    qtyTb?.SelectAll();
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            }
            catch { }
        }
'@
    $content = [regex]::Replace($content, $oldMethod, $newMethod)
    [System.IO.File]::WriteAllText($csPath, $content, [System.Text.UTF8Encoding]::new($true))
    Write-Host "  OK: Item selection fixed" -ForegroundColor Green
}
# ============================================================
# 2) إنشاء InvoicesViewModel.cs كامل
# ============================================================
Write-Host "`n[2/4] Creating InvoicesViewModel.cs..." -ForegroundColor Yellow
 $vmPath = Join-Path $VMsDir 'InvoicesViewModel.cs'
Copy-Item $vmPath ($vmPath + '.bak_v3') -Force -ErrorAction SilentlyContinue

 $vmContent = @'
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class InvoiceRow : ViewModelBase
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string PartyName { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string Status { get; set; }
        public string TypeDisplay
        {
            get
            {
                if (Type == "sales") return "\u0645\u0628\u064A\u0639\u0627\u062A"; // مبيعات
                if (Type == "purchase") return "\u0645\u0634\u062A\u0631\u064A\u0627\u062A"; // مشتريات
                return Type ?? "";
            }
        }
        public string DateDisplay { get { return Date.ToString("yyyy-MM-dd"); } }
        public string TotalDisplay { get { return Total.ToString("N2"); } }
        public string PaidDisplay { get { return Paid.ToString("N2"); } }
        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }
    }

    public class InvoicesViewModel : ViewModelBase
    {
        public ObservableCollection<InvoiceRow> Invoices { get; set; }
        private InvoiceRow _selectedInvoice;
        public InvoiceRow SelectedInvoice
        {
            get { return _selectedInvoice; }
            set { _selectedInvoice = value; OnPropertyChanged("SelectedInvoice"); }
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged("StatusMessage"); }
        }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewInvoiceCommand { get; set; }
        public ICommand EditInvoiceCommand { get; set; }

        public InvoicesViewModel()
        {
            Invoices = new ObservableCollection<InvoiceRow>();
            RefreshCommand = new UiRelayCommand(_ => LoadInvoices());
            NewInvoiceCommand = new UiRelayCommand(_ => OpenNewInvoice());
            EditInvoiceCommand = new UiRelayCommand(_ => OpenEditInvoice(), _ => SelectedInvoice != null);
            LoadInvoices();
        }

        public void LoadInvoices()
        {
            Invoices.Clear();
            try
            {
                string cs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount,
                        COALESCE(p.name, '') AS party_name, i.status
                        FROM invoices i LEFT JOIN parties p ON i.party_id = p.id
                        ORDER BY i.id DESC LIMIT 200";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Invoices.Add(new InvoiceRow
                            {
                                Id = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                                InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2),
                                Type = r.IsDBNull(3) ? "" : r.GetString(3),
                                Total = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4)),
                                Paid = r.IsDBNull(5) ? 0 : Convert.ToDecimal(r.GetValue(5)),
                                PartyName = r.IsDBNull(6) ? "" : r.GetString(6),
                                Status = r.IsDBNull(7) ? "" : r.GetString(7)
                            });
                        }
                    }
                }
                StatusMessage = "Loaded: " + Invoices.Count + " invoices";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private void OpenNewInvoice()
        {
            try
            {
                var win = new InvoiceEditWindow(null);
                win.ShowDialog();
                LoadInvoices();
            }
            catch { }
        }

        private void OpenEditInvoice()
        {
            try
            {
                if (_selectedInvoice == null) return;
                var win = new InvoiceEditWindow(_selectedInvoice.Id);
                win.ShowDialog();
                LoadInvoices();
            }
            catch { }
        }
    }
}
'@
[System.IO.File]::WriteAllText($vmPath, $vmContent, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoicesViewModel.cs created" -ForegroundColor Green
# ============================================================
# 3) إنشاء InvoicesView.xaml كامل (ASCII + x:Name)
# ============================================================
Write-Host "`n[3/4] Creating InvoicesView.xaml..." -ForegroundColor Yellow
 $xamlPath = Join-Path $ViewsDir 'InvoicesView.xaml'
Copy-Item $xamlPath ($xamlPath + '.bak_v3') -Force -ErrorAction SilentlyContinue

 $xamlContent = @'
<UserControl x:Class="Techtoon.UI.Views.InvoicesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             FlowDirection="RightToLeft" Background="#F5F7FA" FontFamily="Tahoma"
             Loaded="UserControl_Loaded">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Grid Grid.Row="0" Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock x:Name="LblTitle" Grid.Column="0" FontSize="20" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
            <Button x:Name="BtnRefresh" Grid.Column="1" Content="Refresh" Background="#26A69A" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding RefreshCommand}"/>
            <Button x:Name="BtnNew" Grid.Column="2" Content="+ New" Background="#00897B" Foreground="White" FontWeight="Bold" Padding="18,8" BorderThickness="0" Cursor="Hand" Command="{Binding NewInvoiceCommand}"/>
        </Grid>

        <!-- Status -->
        <TextBlock x:Name="LblStatus" Grid.Row="1" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" Margin="0,0,0,8"/>

        <!-- DataGrid -->
        <Border Grid.Row="2" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <DataGrid x:Name="DgInvoices"
                      ItemsSource="{Binding Invoices}"
                      SelectedItem="{Binding SelectedInvoice}"
                      AutoGenerateColumns="False" IsReadOnly="True"
                      HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#ECEFF1" Background="White"
                      FontSize="13" CanUserAddRows="False" CanUserDeleteRows="False"
                      MouseDoubleClick="DgInvoices_MouseDoubleClick">
                <DataGrid.Columns>
                    <DataGridTextColumn x:Name="ColNum" Binding="{Binding InvoiceNumber}" Width="140"/>
                    <DataGridTextColumn x:Name="ColDate" Binding="{Binding DateDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColType" Binding="{Binding TypeDisplay}" Width="100"/>
                    <DataGridTextColumn x:Name="ColParty" Binding="{Binding PartyName}" Width="*"/>
                    <DataGridTextColumn x:Name="ColTotal" Binding="{Binding TotalDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="FontWeight" Value="SemiBold"/><Setter Property="Foreground" Value="#00695C"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColPaid" Binding="{Binding PaidDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="Foreground" Value="#2E7D32"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColRem" Binding="{Binding RemainingDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="FontWeight" Value="Bold"/><Setter Property="Foreground" Value="#E65100"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColStatus" Binding="{Binding Status}" Width="90"/>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
'@
[System.IO.File]::WriteAllText($xamlPath, $xamlContent, [System.Text.ASCIIEncoding]::new())
Write-Host "  OK: InvoicesView.xaml created" -ForegroundColor Green

# ============================================================
# 4) إنشاء InvoicesView.xaml.cs كامل
# ============================================================
Write-Host "`n[4/4] Creating InvoicesView.xaml.cs..." -ForegroundColor Yellow
 $csPath = Join-Path $ViewsDir 'InvoicesView.xaml.cs'
Copy-Item $csPath ($csPath + '.bak_v3') -Force -ErrorAction SilentlyContinue

 $csContent = @'
using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoicesView : UserControl
    {
        public InvoicesView()
        {
            InitializeComponent();
            SetArabicTexts();
            DataContext = new InvoicesViewModel();
        }

        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\u0627\u0644\u0641\u0648\u0627\u062A\u064A\u0631"; } catch { } // الفواتير
            try { BtnRefresh.Content = "\uD83D\uDD04 \u062A\u062D\u062F\u064A\u062B"; } catch { } // 🔄 تحديث
            try { BtnNew.Content = "+ \u0641\u0627\u062A\u0648\u0631\u0629 \u062C\u062F\u064A\u062F\u0629"; } catch { } // + فاتورة جديدة
            try { ColNum.Header = "\u0627\u0644\u0631\u0642\u0645"; } catch { } // الرقم
            try { ColDate.Header = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E"; } catch { } // التاريخ
            try { ColType.Header = "\u0627\u0644\u0646\u0648\u0639"; } catch { } // النوع
            try { ColParty.Header = "\u0627\u0644\u0637\u0631\u0641"; } catch { } // الطرف
            try { ColTotal.Header = "\u0627\u0644\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { } // الإجمالي
            try { ColPaid.Header = "\u0627\u0644\u0645\u062F\u0641\u0648\u0639"; } catch { } // المدفوع
            try { ColRem.Header = "\u0627\u0644\u0645\u062A\u0628\u0642\u064A"; } catch { } // المتبقي
            try { ColStatus.Header = "\u0627\u0644\u062D\u0627\u0644\u0629"; } catch { } // الحالة
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm) vm.LoadInvoices();
        }

        private void DgInvoices_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null)
            {
                var win = new InvoiceEditWindow(vm.SelectedInvoice.Id);
                win.ShowDialog();
                vm.LoadInvoices();
            }
        }
    }
}
'@
[System.IO.File]::WriteAllText($csPath, $csContent, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoicesView.xaml.cs created" -ForegroundColor Green

# ============================================================
# البناء
# ============================================================
Write-Host "`n===== Build =====" -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host "`nDONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green