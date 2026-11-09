using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class AuditLogRow : ViewModelBase
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string Details { get; set; }
        
        public string ActionDisplay => Action switch
        {
            "CREATE" => "➕ إضافة",
            "UPDATE" => "✏️ تعديل",
            "DELETE" => "🗑️ حذف",
            "LOGIN" => "🔓 تسجيل دخول",
            "LOGOUT" => "🔒 تسجيل خروج",
            _ => Action
        };
    }

    public class AuditLogViewModel : ViewModelBase
    {
        public ObservableCollection<AuditLogRow> Logs { get; } = new();

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); _ = LoadLogsAsync(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }

        // P0-4 Batch 10: pagination contract مع API
        private int _page = 1;
        public int Page { get => _page; set { if (SetProperty(ref _page, System.Math.Max(1, value))) { OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(PageInfo)); } } }
        private int _pageSize = 50;
        public int PageSize { get => _pageSize; set { if (SetProperty(ref _pageSize, System.Math.Max(1, value))) { OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(PageInfo)); } } }
        private int _totalCount;
        public int TotalCount { get => _totalCount; set { if (SetProperty(ref _totalCount, value)) { OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(PageInfo)); } } }
        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public string PageInfo => TotalPages > 0 ? $"صفحة {Page} من {TotalPages} ({TotalCount} سجل)" : $"صفحة {Page} (0 سجل)";

        public AuditLogViewModel()
        {
            RefreshCommand = new RelayCommand(() => _ = LoadLogsAsync());
            NextPageCommand = new RelayCommand(() => { if (TotalPages == 0 || Page < TotalPages) { Page++; _ = LoadLogsAsync(); } });
            PrevPageCommand = new RelayCommand(() => { if (Page > 1) { Page--; _ = LoadLogsAsync(); } });
            _ = LoadLogsAsync();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public async System.Threading.Tasks.Task LoadLogsAsync()
        {
            Logs.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                await conn.OpenAsync();
                string where = !string.IsNullOrWhiteSpace(SearchText) ? " AND (action ILIKE @q OR entity_type ILIKE @q OR user_name ILIKE @q OR details ILIKE @q)" : "";
                using (var cc = new NpgsqlCommand("SELECT COUNT(*) FROM app_audit_logs WHERE 1=1" + where, conn))
                {
                    if (!string.IsNullOrWhiteSpace(SearchText)) cc.Parameters.AddWithValue("q", "%" + SearchText + "%");
                    TotalCount = Convert.ToInt32(await cc.ExecuteScalarAsync());
                }
                if (TotalPages > 0 && Page > TotalPages) Page = TotalPages;
                string sql = @"SELECT id, created_at, COALESCE(user_name,''), action, entity_type, entity_id, COALESCE(details,'') 
                    FROM app_audit_logs WHERE 1=1" + where + " ORDER BY created_at DESC LIMIT @limit OFFSET @offset";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                cmd.Parameters.AddWithValue("limit", PageSize);
                cmd.Parameters.AddWithValue("offset", (Page - 1) * PageSize);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    Logs.Add(new AuditLogRow
                    {
                        Id = r.GetInt32(0),
                        CreatedAt = r.GetDateTime(1),
                        UserName = r.IsDBNull(2) ? "" : r.GetString(2),
                        Action = r.IsDBNull(3) ? "" : r.GetString(3),
                        EntityType = r.IsDBNull(4) ? "" : r.GetString(4),
                        EntityId = r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                        Details = r.IsDBNull(6) ? "" : r.GetString(6)
                    });
                StatusMessage = "Loaded " + Logs.Count;
            }
            catch (Exception ex) { StatusMessage = "Err: " + ex.Message; }
        }
    }
}