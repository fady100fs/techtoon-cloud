using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Techtoon.UI.Services;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class DamagedItemsViewModel : ViewModelBase
    {
        public ObservableCollection<DamagedItem> Items { get; set; }
        private DamagedItem _selected;
        public DamagedItem Selected { get => _selected; set { _selected = value; OnPropertyChanged("Selected"); } }

        private string _statusFilter = "";
        public string StatusFilter { get => _statusFilter; set { _statusFilter = value; OnPropertyChanged("StatusFilter"); } }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewCommand { get; set; }
        public ICommand OpenCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand FilterAllCommand { get; set; }
        public ICommand FilterDraftCommand { get; set; }
        public ICommand FilterInspectionCommand { get; set; }

        public DamagedItemsViewModel()
        {
            Items = new ObservableCollection<DamagedItem>();
            RefreshCommand = new UiRelayCommand(_ => Load());
            NewCommand = new UiRelayCommand(_ => NewItem());
            OpenCommand = new UiRelayCommand(_ => OpenItem(), _ => Selected != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteItem(), _ => Selected != null);
            FilterAllCommand = new UiRelayCommand(_ => { StatusFilter = ""; Load(); });
            FilterDraftCommand = new UiRelayCommand(_ => { StatusFilter = "draft"; Load(); });
            FilterInspectionCommand = new UiRelayCommand(_ => { StatusFilter = "under_inspection"; Load(); });
            Load();
        }

        public void Load()
        {
            Items.Clear();
            try
            {
                var list = DamagedItemsService.GetAll(string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter);
                foreach (var i in list) Items.Add(i);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void NewItem()
        {
            try
            {
                var win = new DamagedItemEditWindow();
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void OpenItem()
        {
            if (Selected == null) return;
            try
            {
                var win = new DamagedItemEditWindow();
                win.LoadForEdit(Selected.Id);
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void DeleteItem()
        {
            if (Selected == null) return;
            if (Selected.Status != "draft") { MessageBox.Show("لا يمكن حذف سجل غير مسودة"); return; }
            if (MessageBox.Show($"حذف سجل التالف #{Selected.Id}؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try { DamagedItemsService.Delete(Selected.Id); Load(); }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}