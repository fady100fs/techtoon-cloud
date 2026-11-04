using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.Core.Models;

namespace Techtoon.UI.Views
{
    public class CategoryOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public partial class ItemEditWindow : Window
    {
        private readonly Item? _item;
        private int _itemId = 0;

        public static string GetCS()
        {
            return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
        }

        public ItemEditWindow(Item? item = null)
        {
            InitializeComponent();
            _item = item;
            if (item != null) _itemId = item.Id;
            SetArabicTexts();
        }

        private void SetArabicTexts()
        {
            try { Title = _item != null ? "\u062A\u0639\u062F\u064A\u0644 \u0635\u0646\u0641" : "\u0635\u0646\u0641 \u062C\u062F\u064A\u062F"; } catch { }
            try { LblTitle.Text = _item != null ? "\u062A\u0639\u062F\u064A\u0644 \u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u0635\u0646\u0641" : "\u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641 \u062C\u062F\u064A\u062F"; } catch { }
            try { LblName.Text = "\u0627\u0644\u0627\u0633\u0645:"; } catch { }
            try { LblBarcode.Text = "\u0628\u0627\u0631\u0643\u0648\u062F:"; } catch { }
            try { LblSellPrice.Text = "\u0633\u0639\u0631 \u0627\u0644\u0628\u064A\u0639:"; } catch { }
            try { LblCostPrice.Text = "\u0627\u0644\u062A\u0643\u0644\u0641\u0629:"; } catch { }
            try { LblMinStock.Text = "\u0627\u0644\u062D\u062F \u0627\u0644\u0623\u062F\u0646\u0649:"; } catch { }
            try { LblOpeningQty.Text = "\u0627\u0644\u0631\u0635\u064A\u062F \u0627\u0644\u0627\u0641\u062A\u062A\u0627\u062D\u064A:"; } catch { }
            try { LblCategory.Text = "\u0627\u0644\u062A\u0635\u0646\u064A\u0641:"; } catch { }
            try { ChkActive.Content = "\u0646\u0634\u0637"; } catch { }
            try { BtnSave.Content = "\uD83D\uDCBE \u062D\u0641\u0638"; } catch { }
            try { BtnCancel.Content = "\u2716 \u0625\u0644\u063A\u0627\u0621"; } catch { }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            if (_item != null && _item.Id > 0) LoadItemData(_item.Id);
        }

        private void LoadCategories()
        {
            try
            {
                CmbCategory.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM categories ORDER BY name", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            CmbCategory.Items.Add(new CategoryOption { Id = r.GetInt32(0), Name = r.GetString(1) });
                }
            }
            catch { }
        }

        private void LoadItemData(int itemId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT name, barcode, sell_price, cost_price, min_stock, opening_quantity, category_id, is_active FROM items WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", itemId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                TxtName.Text = r.IsDBNull(0) ? "" : r.GetString(0);
                                TxtBarcode.Text = r.IsDBNull(1) ? "" : r.GetString(1);
                                TxtSellPrice.Text = (r.IsDBNull(2) ? 0 : r.GetDouble(2)).ToString("N2");
                                TxtCostPrice.Text = (r.IsDBNull(3) ? 0 : r.GetDouble(3)).ToString("N2");
                                TxtMinStock.Text = (r.IsDBNull(4) ? 0 : r.GetDouble(4)).ToString("N2");
                                TxtOpeningQty.Text = (r.IsDBNull(5) ? 0 : r.GetDouble(5)).ToString("N2");
                                int catId = r.IsDBNull(6) ? 0 : r.GetInt32(6);
                                if (catId > 0)
                                    foreach (CategoryOption c in CmbCategory.Items)
                                        if (c.Id == catId) { CmbCategory.SelectedItem = c; break; }
                                ChkActive.IsChecked = r.IsDBNull(7) ? true : r.GetBoolean(7);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtName.Text))
                { LblStatus.Text = "\u0627\u062F\u062E\u0644 \u0627\u0644\u0627\u0633\u0645"; return; }

                double sellPrice, costPrice, minStock, openingQty;
                double.TryParse(TxtSellPrice.Text, out sellPrice);
                double.TryParse(TxtCostPrice.Text, out costPrice);
                double.TryParse(TxtMinStock.Text, out minStock);
                double.TryParse(TxtOpeningQty.Text, out openingQty);

                int? catId = null;
                if (CmbCategory.SelectedItem is CategoryOption cat) catId = cat.Id;

                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    if (_itemId > 0)
                    {
                        // تعديل موجود
                        using (var cmd = new NpgsqlCommand(
                            @"UPDATE items SET name=@name, barcode=@bc, sell_price=@sp, cost_price=@cp,
                            min_stock=@ms, opening_quantity=@oq, category_id=@ci, is_active=@ia WHERE id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("id", _itemId);
                            cmd.Parameters.AddWithValue("name", TxtName.Text.Trim());
                            cmd.Parameters.AddWithValue("bc", (object)TxtBarcode.Text ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("sp", sellPrice);
                            cmd.Parameters.AddWithValue("cp", costPrice);
                            cmd.Parameters.AddWithValue("ms", minStock);
                            cmd.Parameters.AddWithValue("oq", openingQty);
                            cmd.Parameters.AddWithValue("ci", (object)catId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("ia", ChkActive.IsChecked ?? true);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // إضافة جديد
                        using (var cmd = new NpgsqlCommand(
                            @"INSERT INTO items (name, barcode, sell_price, cost_price, min_stock, opening_quantity, category_id, is_active, is_kit)
                            VALUES (@name, @bc, @sp, @cp, @ms, @oq, @ci, @ia, FALSE) RETURNING id", conn))
                        {
                            cmd.Parameters.AddWithValue("name", TxtName.Text.Trim());
                            cmd.Parameters.AddWithValue("bc", (object)TxtBarcode.Text ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("sp", sellPrice);
                            cmd.Parameters.AddWithValue("cp", costPrice);
                            cmd.Parameters.AddWithValue("ms", minStock);
                            cmd.Parameters.AddWithValue("oq", openingQty);
                            cmd.Parameters.AddWithValue("ci", (object)catId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("ia", ChkActive.IsChecked ?? true);
                            var o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value) _itemId = Convert.ToInt32(o);
                        }
                    }
                }
                LblStatus.Text = "\u062A\u0645 \u0627\u0644\u062D\u0641\u0638 \u0628\u0646\u062C\u0627\u062D";
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
