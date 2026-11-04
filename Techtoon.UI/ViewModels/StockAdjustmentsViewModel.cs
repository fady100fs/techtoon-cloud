using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Techtoon.UI.Services;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class StockAdjustmentsViewModel : ViewModelBase
    {
        public ObservableCollection<StockAdjustment> Adjustments { get; set; }
        private StockAdjustment _selected;
        public StockAdjustment Selected { get => _selected; set { _selected = value; OnPropertyChanged("Selected"); } }

        private string _statusFilter = "";
        public string StatusFilter { get => _statusFilter; set { _statusFilter = value; OnPropertyChanged("StatusFilter"); } }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewCommand { get; set; }
        public ICommand OpenCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand FilterAllCommand { get; set; }
        public ICommand FilterDraftCommand { get; set; }
        public ICommand FilterPendingCommand { get; set; }
        public ICommand FilterExecutedCommand { get; set; }

        public StockAdjustmentsViewModel()
        {
            Adjustments = new ObservableCollection<StockAdjustment>();
            RefreshCommand = new UiRelayCommand(_ => Load());
            NewCommand = new UiRelayCommand(_ => NewAdjustment());
            OpenCommand = new UiRelayCommand(_ => OpenAdjustment(), _ => Selected != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteAdjustment(), _ => Selected != null);
            FilterAllCommand = new UiRelayCommand(_ => { StatusFilter = ""; Load(); });
            FilterDraftCommand = new UiRelayCommand(_ => { StatusFilter = "draft"; Load(); });
            FilterPendingCommand = new UiRelayCommand(_ => { StatusFilter = "pending"; Load(); });
            FilterExecutedCommand = new UiRelayCommand(_ => { StatusFilter = "executed"; Load(); });
            Load();
        }

        public void Load()
        {
            Adjustments.Clear();
            try
            {
                var list = InventoryAdjustmentService.GetAdjustments(string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter);
                foreach (var a in list) Adjustments.Add(a);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void NewAdjustment()
        {
            try
            {
                var win = new StockAdjustmentEditWindow();
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void OpenAdjustment()
        {
            if (Selected == null) return;
            try
            {
                var win = new StockAdjustmentEditWindow();
                win.LoadForEdit(Selected.Id);
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
        private void DeleteAdjustment()
        {
            if (Selected == null) return;
            if (Selected.Status == "approved" || Selected.Status == "executed")
            {
                MessageBox.Show("لا يمكن حذف تسوية معتمدة أو منفذة");
                return;
            }
            var result = MessageBox.Show($"حذف التسوية #{Selected.Id} ({Selected.DocumentNumber})؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            try
            {
                InventoryAdjustmentService.DeleteAdjustment(Selected.Id);
                Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}