# navigation.py
"""
نظام التنقل - قائمة جانبية مع مجموعات
"""

import streamlit as st

# تعريف المجموعات والصفحات
NAVIGATION_GROUPS = {
    "تسجيل": {
        "icon": "🔐",
        "pages": [
            {"name": "تسجيل الدخول", "file": "14__تسجيل_الدخول", "icon": "👤"},
            {"name": "إدارة المستخدمين", "file": "21__إدارة_المستخدمين", "icon": "👥"},
        ]
    },
    "تكويد": {
        "icon": "📋",
        "pages": [
            {"name": "الأصناف", "file": "2_📦_الأصناف", "icon": "📦"},
            {"name": "التصنيفات", "file": "20_🗂️_التصنيفات", "icon": "🗂️"},
            {"name": "العملاء والموردين", "file": "3_👥_العملاء_والموردين", "icon": "👥"},
        ]
    },
    "عمليات": {
        "icon": "⚙️",
        "pages": [
            {"name": "الفواتير", "file": "4_🧾_الفواتير", "icon": "🧾"},
            {"name": "فهرس الفواتير", "file": "6_📋_فهرس_الفواتير", "icon": "📋"},
            {"name": "المدفوعات", "file": "8_💰_المدفوعات", "icon": "💰"},
            {"name": "المصروفات", "file": "22_💸_المصروفات", "icon": "💸"},
            {"name": "الخزائن", "file": "19_🏦_الخزائن", "icon": "🏦"},
        ]
    },
    "مخزون": {
        "icon": "📦",
        "pages": [
            {"name": "إدارة المخزون", "file": "23_📦_إدارة_المخزون", "icon": ""},
            {"name": "الأصول الثابتة", "file": "24__الأصول_الثابتة", "icon": "🏭"},
        ]
    },
    "مالية": {
        "icon": "💼",
        "pages": [
            {"name": "القروض", "file": "26_💼_إدارة_القروض", "icon": "💼"},
            {"name": "الموظفين والرواتب", "file": "27_👥_الموظفين_والرواتب", "icon": "👥"},
            {"name": "الإغلاق المحاسبي", "file": "29_📅_الإغلاق_المحاسبي", "icon": "📅"},
            {"name": "مراكز التكلفة", "file": "30_🏢_مراكز_التكلفة", "icon": "🏢"},
        ]
    },
    "تقارير": {
        "icon": "📊",
        "pages": [
            {"name": "لوحة التحكم", "file": "1__لوحة_التحكم", "icon": "🏠"},
            {"name": "التقارير", "file": "5_📊_التقارير", "icon": "📊"},
            {"name": "تقارير متقدمة", "file": "15_📈_تقارير_متقدمة", "icon": ""},
            {"name": "تحليل الربحية", "file": "25_📈_تحليل_الربحية", "icon": "📈"},
            {"name": "تقرير الأرباح", "file": "18_💰_تقرير_الارباح_الدقيق", "icon": "💰"},
        ]
    },
    "نظام": {
        "icon": "⚙️",
        "pages": [
            {"name": "النسخ الاحتياطي", "file": "11_💾_النسخ_الاحتياطي", "icon": "💾"},
        ]
    }
}


def show_sidebar_navigation():
    """عرض التنقل في الشريط الجانبي"""
    
    st.sidebar.markdown("---")
    st.sidebar.markdown("### 📑 القائمة الرئيسية")
    st.sidebar.markdown("---")
    
    for group_name, group_data in NAVIGATION_GROUPS.items():
        # إنشاء expander لكل مجموعة
        with st.sidebar.expander(f"{group_data['icon']} {group_name}", expanded=False):
            for page in group_data["pages"]:
                # زر لكل صفحة
                if st.sidebar.button(
                    f"{page['icon']} {page['name']}",
                    key=f"nav_{page['file']}",
                    use_container_width=True
                ):
                    # الانتقال للصفحة
                    st.switch_page(f"pages/{page['file']}.py")