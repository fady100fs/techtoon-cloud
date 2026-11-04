# -*- coding: utf-8 -*-
from pathlib import Path
from datetime import datetime
import py_compile

patches = []

print("1) Adding monospace + Ctrl+Enter to ui_enhancements...")

p = Path("ui_enhancements.py")
if p.exists():
    src = p.read_text(encoding="utf-8")
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    bak = p.with_suffix(f".py.bak_{ts}")
    bak.write_bytes(p.read_bytes())

    extra_css = """
    /* أرقام بعرض ثابت (monospace) */
    .mono, [data-testid='stDataFrame'] td {
        font-family: 'Consolas', 'Courier New', monospace !important;
        font-variant-numeric: tabular-nums;
    }
    
    /* مؤشر "حفظ تلقائي" */
    .autosave-indicator {
        position: fixed;
        top: 12px;
        right: 20px;
        background: rgba(16, 185, 129, 0.9);
        color: white;
        padding: 4px 10px;
        border-radius: 12px;
        font-size: 11px;
        z-index: 9999;
        opacity: 0;
        transition: opacity 0.3s;
    }
    .autosave-indicator.show {
        opacity: 1;
    }
    
    /* تحسين حقول الإدخال */
    .stTextInput > div > div > input,
    .stNumberInput > div > div > input {
        transition: border-color 0.2s, box-shadow 0.2s;
    }
    .stTextInput > div > div > input:focus,
    .stNumberInput > div > div > input:focus {
        border-color: #10b981 !important;
        box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2) !important;
    }
"""

    if "autosave-indicator" not in src:
        src = src.replace('    </style>', extra_css + '    </style>')
        patches.append("ui_enhancements: monospace + autosave indicator + input focus")

    if "def show_autosave" not in src:
        src += '''

def show_autosave(message="💾 تم الحفظ التلقائي"):
    """عرض مؤشر حفظ تلقائي يختفي بعد 2 ثانية."""
    st.markdown(f"""
    <div class="autosave-indicator" id="autosave-ind">{message}</div>
    <script>
        const el = document.getElementById('autosave-ind');
        if (el) {{
            el.classList.add('show');
            setTimeout(() => el.classList.remove('show'), 2000);
        }}
    </script>
    """, unsafe_allow_html=True)
'''
        patches.append("ui_enhancements: show_autosave function")

    p.write_text(src, encoding="utf-8")
    try:
        py_compile.compile(str(p), doraise=True)
        print("   OK")
    except Exception as e:
        print(f"   FAIL: {e}")
        p.write_bytes(bak.read_bytes())

print("\n2) Adding Ctrl+Enter shortcut...")

p = Path("shortcuts.py")
if p.exists():
    src = p.read_text(encoding="utf-8")
    ts2 = datetime.now().strftime("%Y%m%d_%H%M%S")
    bak2 = p.with_suffix(f".py.bak_{ts2}")
    bak2.write_bytes(p.read_bytes())

    if "Ctrl+Enter" not in src and "ctrlKey && event.key === 'Enter'" not in src:
        idx = src.find("def install_shortcuts")
        if idx >= 0:
            return_idx = src.find("return kb", idx)
            if return_idx >= 0:
                insert_code = '''
    # Ctrl+Enter = حفظ سريع
    components.html("""
    <script>
    document.addEventListener('keydown', function(e) {
        if (e.ctrlKey && e.key === 'Enter') {
            e.preventDefault();
            const btns = document.querySelectorAll('button');
            for (const btn of btns) {
                if (btn.textContent.includes('حفظ') || btn.textContent.includes('💾')) {
                    btn.click();
                    break;
                }
            }
        }
    });
    </script>
    """, height=0, width=0)
    
'''
                src = src[:return_idx] + insert_code + "    " + src[return_idx:]
                patches.append("shortcuts.py: Ctrl+Enter for quick save")

    p.write_text(src, encoding="utf-8")
    try:
        py_compile.compile(str(p), doraise=True)
        print("   OK")
    except Exception as e:
        print(f"   FAIL: {e}")
        p.write_bytes(bak2.read_bytes())

print("\n" + "=" * 50)
print("  Summary:")
for patch in patches:
    print(f"  + {patch}")
print()
print("  New features:")
print("    1) Monospace numbers (better alignment in tables)")
print("    2) Autosave indicator (visual feedback)")
print("    3) Ctrl+Enter = quick save")
print("    4) Input focus glow (green border on focus)")