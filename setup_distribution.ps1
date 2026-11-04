$ErrorActionPreference = 'Stop'

# ============================================================
# Techtoon Accounting - Distribution Setup Script
# Creates license system, tools, and build files.
# Does NOT touch BUILD.md, database, or secrets.
# ============================================================

$Root = 'D:\PROG\Accounting Prog\Techtoon Accounting'
if (-not (Test-Path -LiteralPath $Root)) {
    $Root = (Get-Location).Path
}
Set-Location -LiteralPath $Root

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Techtoon Accounting - Distribution Setup" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Project root: $Root" -ForegroundColor Gray
Write-Host ""

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Write-FileUtf8NoBom {
    param([string]$Path, [string]$Content)
    $dir = Split-Path $Path -Parent
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
    Write-Host "  [OK] $Path" -ForegroundColor Green
}

function Backup-FileIfExists {
    param([string]$Path)
    if (Test-Path -LiteralPath $Path) {
        $bak = "$Path.bak-$stamp"
        Copy-Item -LiteralPath $Path -Destination $bak -Force
        Write-Host "  [BAK] $bak" -ForegroundColor Yellow
    }
}

# ============================================================
# 1) Create folders
# ============================================================
Write-Host "[1/8] Creating folders..." -ForegroundColor Yellow
foreach ($dir in @('tools', 'distribution', 'keys', 'licenses')) {
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}
Write-Host "  [OK] folders ready" -ForegroundColor Green

# ============================================================
# 2) Backup existing license files if present
# ============================================================
Write-Host "[2/8] Backing up existing files..." -ForegroundColor Yellow
Backup-FileIfExists 'license_manager.py'
Backup-FileIfExists 'license_ui.py'
Backup-FileIfExists 'launcher.py'
Backup-FileIfExists '.gitignore'

# ============================================================
# 3) license_manager.py
# ============================================================
Write-Host "[3/8] Writing license_manager.py..." -ForegroundColor Yellow

$licenseManager = @'
# license_manager.py
"""
Techtoon Accounting - Secure Trial/License Manager

- Trial: 30 days from first run
- License file: license.key located next to executable
- License is bound to machine ID
- License is verified using Ed25519 public key embedded in app
- Private key is NOT distributed and NOT inside EXE
"""

from __future__ import annotations

import base64
import hashlib
import json
import os
import platform
import subprocess
import sys
import winreg
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Optional

TRIAL_DAYS = 30
LICENSE_FILENAME = "license.key"
PUBLIC_KEY_FILENAME = "license_public.pem"
APP_TRIAL_SALT = "TechtoonTrial2026::DoNotEdit"

if getattr(sys, "frozen", False):
    APP_DIR = Path(sys.executable).resolve().parent
else:
    APP_DIR = Path(__file__).resolve().parent

LICENSE_FILE = APP_DIR / LICENSE_FILENAME


def _resource_path(filename: str) -> Path:
    candidates = []
    if getattr(sys, "frozen", False):
        meipass = getattr(sys, "_MEIPASS", None)
        if meipass:
            candidates.append(Path(meipass) / filename)
    candidates.append(APP_DIR / filename)
    for p in candidates:
        if p.exists():
            return p
    return candidates[-1]


def _now_utc() -> datetime:
    return datetime.now(timezone.utc)


def _registry_machine_guid() -> str:
    try:
        with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Cryptography") as key:
            value, _ = winreg.QueryValueEx(key, "MachineGuid")
            return str(value).strip()
    except Exception:
        return ""


def _powershell_uuid() -> str:
    try:
        cmd = [
            "powershell", "-NoProfile", "-NonInteractive", "-Command",
            "(Get-CimInstance -ClassName Win32_ComputerSystemProduct).UUID",
        ]
        out = subprocess.check_output(cmd, stderr=subprocess.DEVNULL, timeout=8).decode(errors="ignore").strip()
        if out and out.upper() != "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF":
            return out
    except Exception:
        pass
    return ""


def get_machine_id() -> str:
    source = _registry_machine_guid()
    kind = "MACHINE_GUID"
    if not source:
        source = _powershell_uuid()
        kind = "SMBIOS_UUID"
    if not source:
        source = "|".join([platform.node(), platform.machine(), platform.system()])
        kind = "FALLBACK"
    raw = f"TECHTOON|{kind}|{source}"
    return hashlib.sha256(raw.encode("utf-8")).hexdigest()[:32].upper()


try:
    from cryptography.exceptions import InvalidSignature
    from cryptography.hazmat.primitives import serialization
    from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PublicKey
    CRYPTO_OK = True
except Exception:
    CRYPTO_OK = False


def _load_public_key():
    if not CRYPTO_OK:
        return None
    path = _resource_path(PUBLIC_KEY_FILENAME)
    if not path.exists():
        return None
    try:
        pem = path.read_bytes()
        return serialization.load_pem_public_key(pem)
    except Exception:
        return None


def _canonical_payload(payload: dict) -> bytes:
    return json.dumps(payload, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")


def verify_license_content(content: str) -> tuple[bool, dict, str]:
    if not content or not content.strip():
        return False, {}, "empty license file"
    try:
        data = json.loads(content)
    except Exception:
        return False, {}, "invalid JSON"
    payload = data.get("payload")
    signature_b64 = data.get("signature")
    if not isinstance(payload, dict) or not signature_b64:
        return False, {}, "missing payload/signature"
    public_key = _load_public_key()
    if public_key is None:
        return False, payload, "public key or cryptography module missing"
    try:
        signature = base64.urlsafe_b64decode(signature_b64)
    except Exception:
        return False, payload, "invalid signature encoding"
    try:
        public_key.verify(signature, _canonical_payload(payload))
    except InvalidSignature:
        return False, payload, "signature invalid"
    except Exception as exc:
        return False, payload, f"verification error: {exc}"
    if payload.get("schema") != 1:
        return False, payload, "unsupported license schema"
    machine_id = payload.get("machine_id")
    if not machine_id or machine_id != get_machine_id():
        return False, payload, "license is bound to another machine"
    expires_at = payload.get("expires_at")
    if expires_at:
        try:
            exp_dt = datetime.fromisoformat(str(expires_at).replace("Z", "+00:00"))
        except Exception:
            return False, payload, "invalid expiry date"
        if _now_utc() > exp_dt:
            return False, payload, "license expired"
    return True, payload, "ok"


def _trial_dirs() -> list[Path]:
    appdata = Path(os.environ.get("APPDATA", str(Path.home() / "AppData" / "Roaming")))
    local_appdata = Path(os.environ.get("LOCALAPPDATA", str(Path.home() / "AppData" / "Local")))
    program_data = Path(os.environ.get("ProgramData", r"C:\ProgramData"))
    public = Path(os.environ.get("PUBLIC", r"C:\Users\Public"))
    return [
        appdata / ".techtoon",
        local_appdata / ".techtoon",
        program_data / ".techtoon",
        public / ".techtoon",
    ]


def _trial_files() -> list[Path]:
    return [d / ".trial_start" for d in _trial_dirs()]


def _encode_trial_token(date_iso: str) -> str:
    machine_hash = hashlib.sha256(f"{APP_TRIAL_SALT}|{get_machine_id()}|{date_iso}".encode("utf-8")).hexdigest()[:24]
    obj = {"d": date_iso, "h": machine_hash}
    raw = json.dumps(obj, separators=(",", ":")).encode("utf-8")
    return base64.urlsafe_b64encode(raw).decode("ascii")


def _decode_trial_token(token: str) -> Optional[str]:
    try:
        raw = base64.urlsafe_b64decode(token.strip().encode("ascii"))
        obj = json.loads(raw.decode("utf-8"))
        date_iso = str(obj.get("d", ""))
        provided_hash = str(obj.get("h", ""))
        expected_hash = hashlib.sha256(f"{APP_TRIAL_SALT}|{get_machine_id()}|{date_iso}".encode("utf-8")).hexdigest()[:24]
        if provided_hash != expected_hash:
            return None
        datetime.fromisoformat(date_iso)
        return date_iso
    except Exception:
        return None


def _read_registry_trial() -> Optional[str]:
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Techtoon\Trial") as key:
            value, _ = winreg.QueryValueEx(key, "StartUTC")
            return _decode_trial_token(str(value))
    except Exception:
        return None


def _write_registry_trial(token: str) -> None:
    try:
        key = winreg.CreateKey(winreg.HKEY_CURRENT_USER, r"Software\Techtoon\Trial")
        winreg.SetValueEx(key, "StartUTC", 0, winreg.REG_SZ, token)
        winreg.CloseKey(key)
    except Exception:
        pass


def _get_or_create_trial_start() -> datetime:
    candidates: list[datetime] = []
    for file_path in _trial_files():
        if file_path.exists():
            try:
                token = file_path.read_text(encoding="utf-8", errors="ignore")
                date_iso = _decode_trial_token(token)
                if date_iso:
                    candidates.append(datetime.fromisoformat(date_iso))
            except Exception:
                pass
    reg_date = _read_registry_trial()
    if reg_date:
        candidates.append(datetime.fromisoformat(reg_date))
    if candidates:
        return min(candidates)
    start = _now_utc()
    token = _encode_trial_token(start.isoformat())
    for file_path in _trial_files():
        try:
            file_path.parent.mkdir(parents=True, exist_ok=True)
            file_path.write_text(token, encoding="utf-8")
        except Exception:
            pass
    _write_registry_trial(token)
    return start


def _base_status() -> dict:
    return {
        "allowed": False,
        "type": "unknown",
        "message": "",
        "days_left": 0,
        "issued_to": "",
        "machine_id": get_machine_id(),
        "expires_at": None,
        "license_file": str(LICENSE_FILE),
        "trial_start": None,
        "license_invalid_reason": None,
    }


def get_license_status() -> dict:
    status = _base_status()
    if LICENSE_FILE.exists():
        try:
            content = LICENSE_FILE.read_text(encoding="utf-8", errors="ignore")
        except Exception as exc:
            content = ""
            status["license_invalid_reason"] = f"cannot read license file: {exc}"
        ok, payload, reason = verify_license_content(content)
        if ok:
            status["allowed"] = True
            status["type"] = payload.get("license_type", "paid")
            status["issued_to"] = payload.get("customer", "")
            status["expires_at"] = payload.get("expires_at")
            if status["type"] == "trial":
                status["message"] = "ترخيص تجريبي فعّال"
            elif status["type"] == "subscription":
                status["message"] = "اشتراك فعّال"
            else:
                status["message"] = "ترخيص مدفوع فعّال"
            expires_at = payload.get("expires_at")
            if expires_at:
                try:
                    exp_dt = datetime.fromisoformat(str(expires_at).replace("Z", "+00:00"))
                    status["days_left"] = max(0, (exp_dt - _now_utc()).days)
                except Exception:
                    status["days_left"] = 0
            else:
                status["days_left"] = 9999
            return status
        status["license_invalid_reason"] = reason
        status["message"] = f"ملف الترخيص غير صالح لهذا الجهاز: {reason}"
    trial_start = _get_or_create_trial_start()
    status["trial_start"] = trial_start.isoformat()
    elapsed_days = (_now_utc() - trial_start).days
    days_left = max(0, TRIAL_DAYS - int(elapsed_days))
    if days_left > 0:
        status["allowed"] = True
        if status["type"] == "unknown":
            status["type"] = "trial"
        status["days_left"] = days_left
        if status["message"]:
            status["message"] += f" | فترة تجريبية: {days_left} يوم متبقي"
        else:
            status["message"] = f"فترة تجريبية: {days_left} يوم متبقي"
        return status
    if status["type"] == "unknown":
        status["type"] = "expired"
    if not status["message"]:
        status["message"] = "انتهت الفترة التجريبية. ضع ملف license.key صالحًا في مجلد البرنامج."
    return status


def is_allowed() -> bool:
    return bool(get_license_status().get("allowed"))


def install_license(content: str) -> tuple[bool, str]:
    ok, payload, reason = verify_license_content(content)
    if not ok:
        return False, f"تعذر تفعيل الترخيص: {reason}"
    try:
        data = json.loads(content)
        LICENSE_FILE.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
        return True, "تم تفعيل الترخيص بنجاح"
    except Exception as exc:
        return False, f"تعذر كتابة ملف الترخيص: {exc}"
'@

Write-FileUtf8NoBom -Path 'license_manager.py' -Content $licenseManager

# ============================================================
# 4) license_ui.py
# ============================================================
Write-Host "[4/8] Writing license_ui.py..." -ForegroundColor Yellow

$licenseUi = @'
# license_ui.py
"""Techtoon Accounting - License/Trial UI window."""

from __future__ import annotations

import sys
import threading
import time
from pathlib import Path

try:
    import webview
except Exception:
    webview = None


LICENSE_HTML = """<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
<meta charset="UTF-8">
<title>Techtoon Accounting - الترخيص</title>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; }
html, body { height: 100%; width: 100%; background: #0f172a; color: #e5e7eb; overflow: hidden; }
.wrap { height: 100vh; display: flex; align-items: center; justify-content: center; padding: 24px; }
.card { width: 680px; max-width: 100%; background: linear-gradient(145deg, #111827, #1f2937); border: 1px solid rgba(255,255,255,0.08); border-radius: 22px; padding: 28px; box-shadow: 0 20px 60px rgba(0,0,0,0.45); }
.logo { font-size: 48px; text-align: center; margin-bottom: 10px; }
h1 { text-align: center; font-size: 28px; margin-bottom: 6px; color: #fff; }
.subtitle { text-align: center; color: #9ca3af; margin-bottom: 22px; }
.status { border-radius: 16px; padding: 18px; margin-bottom: 18px; border: 1px solid rgba(255,255,255,0.08); background: rgba(255,255,255,0.04); }
.status.ok { border-color: rgba(16,185,129,0.45); }
.status.warn { border-color: rgba(245,158,11,0.45); }
.status.error { border-color: rgba(239,68,68,0.45); }
.status-head { display: flex; gap: 12px; align-items: center; margin-bottom: 8px; }
.status-icon { font-size: 28px; }
.status-title { font-size: 20px; font-weight: 700; }
.status-detail { color: #d1d5db; line-height: 1.7; }
.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 14px; }
.box { background: rgba(255,255,255,0.04); border: 1px solid rgba(255,255,255,0.07); border-radius: 14px; padding: 14px; }
.label { color: #9ca3af; font-size: 13px; margin-bottom: 6px; }
.value { color: #fff; font-weight: 700; word-break: break-all; }
.note { margin-top: 16px; padding: 14px; border-radius: 14px; background: rgba(59,130,246,0.10); border: 1px solid rgba(59,130,246,0.25); color: #dbeafe; line-height: 1.8; }
.actions { margin-top: 18px; display: flex; justify-content: center; }
button { background: linear-gradient(90deg, #10b981, #3b82f6); color: white; border: none; border-radius: 12px; padding: 12px 22px; font-size: 15px; font-weight: 700; cursor: pointer; }
button:hover { opacity: 0.95; }
</style>
</head>
<body>
<div class="wrap">
  <div class="card">
    <div class="logo">💼</div>
    <h1>Techtoon Accounting</h1>
    <div class="subtitle">نظام المحاسبة المتكامل</div>
    <div class="status {BOX_CLASS}">
      <div class="status-head">
        <div class="status-icon">{ICON}</div>
        <div class="status-title">{TITLE}</div>
      </div>
      <div class="status-detail">{DETAIL}</div>
    </div>
    <div class="grid">
      <div class="box"><div class="label">نوع الحالة</div><div class="value">{TYPE_AR}</div></div>
      <div class="box"><div class="label">الأيام المتبقية</div><div class="value">{DAYS}</div></div>
      <div class="box"><div class="label">Machine ID</div><div class="value">{MID}</div></div>
      <div class="box"><div class="label">تاريخ الانتهاء</div><div class="value">{EXPIRES}</div></div>
    </div>
    <div class="note">
      لتفعيل البرنامج بشكل دائم، أرسل <b>Machine ID</b> أعلاه إلى المورّد، وستستلم ملف <b>license.key</b>.<br>
      ضع الملف في نفس مجلد <b>TechtoonAccounting.exe</b>.<br>
      ملف الترخيص مرتبط بهذا الجهاز ولا يعمل على جهاز آخر.
    </div>
    <div class="actions"><button onclick="window.close()">إغلاق</button></div>
  </div>
</div>
</body>
</html>
"""


def _type_ar(type_key: str) -> str:
    mapping = {
        "trial": "تجريبي",
        "paid": "مدفوع",
        "subscription": "اشتراك",
        "expired": "منتهي",
        "invalid": "غير صالح",
        "unknown": "غير معروف",
    }
    return mapping.get(type_key, type_key or "غير معروف")


def show_license_window(status: dict, duration: float | None = None) -> None:
    lic_type = status.get("type", "unknown")
    allowed = bool(status.get("allowed"))
    days_left = status.get("days_left", 0)
    machine_id = status.get("machine_id", "")
    expires_at = status.get("expires_at") or "—"
    message = status.get("message", "")

    if allowed:
        if lic_type == "trial":
            box_class = "warn"
            icon = "⏳"
            title = "وضع تجريبي"
        else:
            box_class = "ok"
            icon = "✅"
            title = "ترخيص فعّال"
    else:
        if lic_type == "expired":
            box_class = "warn"
            icon = "⚠️"
            title = "انتهت الفترة التجريبية"
        else:
            box_class = "error"
            icon = "❌"
            title = "ترخيص غير صالح"

    days_text = "غير محدود" if days_left >= 9999 else str(days_left)

    html = (
        LICENSE_HTML
        .replace("{BOX_CLASS}", box_class)
        .replace("{ICON}", icon)
        .replace("{TITLE}", title)
        .replace("{DETAIL}", message or "لا توجد تفاصيل إضافية.")
        .replace("{TYPE_AR}", _type_ar(lic_type))
        .replace("{DAYS}", days_text)
        .replace("{MID}", machine_id or "غير متاح")
        .replace("{EXPIRES}", str(expires_at))
    )

    if webview is None:
        print("=" * 70)
        print(title)
        print(message)
        print(f"Type: {lic_type}")
        print(f"Days left: {days_text}")
        print(f"Machine ID: {machine_id}")
        print("=" * 70)
        if duration:
            time.sleep(duration)
        return

    window = webview.create_window(
        "Techtoon Accounting - الترخيص",
        html=html,
        width=760,
        height=720,
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

    try:
        webview.start()
    except Exception:
        pass


def wait_and_close(status: dict, seconds: float = 3.0) -> None:
    show_license_window(status, duration=seconds)
'@

Write-FileUtf8NoBom -Path 'license_ui.py' -Content $licenseUi

# ============================================================
# 5) license_gate.py
# ============================================================
Write-Host "[5/8] Writing license_gate.py..." -ForegroundColor Yellow

$licenseGate = @'
# license_gate.py
"""Lightweight license gate for Techtoon Accounting."""

from __future__ import annotations

import os
import sys

from license_manager import get_license_status
from license_ui import show_license_window


def enforcement_enabled() -> bool:
    if getattr(sys, "frozen", False):
        return True
    return str(os.getenv("TECHTOON_REQUIRE_LICENSE", "0")).strip().lower() in ("1", "true", "yes", "on")


def check_and_continue(show_success_window: bool = False) -> bool:
    if not enforcement_enabled():
        return True
    status = get_license_status()
    if status.get("allowed"):
        if show_success_window:
            show_license_window(status, duration=2.0)
        return True
    show_license_window(status)
    return False
'@

Write-FileUtf8NoBom -Path 'license_gate.py' -Content $licenseGate

# ============================================================
# 6) tools scripts
# ============================================================
Write-Host "[6/8] Writing tools..." -ForegroundColor Yellow

$generateKeys = @'
# tools/generate_keys.py
"""Generate Ed25519 key pair for Techtoon licensing."""

from __future__ import annotations

import sys
from pathlib import Path

try:
    from cryptography.hazmat.primitives import serialization
    from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey
except Exception as exc:
    print("Missing cryptography package.")
    print("Install: python -m pip install cryptography")
    sys.exit(1)

ROOT = Path(__file__).resolve().parents[1]
KEYS_DIR = ROOT / "keys"
PRIVATE_PATH = KEYS_DIR / "license_private.pem"
PUBLIC_PATH = ROOT / "license_public.pem"


def main() -> None:
    force = "--force" in sys.argv
    KEYS_DIR.mkdir(parents=True, exist_ok=True)
    if PRIVATE_PATH.exists() and not force:
        print(f"Private key already exists: {PRIVATE_PATH}")
        print("Use --force to overwrite. WARNING: old licenses will become invalid.")
        sys.exit(1)
    private_key = Ed25519PrivateKey.generate()
    private_pem = private_key.private_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PrivateFormat.PKCS8,
        encryption_algorithm=serialization.NoEncryption(),
    )
    public_pem = private_key.public_key().public_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PublicFormat.Pem,
    )
    PRIVATE_PATH.write_bytes(private_pem)
    PUBLIC_PATH.write_bytes(public_pem)
    try:
        PRIVATE_PATH.chmod(0o600)
    except Exception:
        pass
    print("Keys generated successfully.")
    print(f"Private key: {PRIVATE_PATH}")
    print(f"Public key:  {PUBLIC_PATH}")
    print("")
    print("IMPORTANT:")
    print("- Keep private key offline/secure.")
    print("- Do NOT commit private key.")
    print("- Public key can be included in EXE.")


if __name__ == "__main__":
    main()
'@

$generateLicense = @'
# tools/generate_license.py
"""Generate license.key for a specific machine ID."""

from __future__ import annotations

import argparse
import base64
import json
import os
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

try:
    from cryptography.hazmat.primitives import serialization
except Exception as exc:
    print("Missing cryptography package.")
    print("Install: python -m pip install cryptography")
    sys.exit(1)

ROOT = Path(__file__).resolve().parents[1]
PRIVATE_PATH = ROOT / "keys" / "license_private.pem"


def canonical_payload(payload: dict) -> bytes:
    return json.dumps(payload, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate Techtoon license.key")
    parser.add_argument("--machine-id", required=True, help="Target machine ID")
    parser.add_argument("--customer", default="Customer", help="Customer name")
    parser.add_argument("--days", type=int, default=365, help="Validity days. 0 = perpetual")
    parser.add_argument("--type", default="paid", choices=["paid", "trial", "subscription"])
    parser.add_argument("--out", default="license.key", help="Output license file")
    args = parser.parse_args()

    if not PRIVATE_PATH.exists():
        print(f"Private key not found: {PRIVATE_PATH}")
        print("Run first: python tools/generate_keys.py")
        sys.exit(1)

    private_pem = PRIVATE_PATH.read_bytes()
    private_key = serialization.load_pem_private_key(private_pem, password=None)
    now = datetime.now(timezone.utc)
    expires_at = None
    if args.days and args.days > 0:
        expires_at = (now + timedelta(days=args.days)).isoformat()

    payload = {
        "schema": 1,
        "machine_id": args.machine_id.strip().upper(),
        "customer": args.customer,
        "license_type": args.type,
        "issued_at": now.isoformat(),
        "expires_at": expires_at,
        "nonce": os.urandom(12).hex(),
    }
    signature = private_key.sign(canonical_payload(payload))
    document = {
        "payload": payload,
        "signature": base64.urlsafe_b64encode(signature).decode("ascii"),
    }
    out_path = Path(args.out)
    if not out_path.is_absolute():
        out_path = ROOT / out_path
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(json.dumps(document, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"License generated: {out_path}")
    print(f"Machine ID: {payload['machine_id']}")
    print(f"Type: {payload['license_type']}")
    print(f"Expires: {payload['expires_at'] or 'Perpetual'}")


if __name__ == "__main__":
    main()
'@

$showMachineId = @'
# tools/show_machine_id.py
"""Print current machine ID for development/testing."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from license_manager import get_machine_id

print(get_machine_id())
'@

Write-FileUtf8NoBom -Path 'tools/generate_keys.py' -Content $generateKeys
Write-FileUtf8NoBom -Path 'tools/generate_license.py' -Content $generateLicense
Write-FileUtf8NoBom -Path 'tools/show_machine_id.py' -Content $showMachineId

# ============================================================
# 7) distribution files
# ============================================================
Write-Host "[7/8] Writing distribution files..." -ForegroundColor Yellow

$buildRelease = @'
# distribution/build_release.ps1
param(
    [switch]$OneFile,
    [switch]$IncludeEnv,
    [string]$Version = "8.0"
)

$ErrorActionPreference = "Stop"

if ($PSScriptRoot) {
    $Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
} else {
    $Root = (Get-Location).Path
}

Set-Location -LiteralPath $Root

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Techtoon Accounting - Release Builder v$Version" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Project root: $Root" -ForegroundColor Gray
Write-Host ""

$pythonCmd = $null
if (Get-Command python -ErrorAction SilentlyContinue) { $pythonCmd = "python" }
elseif (Get-Command py -ErrorAction SilentlyContinue) { $pythonCmd = "py" }
else { throw "Python not found." }

Write-Host "[1/7] Using Python: $pythonCmd" -ForegroundColor Green

Write-Host "[2/7] Checking required packages..." -ForegroundColor Yellow
& $pythonCmd -c "import PyInstaller" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "PyInstaller not found. Installing..." -ForegroundColor Yellow
    & $pythonCmd -m pip install pyinstaller pywebview cryptography psycopg2-binary
}
& $pythonCmd -c "import cryptography" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "cryptography not found. Installing..." -ForegroundColor Yellow
    & $pythonCmd -m pip install cryptography
}

Write-Host "[3/7] Checking license public key..." -ForegroundColor Yellow
if (-not (Test-Path -LiteralPath "license_public.pem")) {
    Write-Host "license_public.pem not found. Generating keys..." -ForegroundColor Yellow
    & $pythonCmd "tools/generate_keys.py"
}
if (-not (Test-Path -LiteralPath "license_public.pem")) {
    throw "license_public.pem still missing after key generation."
}

Write-Host "[4/7] Cleaning old builds..." -ForegroundColor Yellow
foreach ($item in @("build", "dist", "TechtoonAccounting.spec", "release")) {
    if (Test-Path -LiteralPath $item) {
        Remove-Item -LiteralPath $item -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "[5/7] Preparing PyInstaller arguments..." -ForegroundColor Yellow

$argsList = @(
    "--noconfirm", "--clean", "--windowed",
    "--name", "TechtoonAccounting",
    "--hidden-import", "psycopg2",
    "--hidden-import", "streamlit",
    "--hidden-import", "boto3",
    "--hidden-import", "webview",
    "--hidden-import", "cryptography",
    "--hidden-import", "cryptography.hazmat.backends.openssl.backend",
    "--hidden-import", "cryptography.hazmat.primitives.asymmetric.ed25519",
    "--collect-all", "streamlit",
    "launcher.py"
)

if ($OneFile) {
    $argsList = @("--onefile") + $argsList
} else {
    $argsList = @("--onedir") + $argsList
}

if (Test-Path -LiteralPath "logo.ico") {
    $argsList = @("--icon", "logo.ico") + $argsList
}

$dataToAdd = @(
    @{ Path = "pages"; Dest = "pages;pages" },
    @{ Path = ".streamlit"; Dest = ".streamlit;.streamlit" },
    @{ Path = "fonts"; Dest = "fonts;fonts" },
    @{ Path = "license_public.pem"; Dest = "license_public.pem;." },
    @{ Path = "input_helpers.py"; Dest = "input_helpers.py;." },
    @{ Path = "cache_helpers.py"; Dest = "cache_helpers.py;." },
    @{ Path = "database.py"; Dest = "database.py;." },
    @{ Path = "models.py"; Dest = "models.py;." },
    @{ Path = "services.py"; Dest = "services.py;." },
    @{ Path = "sidebar.py"; Dest = "sidebar.py;." },
    @{ Path = "navigation_helper.py"; Dest = "navigation_helper.py;." },
    @{ Path = "local_mirror.py"; Dest = "local_mirror.py;." },
    @{ Path = "sync_manager.py"; Dest = "sync_manager.py;." },
    @{ Path = "s3_backup.py"; Dest = "s3_backup.py;." },
    @{ Path = "invoice_page_common.py"; Dest = "invoice_page_common.py;." },
    @{ Path = "license_manager.py"; Dest = "license_manager.py;." },
    @{ Path = "license_ui.py"; Dest = "license_ui.py;." },
    @{ Path = "license_gate.py"; Dest = "license_gate.py;." }
)

foreach ($d in $dataToAdd) {
    if (Test-Path -LiteralPath $d.Path) {
        $argsList = @("--add-data", $d.Dest) + $argsList
    }
}

$excludeModules = @("torch", "torchvision", "tensorflow", "keras", "PyQt5", "PySide2", "PySide6")
foreach ($mod in $excludeModules) {
    $argsList = @("--exclude-module", $mod) + $argsList
}

Write-Host "[6/7] Running PyInstaller..." -ForegroundColor Yellow
Write-Host "This may take 5-15 minutes." -ForegroundColor Gray
Write-Host ""

& pyinstaller @argsList
if ($LASTEXITCODE -ne 0) { throw "PyInstaller failed. Check output above." }

Write-Host "[7/7] Creating release package..." -ForegroundColor Yellow

$releaseName = "TechtoonAccounting_v${Version}_windows"
$releaseDir = Join-Path $Root "release\$releaseName"
if (Test-Path -LiteralPath $releaseDir) { Remove-Item -LiteralPath $releaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

if ($OneFile) {
    $builtExe = Join-Path $Root "dist\TechtoonAccounting.exe"
    if (-not (Test-Path -LiteralPath $builtExe)) { throw "Built EXE not found: $builtExe" }
    Copy-Item -LiteralPath $builtExe -Destination $releaseDir -Force
} else {
    $builtDir = Join-Path $Root "dist\TechtoonAccounting"
    if (-not (Test-Path -LiteralPath $builtDir)) { throw "Built folder not found: $builtDir" }
    Copy-Item -Path (Join-Path $builtDir "*") -Destination $releaseDir -Recurse -Force
}

Copy-Item -LiteralPath (Join-Path $Root "license_public.pem") -Destination $releaseDir -Force

$readmeSrc = Join-Path $Root "distribution\README_CLIENT.md"
if (Test-Path -LiteralPath $readmeSrc) {
    Copy-Item -LiteralPath $readmeSrc -Destination (Join-Path $releaseDir "README_CLIENT.md") -Force
}

$showSrc = Join-Path $Root "distribution\ShowMachineId.bat"
if (Test-Path -LiteralPath $showSrc) {
    Copy-Item -LiteralPath $showSrc -Destination (Join-Path $releaseDir "ShowMachineId.bat") -Force
}

$envExample = Join-Path $Root ".env.example"
if (Test-Path -LiteralPath $envExample) {
    Copy-Item -LiteralPath $envExample -Destination (Join-Path $releaseDir ".env.example") -Force
}

if ($IncludeEnv) {
    $realEnv = Join-Path $Root ".env"
    if (Test-Path -LiteralPath $realEnv) {
        Copy-Item -LiteralPath $realEnv -Destination (Join-Path $releaseDir ".env") -Force
        Write-Host "WARNING: Real .env copied into release. Do not distribute publicly." -ForegroundColor Red
    }
}

$licenseNote = Join-Path $releaseDir "PUT_LICENSE_KEY_HERE.txt"
@"
Place your license.key file in this folder.

The file must be named:
license.key

It must be generated for this machine's Machine ID.
Do not rename it.
Do not move it to another computer.
"@ | Out-File -FilePath $licenseNote -Encoding UTF8

$zipPath = Join-Path $Root "release\$releaseName.zip"
Compress-Archive -Path (Join-Path $releaseDir "*") -DestinationPath $zipPath -Force

$hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
$hashPath = Join-Path $Root "release\$releaseName.zip.sha256"
"$($hash.Hash)  $releaseName.zip" | Out-File -FilePath $hashPath -Encoding ASCII

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host " Release build complete" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Release folder: $releaseDir" -ForegroundColor Cyan
Write-Host "ZIP file:       $zipPath" -ForegroundColor Cyan
Write-Host "SHA256:         $hashPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test the EXE on a clean Windows machine if possible."
Write-Host "2. Get Machine ID from client using ShowMachineId.bat."
Write-Host "3. Generate license.key using tools/generate_license.py."
Write-Host "4. Send license.key separately to client."
'@

$readmeClient = @'
# Techtoon Accounting - نسخة التوزيع

## التشغيل
1. افتح مجلد البرنامج.
2. شغّل:

```text
TechtoonAccounting.exe