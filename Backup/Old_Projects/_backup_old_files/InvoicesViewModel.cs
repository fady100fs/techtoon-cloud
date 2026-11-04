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
                    // Ø§Ø­Ø°Ù Ø§Ù„Ø¨Ù†ÙˆØ¯ Ø£ÙˆÙ„Ø§Ù‹
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_lines WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        cmd.ExecuteNonQuery();
                    }
                    // Ø§Ø­Ø°Ù Ø§Ù„Ø¯ÙØ¹Ø§Øª
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_payments WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        cmd.ExecuteNonQuery();
                    }
                    // Ø§Ø­Ø°Ù Ø§Ù„ÙØ§ØªÙˆØ±Ø©
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