using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    public class DataManagementViewModel : ViewModelBase
    {
        public ObservableCollection<DataPreviewItem> PreviewItems { get; set; }

        public ICommand DeleteInvoicesCommand { get; set; }
        public ICommand ClearInventoryCommand { get; set; }
        public ICommand DeleteStockOpsCommand { get; set; }
        public ICommand DeleteFinanceCommand { get; set; }
        public ICommand FactoryResetCommand { get; set; }

        public DataManagementViewModel()
        {
            PreviewItems = new ObservableCollection<DataPreviewItem>();
            DeleteInvoicesCommand = new UiRelayCommand(_ => PreviewAndExecute("invoices"));
            ClearInventoryCommand = new UiRelayCommand(_ => PreviewAndExecute("inventory"));
            DeleteStockOpsCommand = new UiRelayCommand(_ => PreviewAndExecute("stock_ops"));
            DeleteFinanceCommand = new UiRelayCommand(_ => PreviewAndExecute("finance"));
            FactoryResetCommand = new UiRelayCommand(_ => ExecuteFactoryReset());
        }

        private void PreviewAndExecute(string type)
        {
            PreviewItems.Clear();
            try
            {
                var items = type switch
                {
                    "invoices" => DataManagementService.PreviewDeleteInvoices(),
                    "inventory" => DataManagementService.PreviewClearInventory(),
                    "stock_ops" => DataManagementService.PreviewDeleteStockOps(),
                    "finance" => DataManagementService.PreviewDeleteFinance(),
                    _ => new System.Collections.Generic.List<DataPreviewItem>()
                };

                long total = items.Sum(x => x.RowCount);
                string msg = "البيانات التي سيتم حذفها:\n\n";
                foreach (var item in items)
                    msg += $"{item.TableName}: {item.RowCount} صف\n";
                msg += $"\nالإجمالي: {total} صف\n\nهل تريد المتابعة؟ (موصى بإنشاء نسخة احتياطية أولاً)";

                if (MessageBox.Show(msg, "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // Double confirmation
                    if (MessageBox.Show("تحذير نهائي: لا يمكن التراجع عن هذا الحذف. هل أنت متأكد؟", "تأكيد نهائي", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.Yes)
                    {
                        ExecuteDelete(type);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void ExecuteDelete(string type)
        {
            try
            {
                switch (type)
                {
                    case "invoices": DataManagementService.ExecuteDeleteInvoices(); break;
                    case "inventory": DataManagementService.ExecuteClearInventory(); break;
                    case "stock_ops": DataManagementService.ExecuteDeleteStockOps(); break;
                    case "finance": DataManagementService.ExecuteDeleteFinance(); break;
                }
                MessageBox.Show("تم الحذف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void ExecuteFactoryReset()
        {
            string confirmText = "RESET";
            string input = Microsoft.VisualBasic.Interaction.InputBox($"هذا سيمسح كل بيانات العمل!\nللتأكيد، اكتب: {confirmText}", "إعادة ضبط المصنع", "", -1, -1);
            
            if (input != confirmText)
            {
                if (!string.IsNullOrEmpty(input)) MessageBox.Show("النص غير صحيح. تم الإلغاء.");
                return;
            }

            if (MessageBox.Show("هل تم إنشاء نسخة احتياطية؟ هذه العملية ستحذف كل شيء!", "تأكيد أخير", MessageBoxButton.YesNo, MessageBoxImage.Stop) != MessageBoxResult.Yes)
                return;

            try
            {
                DataManagementService.ExecuteFactoryReset(true); // true = keep users/roles
                MessageBox.Show("تمت إعادة ضبط المصنع بنجاح. يمكنك البدء من جديد.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}