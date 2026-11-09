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
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadLogs(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }

        public AuditLogViewModel()
        {
            RefreshCommand = new RelayCommand(LoadLogs);
            LoadLogs();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadLogs()
        {
            Logs.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT id, created_at, COALESCE(user_name,''), action, entity_type, entity_id, COALESCE(details,'') 
                    FROM app_audit_logs WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (action ILIKE @q OR entity_type ILIKE @q OR user_name ILIKE @q OR details ILIKE @q)";
                sql += " ORDER BY created_at DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
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