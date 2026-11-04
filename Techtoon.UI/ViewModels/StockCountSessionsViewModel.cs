using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Services;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class CountWarehouseOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class StockCountSessionsViewModel : ViewModelBase
    {
        private readonly string _cs = SecureConnectionManager.GetConnectionString();

        public ObservableCollection<StockCountSession> Sessions { get; set; }
        private StockCountSession _selected;
        public StockCountSession Selected { get => _selected; set { _selected = value; OnPropertyChanged("Selected"); } }

        private string _statusFilter = "";
        public string StatusFilter { get => _statusFilter; set { _statusFilter = value; OnPropertyChanged("StatusFilter"); } }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewSessionCommand { get; set; }
        public ICommand OpenSessionCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand FilterAllCommand { get; set; }
        public ICommand FilterDraftCommand { get; set; }
        public ICommand FilterInProgressCommand { get; set; }
        public ICommand FilterApprovedCommand { get; set; }

        public StockCountSessionsViewModel()
        {
            Sessions = new ObservableCollection<StockCountSession>();
            RefreshCommand = new UiRelayCommand(_ => Load());
            NewSessionCommand = new UiRelayCommand(_ => NewSession());
            OpenSessionCommand = new UiRelayCommand(_ => OpenSession(), _ => Selected != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteSession(), _ => Selected != null);
            FilterAllCommand = new UiRelayCommand(_ => { StatusFilter = ""; Load(); });
            FilterDraftCommand = new UiRelayCommand(_ => { StatusFilter = "draft"; Load(); });
            FilterInProgressCommand = new UiRelayCommand(_ => { StatusFilter = "in_progress"; Load(); });
            FilterApprovedCommand = new UiRelayCommand(_ => { StatusFilter = "approved"; Load(); });
            Load();
        }

        public void Load()
        {
            Sessions.Clear();
            try
            {
                var list = InventoryCountService.GetSessions(string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter);
                foreach (var s in list) Sessions.Add(s);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void NewSession()
        {
            try
            {
                var win = new StockCountSessionEditWindow();
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void OpenSession()
        {
            if (Selected == null) return;
            try
            {
                var win = new StockCountSessionEditWindow();
                win.LoadForEdit(Selected.Id);
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
        private void DeleteSession()
        {
            if (Selected == null) return;
            if (Selected.Status == "approved" || Selected.Status == "executed")
            {
                MessageBox.Show("لا يمكن حذف جلسة معتمدة أو منفذة");
                return;
            }
            var result = MessageBox.Show($"حذف الجلسة #{Selected.Id} ({Selected.DocumentNumber})؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            try
            {
                InventoryCountService.DeleteSession(Selected.Id);
                Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}