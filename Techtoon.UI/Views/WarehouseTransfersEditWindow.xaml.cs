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

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var fromId = ((ComboItem)CmbFromWh.SelectedItem)?.Id ?? 0;
            var toId = ((ComboItem)CmbToWh.SelectedItem)?.Id ?? 0;
            var itemId = ((ComboItem)CmbItem.SelectedItem)?.Id ?? 0;
            var notes = TxtNotes.Text;

            if (!decimal.TryParse(TxtQty.Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture, out var qty) || qty <= 0)
            {
                LblStatus.Text = "الكمية غير صحيحة.";
                return;
            }
            if (fromId <= 0 || toId <= 0) { LblStatus.Text = "اختر المخزن المرسل والمستقبل."; return; }
            if (itemId <= 0) { LblStatus.Text = "اختر الصنف."; return; }
            if (fromId == toId) { LblStatus.Text = "لا يمكن التحويل إلى نفس المخزن."; return; }

            var svc = ClosedLoopDetailsService.Create();
            if (_id > 0)
            {
                // تحديث: نستبدل سطور التحويل ونُسجّل حركة في السجل
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                try
                {
                    using (var upd = new NpgsqlCommand(@"UPDATE warehouse_transfers
                        SET from_warehouse_id=@fw, to_warehouse_id=@tw, item_id=@item, quantity=@qty, notes=@notes
                        WHERE id=@id", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@fw", fromId);
                        upd.Parameters.AddWithValue("@tw", toId);
                        upd.Parameters.AddWithValue("@item", itemId);
                        upd.Parameters.AddWithValue("@qty", qty);
                        upd.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@id", _id);
                        upd.ExecuteNonQuery();
                    }
                    using (var del = new NpgsqlCommand("DELETE FROM warehouse_transfer_lines WHERE transfer_id=@id", conn, tx))
                    {
                        del.Parameters.AddWithValue("@id", _id);
                        del.ExecuteNonQuery();
                    }
                    using (var ins = new NpgsqlCommand(@"INSERT INTO warehouse_transfer_lines
                        (transfer_id, item_id, requested_quantity, shipped_quantity, received_quantity)
                        VALUES (@tid, @iid, @rq, @rq, @rq)", conn, tx))
                    {
                        ins.Parameters.AddWithValue("@tid", _id);
                        ins.Parameters.AddWithValue("@iid", itemId);
                        ins.Parameters.AddWithValue("@rq", qty);
                        ins.ExecuteNonQuery();
                    }
                    await ClosedLoopDetailsService.LogTransferAsync(conn, tx, _id, "update", 1, null, null, "تعديل التحويل");
                    tx.Commit();
                }
                catch
                {
                    try { tx.Rollback(); } catch { }
                    throw;
                }
            }
            else
            {
                // إنشاء: التحويل + سطوره + سجل حركاته — في معاملة واحدة
                var lines = new System.Collections.Generic.List<ClosedLoopDetailsService.TransferLine>
                {
                    new ClosedLoopDetailsService.TransferLine
                    {
                        ItemId = itemId,
                        RequestedQuantity = qty,
                        ShippedQuantity = qty,
                        ReceivedQuantity = qty
                    }
                };
                _id = await svc.CreateTransferAsync(fromId, toId, DateTime.Now, lines, notes, "draft");
            }

            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "WarehouseTransfer", _id, "Saved from EditWindow");
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