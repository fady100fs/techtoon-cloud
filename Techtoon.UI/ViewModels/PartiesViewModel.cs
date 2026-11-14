using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Services;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class PartiesViewModel : ViewModelBase
{
    private ObservableCollection<Party> _parties = new();
    private string _searchText = "";
    private Party? _selectedParty;

    public ObservableCollection<Party> Parties { get => _parties; set => SetProperty(ref _parties, value); }
    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) ApplyFilter(); } }
    public Party? SelectedParty { get => _selectedParty; set => SetProperty(ref _selectedParty, value); }

    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public PartiesViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedParty), () => SelectedParty != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedParty != null && AuthService.HasPermission("parties.delete"));
    }

    public async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var list = await db.Parties.Include(p => p.Account).OrderBy(p => p.Name).Take(500).ToListAsync();
            _allParties = new ObservableCollection<Party>(list);
            Parties = new ObservableCollection<Party>(_allParties);
            ApplyFilter();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void ApplyFilter()
    {
        // مسح البحث يجب أن يُعيد القائمة كاملة (كان return مبكرًا يترك القائمة مُصفّحة للأبد)
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            if (_allParties == null) return;
            Parties = new ObservableCollection<Party>(_allParties);
            return;
        }
        var s = SearchText.Trim().ToLower();
        var source = _allParties ?? Parties;
        var filtered = source.Where(p => (p.Name?.ToLower().Contains(s) == true) || (p.Phone?.ToLower().Contains(s) == true)).ToList();
        Parties = new ObservableCollection<Party>(filtered);
    }

    /// <summary>نسخة كاملة من الأطراف — تُستخدم لإلغاء البحث.</summary>
    private ObservableCollection<Party>? _allParties;

    private void OpenEdit(Party? party)
    {
        var window = new PartyEditWindow(party) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedParty == null) return;
        if (!AuthService.HasPermission("parties.delete")) { System.Windows.MessageBox.Show("لا صلاحية لحذف الأطراف — يُرجى مراجعة مدير النظام.", "لا صلاحية", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
        var partyName = SelectedParty.Name ?? "";
        var confirm = System.Windows.MessageBox.Show(
            $"هل تريد حذف العميل «{partyName}»؟", "تأكيد",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            // نتحقق من السجلات المرتبطة قبل الحذف.
            // بدون هذا يفشل الحذف برسالة EF غامضة:
            // «An error occurred while saving the entity changes»
            var refs = await CountReferencesAsync(SelectedParty.Id);
            if (refs.Count > 0)
            {
                System.Windows.MessageBox.Show(
                    $"لا يمكن حذف العميل «{partyName}» لوجود سجلات مرتبطة به:\n\n" +
                    string.Join("\n", refs.Select(r => "• " + r)) +
                    "\n\nاحذف السجلات المرتبطة أولًا، أو ألغِ العملية.",
                    "لا يمكن الحذف", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var p = await db.Parties.FirstOrDefaultAsync(x => x.Id == SelectedParty.Id);
            if (p != null) { db.Parties.Remove(p); await db.SaveChangesAsync(); await LoadAsync(); }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ErrorMessages.Describe(ex, "تعذّر حذف العميل"),
                "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>عدد السجلات المرتبطة بالعميل (لكل نوع).</summary>
    private static async Task<List<string>> CountReferencesAsync(int partyId)
    {
        var result = new List<string>();
        try
        {
            var cs = Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
            await using var db = new Npgsql.NpgsqlConnection(cs);
            await db.OpenAsync();

            async Task<int> Count(string sql)
            {
                await using var cmd = new Npgsql.NpgsqlCommand(sql, db);
                // بدون هذا المعامل يفشل الاستعلام ويُبتلع الاستثناء فتُفقد القائمة
                cmd.Parameters.AddWithValue("@id", partyId);
                var v = await cmd.ExecuteScalarAsync();
                return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
            }

            var invoices = await Count("SELECT COUNT(*) FROM invoices WHERE party_id=@id");
            if (invoices > 0) result.Add($"{invoices} فاتورة");

            var payments = await Count("SELECT COUNT(*) FROM payments WHERE party_id=@id");
            if (payments > 0) result.Add($"{payments} سند قبض/صرف");

            var subs = await Count("SELECT COUNT(*) FROM subscriptions WHERE party_id=@id");
            if (subs > 0) result.Add($"{subs} اشتراك");

            var invoicePayments = await Count(
                "SELECT COUNT(*) FROM invoice_payments ip JOIN invoices i ON i.id=ip.invoice_id WHERE i.party_id=@id");
            if (invoicePayments > 0) result.Add($"{invoicePayments} دفعة فاتورة");

            var badDebts = await Count("SELECT COUNT(*) FROM bad_debt_writeoffs WHERE party_id=@id");
            if (badDebts > 0) result.Add($"{badDebts} عملية شطب دين");
        }
        catch (Exception ex)
        {
            // لا نمنع الحذف بسبب تعذّر الفحص، لكن نُظهر الخطأ عند فشل الحذف نفسه.
            System.Diagnostics.Debug.WriteLine("فشل فحص مراجع العميل: " + ex.Message);
        }
        return result;
    }
}
