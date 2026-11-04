using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Techtoon.UI.Services;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class WriteoffRequestsViewModel : ViewModelBase
    {
        public ObservableCollection<WriteoffRequest> Requests { get; set; }
        private WriteoffRequest _selected;
        public WriteoffRequest Selected { get => _selected; set { _selected = value; OnPropertyChanged("Selected"); } }

        private string _statusFilter = "";
        public string StatusFilter { get => _statusFilter; set { _statusFilter = value; OnPropertyChanged("StatusFilter"); } }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewCommand { get; set; }
        public ICommand OpenCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand FilterAllCommand { get; set; }
        public ICommand FilterPendingCommand { get; set; }
        public ICommand FilterApprovedCommand { get; set; }

        public WriteoffRequestsViewModel()
        {
            Requests = new ObservableCollection<WriteoffRequest>();
            RefreshCommand = new UiRelayCommand(_ => Load());
            NewCommand = new UiRelayCommand(_ => NewRequest());
            OpenCommand = new UiRelayCommand(_ => OpenRequest(), _ => Selected != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteRequest(), _ => Selected != null);
            FilterAllCommand = new UiRelayCommand(_ => { StatusFilter = ""; Load(); });
            FilterPendingCommand = new UiRelayCommand(_ => { StatusFilter = "pending_approval"; Load(); });
            FilterApprovedCommand = new UiRelayCommand(_ => { StatusFilter = "approved"; Load(); });
            Load();
        }

        public void Load()
        {
            Requests.Clear();
            try
            {
                var list = WriteoffService.GetAll(string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter);
                foreach (var r in list) Requests.Add(r);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void NewRequest()
        {
            try
            {
                var win = new WriteoffRequestEditWindow();
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void OpenRequest()
        {
            if (Selected == null) return;
            try
            {
                var win = new WriteoffRequestEditWindow();
                win.LoadForEdit(Selected.Id);
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void DeleteRequest()
        {
            if (Selected == null) return;
            if (Selected.Status != "draft" && Selected.Status != "rejected")
            {
                MessageBox.Show("لا يمكن حذف طلب معتمد أو منفذ");
                return;
            }
            if (MessageBox.Show($"حذف طلب الشطب #{Selected.Id}؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try { WriteoffService.Delete(Selected.Id); Load(); }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}