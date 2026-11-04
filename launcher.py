# launcher.py - Desktop wrapper for Techtoon Accounting
import atexit
import ctypes
import os
import socket
import subprocess
import sys
import time
from ctypes import wintypes
from pathlib import Path

import webview

ROOT = Path(__file__).resolve().parent
TITLE = "Techtoon Accounting"
NO_WINDOW = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0

# â•â•â• Single Instance Lock â•â•â•
_mutex_handle = None


def _ensure_single_instance():
    global _mutex_handle
    if os.name != "nt":
        return True
    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    _mutex_handle = kernel32.CreateMutexW(None, False, "Local\\Techtoon_Mutex_v2")
    return ctypes.get_last_error() != 183


def _focus_existing():
    if os.name != "nt":
        return
    try:
        user32 = ctypes.WinDLL("user32", use_last_error=True)
        hwnd = user32.FindWindowW(None, TITLE)
        if hwnd:
            user32.ShowWindow(hwnd, 9)
            user32.SetForegroundWindow(hwnd)
    except Exception:
        pass


def _set_taskbar_icon():
    """Windows: Ø§Ø¬Ø¹Ù„ Ø£ÙŠÙ‚ÙˆÙ†Ø© Ø§Ù„ØªØ·Ø¨ÙŠÙ‚ logo.ico ÙÙŠ Ø´Ø±ÙŠØ· Ø§Ù„Ù…Ù‡Ø§Ù…."""
    if os.name != "nt":
        return
    try:
        # AppUserModelID Ù„ØªØ¬Ù…ÙŠØ¹ Ø§Ù„Ù†ÙˆØ§ÙØ° ØªØ­Øª Ø£ÙŠÙ‚ÙˆÙ†Ø© ÙˆØ§Ø­Ø¯Ø©
        ctypes.windll.shell32.SetCurrentProcessExplicitAppUserModelID(
            "Techtoon.Accounting.Desktop.1"
        )
    except Exception:
        pass


# â•â•â• Port + Streamlit â•â•â•
def _find_free_port():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.bind(("", 0))
        return s.getsockname()[1]


def _port_open(port):
    with socket.socket() as s:
        s.settimeout(0.5)
        return s.connect_ex(("127.0.0.1", port)) == 0


PORT = 8501
URL = f"http://localhost:{PORT}"

# â•â•â• Ø´Ø§Ø´Ø© ØªØ­Ù…ÙŠÙ„ Ø£Ù†ÙŠÙ‚Ø© â•â•â•
LOADING_HTML = '''
<!DOCTYPE html>
<html dir="rtl">
<head>
<meta charset="UTF-8">
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  html, body {
    height: 100vh; width: 100vw;
    background: linear-gradient(135deg, #0e1117 0%, #1a1f2e 100%);
    font-family: 'Segoe UI', Tahoma, sans-serif;
    display: flex; align-items: center; justify-content: center;
    overflow: hidden;
    user-select: none;
  }
  .container {
    text-align: center;
    animation: fadeIn 0.8s ease-out;
  }
  @keyframes fadeIn {
    from { opacity: 0; transform: translateY(20px); }
    to   { opacity: 1; transform: translateY(0); }
  }
  .logo {
    width: 110px; height: 110px;
    background: linear-gradient(135deg, #10b981, #059669);
    border-radius: 24px;
    display: flex; align-items: center; justify-content: center;
    font-size: 64px;
    margin: 0 auto 30px;
    box-shadow: 0 20px 60px rgba(16, 185, 129, 0.35);
    animation: pulse 2.5s ease-in-out infinite;
  }
  @keyframes pulse {
    0%, 100% { transform: scale(1); box-shadow: 0 20px 60px rgba(16,185,129,0.35); }
    50%      { transform: scale(1.05); box-shadow: 0 25px 80px rgba(16,185,129,0.55); }
  }
  h1 {
    color: #ffffff;
    font-size: 32px;
    font-weight: 700;
    margin-bottom: 12px;
    letter-spacing: -0.5px;
  }
  .subtitle {
    color: #94a3b8;
    font-size: 16px;
    margin-bottom: 40px;
  }
  .spinner {
    width: 220px;
    height: 4px;
    background: #1e293b;
    border-radius: 4px;
    margin: 0 auto;
    overflow: hidden;
    position: relative;
  }
  .spinner::after {
    content: "";
    position: absolute;
    left: 0; top: 0; bottom: 0;
    width: 40%;
    background: linear-gradient(90deg, #10b981, #3b82f6);
    border-radius: 4px;
    animation: slide 1.6s ease-in-out infinite;
  }
  @keyframes slide {
    0%   { left: -40%; }
    100% { left: 100%; }
  }
  .status {
    color: #64748b;
    font-size: 13px;
    margin-top: 24px;
  }
  .dots::after {
    content: "";
    animation: dots 1.5s steps(4, end) infinite;
  }
  @keyframes dots {
    0%, 20%  { content: ""; }
    40%      { content: "."; }
    60%      { content: ".."; }
    80%, 100%{ content: "..."; }
  }
</style>
</head>
<body>
  <div class="container">
    <div class="logo">&#x1F4BC;</div>
    <h1>Techtoon Accounting</h1>
    <div class="subtitle">&#x0646;&#x0638;&#x0627;&#x0645; &#x0627;&#x0644;&#x0645;&#x062D;&#x0627;&#x0633;&#x0628;&#x0629; &#x0627;&#x0644;&#x0645;&#x062A;&#x0643;&#x0627;&#x0645;&#x0644;</div>
    <div class="spinner"></div>
    <div class="status">&#x062C;&#x0627;&#x0631;&#x064A; &#x0627;&#x0644;&#x062A;&#x062D;&#x0645;&#x064A;&#x0644;<span class="dots"></span></div>
  </div>
</body>
</html>
'''

ERROR_HTML = '''
<!DOCTYPE html>
<html dir="rtl">
<head><meta charset="UTF-8"><style>
body { background: #0e1117; color: #fff; font-family: Tahoma; padding: 60px; text-align: center; }
h1 { color: #ef4444; font-size: 28px; margin-bottom: 20px; }
.box { display: inline-block; background: #1e293b; padding: 24px 36px; border-radius: 12px; text-align: right; line-height: 2.2; margin: 20px auto; }
.hint { color: #94a3b8; margin-top: 30px; font-size: 14px; }
</style></head>
<body>
<h1>&#x26A0; &#x062A;&#x0639;&#x0630;&#x0631; &#x062A;&#x0634;&#x063A;&#x064A;&#x0644; &#x0627;&#x0644;&#x062E;&#x0627;&#x062F;&#x0645;</h1>
<div class="box">
  &#x2022; .env / DATABASE_URL<br>
  &#x2022; &#x0627;&#x0644;&#x0627;&#x062A;&#x0635;&#x0627;&#x0644; &#x0628;&#x0627;&#x0644;&#x0625;&#x0646;&#x062A;&#x0631;&#x0646;&#x062A;<br>
  &#x2022; Python + packages<br>
  &#x2022; streamlit.log
</div>
<p class="hint">&#x0623;&#x063A;&#x0644;&#x0642; &#x0627;&#x0644;&#x0646;&#x0627;&#x0641;&#x0630;&#x0629; &#x0648;&#x062D;&#x0627;&#x0648;&#x0644; &#x0645;&#x0631;&#x0629; &#x0623;&#x062E;&#x0631;&#x0649;.</p>
</body>
</html>
'''

server = None


def start_server():
    log = open(ROOT / "streamlit.log", "ab")
    env = {**os.environ, "PYTHONUTF8": "1", "PYTHONIOENCODING": "utf-8"}
    return subprocess.Popen(
        [
            sys.executable, "-m", "streamlit", "run", str(ROOT / "app.py"),
            "--server.port", str(PORT),
            "--server.address", "127.0.0.1",
            "--server.headless", "true",
            "--browser.gatherUsageStats", "false",
        ],
        cwd=ROOT, env=env,
        stdin=subprocess.DEVNULL, stdout=log, stderr=log,
        creationflags=NO_WINDOW,
    )


def wait_ready(timeout=90):
    end = time.time() + timeout
    while time.time() < end:
        if _port_open(PORT):
            return True
        if server and server.poll() is not None:
            return False
        time.sleep(0.5)
    return False


def stop_server():
    if server and server.poll() is None:
        if os.name == "nt":
            subprocess.run(
                ["taskkill", "/F", "/T", "/PID", str(server.pid)],
                creationflags=NO_WINDOW, capture_output=True,
            )
        else:
            server.terminate()


def boot(window):
    global server, PORT, URL
    if _port_open(PORT):
        PORT = _find_free_port()
        URL = f"http://localhost:{PORT}"
    server = start_server()
    if wait_ready():
        window.load_url(URL)
    else:
        window.load_html(ERROR_HTML)




# ═══════════════════════════════════════════════════
#  فحص الترخيص قبل فتح التطبيق
# ═══════════════════════════════════════════════════
def _check_license():
    """يتحقق من الترخيص. يرجع True إذا كان صالحاً."""
    try:
        from license_manager import get_license_status
        from license_ui import show_license_window

        status = get_license_status()

        if status.get("valid"):
            return True

        # الترخيص غير صالح — اعرض شاشة التنشيط
        show_license_window(status)
        return False
    except Exception as e:
        # في حالة الخطأ، اسمح بالدخول (تجنباً لمنع المستخدم من التطبيق)
        return True

def main():
    if not _ensure_single_instance():
        _focus_existing()
        sys.exit(0)

    _set_taskbar_icon()
    atexit.register(stop_server)

    try:
        webview.settings["ALLOW_DOWNLOADS"] = True
    except Exception:
        pass

    window = webview.create_window(
        TITLE, html=LOADING_HTML,
        width=1400, height=850, min_size=(1000, 650),
        background_color="#0e1117",
    )
    webview.start(
        boot, window,
        private_mode=False,
        storage_path=str(ROOT / ".webview_data"),
        debug=False,
    )
    stop_server()


if __name__ == "__main__":
    main()