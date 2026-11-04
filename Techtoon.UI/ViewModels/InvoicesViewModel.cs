using System;
using System.Collections.ObjectModel;
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
        public string TypeDisplay { get { return Type == "sales" ? "\u0645\u0628\u064A\u0639\u0627\u062A" : (Type == "purchase" ? "\u0645\u0634\u062A\u0631\u064A\u0627\u062A" : Type ?? ""); } }
        public string DateDisplay { get { return Date.ToString("yyyy-MM-dd"); } }
        public string TotalDisplay { get { return Total.ToString("N2"); } }
        public string PaidDisplay { get { return Paid.ToString("N2"); } }
        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }
    }

    public class InvoicesViewModel : ViewModelBase
    {
        public ObservableCollection<InvoiceRow> Invoices { get; set; }
        private InvoiceRow _selectedInvoice;
        public InvoiceRow SelectedInvoice { get { return _selectedInvoice; } set { _selectedInvoice = value; OnPropertyChanged("SelectedInvoice"); } }

        private string _statusMessage = "";
        public string StatusMessage { get { return _statusMessage; } set { _statusMessage = value; OnPropertyChanged("StatusMessage"); } }

        // === Search filters ===
        private string _searchByNumber = "";
        public string SearchByNumber { get { return _searchByNumber; } set { _searchByNumber = value; OnPropertyChanged("SearchByNumber"); } }

        private string _searchByParty = "";
        public string SearchByParty { get { return _searchByParty; } set { _searchByParty = value; OnPropertyChanged("SearchByParty"); } }

        private DateTime? _fromDate = null;
        public DateTime? FromDate { get { return _fromDate; } set { _fromDate = value; OnPropertyChanged("FromDate"); } }

        private DateTime? _toDate = null;
        public DateTime? ToDate { get { return _toDate; } set { _toDate = value; OnPropertyChanged("ToDate"); } }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewInvoiceCommand { get; set; }
        public ICommand EditInvoiceCommand { get; set; }
        public ICommand DeleteInvoiceCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        public ICommand ClearSearchCommand { get; set; }

        public InvoicesViewModel()
        {
            Invoices = new ObservableCollection<InvoiceRow>();
            RefreshCommand = new UiRelayCommand(_ => LoadInvoices());
            NewInvoiceCommand = new UiRelayCommand(_ => OpenNew());
            EditInvoiceCommand = new UiRelayCommand(_ => OpenEdit(), _ => SelectedInvoice != null);
            DeleteInvoiceCommand = new UiRelayCommand(_ => DeleteSelected(), _ => SelectedInvoice != null);
            SearchCommand = new UiRelayCommand(_ => LoadInvoices());
            ClearSearchCommand = new UiRelayCommand(_ => ClearSearch());
            LoadInvoices();
        }

        public static string GetCS() { return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"; }

        private void ClearSearch()
        {
            SearchByNumber = "";
            SearchByParty = "";
            FromDate = null;
            ToDate = null;
            LoadInvoices();
        }

        public void LoadInvoices()
        {
            Invoices.Clear();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    var sql = "SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount, " +
                              "COALESCE(p.name,'') AS party_name, i.status " +
                              "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id " +
                              "WHERE 1=1";
                    var conditions = new System.Collections.Generic.List<string>();
                                        if (!string.IsNullOrWhiteSpace(SearchByNumber))
                    {
                        conditions.Add("i.invoice_number ILIKE @num");
                    }
                    if (!string.IsNullOrWhiteSpace(SearchByParty))
                    {
                        conditions.Add("p.name ILIKE @party");
                    }
                    if (FromDate.HasValue)
                    {
                        conditions.Add("i.date >= @from");
                    }
                    if (ToDate.HasValue)
                    {
                        conditions.Add("i.date < @to");
                    }

                    if (conditions.Count > 0)
                        sql += " AND " + string.Join(" AND ", conditions);
                    sql += " ORDER BY i.id DESC LIMIT 500";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(SearchByNumber))
                            cmd.Parameters.AddWithValue("num", "%" + SearchByNumber.Trim() + "%");
                        if (!string.IsNullOrWhiteSpace(SearchByParty))
                            cmd.Parameters.AddWithValue("party", "%" + SearchByParty.Trim() + "%");
                        if (FromDate.HasValue)
                            cmd.Parameters.AddWithValue("from", FromDate.Value);
                        if (ToDate.HasValue)
                            cmd.Parameters.AddWithValue("to", ToDate.Value.AddDays(1));

                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                Invoices.Add(new InvoiceRow
                                {
                                    Id = r.GetInt32(0),
                                    InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                    Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2),
                                    Type = r.IsDBNull(3) ? "" : r.GetString(3),
                                    Total = r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4),
                                    Paid = r.IsDBNull(5) ? 0 : (decimal)r.GetDouble(5),
                                    PartyName = r.IsDBNull(6) ? "" : r.GetString(6),
                                    Status = r.IsDBNull(7) ? "" : r.GetString(7)
                                });
                    }
                }
                StatusMessage = "Loaded: " + Invoices.Count + " invoices";
            }
            catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
        }

        private void OpenNew() { try { var w = new InvoiceEditWindow(null); w.ShowDialog(); LoadInvoices(); } catch { } }
        private void OpenEdit() { try { if (_selectedInvoice == null) return; var w = new InvoiceEditWindow(_selectedInvoice.Id); w.ShowDialog(); LoadInvoices(); } catch { } }
        private void DeleteSelected()
        {
            if (_selectedInvoice == null) return;
            try
            {
                var res = System.Windows.MessageBox.Show("\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0647\u0630\u0647 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629\u061F", "\u062A\u0623\u0643\u064A\u062F", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (res != System.Windows.MessageBoxResult.Yes) return;
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    foreach (var t in new[] { "invoice_lines", "invoice_payments" }) { using (var c = new NpgsqlCommand("DELETE FROM " + t + " WHERE invoice_id=@id", conn)) { c.Parameters.AddWithValue("id", _selectedInvoice.Id); c.ExecuteNonQuery(); } }
                    using (var c = new NpgsqlCommand("DELETE FROM invoices WHERE id=@id", conn)) { c.Parameters.AddWithValue("id", _selectedInvoice.Id); c.ExecuteNonQuery(); }
                }
                LoadInvoices();
            }
            catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
        }
    }
}