import streamlit as st
import database as db
import pandas as pd

def show():
    st.title("📦 أرصدة المخزون")
    search = st.text_input("🔍 بحث", placeholder="اسم الصنف أو الباركود")
    query = "SELECT i.id, i.name, i.barcode, w.name, COALESCE(i.opening_quantity,0) + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id),0), COALESCE(sl.reserved_quantity,0), COALESCE(sl.damaged_quantity,0), COALESCE(sl.under_inspection_quantity,0), COALESCE(i.avg_cost_price, i.cost_price, 0), COALESCE(i.min_stock,0) FROM items i CROSS JOIN warehouses w LEFT JOIN stock_levels sl ON sl.item_id=i.id AND sl.warehouse_id=w.id WHERE i.is_active = TRUE AND i.is_kit IS NOT TRUE AND w.is_active = TRUE"
    params = []
    if search:
        query += " AND (i.name ILIKE %s OR i.barcode ILIKE %s)"
        params.extend([f"%{search}%", f"%{search}%"])
    query += " ORDER BY i.name"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            data = []
            for r in rows:
                total = float(r[4]); res = float(r[5]); dmg = float(r[6]); ins = float(r[7]); cost = float(r[8]); min_s = float(r[9])
                data.append({"الصنف": r[1], "الباركود": r[2], "المخزن": r[3], "الإجمالي": total, "المتاح": total-res-dmg-ins, "محجوز": res, "تالف": dmg, "تحت فحص": ins, "التكلفة": cost, "قيمة المخزون": total*cost, "الحد الأدنى": min_s, "الحالة": "⚠️ منخفض" if total<=min_s else "✅ طبيعي"})
            df = pd.DataFrame(data)
            st.dataframe(df, use_container_width=True, hide_index=True)
            st.metric("إجمالي قيمة المخزون", f"{df['قيمة المخزون'].sum():,.2f}")
        else: st.info("لا توجد بيانات")
    except Exception as e: st.error(f"خطأ: {e}")