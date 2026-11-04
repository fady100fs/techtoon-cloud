# license_ui.py
"""شاشة الترخيص — تظهر قبل فتح التطبيق."""
import json
import threading
import time
from pathlib import Path

import webview


LICENSE_HTML = """
<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
<meta charset="UTF-8">
<title>Techtoon Accounting - الترخيص</title>
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; font-family: 'Segoe UI', Tahoma, sans-serif; }
  html, body {
    height: 100vh; width: 100vw;
    background: linear-gradient(135deg, #0e1117 0%, #1a1f2e 100%);
    color: #fff;
    display: flex; align-items: center; justify-content: center;
    overflow: hidden; user-select: none;
  }
  .card {
    width: 560px; padding: 50px 40px;
    background: rgba(30, 41, 59, 0.6);
    border: 1px solid rgba(16, 185, 129, 0.3);
    border-radius: 24px;
    backdrop-filter: blur(20px);
    box-shadow: 0 30px 80px rgba(0,0,0,0.5);
    text-align: center;
    animation: slideIn 0.5s ease-out;
  }
  @keyframes slideIn {
    from { opacity: 0; transform: translateY(-20px); }
    to { opacity: 1; transform: translateY(0); }
  }
  .logo {
    width: 90px; height: 90px;
    background: linear-gradient(135deg, #10b981, #059669);
    border-radius: 22px;
    display: flex; align-items: center; justify-content: center;
    font-size: 52px;
    margin: 0 auto 25px;
    box-shadow: 0 15px 40px rgba(16, 185, 129, 0.4);
  }
  h1 { font-size: 28px; margin-bottom: 10px; color: #fff; }
  .subtitle { color: #94a3b8; margin-bottom: 30px; font-size: 15px; }
  .status-box {
    padding: 20px 25px;
    background: rgba(16, 185, 129, 0.1);
    border: 1px solid rgba(16, 185, 129, 0.3);
    border-radius: 14px;
    margin-bottom: 25px;
  }
  .status-box.error {
    background: rgba(239, 68, 68, 0.1);
    border-color: rgba(239, 68, 68, 0.4);
  }
  .status-box.warning {
    background: rgba(245, 158, 11, 0.1);
    border-color: rgba(245, 158, 11, 0.4);
  }
  .status-icon { font-size: 42px; margin-bottom: 10px; }
  .status-text { font-size: 17px; font-weight: 600; margin-bottom: 6px; }
  .status-detail { color: #cbd5e1; font-size: 14px; }
  .mid-section { margin-top: 25px; padding-top: 25px; border-top: 1px solid #334155; }
  .mid-label { color: #94a3b8; font-size: 13px; margin-bottom: 10px; }
  .mid-box {
    padding: 14px 18px;
    background: #0f172a;
    border: 1px solid #334155;
    border-radius: 10px;
    font-family: 'Consolas', monospace;
    font-size: 16px;
    letter-spacing: 1px;
    color: #10b981;
    display: flex; align-items: center; justify-content: space-between;
  }
  .copy-btn {
    padding: 6px 14px;
    background: #10b981;
    color: #fff;
    border: none;
    border-radius: 8px;
    font-size: 13px;
    cursor: pointer;
    transition: 0.2s;
  }
  .copy-btn:hover { background: #059669; transform: scale(1.05); }
  .copy-btn:active { transform: scale(0.95); }
  .hint { color: #64748b; font-size: 13px; margin-top: 20px; line-height: 1.7; }
  .progress {
    width: 100%; height: 4px; background: #1e293b;
    border-radius: 4px; margin-top: 25px; overflow: hidden;
  }
  .progress::after {
    content: ""; display: block;
    width: 40%; height: 100%;
    background: linear-gradient(90deg, #10b981, #3b82f6);
    animation: loading 1.5s ease-in-out infinite;
  }
  @keyframes loading {
    0% { transform: translateX(-150%); }
    100% { transform: translateX(350%); }
  }
</style>
</head>
<body>
  <div class="card">
    <div class="logo" id="logo">&#x1F4BC;</div>
    <h1>Techtoon Accounting</h1>
    <div class="subtitle">نظام المحاسبة المتكامل</div>

    <div class="status-box {BOX_CLASS}" id="statusBox">
      <div class="status-icon" id="statusIcon">{ICON}</div>
      <div class="status-text" id="statusText">{TITLE}</div>
      <div class="status-detail" id="statusDetail">{DETAIL}</div>
    </div>

    <div class="mid-section" id="midSection" style="display: {MID_DISPLAY}">
      <div class="mid-label">معرّف الجهاز (Machine ID)</div>
      <div class="mid-box">
        <span id="midValue">{MID}</span>
        <button class="copy-btn" onclick="copyMid()">نسخ</button>
      </div>
      <div class="hint">
        أرسل هذا الرقم للبائع لاستلام مفتاح الترخيص<br>
        ضع ملف <code>license.key</code> بجانب البرنامج ثم أعد التشغيل
      </div>
    </div>

    <div class="progress" id="progressBar" style="display: {PROGRESS_DISPLAY}"></div>
  </div>

<script>
function copyMid() {
  const t = document.getElementById('midValue').innerText;
  navigator.clipboard.writeText(t).then(() => {
    const btn = event.target;
    btn.innerText = 'تم النسخ!';
    btn.style.background = '#059669';
    setTimeout(() => {
      btn.innerText = 'نسخ';
      btn.style.background = '#10b981';
    }, 2000);
  });
}
</script>
</body>
</html>
"""


def show_license_window(status: dict, duration: float = None):
    """
    يعرض شاشة الترخيص.
    - إذا duration محدد: يُغلق تلقائياً بعد هذه المدة (شاشة مؤقتة)
    - إذا None: يبقى حتى يُغلق يدوياً (شاشة خطأ)
    """
    mid = status.get("machine_id", "")
    lic_type = status.get("type", "")
    valid = status.get("valid", False)
    msg = status.get("message", "")

    if valid:
        if lic_type == "lifetime":
            box_class = ""
            icon = "&#x2705;"
            title = "رخصة مدى الحياة"
            detail = f"مسجّلة لـ: {status.get('issued_to') or 'المستخدم'}"
        else:
            box_class = ""
            icon = "&#x23F3;"
            title = "رخصة تجريبية"
            detail = f"{status.get('days_left')} يوم متبقي"
        mid_display = "none"
        progress_display = "block"
    elif lic_type == "expired":
        box_class = "warning"
        icon = "&#x26A0;&#xFE0F;"
        title = "انتهت الفترة التجريبية"
        detail = msg
        mid_display = "block"
        progress_display = "none"
    else:
        box_class = "error"
        icon = "&#x274C;"
        title = "خطأ في الترخيص"
        detail = msg
        mid_display = "block"
        progress_display = "none"

    html = (
        LICENSE_HTML
        .replace("{BOX_CLASS}", box_class)
        .replace("{ICON}", icon)
        .replace("{TITLE}", title)
        .replace("{DETAIL}", detail)
        .replace("{MID}", mid)
        .replace("{MID_DISPLAY}", mid_display)
        .replace("{PROGRESS_DISPLAY}", progress_display)
    )

    window = webview.create_window(
        "Techtoon Accounting",
        html=html,
        width=680,
        height=680,
        resizable=False,
        confirm_close=False,
    )

    if duration is not None:
        def _auto_close():
            time.sleep(duration)
            try:
                window.destroy()
            except Exception:
                pass
        threading.Thread(target=_auto_close, daemon=True).start()

    webview.start()


def wait_and_close(status: dict, seconds: float = 3.0):
    """يعرض شاشة ترخيص ناجحة ويُغلقها تلقائياً."""
    show_license_window(status, duration=seconds)