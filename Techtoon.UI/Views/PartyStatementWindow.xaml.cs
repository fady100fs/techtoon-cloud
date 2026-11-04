using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public class PartyStatementParty
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class StatementRow
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public partial class PartyStatementWindow : Window
    {
        private readonly string _connectionString =
            "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;" +
            "Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;" +
            "SSL Mode=Require;Trust Server Certificate=true";

        public ObservableCollection<PartyStatementParty> Parties { get; } = new ObservableCollection<PartyStatementParty>();
        public ObservableCollection<StatementRow> Rows { get; } = new ObservableCollection<StatementRow>();

        private decimal _openingBalance = 0;
        private decimal _totalInvoices = 0;
        private decimal _totalPayments = 0;

        public PartyStatementWindow()
        {
            InitializeComponent();
            DataContext = this;
            dpFrom.SelectedDate = DateTime.Today.AddMonths(-3);
            dpTo.SelectedDate = DateTime.Today;
            LoadParties();
            dgTransactions.ItemsSource = Rows;
        }

        private void LoadParties()
        {
            Parties.Clear();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string sql = "SELECT id, name FROM parties ";
            bool hasIsActive = false;
            using (var chk = new NpgsqlCommand(
                "SELECT column_name FROM information_schema.columns WHERE table_name='parties' AND column_name='is_active'", conn))
            using (var rdr = chk.ExecuteReader())
                hasIsActive = rdr.Read();
            if (hasIsActive) sql += "WHERE is_active = TRUE ";
            sql += "ORDER BY name";
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Parties.Add(new PartyStatementParty { Id = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            cmbParty.ItemsSource = Parties;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (cmbParty.SelectedValue == null)
            {
                MessageBox.Show("اختر الطرف");
                return;
            }
            LoadStatement();
        }

        private void LoadStatement()
        {
            int partyId = (int)cmbParty.SelectedValue;
            DateTime from = dpFrom.SelectedDate ?? DateTime.Today.AddMonths(-3);
            DateTime to = dpTo.SelectedDate ?? DateTime.Today;

            Rows.Clear();
            _totalInvoices = 0;
            _totalPayments = 0;

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // 1) الرصيد الافتتاحي
            using (var cmd = new NpgsqlCommand(
                "SELECT COALESCE(SUM(total - paid_amount), 0) FROM invoices " +
                "WHERE party_id = @pid AND date < @from AND status <> 'cancelled'", conn))
            {
                cmd.Parameters.AddWithValue("@pid", partyId);
                cmd.Parameters.AddWithValue("@from", from);
                _openingBalance = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            // 2) الفواتير في الفترة
            using (var cmd = new NpgsqlCommand(
                "SELECT date, invoice_number, total, paid_amount, notes FROM invoices " +
                "WHERE party_id = @pid AND date BETWEEN @from AND @to AND status <> 'cancelled' " +
                "ORDER BY date", conn))
            {
                cmd.Parameters.AddWithValue("@pid", partyId);
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.AddDays(1));
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var total = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
                    _totalInvoices += total;
                    Rows.Add(new StatementRow
                    {
                        Date = reader.GetDateTime(0),
                        Type = "فاتورة",
                        Reference = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Debit = total,
                        Credit = 0
                    });
                }
            }

            // 3) المدفوعات في الفترة
            using (var cmd = new NpgsqlCommand(
                "SELECT date, reference_number, amount, notes FROM payments " +
                "WHERE party_id = @pid AND date BETWEEN @from AND @to " +
                "ORDER BY date", conn))
            {
                cmd.Parameters.AddWithValue("@pid", partyId);
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.AddDays(1));
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var amount = reader.IsDBNull(2) ? 0m : (decimal)reader.GetDouble(2);
                    _totalPayments += amount;
                    Rows.Add(new StatementRow
                    {
                        Date = reader.GetDateTime(0),
                        Type = "سند",
                        Reference = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Debit = 0,
                        Credit = amount
                    });
                }
            }

            // 4) ترتيب + احتساب الرصيد الجاري
            var sorted = Rows.Cast<dynamic>().ToList();
            sorted.Sort((a, b) => a.Date.CompareTo(b.Date));
            Rows.Clear();
            decimal runningBalance = _openingBalance;
            foreach (var r in sorted)
            {
                runningBalance += r.Debit - r.Credit;
                r.Balance = runningBalance;
                Rows.Add(r);
            }

            // 5) عرض المجاميع
            lblOpening.Text = _openingBalance.ToString("0.00");
            lblTotalInvoices.Text = _totalInvoices.ToString("0.00");
            lblTotalPayments.Text = _totalPayments.ToString("0.00");
            lblClosing.Text = runningBalance.ToString("0.00");
        }

        private void BtnPdf_Click(object sender, RoutedEventArgs e)
        {
            if (cmbParty.SelectedValue == null)
            {
                MessageBox.Show("اختر الطرف أولًا");
                return;
            }
            var dlg = new SaveFileDialog
            {
                Filter = "PDF Files|*.pdf",
                FileName = "Statement_" + ((PartyStatementParty)cmbParty.SelectedItem).Name + "_" + (dpFrom.SelectedDate?.ToString("yyyyMMdd") ?? "") + "_" + (dpTo.SelectedDate?.ToString("yyyyMMdd") ?? "") + ".pdf"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                int partyId = (int)cmbParty.SelectedValue;
                string partyName = ((PartyStatementParty)cmbParty.SelectedItem).Name;
                DateTime from = dpFrom.SelectedDate ?? DateTime.Today;
                DateTime to = dpTo.SelectedDate ?? DateTime.Today;
                var svc = new StatementPdfService(_connectionString);
                svc.Export(partyId, partyName, from, to, _openingBalance, Rows.Cast<dynamic>().ToList(), dlg.FileName);
                MessageBox.Show("تم التصدير بنجاح:\n" + dlg.FileName, "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ التصدير: " + ex.Message);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

