# rebuild_payments.py - إعادة كتابة كاملة لصفحة المدفوعات
# يستخدم Npgsql مباشرة (بدون EF Core، بدون Reflection)
import os, shutil

PROJECT = r"D:\PROG\Accounting Prog\Techtoon Accounting C+"
VIEWS = os.path.join(PROJECT, "Techtoon.UI", "Views")
VMS = os.path.join(PROJECT, "Techtoon.UI", "ViewModels")
CS = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"

print("=" * 60)
print("  REBUILD Payments from scratch (Npgsql direct)")
print("=" * 60)

# ============================================================
# 1) كتابة PaymentsViewModel.cs جديد (بسيط + Npgsql)
# ============================================================
print("\n[1/3] Writing PaymentsViewModel.cs...")
vm_path = os.path.join(VMS, "PaymentsViewModel.cs")
if os.path.exists(vm_path):
    shutil.copy2(vm_path, vm_path + ".bak_rebuild")

vm_content = '''using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class PaymentRow : ViewModelBase
    {
        public int Id { get; set; }
        public string VoucherNo { get; set; }
        public DateTime Date { get; set; }
        public string PartyName { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string Notes { get; set; }
        public string DateDisplay { get { return Date.ToString("yyyy-MM-dd"); } }
        public string AmountDisplay { get { return Amount.ToString("N2"); } }
        public string TypeDisplay
        {
            get
            {
                if (Type == "receipt") return "\\u0642\\u0628\\u0636";
                if (Type == "payment") return "\\u0635\\u0631\\u0641";
                return Type ?? "";
            }
        }
    }

    public class PaymentsViewModel : ViewModelBase
    {
        public ObservableCollection<PaymentRow> Payments { get; set; }
        private PaymentRow _selectedPayment;
        public PaymentRow SelectedPayment
        {
            get { return _selectedPayment; }
            set { _selectedPayment = value; OnPropertyChanged("SelectedPayment"); }
        }
        private string _searchText = "";
        public string SearchText
        {
            get { return _searchText; }
            set { _searchText = value; OnPropertyChanged("SearchText"); LoadPayments(); }
        }
        private string _statusMessage = "";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged("StatusMessage"); }
        }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewReceiptCommand { get; set; }
        public ICommand NewPaymentCommand { get; set; }
        public ICommand DeleteCommand { get; set; }

        public PaymentsViewModel()
        {
            Payments = new ObservableCollection<PaymentRow>();
            RefreshCommand = new UiRelayCommand(_ => LoadPayments());
            NewReceiptCommand = new UiRelayCommand(_ => OpenEditor("receipt"));
            NewPaymentCommand = new UiRelayCommand(_ => OpenEditor("payment"));
            DeleteCommand = new UiRelayCommand(p => { if (p is PaymentRow r) DeletePayment(r); });
            LoadPayments();
        }

        public static string GetCS()
        {
            return "''' + CS + '''";
        }

        public void LoadPayments()
        {
            Payments.Clear();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    string sql = @"SELECT id, voucher_no, date, party_name, type, amount, method, notes
                        FROM payments ORDER BY id DESC LIMIT 500";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Payments.Add(new PaymentRow
                            {
                                Id = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                                VoucherNo = r.IsDBNull(1) ? "" : r.GetString(1),
                                Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2),
                                PartyName = r.IsDBNull(3) ? "" : r.GetString(3),
                                Type = r.IsDBNull(4) ? "" : r.GetString(4),
                                Amount = r.IsDBNull(5) ? 0 : r.GetDecimal(5),
                                Method = r.IsDBNull(6) ? "" : r.GetString(6),
                                Notes = r.IsDBNull(7) ? "" : r.GetString(7)
                            });
                        }
                    }
                }
                StatusMessage = "Loaded: " + Payments.Count + " payments";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private void OpenEditor(string type)
        {
            try
            {
                var win = new PaymentEditWindow(type);
                win.ShowDialog();
                LoadPayments();
            }
            catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
        }

        public void DeletePayment(PaymentRow row)
        {
            if (row == null) return;
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "\\u0647\\u0644 \\u062A\\u0631\\u064A\\u062F \\u062D\\u0630\\u0641 \\u0627\\u0644\\u0633\\u0646\\u062F \\u0631\\u0642\\u0645 " + row.VoucherNo + "?",
                    "\\u062A\\u0623\\u0643\\u064A\\u062F \\u0627\\u0644\\u062D\\u0630\\u0641",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.Yes) return;

                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("DELETE FROM payments WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", row.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadPayments();
            }
            catch (Exception ex) { StatusMessage = "Delete error: " + ex.Message; }
        }
    }
}
'''
with open(vm_path, "w", encoding="utf-8-sig") as f:
    f.write(vm_content)
print("  OK: PaymentsViewModel.cs")

# ============================================================
# 2) كتابة PaymentsView.xaml + .xaml.cs (ASCII + Arabic in code)
# ============================================================
print("\n[2/3] Writing PaymentsView.xaml + .xaml.cs...")
xaml_path = os.path.join(VIEWS, "PaymentsView.xaml")
cs_path = os.path.join(VIEWS, "PaymentsView.xaml.cs")

if os.path.exists(xaml_path):
    shutil.copy2(xaml_path, xaml_path + ".bak_rebuild")
if os.path.exists(cs_path):
    shutil.copy2(cs_path, cs_path + ".bak_rebuild")

xaml = '''<UserControl x:Class="Techtoon.UI.Views.PaymentsView"
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
            <Button x:Name="BtnReceipt" Grid.Column="2" Background="#00897B" Foreground="White" FontWeight="Bold" Padding="18,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding NewReceiptCommand}"/>
            <Button x:Name="BtnPayment" Grid.Column="3" Background="#FB8C00" Foreground="White" FontWeight="Bold" Padding="18,8" BorderThickness="0" Cursor="Hand" Command="{Binding NewPaymentCommand}"/>
        </Grid>
        <TextBlock x:Name="LblStatus" Grid.Row="1" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" Margin="0,0,0,8"/>
        <Border Grid.Row="2" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <DataGrid x:Name="DgPayments" ItemsSource="{Binding Payments}" SelectedItem="{Binding SelectedPayment}"
                      AutoGenerateColumns="False" IsReadOnly="True" CanUserAddRows="False"
                      HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#ECEFF1" Background="White" FontSize="13">
                <DataGrid.Columns>
                    <DataGridTextColumn x:Name="ColVoucher" Binding="{Binding VoucherNo}" Width="140"/>
                    <DataGridTextColumn x:Name="ColDate" Binding="{Binding DateDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColParty" Binding="{Binding PartyName}" Width="*"/>
                    <DataGridTextColumn x:Name="ColType" Binding="{Binding TypeDisplay}" Width="90"/>
                    <DataGridTextColumn x:Name="ColAmount" Binding="{Binding AmountDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle>
                            <Style TargetType="TextBlock">
                                <Setter Property="HorizontalAlignment" Value="Right"/>
                                <Setter Property="FontWeight" Value="SemiBold"/>
                                <Setter Property="Foreground" Value="#00695C"/>
                            </Style>
                        </DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColMethod" Binding="{Binding Method}" Width="100"/>
                    <DataGridTemplateColumn x:Name="ColDel" Width="60">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Button Content="X" Background="#E53935" Foreground="White" BorderThickness="0" FontSize="12" Cursor="Hand" Padding="8,2"
                                        Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
'''

cs_view = '''using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class PaymentsView : UserControl
    {
        public PaymentsView()
        {
            InitializeComponent();
            SetArabicTexts();
            DataContext = new PaymentsViewModel();
        }

        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\\u0627\\u0644\\u0645\\u062F\\u0641\\u0648\\u0639\\u0627\\u062A \\u0648\\u0627\\u0644\\u0633\\u0646\\u062F\\u0627\\u062A"; } catch { }
            try { BtnRefresh.Content = "\\uD83D\\uDD04 \\u062A\\u062D\\u062F\\u064A\\u062B"; } catch { }
            try { BtnReceipt.Content = "+ \\u0633\\u0646\\u062F \\u0642\\u0628\\u0636"; } catch { }
            try { BtnPayment.Content = "+ \\u0633\\u0646\\u062F \\u0635\\u0631\\u0641"; } catch { }
            try { ColVoucher.Header = "\\u0631\\u0642\\u0645 \\u0627\\u0644\\u0633\\u0646\\u062F"; } catch { }
            try { ColDate.Header = "\\u0627\\u0644\\u062A\\u0627\\u0631\\u064A\\u062E"; } catch { }
            try { ColParty.Header = "\\u0627\\u0644\\u0637\\u0631\\u0641"; } catch { }
            try { ColType.Header = "\\u0627\\u0644\\u0646\\u0648\\u0639"; } catch { }
            try { ColAmount.Header = "\\u0627\\u0644\\u0645\\u0628\\u0644\\u063A"; } catch { }
            try { ColMethod.Header = "\\u0627\\u0644\\u0637\\u0631\\u064A\\u0642\\u0629"; } catch { }
            try { ColDel.Header = "\\u062D\\u0630\\u0641"; } catch { }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PaymentsViewModel vm) vm.LoadPayments();
        }
    }
}
'''

with open(xaml_path, "w", encoding="ascii") as f:
    f.write(xaml)
with open(cs_path, "w", encoding="utf-8-sig") as f:
    f.write(cs_view)
print("  OK: PaymentsView.xaml + .xaml.cs")

# ============================================================
# 3) كتابة PaymentEditWindow.xaml + .xaml.cs (نافذة السند)
# ============================================================
print("\n[3/3] Writing PaymentEditWindow.xaml + .xaml.cs...")
xaml_edit_path = os.path.join(VIEWS, "PaymentEditWindow.xaml")
cs_edit_path = os.path.join(VIEWS, "PaymentEditWindow.xaml.cs")

if os.path.exists(xaml_edit_path):
    shutil.copy2(xaml_edit_path, xaml_edit_path + ".bak_rebuild")
if os.path.exists(cs_edit_path):
    shutil.copy2(cs_edit_path, cs_edit_path + ".bak_rebuild")

xaml_edit = '''<Window x:Class="Techtoon.UI.Views.PaymentEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Voucher" Height="700" Width="500"
        WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft"
        Background="#F5F7FA" Loaded="Window_Loaded" FontFamily="Tahoma">
    <Grid Margin="14">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <TextBlock x:Name="LblTitle" Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" FontSize="18" FontWeight="Bold" Foreground="#00695C" Margin="0,0,0,12"/>

        <TextBlock x:Name="LblVoucher" Grid.Row="1" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <TextBox x:Name="TxtVoucher" Grid.Row="1" Grid.Column="1" Padding="6,4" Margin="0,3" IsReadOnly="True"/>

        <TextBlock x:Name="LblDate" Grid.Row="2" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <DatePicker x:Name="DpDate" Grid.Row="2" Grid.Column="1" Padding="6,4" Margin="0,3"/>

        <TextBlock x:Name="LblParty" Grid.Row="3" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbParty" Grid.Row="3" Grid.Column="1" Padding="6,4" Margin="0,3" SelectionChanged="CmbParty_SelectionChanged"/>

        <TextBlock x:Name="LblInvoice" Grid.Row="4" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbInvoice" Grid.Row="4" Grid.Column="1" Padding="6,4" Margin="0,3" SelectionChanged="CmbInvoice_SelectionChanged"/>

        <TextBlock x:Name="LblAmount" Grid.Row="5" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <TextBox x:Name="TxtAmount" Grid.Row="5" Grid.Column="1" Padding="6,4" Margin="0,3" TextAlignment="Right"/>

        <TextBlock x:Name="LblMethod" Grid.Row="6" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbMethod" Grid.Row="6" Grid.Column="1" Padding="6,4" Margin="0,3"/>

        <TextBlock x:Name="LblCashBox" Grid.Row="7" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbCashBox" Grid.Row="7" Grid.Column="1" Padding="6,4" Margin="0,3"/>

        <TextBlock x:Name="LblAccount" Grid.Row="8" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbAccount" Grid.Row="8" Grid.Column="1" Padding="6,4" Margin="0,3"/>

        <TextBlock x:Name="LblNotes" Grid.Row="9" Grid.Column="0" VerticalAlignment="Top" Margin="0,5,8,5"/>
        <TextBox x:Name="TxtNotes" Grid.Row="9" Grid.Column="1" Padding="6,4" Margin="0,3" Height="60" TextWrapping="Wrap" AcceptsReturn="True"/>

        <TextBlock x:Name="LblStatus" Grid.Row="10" Grid.Column="0" Grid.ColumnSpan="2" Foreground="#00897B" FontSize="12" Margin="0,5,0,5" TextWrapping="Wrap"/>

        <StackPanel Grid.Row="12" Grid.Column="0" Grid.ColumnSpan="2" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,10,0,0">
            <Button x:Name="BtnSave" Background="#00897B" Foreground="White" FontWeight="Bold" FontSize="14" Padding="30,10" BorderThickness="0" Cursor="Hand" Margin="0,0,10,0" Click="BtnSave_Click"/>
            <Button x:Name="BtnCancel" Background="#ECEFF1" Foreground="#37474F" FontWeight="SemiBold" FontSize="13" Padding="20,10" BorderThickness="0" Cursor="Hand" Click="BtnCancel_Click"/>
        </StackPanel>
    </Grid>
</Window>
'''

cs_edit = '''using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Techtoon.UI.Views
{
    public class PartyOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public class InvoiceOption
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string Display
        {
            get
            {
                return InvoiceNumber + " - Rem: " + Remaining.ToString("N2");
            }
        }
    }

    public class CashBoxOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public class AccountOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public partial class PaymentEditWindow : Window
    {
        private readonly string _paymentType;
        private List<PartyOption> _parties = new List<PartyOption>();
        private List<InvoiceOption> _invoices = new List<InvoiceOption>();
        private List<CashBoxOption> _cashBoxes = new List<CashBoxOption>();
        private List<AccountOption> _accounts = new List<AccountOption>();

        public static string GetCS()
        {
            return "''' + CS + '''";
        }

        public PaymentEditWindow(string paymentType)
        {
            InitializeComponent();
            _paymentType = paymentType;
            SetArabicTexts();
        }

        private void SetArabicTexts()
        {
            try
            {
                if (_paymentType == "receipt")
                {
                    Title = "\\u0633\\u0646\\u062F \\u0642\\u0628\\u0636";
                    LblTitle.Text = "\\u0633\\u0646\\u062F \\u0642\\u0628\\u0636 \\u062C\\u062F\\u064A\\u062F";
                }
                else
                {
                    Title = "\\u0633\\u0646\\u062F \\u0635\\u0631\\u0641";
                    LblTitle.Text = "\\u0633\\u0646\\u062F \\u0635\\u0631\\u0641 \\u062C\\u062F\\u064A\\u062F";
                }
                LblVoucher.Text = "\\u0631\\u0642\\u0645 \\u0627\\u0644\\u0633\\u0646\\u062F:";
                LblDate.Text = "\\u0627\\u0644\\u062A\\u0627\\u0631\\u064A\\u062E:";
                LblParty.Text = "\\u0627\\u0644\\u0637\\u0631\\u0641:";
                LblInvoice.Text = "\\u0641\\u0627\\u062A\\u0648\\u0631\\u0629 \\u0645\\u0631\\u062A\\u0628\\u0637\\u0629:";
                LblAmount.Text = "\\u0627\\u0644\\u0645\\u0628\\u0644\\u063A:";
                LblMethod.Text = "\\u0637\\u0631\\u064A\\u0642\\u0629 \\u0627\\u0644\\u062F\\u0641\\u0639:";
                LblCashBox.Text = "\\u0627\\u0644\\u062E\\u0632\\u064A\\u0646\\u0629:";
                LblAccount.Text = "\\u0627\\u0644\\u062D\\u0633\\u0627\\u0628:";
                LblNotes.Text = "\\u0645\\u0644\\u0627\\u062D\\u0638\\u0627\\u062A:";
                BtnSave.Content = "\\uD83D\\uDCBE \\u062D\\u0641\\u0638";
                BtnCancel.Content = "\\u2716 \\u0625\\u0644\\u063A\\u0627\\u0621";

                CmbMethod.Items.Clear();
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\\u0646\\u0642\\u062F\\u064A", Tag = "cash" });
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\\u0628\\u0637\\u0627\\u0642\\u0629", Tag = "card" });
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\\u062A\\u062D\\u0648\\u064A\\u0644 \\u0628\\u0646\\u0643\\u064A", Tag = "bank" });
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\\u0634\\u064A\\u0643", Tag = "check" });
                CmbMethod.SelectedIndex = 0;
            }
            catch { }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DpDate.SelectedDate = DateTime.Today;
            TxtVoucher.Text = GenerateVoucherNumber();
            LoadParties();
            LoadCashBoxes();
            LoadAccounts();
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private string GenerateVoucherNumber()
        {
            try
            {
                string prefix = _paymentType == "receipt" ? "RCV" : "PAY";
                int year = DateTime.Now.Year;
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    string sql = "SELECT voucher_no FROM payments WHERE voucher_no LIKE @p ORDER BY voucher_no DESC LIMIT 1";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("p", prefix + "-" + year + "-%");
                        var o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                        {
                            string last = o.ToString();
                            int dashIdx = last.LastIndexOf('-');
                            if (dashIdx >= 0 && int.TryParse(last.Substring(dashIdx + 1), out int seq))
                                return prefix + "-" + year + "-" + (seq + 1).ToString("D4");
                        }
                    }
                }
            }
            catch { }
            string pf = _paymentType == "receipt" ? "RCV" : "PAY";
            return pf + "-" + DateTime.Now.Year + "-0001";
        }

        private void LoadParties()
        {
            try
            {
                _parties.Clear();
                CmbParty.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM parties ORDER BY name LIMIT 500", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var p = new PartyOption { Id = r.GetInt32(0), Name = r.GetString(1) };
                            _parties.Add(p);
                            CmbParty.Items.Add(p);
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadCashBoxes()
        {
            try
            {
                _cashBoxes.Clear();
                CmbCashBox.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes ORDER BY name LIMIT 100", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var c = new CashBoxOption { Id = r.GetInt32(0), Name = r.GetString(1) };
                            _cashBoxes.Add(c);
                            CmbCashBox.Items.Add(c);
                        }
                    }
                }
                if (CmbCashBox.Items.Count > 0) CmbCashBox.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadAccounts()
        {
            try
            {
                _accounts.Clear();
                CmbAccount.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM accounts ORDER BY name LIMIT 500", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var a = new AccountOption { Id = r.GetInt32(0), Name = r.GetString(1) };
                            _accounts.Add(a);
                            CmbAccount.Items.Add(a);
                        }
                    }
                }
                if (CmbAccount.Items.Count > 0) CmbAccount.SelectedIndex = 0;
            }
            catch { }
        }

        private void CmbParty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                CmbInvoice.Items.Clear();
                _invoices.Clear();
                if (CmbParty.SelectedItem is PartyOption party)
                {
                    string invoiceType = _paymentType == "receipt" ? "sales" : "purchase";
                    using (var conn = new NpgsqlConnection(GetCS()))
                    {
                        conn.Open();
                        string sql = @"SELECT id, invoice_number, total, paid_amount
                            FROM invoices
                            WHERE party_id = @pid AND type = @type AND paid_amount < total
                            ORDER BY id DESC LIMIT 50";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("pid", party.Id);
                            cmd.Parameters.AddWithValue("type", invoiceType);
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    var inv = new InvoiceOption
                                    {
                                        Id = r.GetInt32(0),
                                        InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                        Total = r.IsDBNull(2) ? 0 : r.GetDecimal(2),
                                        Paid = r.IsDBNull(3) ? 0 : r.GetDecimal(3)
                                    };
                                    _invoices.Add(inv);
                                    CmbInvoice.Items.Add(inv);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void CmbInvoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbInvoice.SelectedItem is InvoiceOption inv)
                {
                    if (string.IsNullOrWhiteSpace(TxtAmount.Text) || TxtAmount.Text == "0")
                    {
                        TxtAmount.Text = inv.Remaining.ToString("N2");
                    }
                }
            }
            catch { }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(CmbParty.SelectedItem is PartyOption party))
                {
                    LblStatus.Text = "\\u0627\\u062E\\u062A\\u0631 \\u0627\\u0644\\u0637\\u0631\\u0641";
                    return;
                }
                if (!decimal.TryParse(TxtAmount.Text, out decimal amount) || amount <= 0)
                {
                    LblStatus.Text = "\\u0627\\u0644\\u0645\\u0628\\u0644\\u063A \\u063A\\u064A\\u0631 \\u0635\\u062D\\u064A\\u062D";
                    return;
                }

                string method = "cash";
                if (CmbMethod.SelectedItem is ComboBoxItem mi && mi.Tag != null) method = mi.Tag.ToString();

                int? cashBoxId = null;
                if (CmbCashBox.SelectedItem is CashBoxOption cb) cashBoxId = cb.Id;

                int? accountId = null;
                if (CmbAccount.SelectedItem is AccountOption ac) accountId = ac.Id;

                int? linkedInvoiceId = null;
                if (CmbInvoice.SelectedItem is InvoiceOption inv) linkedInvoiceId = inv.Id;

                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    string sql = @"INSERT INTO payments (voucher_no, date, party_id, type, amount, method, cash_box_id, account_id, notes)
                        VALUES (@vno, @dt, @pid, @tp, @amt, @mth, @cbid, @acid, @nt)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("vno", TxtVoucher.Text);
                        cmd.Parameters.AddWithValue("dt", DpDate.SelectedDate ?? DateTime.Today);
                        cmd.Parameters.AddWithValue("pid", party.Id);
                        cmd.Parameters.AddWithValue("tp", _paymentType);
                        cmd.Parameters.AddWithValue("amt", amount);
                        cmd.Parameters.AddWithValue("mth", method);
                        cmd.Parameters.AddWithValue("cbid", (object)cashBoxId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("acid", (object)accountId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("nt", (object)TxtNotes.Text ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    // تحديث paid_amount للفاتورة المرتبطة
                    if (linkedInvoiceId.HasValue)
                    {
                        string updSql = "UPDATE invoices SET paid_amount = paid_amount + @amt WHERE id = @pid";
                        using (var upCmd = new NpgsqlCommand(updSql, conn))
                        {
                            upCmd.Parameters.AddWithValue("amt", amount);
                            upCmd.Parameters.AddWithValue("pid", linkedInvoiceId.Value);
                            upCmd.ExecuteNonQuery();
                        }
                    }
                }

                LblStatus.Text = "\\u062A\\u0645 \\u0627\\u0644\\u062D\\u0641\\u0638 \\u0628\\u0646\\u062C\\u0627\\u062D";
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LblStatus.Text = "\\u062E\\u0637\\u0623: " + ex.Message;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
'''

with open(xaml_edit_path, "w", encoding="ascii") as f:
    f.write(xaml_edit)
with open(cs_edit_path, "w", encoding="utf-8-sig") as f:
    f.write(cs_edit)
print("  OK: PaymentEditWindow.xaml + .xaml.cs")

# ============================================================
# حذف PaymentEditViewModel.cs القديم (لم نعد بحاجة له)
# ============================================================
old_vm = os.path.join(VMS, "PaymentEditViewModel.cs")
if os.path.exists(old_vm):
    shutil.copy2(old_vm, old_vm + ".bak_rebuild")
    # اترك الملف لكن اجعله فارغ/ملاحظة
    with open(old_vm, "w", encoding="utf-8-sig") as f:
        f.write("// PaymentEditViewModel.cs - moved to PaymentEditWindow.xaml.cs\n")
    print("  OK: Cleared old PaymentEditViewModel.cs (not needed)")

print()
print("=" * 60)
print("  DONE! Now run:")
print("  dotnet build Techtoon.UI")
print("  dotnet run --project Techtoon.UI")
print("=" * 60)