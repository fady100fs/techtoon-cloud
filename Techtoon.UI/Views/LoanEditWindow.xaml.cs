using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public class LoanBankOption { public int Id { get; set; } public string Name { get; set; } }

    public partial class LoanEditWindow : Window
    {
        public bool Saved { get; private set; }
        public ObservableCollection<LoanBankOption> Banks { get; } = new ObservableCollection<LoanBankOption>();

        private Loan _editLoan;
        private bool _isEditMode = false;

        public LoanEditWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadBanks();
            cmbType.SelectionChanged += CmbType_SelectionChanged;
        }

        private void LoadBanks()
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE is_active = TRUE ORDER BY name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) Banks.Add(new LoanBankOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            cmbBank.ItemsSource = Banks;
        }

        public void LoadForEdit(Loan loan)
        {
            _editLoan = loan;
            _isEditMode = true;
            Title = "تعديل قرض: " + loan.LoanName;

            txtName.Text = loan.LoanName;
            txtAmount.Text = loan.PrincipalAmount.ToString("0.00");
            txtRate.Text = loan.InterestRate.ToString("0.00");
            dpDisburse.SelectedDate = loan.DisbursementDate;
            dpFirstPay.SelectedDate = loan.FirstPaymentDate;
            txtNum.Text = loan.NumInstallments > 0 ? loan.NumInstallments.ToString() : "";
            txtNotes.Text = loan.Notes ?? "";

            // تعطيل الحقول التي لا يجب تعديلها بعد الإنشاء
            txtAmount.IsEnabled = false;
            txtNum.IsEnabled = false;
            cmbType.IsEnabled = false;
            cmbMethod.IsEnabled = false;
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isInstallment = (cmbType.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "installment";
            lblNum.Visibility = isInstallment ? Visibility.Visible : Visibility.Collapsed;
            txtNum.Visibility = isInstallment ? Visibility.Visible : Visibility.Collapsed;
            lblMethod.Visibility = isInstallment ? Visibility.Visible : Visibility.Collapsed;
            cmbMethod.Visibility = isInstallment ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("الاسم مطلوب"); return; }
            if (!double.TryParse(txtAmount.Text, out double amount) || amount <= 0) { MessageBox.Show("المبلغ غير صحيح"); return; }
            if (!double.TryParse(txtRate.Text, out double rate)) { MessageBox.Show("سعر الفائدة غير صحيح"); return; }

            var typeItem = cmbType.SelectedItem as ComboBoxItem;
            string loanType = typeItem?.Tag?.ToString() ?? "installment";
            int num = 0;
            if (loanType == "installment")
            {
                if (!int.TryParse(txtNum.Text, out num) || num <= 0) { MessageBox.Show("عدد الأقساط غير صحيح"); return; }
            }

            var loan = new Loan
            {
                LoanName = txtName.Text,
                LoanType = loanType,
                BankAccountId = cmbBank.SelectedValue as int?,
                PrincipalAmount = amount,
                DisbursementDate = dpDisburse.SelectedDate ?? DateTime.Today,
                FirstPaymentDate = dpFirstPay.SelectedDate,
                NumInstallments = num,
                InterestRate = rate,
                InterestCalcMethod = (cmbMethod.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "declining",
                Status = "active",
                Notes = txtNotes.Text
            };

            try
            {
                if (_isEditMode)
                {
                    loan.Id = _editLoan.Id;
                    LoansService.UpdateLoan(loan);
                }
                else
                {
                    LoansService.CreateLoan(loan, 1);
                }
                Saved = true;
                MessageBox.Show("تم الحفظ بنجاح");
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}