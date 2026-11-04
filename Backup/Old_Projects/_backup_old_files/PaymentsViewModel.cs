using System;
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
                if (Type == "receipt") return "\u0642\u0628\u0636";
                if (Type == "payment") return "\u0635\u0631\u0641";
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
        public ICommand EditCommand { get; set; }

        public PaymentsViewModel()
        {
            Payments = new ObservableCollection<PaymentRow>();
            RefreshCommand = new UiRelayCommand(_ => LoadPayments());
            NewReceiptCommand = new UiRelayCommand(_ => OpenEditor("receipt"));
            NewPaymentCommand = new UiRelayCommand(_ => OpenEditor("payment"));
            DeleteCommand = new UiRelayCommand(p => { if (p is PaymentRow r) DeletePayment(r); });
            EditCommand = new UiRelayCommand(p => { if (p is PaymentRow r) OpenEditorForEdit(r); });
            LoadPayments();
        }

        public static string GetCS()
        {
            return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
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
                                Amount = r.IsDBNull(5) ? 0 : (decimal)r.GetDouble(5),
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

        private void OpenEditorForEdit(PaymentRow row)
{
    try
    {
        var win = new PaymentEditWindow(row.Type, row.Id);
        win.ShowDialog();
        LoadPayments();
    }
    catch (Exception ex) { StatusMessage = "Edit error: " + ex.Message; }
}

        public void DeletePayment(PaymentRow row)
        {
            if (row == null) return;
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0627\u0644\u0633\u0646\u062F \u0631\u0642\u0645 " + row.VoucherNo + "?",
                    "\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u062D\u0630\u0641",
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
