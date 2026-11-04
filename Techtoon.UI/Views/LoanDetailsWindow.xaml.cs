using System;
using System.Collections.ObjectModel;
using System.Windows;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public partial class LoanDetailsWindow : Window
    {
        private int _loanId;
        private Loan _loan;
        public ObservableCollection<LoanInstallment> Schedule { get; } = new ObservableCollection<LoanInstallment>();

        public LoanDetailsWindow()
        {
            InitializeComponent();
            DataContext = this;
            dgSchedule.ItemsSource = Schedule;
        }

        public void LoadForEdit(int loanId)
        {
            _loanId = loanId;
            _loan = LoansService.GetById(loanId);
            if (_loan == null) return;

            lblTitle.Text = _loan.LoanName;
            lblPrincipal.Text = _loan.PrincipalDisplay;
            lblRate.Text = _loan.InterestRate + "%";
            lblStatus.Text = _loan.StatusDisplay;

            Schedule.Clear();
            var list = LoansService.GetSchedule(loanId);
            foreach (var i in list) Schedule.Add(i);

            // Show/hide repay button based on loan type
            btnRepay.Visibility = _loan.LoanType == "overdraft" ? Visibility.Visible : Visibility.Visible;
        }

        private void BtnRepay_Click(object sender, RoutedEventArgs e)
        {
            if (_loan.LoanType == "overdraft")
            {
                // Overdraft repayment
                var amountStr = Microsoft.VisualBasic.Interaction.InputBox("أدخل مبلغ السداد:", "سداد جاري مدين", "", -1, -1);
                if (string.IsNullOrEmpty(amountStr)) return;
                if (!double.TryParse(amountStr, out double amount)) { MessageBox.Show("مبلغ غير صحيح"); return; }
                try
                {
                    OverdraftService.Repay(_loanId, amount, DateTime.Now, "Repayment", 1);
                    MessageBox.Show("تم السداد بنجاح");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
            else
            {
                // Installment repayment
                if (dgSchedule.SelectedItem is LoanInstallment inst)
                {
                    var amountStr = Microsoft.VisualBasic.Interaction.InputBox($"أدخل مبلغ السداد للقسط {inst.InstallmentNumber} (المتبقي: {inst.TotalAmount - inst.PaidAmount}):", "سداد قسط", "", -1, -1);
                    if (string.IsNullOrEmpty(amountStr)) return;
                    if (!double.TryParse(amountStr, out double amount)) { MessageBox.Show("مبلغ غير صحيح"); return; }
                    try
                    {
                        LoansService.RecordRepayment(inst.Id, amount, DateTime.Now, 1);
                        MessageBox.Show("تم السداد بنجاح");
                        LoadForEdit(_loanId); // Refresh
                    }
                    catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
                }
                else
                {
                    MessageBox.Show("اختر قسطاً من الجدول أولاً");
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}