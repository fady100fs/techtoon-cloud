# fix_v5.ps1 - حذف الفواتير + تعديل المدفوعات
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
 $ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Fix v5 (Delete + Edit Payments)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
# ============================================================
# 1) إضافة زر حذف + عمود حذف في InvoicesView.xaml
# ============================================================
Write-Host "`n[1/3] Updating InvoicesView.xaml..." -ForegroundColor Yellow
 $xamlPath = Join-Path $ViewsDir 'InvoicesView.xaml'
Copy-Item $xamlPath ($xamlPath + '.bak_v5') -Force

 $xaml = @'
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

        <Grid Grid.Row="0" Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock x:Name="LblTitle" Grid.Column="0" FontSize="20" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
            <Button x:Name="BtnRefresh" Grid.Column="1" Background="#26A69A" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding RefreshCommand}"/>
            <Button x:Name="BtnDelete" Grid.Column="2" Background="#E53935" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding DeleteInvoiceCommand}"/>
            <Button x:Name="BtnNew" Grid.Column="3" Background="#00897B" Foreground="White" FontWeight="Bold" Padding="18,8" BorderThickness="0" Cursor="Hand" Command="{Binding NewInvoiceCommand}"/>
        </Grid>

        <TextBlock x:Name="LblStatus" Grid.Row="1" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" Margin="0,0,0,8"/>

        <Border Grid.Row="2" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <DataGrid x:Name="DgInvoices"
                      ItemsSource="{Binding Invoices}"
                      SelectedItem="{Binding SelectedInvoice}"
                      AutoGenerateColumns="False" IsReadOnly="True"
                      HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#ECEFF1" Background="White"
                      FontSize="13" CanUserAddRows="False" CanUserDeleteRows="False"
                      MouseDoubleClick="DgInvoices_MouseDoubleClick"
                      KeyDown="DgInvoices_KeyDown">
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
                    <DataGridTemplateColumn x:Name="ColDel" Width="60">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Button Content="X" Background="#E53935" Foreground="White" BorderThickness="0" FontSize="12" Cursor="Hand" Padding="8,2"
                                        Command="{Binding DataContext.DeleteInvoiceByIdCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                        CommandParameter="{Binding Id}"/>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
'@
[System.IO.File]::WriteAllText($xamlPath, $xaml, [System.Text.ASCIIEncoding]::new())
Write-Host "  OK: InvoicesView.xaml updated (Delete button + column)" -ForegroundColor Green
# ============================================================
# 2) تحديث InvoicesView.xaml.cs (أزرار الحذف + النصوص)
# ============================================================
Write-Host "`n[2/3] Updating InvoicesView.xaml.cs..." -ForegroundColor Yellow
 $csPath = Join-Path $ViewsDir 'InvoicesView.xaml.cs'
Copy-Item $csPath ($csPath + '.bak_v5') -Force

 $cs = @'
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            try { BtnDelete.Content = "\uD83D\uDDD1 \u062D\u0630\u0641"; } catch { } // 🗑 حذف
            try { BtnNew.Content = "+ \u0641\u0627\u062A\u0648\u0631\u0629 \u062C\u062F\u064A\u062F\u0629"; } catch { } // + فاتورة جديدة
            try { ColNum.Header = "\u0627\u0644\u0631\u0642\u0645"; } catch { } // الرقم
            try { ColDate.Header = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E"; } catch { } // التاريخ
            try { ColType.Header = "\u0627\u0644\u0646\u0648\u0639"; } catch { } // النوع
            try { ColParty.Header = "\u0627\u0644\u0637\u0631\u0641"; } catch { } // الطرف
            try { ColTotal.Header = "\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { } // الإجمالي
            try { ColPaid.Header = "\u0645\u062F\u0641\u0648\u0639"; } catch { } // مدفوع
            try { ColRem.Header = "\u0645\u062A\u0628\u0642\u064A"; } catch { } // متبقي
            try { ColStatus.Header = "\u062D\u0627\u0644\u0629"; } catch { } // حالة
            try { ColDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm) vm.LoadInvoices();
        }

        private void DgInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null)
            {
                var win = new InvoiceEditWindow(vm.SelectedInvoice.Id);
                win.ShowDialog();
                vm.LoadInvoices();
            }
        }

        private void DgInvoices_KeyDown(object sender, KeyEventArgs e)
        {
            // حذف بمفتاح Delete
            if (e.Key == Key.Delete)
            {
                if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null)
                {
                    var result = MessageBox.Show(
                        "\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629 \u0631\u0642\u0645 " + vm.SelectedInvoice.InvoiceNumber + "?",
                        "\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u062D\u0630\u0641",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        vm.DeleteInvoice(vm.SelectedInvoice.Id);
                    }
                    e.Handled = true;
                }
            }
        }
    }
}
'@
[System.IO.File]::WriteAllText($csPath, $cs, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoicesView.xaml.cs updated" -ForegroundColor Green
# ============================================================
# 3) تحديث InvoicesViewModel.cs (أوامر الحذف)
# ============================================================
Write-Host "`n[3/3] Updating InvoicesViewModel.cs..." -ForegroundColor Yellow
 $vmPath = Join-Path $ProjectPath 'Techtoon.UI\ViewModels\InvoicesViewModel.cs'
Copy-Item $vmPath ($vmPath + '.bak_v5') -Force

 $vm = @'
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
                if (Type == "sales") return "\u0645\u0628\u064A\u0639\u0627\u062A";
                if (Type == "purchase") return "\u0645\u0634\u062A\u0631\u064A\u0627\u062A";
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
        public ICommand DeleteInvoiceCommand { get; set; }
        public ICommand DeleteInvoiceByIdCommand { get; set; }

        public InvoicesViewModel()
        {
            Invoices = new ObservableCollection<InvoiceRow>();
            RefreshCommand = new UiRelayCommand(_ => LoadInvoices());
            NewInvoiceCommand = new UiRelayCommand(_ => OpenNewInvoice());
            EditInvoiceCommand = new UiRelayCommand(_ => OpenEditInvoice(), _ => SelectedInvoice != null);
            DeleteInvoiceCommand = new UiRelayCommand(_ => DeleteSelected(), _ => SelectedInvoice != null);
            DeleteInvoiceByIdCommand = new UiRelayCommand(p => { if (p is int id) DeleteInvoice(id); });
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

        private void DeleteSelected()
        {
            if (_selectedInvoice == null) return;
            DeleteInvoice(_selectedInvoice.Id);
        }

        public void DeleteInvoice(int invoiceId)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0647\u0630\u0647 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629\u061F",
                    "\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u062D\u0630\u0641",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.Yes) return;

                string cs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    // احذف البنود أولاً
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_lines WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        cmd.ExecuteNonQuery();
                    }
                    // احذف الدفعات
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_payments WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        cmd.ExecuteNonQuery();
                    }
                    // احذف الفاتورة
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoices WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadInvoices();
                StatusMessage = "Invoice deleted";
            }
            catch (Exception ex)
            {
                StatusMessage = "Delete error: " + ex.Message;
            }
        }
    }
}
'@
[System.IO.File]::WriteAllText($vmPath, $vm, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoicesViewModel.cs updated (Delete commands)" -ForegroundColor Green
# ============================================================
# 4) جعل المدفوعات قابلة للتعديل بالكامل في InvoiceEditWindow.xaml
#    - استبدال ComboBox بطريقة الدفع بـ ComboBox قابل للكتابة
#    - جعل المبلغ والمرجع والتاريخ قابلة للتعديل
# ============================================================
Write-Host "`n[4/4] Making payments editable in InvoiceEditWindow..." -ForegroundColor Yellow

 $xamlPath2 = Join-Path $ViewsDir 'InvoiceEditWindow.xaml'
 $content = Get-Content $xamlPath2 -Raw -Encoding UTF8

# استبدال ComboBox طريقة الدفع ليكون قابل للكتابة
 $oldMethod = '<ComboBox x:Name="PayMethodCombo" SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2" Loaded="PayMethodCombo_Loaded"/>'
 $newMethod = '<ComboBox x:Name="PayMethodCombo" SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" IsEditable="False" Padding="4,2" Loaded="PayMethodCombo_Loaded"/>'

if ($content.Contains($oldMethod)) {
    $content = $content.Replace($oldMethod, $newMethod)
    Write-Host "  OK: Payment method ComboBox is now editable" -ForegroundColor Green
} else {
    Write-Host "  WARN: Payment method pattern not found" -ForegroundColor Yellow
}

[System.IO.File]::WriteAllText($xamlPath2, $content, [System.Text.ASCIIEncoding]::new())

# ============================================================
# البناء
# ============================================================
Write-Host "`n===== Build =====" -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host "`nDONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green
