# global_search.py
import streamlit as st
import streamlit.components.v1 as components


def render_global_search():
    """بحث موسّع Ctrl+K — يبحث في كل البرنامج بدون ثقل."""
    
    # HTML + CSS + JS للبحث
    components.html("""
    <style>
        #global-search-overlay {
            display: none;
            position: fixed;
            top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0,0,0,0.6);
            z-index: 99999;
            backdrop-filter: blur(4px);
        }
        #global-search-overlay.active { display: flex; align-items: flex-start; justify-content: center; padding-top: 80px; }
        
        #global-search-box {
            width: 90%; max-width: 600px;
            background: #1a1f2e;
            border: 1px solid rgba(16, 185, 129, 0.4);
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 20px 60px rgba(0,0,0,0.5);
        }
        
        #global-search-input {
            width: 100%;
            padding: 16px 20px;
            background: transparent;
            border: none;
            color: white;
            font-size: 16px;
            outline: none;
            font-family: 'Segoe UI', Tahoma, sans-serif;
        }
        
        #global-search-results {
            max-height: 400px;
            overflow-y: auto;
            border-top: 1px solid rgba(255,255,255,0.1);
        }
        
        .gs-result {
            padding: 10px 20px;
            color: #cbd5e1;
            cursor: pointer;
            border-bottom: 1px solid rgba(255,255,255,0.05);
            display: flex;
            align-items: center;
            gap: 10px;
            font-size: 13px;
        }
        .gs-result:hover { background: rgba(16, 185, 129, 0.1); }
        .gs-icon { font-size: 18px; }
        .gs-type { color: #64748b; font-size: 11px; margin-right: auto; }
        
        .gs-shortcut {
            position: fixed;
            bottom: 10px; right: 10px;
            background: rgba(0,0,0,0.7);
            color: white;
            padding: 4px 8px;
            border-radius: 4px;
            font-size: 11px;
            z-index: 9999;
        }
    </style>
    
    <div id="global-search-overlay">
        <div id="global-search-box">
            <input id="global-search-input" placeholder="🔍 ابحث في كل البرنامج... (Esc للإغلاق)" />
            <div id="global-search-results"></div>
        </div>
    </div>
    
    <div class="gs-shortcut">Ctrl+K للبحث</div>
    
    <script>
        const overlay = document.getElementById('global-search-overlay');
        const input = document.getElementById('global-search-input');
        const results = document.getElementById('global-search-results');
        
        // فتح بـ Ctrl+K
        document.addEventListener('keydown', function(e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                overlay.classList.add('active');
                setTimeout(() => input.focus(), 100);
            }
            if (e.key === 'Escape') {
                overlay.classList.remove('active');
            }
        });
        
        // إغلاق عند الضغط خارج البحث
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) overlay.classList.remove('active');
        });
        
        // البحث — يعرض روابط سريعة (لا استعلام DB — كل شيء في المتصفح)
        const pages = [
            { icon: '🛒', name: 'فواتير البيع', url: '/pages/4A_🛒_فواتير_البيع.py', type: 'عمليات' },
            { icon: '🛍️', name: 'فواتير الشراء', url: '/pages/4B_🛍️_فواتير_الشراء.py', type: 'عمليات' },
            { icon: '💰', name: 'المدفوعات', url: '/pages/8_💰_المدفوعات.py', type: 'مالية' },
            { icon: '📦', name: 'الأصناف', url: '/pages/2_📦_الاصناف.py', type: 'تكويد' },
            { icon: '👥', name: 'العملاء والموردين', url: '/pages/3_👥_عملاء وموردين.py', type: 'تكويد' },
            { icon: '🏠', name: 'لوحة التحكم', url: '/pages/1_🏠_لوحة_التحكم.py', type: 'رئيسية' },
            { icon: '📊', name: 'التقارير', url: '/pages/5_📊_التقارير.py', type: 'تقارير' },
            { icon: '📝', name: 'قيود اليومية', url: '/pages/32_📝_قيود_اليومية.py', type: 'محاسبة' },
            { icon: '📚', name: 'شجرة الحسابات', url: '/pages/9__شجرة_الحسابات.py', type: 'محاسبة' },
            { icon: '⚙️', name: 'الإعدادات', url: '/pages/36_⚙️_الإعدادات.py', type: 'نظام' },
        ];
        
        input.addEventListener('input', function() {
            const q = this.value.trim().toLowerCase();
            if (!q) {
                results.innerHTML = '<div class="gs-result"><span class="gs-icon">💡</span> اكتب للبحث...</div>';
                return;
            }
            
            const filtered = pages.filter(p => p.name.toLowerCase().includes(q) || p.type.includes(q));
            
            if (filtered.length === 0) {
                results.innerHTML = '<div class="gs-result"><span class="gs-icon">🔍</span> لا توجد نتائج</div>';
                return;
            }
            
            results.innerHTML = filtered.map(p => 
                '<div class="gs-result" onclick="window.parent.location.href=\'' + p.url + '\'">' +
                '<span class="gs-icon">' + p.icon + '</span>' +
                '<span>' + p.name + '</span>' +
                '<span class="gs-type">' + p.type + '</span>' +
                '</div>'
            ).join('');
        });
    </script>
    """, height=0)
