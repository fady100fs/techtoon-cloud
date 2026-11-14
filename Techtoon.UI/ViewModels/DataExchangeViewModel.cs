using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Techtoon.Core.Bundles;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>صفّ مجمّع في قائمة المجمّعات.</summary>
    public class BundleItem : ViewModelBase
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "📦";
        public string TablesDisplay { get; set; } = "";
        public override string ToString() => Title;
    }

    /// <summary>تفصيل جدول في معاينة الاستيراد.</summary>
    public class BundlePlanRow : ViewModelBase
    {
        public string Table { get; set; } = "";
        public int ToInsert { get; set; }
        public int ToUpdate { get; set; }
        public int ToSkip { get; set; }
        public int Errors { get; set; }
        public string Summary =>
            Errors > 0
                ? $"{ToInsert} جديد، {ToUpdate} تحديث، {ToSkip} متخطّى — {Errors} خطأ"
                : $"{ToInsert} جديد، {ToUpdate} تحديث، {ToSkip} متخطّى";
    }

    /// <summary>
    /// صفحة الاستيراد/التصدير المُجمَّع (Bundles).
    /// تتيح تصدير مجموعة جداول مترابطة في ملف واحد، واستيرادها ذرّيًا مع معاينة قبل التنفيذ.
    /// </summary>
    public class DataExchangeViewModel : ViewModelBase
    {
        public ObservableCollection<BundleItem> Bundles { get; } = new();
        public ObservableCollection<BundlePlanRow> PlanRows { get; } = new();

        private BundleItem? _selectedBundle;
        public BundleItem? SelectedBundle
        {
            get => _selectedBundle;
            set { SetProperty(ref _selectedBundle, value); OnPropertyChanged(nameof(SelectedBundleDescription)); }
        }

        private string _statusMessage = "اختر مجمّعًا لتصديره أو استيراده.";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private string _bundleInfo = "";
        public string BundleInfo { get => _bundleInfo; set => SetProperty(ref _bundleInfo, value); }

        private bool _hasPlan;
        public bool HasPlan { get => _hasPlan; set => SetProperty(ref _hasPlan, value); }

        private bool _canImport;
        public bool CanImport { get => _canImport; set => SetProperty(ref _canImport, value); }

        private bool _createMissingReferences = true;
        /// <summary>إنشاء السجلات المرجعية المفقودة تلقائيًا (عميل/صنف/حساب غير موجود).</summary>
        public bool CreateMissingReferences
        {
            get => _createMissingReferences;
            set => SetProperty(ref _createMissingReferences, value);
        }

        private int _conflictModeIndex;
        /// <summary>0 = تخطّي الموجود، 1 = تحديث الموجود، 2 = إدراج دائمًا.</summary>
        public int ConflictModeIndex
        {
            get => _conflictModeIndex;
            set { SetProperty(ref _conflictModeIndex, value); OnPropertyChanged(nameof(ConflictModeText)); }
        }

        public string ConflictModeText => ConflictModeIndex switch
        {
            1 => "تحديث الصفوف الموجودة",
            2 => "إدراج دائمًا (قد يكرّر)",
            _ => "تخطّي الصفوف الموجودة (آمن)"
        };

        public string SelectedBundleDescription => SelectedBundle?.Description ?? "";

        public ICommand ExportCommand { get; }
        public ICommand PickAndAnalyzeCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand RefreshCommand { get; }

        public DataExchangeViewModel()
        {
            ExportCommand = new RelayCommand(ExportBundle, () => SelectedBundle != null);
            PickAndAnalyzeCommand = new RelayCommand(PickAndAnalyze);
            ImportCommand = new RelayCommand(ImportBundle, () => CanImport);
            RefreshCommand = new RelayCommand(LoadBundles);
            LoadBundles();
        }

        public void LoadBundles()
        {
            Bundles.Clear();
            foreach (var def in BundleCatalog.All())
            {
                Bundles.Add(new BundleItem
                {
                    Id = def.Id,
                    Title = def.Title,
                    Description = def.Description,
                    Icon = def.Icon,
                    TablesDisplay = string.Join("، ", def.Tables.Select(t => t.Table))
                });
            }
            if (SelectedBundle == null && Bundles.Count > 0) SelectedBundle = Bundles[0];
        }

        // ---------- التصدير ----------

        private void ExportBundle()
        {
            if (SelectedBundle == null) return;
            var def = BundleCatalog.Find(SelectedBundle.Id);
            if (def == null) return;

            var suggested = "techtoon_" + def.Id + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "تصدير " + def.Title,
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = suggested
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                StatusMessage = "جارٍ التصدير إلى Excel...";
                var svc = MasterDataBundleService.Create();
                var path = svc.ExportToExcelFile(def, dialog.FileName);
                var size = new FileInfo(path).Length;
                BundleInfo = $"تم التصدير: {Path.GetFileName(path)} ({size / 1024.0:N1} KB)";
                StatusMessage = BundleInfo;
            }
            catch (Exception ex)
            {
                StatusMessage = ex is InvalidOperationException ? ex.Message : "فشل التصدير: " + ex.Message;
            }
        }

        // ---------- الاستيراد ----------

        private void PickAndAnalyze()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "اختر ملف الحزمة (Excel)",
                Filter = "Excel workbook (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls|All files (*.*)|*.*",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                StatusMessage = "جارٍ تحليل ملف Excel...";
                var bundle = MasterDataBundleService.ReadExcelBundle(dialog.FileName);

                var def = BundleCatalog.Find(bundle.BundleId);
                if (def == null)
                {
                    // الملف بلا معرّف مجمّع — نستخدم المجمّع المحدد في الواجهة
                    if (SelectedBundle == null)
                    {
                        StatusMessage = "الملف لا يحتوي على معرّف مجمّع — اختر مجمّعًا ثم أعد المحاولة.";
                        PlanRows.Clear();
                        HasPlan = false;
                        CanImport = false;
                        return;
                    }
                    def = BundleCatalog.Find(SelectedBundle.Id)!;
                }

                var svc = MasterDataBundleService.Create();
                // المعاينة بنفس خيارات الاستيراد — وإلا خالفت ما سيحدث فعلًا
                var plan = svc.Analyze(bundle, new BundleImportOptions
                {
                    ConflictMode = (BundleConflictMode)ConflictModeIndex,
                    CreateMissingReferences = CreateMissingReferences
                });

                PlanRows.Clear();
                foreach (var t in plan.Tables)
                    PlanRows.Add(new BundlePlanRow
                    {
                        Table = t.Table,
                        ToInsert = t.ToInsert,
                        ToUpdate = t.ToUpdate,
                        ToSkip = t.ToSkip,
                        Errors = t.Errors.Count
                    });

                BundleInfo = $"الملف: {Path.GetFileName(dialog.FileName)} — {bundle.Title} — {bundle.TotalRows} صف";
                StatusMessage = plan.TotalErrors > 0
                    ? $"تحليل: {plan.TotalInsert} جديد، {plan.TotalUpdate} تحديث، {plan.TotalSkip} متخطّى — {plan.TotalErrors} خطأ (لن يتم الاستيراد)."
                    : $"تحليل: {plan.TotalInsert} جديد، {plan.TotalUpdate} تحديث، {plan.TotalSkip} متخطّى — جاهز للاستيراد.";

                HasPlan = true;
                CanImport = plan.CanImport;
                _pendingBundle = bundle;
            }
            catch (Exception ex)
            {
                // صيغة .xls القديمة (Excel 97-2003) غير مدعومة — رسالة واضحة بدل خطأ تقني
                var ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                StatusMessage = (ext == ".xls" || ext == ".xlt" || ext == ".csv")
                    ? "هذه الصيغة غير مدعومة. افتح الملف في Excel ثم احفظه بصيغة Excel Workbook (.xlsx) ثم أعد المحاولة."
                    : "تعذّر قراءة الحزمة: " + ex.Message;
                PlanRows.Clear();
                HasPlan = false;
                CanImport = false;
            }
        }

        private DataBundle? _pendingBundle;

        private void ImportBundle()
        {
            if (_pendingBundle == null) { StatusMessage = "اختر ملف الحزمة أولًا."; return; }

            var result = System.Windows.MessageBox.Show(
                "سيتم الاستيراد في معاملة واحدة (كله أو لا شيء).\n\n" +
                "هل تريد المتابعة؟",
                "تأكيد الاستيراد",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                StatusMessage = "جارٍ الاستيراد...";
                var svc = MasterDataBundleService.Create();
                var options = new BundleImportOptions
                {
                    ConflictMode = ConflictModeIndex switch
                    {
                        1 => BundleConflictMode.Update,
                        2 => BundleConflictMode.InsertAlways,
                        _ => BundleConflictMode.Skip
                    },
                    CreateMissingReferences = CreateMissingReferences
                };

                var res = svc.Import(_pendingBundle, options);
                StatusMessage = res.Success ? res.Message : res.Message + " " + string.Join(" | ", res.Errors);

                if (res.Success)
                {
                    HasPlan = false;
                    CanImport = false;
                    _pendingBundle = null;
                    PlanRows.Clear();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex is InvalidOperationException ? ex.Message : "فشل الاستيراد: " + ex.Message;
            }
        }
    }
}
