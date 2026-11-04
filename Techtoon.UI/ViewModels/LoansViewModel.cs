using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Techtoon.UI.Services;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class LoansViewModel : ViewModelBase
    {
        public ObservableCollection<Loan> Loans { get; set; }
        private Loan _selected;
        public Loan Selected { get => _selected; set { _selected = value; OnPropertyChanged("Selected"); } }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private string _typeFilter = "";
        public string TypeFilter { get => _typeFilter; set { _typeFilter = value; OnPropertyChanged("TypeFilter"); } }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand FilterAllCommand { get; set; }
        public ICommand FilterInstallmentCommand { get; set; }
        public ICommand FilterOverdraftCommand { get; set; }

        public LoansViewModel()
        {
            Loans = new ObservableCollection<Loan>();
            RefreshCommand = new UiRelayCommand(_ => Load());
            NewCommand = new UiRelayCommand(_ => NewLoan());
            EditCommand = new UiRelayCommand(_ => EditLoan(), _ => Selected != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteLoan(), _ => Selected != null);
            FilterAllCommand = new UiRelayCommand(_ => { TypeFilter = ""; Load(); });
            FilterInstallmentCommand = new UiRelayCommand(_ => { TypeFilter = "installment"; Load(); });
            FilterOverdraftCommand = new UiRelayCommand(_ => { TypeFilter = "overdraft"; Load(); });
            Load();
        }

        public void Load()
        {
            Loans.Clear();
            try
            {
                var list = LoansService.GetAllLoans(string.IsNullOrEmpty(TypeFilter) ? null : TypeFilter);
                foreach (var l in list) Loans.Add(l);
                StatusMessage = "Loaded: " + Loans.Count + " loans";
            }
            catch (Exception ex) 
            { 
                StatusMessage = "Error: " + ex.Message; 
            }
        }

        private void NewLoan()
        {
            try
            {
                var win = new LoanEditWindow();
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void EditLoan()
        {
            if (Selected == null) return;
            try
            {
                var win = new LoanEditWindow();
                win.LoadForEdit(Selected);
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void DeleteLoan()
        {
            if (Selected == null) return;
            if (MessageBox.Show($"حذف القرض '{Selected.LoanName}'؟ سيتم حذف جدول الأقساط المرتبط به.", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            
            try
            {
                LoansService.DeleteLoan(Selected.Id);
                Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}