using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class WarehouseTransfersEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public WarehouseTransfersEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u062A\u062D\u0648\u064A\u0644 \u0645\u062E\u0632\u0648\u0646" : "\u0625\u0636\u0627\u0641\u0629 \u062A\u062D\u0648\u064A\u0644 \u0645\u062E\u0632\u0648\u0646"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try {
            CmbFromWh.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, name FROM warehouses WHERE COALESCE(is_active, TRUE) = TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbFromWh.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        } catch { }
        try {
            CmbToWh.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, name FROM warehouses WHERE COALESCE(is_active, TRUE) = TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbToWh.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        } catch { }
        try {
            CmbItem.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, name FROM items WHERE COALESCE(is_kit, FALSE) = FALSE AND COALESCE(is_active, TRUE) = TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbItem.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        } catch { }
        if (_id > 0) LoadData();
    }

    private void LoadData()
    {
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(@"SELECT from_warehouse_id, to_warehouse_id, item_id, quantity, COALESCE(notes,'') FROM warehouse_transfers WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0) && CmbFromWh.Items.Count > 0)
                    for (int j = 0; j < CmbFromWh.Items.Count; j++)
                        if (((ComboItem)CmbFromWh.Items[j]).Id == r.GetInt32(0)) { CmbFromWh.SelectedIndex = j; break; }
                if (!r.IsDBNull(1) && CmbToWh.Items.Count > 0)
                    for (int j = 0; j < CmbToWh.Items.Count; j++)
                        if (((ComboItem)CmbToWh.Items[j]).Id == r.GetInt32(1)) { CmbToWh.SelectedIndex = j; break; }
                if (!r.IsDBNull(2) && CmbItem.Items.Count > 0)
                    for (int j = 0; j < CmbItem.Items.Count; j++)
                        if (((ComboItem)CmbItem.Items[j]).Id == r.GetInt32(2)) { CmbItem.SelectedIndex = j; break; }
                if (!r.IsDBNull(3)) TxtQty.Text = r.GetValue(3).ToString();
                if (!r.IsDBNull(4)) TxtNotes.Text = r.GetValue(4).ToString();
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = ((ComboItem)CmbFromWh.SelectedItem)?.Id ?? 0;
            var p1 = ((ComboItem)CmbToWh.SelectedItem)?.Id ?? 0;
            var p2 = ((ComboItem)CmbItem.SelectedItem)?.Id ?? 0;
            var p3 = TxtQty.Text;
            var p4 = TxtNotes.Text;
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE warehouse_transfers SET from_warehouse_id=@p0, to_warehouse_id=@p1, item_id=@p2, quantity=@p3, notes=@p4 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            double _d3;
            if (!double.TryParse(p3, out _d3)) _d3 = 0;
            cmd.Parameters.AddWithValue("p3", _d3);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO warehouse_transfers (date, from_warehouse_id, to_warehouse_id, item_id, quantity, notes, status, created_by) VALUES (NOW(), @p0, @p1, @p2, @p3, @p4, 'draft', 1)", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            double _d3;
            if (!double.TryParse(p3, out _d3)) _d3 = 0;
            cmd.Parameters.AddWithValue("p3", _d3);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "WarehoueTranfer", _id > 0 ? _id : 0, "Saved from EditWindow");
            DialogResult = true;
            Close();
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_id == 0) return;
        
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            
            var items = new System.Collections.Generic.List<DependencyItem>();
            
            
            if (items.Count > 0)
            {
                var win = new DependenciesWindow(items);
                if (win.ShowDialog() == true && win.SelectedItem != null)
                {
                    var type = Type.GetType($"Techtoon.UI.Views.{win.SelectedItem.TargetWindow}, Techtoon.UI");
                    if (type != null) ((System.Windows.Window)Activator.CreateInstance(type, win.SelectedItem.TargetId)).ShowDialog();
                }
                return;
            }
            
            if (System.Windows.MessageBox.Show("\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u062D\u0630\u0641\u061F", "\u062A\u0623\u0643\u064A\u062F", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            
            using var cmd = new NpgsqlCommand("DELETE FROM warehouse_transfers WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "WarehoueTranfer", _id > 0 ? _id : 0, "Saved from EditWindow");
            DialogResult = true;
            Close();
        }
        catch (Npgsql.PostgresException px)
        {
            System.Windows.MessageBox.Show("\u062E\u0637\u0623 \u0641\u064A \u0642\u0627\u0639\u062F\u0629 \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A: " + px.Message, "\u062E\u0637\u0623: ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("\u062E\u0637\u0623: " + ex.Message, "\u062E\u0637\u0623: ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}