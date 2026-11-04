# -*- coding: utf-8 -*-
"""
clear_voucher_form.py
يضيف تفريغ البيانات بعد حفظ سند القبض أو الصرف
"""
from pathlib import Path
import py_compile, re, unicodedata

patches = []

# ابحث عن صفحات السندات
for f in Path("pages").glob("*.py"):
    name = unicodedata.normalize("NFC", f.name)
    if "41_" in name or "42_" in name or "سند" in name:
        print(f"Patching: {f.name}")
        src = f.read_text(encoding="utf-8")
        
        # ابحث عن st.rerun() بعد الحفظ وأضف قبله تفريغ البيانات
        # النمط: st.success(...) ثم st.balloons() ثم st.rerun()
        
        old_pattern = r'(st\.success\(f"✅ تم تسجيل سند (القبض|الصرف) بمبلغ.*?\)\n\s*st\.balloons\(\)\n\s*st\.rerun\(\))'
        
        new_code = '''st.success(f"✅ تم تسجيل سند \\1 بمبلغ {amount:,.2f} ج.م")
                st.balloons()
                # تفريغ البيانات بعد الحفظ
                for k in list(st.session_state.keys()):
                    if k.startswith("rcv_") or k.startswith("pay_"):
                        del st.session_state[k]
                st.rerun()'''
        
        src_new = re.sub(old_pattern, new_code, src)
        
        if src_new != src:
            src = src_new
            patches.append(f"Cleared form in {f.name}")
        else:
            # محاولة بديلة: ابحث عن st.rerun() بعد st.balloons()
            lines = src.split("\n")
            for i, line in enumerate(lines):
                if "st.balloons()" in line and i+1 < len(lines) and "st.rerun()" in lines[i+1]:
                    # أضف تفريغ البيانات قبل st.rerun()
                    indent = len(lines[i+1]) - len(lines[i+1].lstrip())
                    insert = " " * indent + "# تفريغ البيانات بعد الحفظ\n"
                    insert += " " * indent + 'for k in list(st.session_state.keys()):\n'
                    insert += " " * (indent + 4) + 'if k.startswith("rcv_") or k.startswith("pay_"):\n'
                    insert += " " * (indent + 8) + 'del st.session_state[k]\n'
                    lines.insert(i+1, insert)
                    patches.append(f"Added clear form before rerun at line {i+1}")
                    break
            src = "\n".join(lines)
        
        f.write_text(src, encoding="utf-8")
        try:
            py_compile.compile(str(f), doraise=True)
            print(f"   OK syntax")
        except Exception as e:
            print(f"   FAIL: {e}")

print("\n" + "=" * 50)
print("Summary:")
for patch in patches:
    print(f"  + {patch}")
print("\nRun: streamlit run app.py")
