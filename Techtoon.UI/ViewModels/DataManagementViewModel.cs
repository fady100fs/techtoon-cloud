using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>
    /// إدارة البيانات — يعرض كل مجموعات الحذف ديناميكياً (لا بطاقات ثابتة)
    /// ويتيح الحذف الجزئي: بيانات عميل واحد، أو كل الحركات قبل تاريخ معيّن.
    /// </summary>
    public class DataManagementViewModel : ViewModelBase
    {
        /// <summary>كل مجموعات الحذف.</summary>
        public ObservableCollection<DataGroup> Groups { get; set; }

        /// <summary>التصنيفات مع مجموعاتها.</summary>
        public ObservableCollection<DataGroupCategory> Categories { get; set; }

        /// <summary>معاينة العملية الأخيرة.</summary>
        public ObservableCollection<DataPreviewItem> PreviewItems { get; set; }

        /// <summary>عدد السجلات في المعاينة.</summary>
        public long PreviewTotal { get; set; }

        /// <summary>نص المعاينة.</summary>
        public string PreviewTitle { get; set; } = "";

        /// <summary>هل يوجد محتوى في المعاينة؟</summary>
        public bool HasPreview => PreviewItems.Count > 0;

        /// <summary>حذف مجموعة كاملة.</summary>
        public ICommand DeleteGroupCommand { get; set; }

        /// <summary>حذف بيانات عميل واحد.</summary>
        public ICommand DeletePartyDataCommand { get; set; }

        /// <summary>حذف الحركات قبل تاريخ.</summary>
        public ICommand DeleteBeforeDateCommand { get; set; }

        /// <summary>إعادة ضبط المصنع.</summary>
        public ICommand FactoryResetCommand { get; set; }

        /// <summary>تحديث المعاينة.</summary>
        public ICommand RefreshPreviewCommand { get; set; }

        public DataManagementViewModel()
        {
            Groups = new ObservableCollection<DataGroup>(DataManagementService.Groups);
            PreviewItems = new ObservableCollection<DataPreviewItem>();
            Categories = BuildCategories(Groups);

            DeleteGroupCommand = new UiRelayCommand(p => DeleteGroup(p as DataGroup), _ => AuthService.HasPermission("settings.data"));
            DeletePartyDataCommand = new UiRelayCommand(_ => DeletePartyData(), _ => AuthService.HasPermission("settings.data"));
            DeleteBeforeDateCommand = new UiRelayCommand(_ => DeleteBeforeDate(), _ => AuthService.HasPermission("settings.data"));
            FactoryResetCommand = new UiRelayCommand(_ => ExecuteFactoryReset(), _ => AuthService.HasPermission("settings.data"));
            RefreshPreviewCommand = new UiRelayCommand(_ => RefreshPreview());
        }

        private static ObservableCollection<DataGroupCategory> BuildCategories(IEnumerable<DataGroup> groups)
        {
            var result = new ObservableCollection<DataGroupCategory>();
            foreach (var g in groups.OrderBy(x => x.Category).ThenBy(x => x.Title))
            {
                var cat = result.FirstOrDefault(c => c.Name == g.Category);
                if (cat == null)
                {
                    cat = new DataGroupCategory { Name = g.Category };
                    result.Add(cat);
                }
                cat.Groups.Add(g);
            }
            return result;
        }

        /// <summary>معاينة مجموعة (عند فتح الشاشة أو الطلب).</summary>
        public void RefreshPreview()
        {
            PreviewItems.Clear();
            PreviewTotal = 0;
            PreviewTitle = "";
            OnPropertyChanged(nameof(HasPreview));
        }

        // ==================== حذف مجموعة ====================

        private void DeleteGroup(DataGroup? group)
        {
            if (group == null) return;

            DataPreviewResult preview;
            try
            {
                preview = DataManagementService.Preview(group.Id);
            }
            catch (Exception ex)
            {
                ShowError(ex, "تعذّر إعداد المعاينة");
                return;
            }

            if (!Confirm(preview, $"حذف «{group.Title}»")) return;

            try
            {
                var result = DataManagementService.Execute(group.Id);
                MessageBox.Show(
                    result.Message + "\n\n" + string.Join("\n", result.Details),
                    "تم الحذف", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex, "تعذّر حذف «" + group.Title + "»");
            }
        }

        // ==================== حذف بيانات عميل ====================

        private void DeletePartyData()
        {
            var input = Techtoon.UI.Views.InputDialog.Show(
                System.Windows.Application.Current?.MainWindow,
                "أدخل رقم العميل (ID) الذي تريد حذف بياناته.",
                "حذف بيانات عميل", "");
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!int.TryParse(input.Trim(), out var partyId))
            {
                MessageBox.Show("أدخل رقمًا صحيحًا للعميل.", "قيمة غير صحيحة",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var scope = new DeleteScope { Kind = "party", PartyId = partyId, IncludePartyRecord = false };

            DataPreviewResult preview;
            try
            {
                preview = DataManagementService.PreviewScope(scope);
            }
            catch (Exception ex)
            {
                ShowError(ex, "تعذّر إعداد المعاينة");
                return;
            }

            if (preview.TotalRows == 0)
            {
                MessageBox.Show("لا توجد بيانات مرتبطة بهذا العميل.", "لا يوجد ما يُحذف",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var deleteParty = MessageBox.Show(
                "هل تريد حذف سجل العميل نفسه أيضًا؟\n\n" +
                "«نعم» = حذف كل بيانات العميل مع سجله.\n«لا» = حذف عملياته فقط (يبقى العميل).",
                "نطاق الحذف", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (deleteParty == MessageBoxResult.Cancel) return;
            scope.IncludePartyRecord = deleteParty == MessageBoxResult.Yes;

            if (!Confirm(preview, $"حذف بيانات العميل رقم {partyId}")) return;

            try
            {
                var result = DataManagementService.ExecuteScope(scope);
                MessageBox.Show(
                    result.Message + "\n\n" + string.Join("\n", result.Details),
                    "تم الحذف", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex, "تعذّر حذف بيانات العميل");
            }
        }

        // ==================== حذف الحركات قبل تاريخ ====================

        private void DeleteBeforeDate()
        {
            var input = Techtoon.UI.Views.InputDialog.Show(
                System.Windows.Application.Current?.MainWindow,
                "أدخل التاريخ بصيغة yyyy-MM-dd — سيتم حذف كل الحركات السابقة له.",
                "حذف الحركات قبل تاريخ", DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"));
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!DateTime.TryParse(input.Trim(), out var before))
            {
                MessageBox.Show("أدخل تاريخًا صحيحًا بصيغة yyyy-MM-dd.", "قيمة غير صحيحة",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var scope = new DeleteScope { Kind = "before_date", Before = before };

            DataPreviewResult preview;
            try
            {
                preview = DataManagementService.PreviewScope(scope);
            }
            catch (Exception ex)
            {
                ShowError(ex, "تعذّر إعداد المعاينة");
                return;
            }

            if (preview.TotalRows == 0)
            {
                MessageBox.Show("لا توجد حركات قبل هذا التاريخ.", "لا يوجد ما يُحذف",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!Confirm(preview, $"حذف كل الحركات قبل {before:yyyy-MM-dd}")) return;

            try
            {
                var result = DataManagementService.ExecuteScope(scope);
                MessageBox.Show(
                    result.Message + "\n\n" + string.Join("\n", result.Details),
                    "تم الحذف", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex, "تعذّر حذف الحركات");
            }
        }

        // ==================== إعادة ضبط المصنع ====================

        private void ExecuteFactoryReset()
        {
            var options = new FactoryResetOptions
            {
                KeepUsersAndRoles = true,
                KeepMasterData = false,
                KeepSettings = true
            };

            DataPreviewResult preview;
            try
            {
                preview = DataManagementService.PreviewFactoryReset(options);
            }
            catch (Exception ex)
            {
                ShowError(ex, "تعذّر إعداد المعاينة");
                return;
            }

            var keepMaster = MessageBox.Show(
                "هل تريد الاحتفاظ بالبيانات الأساسية (الأطراف والأصناف والحسابات)؟\n\n" +
                "«نعم» = حذف الحركات فقط.\n«لا» = حذف كل شيء بما فيه البيانات الأساسية.",
                "نطاق إعادة الضبط", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (keepMaster == MessageBoxResult.Cancel) return;
            options.KeepMasterData = keepMaster == MessageBoxResult.Yes;

            if (!Confirm(preview, "إعادة ضبط المصنع")) return;

            const string confirmText = "RESET";
            var input = Techtoon.UI.Views.InputDialog.Show(
                System.Windows.Application.Current?.MainWindow,
                $"هذه العملية ستمحو كل بيانات العمل!\nللتأكيد، اكتب: {confirmText}",
                "إعادة ضبط المصنع", "");
            if (input != confirmText)
            {
                if (!string.IsNullOrEmpty(input))
                    MessageBox.Show("النص غير صحيح. تم الإلغاء.");
                return;
            }

            try
            {
                var result = DataManagementService.ExecuteFactoryReset(options);
                MessageBox.Show(
                    result.Message + "\n\n" + string.Join("\n", result.Details),
                    "تمت إعادة الضبط", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex, "تعذّر تنفيذ إعادة ضبط المصنع");
            }
        }

        // ==================== أدوات ====================

        private bool Confirm(DataPreviewResult preview, string title)
        {
            var msg = "البيانات التي سيتم حذفها:\n\n";
            foreach (var item in preview.Items)
                msg += $"{item.Label} ({item.TableName}) — {item.Action}: {item.RowCount} صف\n";
            msg += $"\nالإجمالي: {preview.TotalRows} صف\n";
            foreach (var note in preview.Notes) msg += "\n" + note + "\n";
            msg += "\nهل تريد المتابعة؟ (موصى بإنشاء نسخة احتياطية أولاً)";

            if (MessageBox.Show(msg, "تأكيد الحذف — " + title, MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return false;

            return MessageBox.Show(
                    "تحذير نهائي: لا يمكن التراجع عن هذا الحذف. هل أنت متأكد؟",
                    "تأكيد نهائي", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.Yes;
        }

        private static void ShowError(Exception ex, string context)
        {
            MessageBox.Show(ErrorMessages.Describe(ex, context), "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>تصنيف يجمع عدة مجموعات (لتسهيل العرض).</summary>
    public class DataGroupCategory
    {
        public string Name { get; set; } = "";
        public ObservableCollection<DataGroup> Groups { get; } = new();
    }
}
