# license_manager.py
"""نظام تراخيص Techtoon - يدعم التشغيل من EXE."""
import hashlib
import hmac
import json
import os
import platform
import subprocess
import sys
import uuid
from datetime import datetime, timedelta
from pathlib import Path

SECRET = b"TECHTOON_LICENSE_2026_V1"
TRIAL_DAYS = 30

# ⭐ مسار التطبيق: بجانب EXE أو بجانب الملف
if getattr(sys, "frozen", False):
    APP_DIR = Path(sys.executable).resolve().parent
else:
    APP_DIR = Path(__file__).resolve().parent

LICENSE_FILE = APP_DIR / "license.key"

_TRIAL_DIR = Path(os.environ.get("APPDATA", str(Path.home()))) / ".techtoon"
_TRIAL_FILE = _TRIAL_DIR / ".trial_start"


def _run_wmic(wmic_class: str, field: str) -> str:
    try:
        cmd = f"wmic {wmic_class} get {field}"
        out = subprocess.check_output(
            cmd, shell=True, stderr=subprocess.DEVNULL,
            creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
        ).decode(errors="ignore")
        lines = [l.strip() for l in out.splitlines() if l.strip()]
        if len(lines) >= 2:
            return lines[1]
    except Exception:
        pass
    return ""


def get_motherboard_serial() -> str:
    try:
        import wmi
        c = wmi.WMI()
        for b in c.Win32_BaseBoard():
            sn = (b.SerialNumber or "").strip()
            if sn and sn.lower() not in ("to be filled by o.e.m.", "default string", "none", ""):
                return sn.upper()
    except Exception:
        pass
    sn = _run_wmic("baseboard", "serialnumber")
    if sn and sn.lower() not in ("to be filled by o.e.m.", "default string", "none", ""):
        return sn.upper()
    return ""


def get_system_uuid() -> str:
    try:
        import wmi
        c = wmi.WMI()
        for cs in c.Win32_ComputerSystemProduct():
            u = (cs.UUID or "").strip()
            if u and u.lower() not in ("00000000-0000-0000-0000-000000000000", ""):
                return u.upper()
    except Exception:
        pass
    u = _run_wmic("csproduct", "uuid")
    if u and u.lower() not in ("00000000-0000-0000-0000-000000000000", ""):
        return u.upper()
    return ""


def get_bios_serial() -> str:
    try:
        import wmi
        c = wmi.WMI()
        for b in c.Win32_BIOS():
            sn = (b.SerialNumber or "").strip()
            if sn and sn.lower() not in ("to be filled by o.e.m.", "default string", "none", ""):
                return sn.upper()
    except Exception:
        pass
    sn = _run_wmic("bios", "serialnumber")
    if sn and sn.lower() not in ("to be filled by o.e.m.", "default string", "none", ""):
        return sn.upper()
    return ""


def get_machine_id() -> str:
    parts = []
    mb = get_motherboard_serial()
    if mb:
        parts.append(f"MB:{mb}")
    su = get_system_uuid()
    if su:
        parts.append(f"UUID:{su}")
    bios = get_bios_serial()
    if bios and bios not in (mb, su):
        parts.append(f"BIOS:{bios}")
    if len(parts) < 2:
        try:
            parts.append(f"NODE:{platform.node() or ''}")
        except Exception:
            pass
        try:
            parts.append(f"MAC:{uuid.getnode()}")
        except Exception:
            pass
    raw = "|".join(parts).encode("utf-8")
    return hashlib.sha256(raw).hexdigest()[:16].upper()


def get_hardware_info() -> dict:
    return {
        "motherboard_serial": get_motherboard_serial() or "غير متاح",
        "system_uuid": get_system_uuid() or "غير متاح",
        "bios_serial": get_bios_serial() or "غير متاح",
        "machine_name": platform.node() or "غير متاح",
        "machine_id": get_machine_id(),
    }


def _sign(data: dict) -> str:
    payload = json.dumps(data, sort_keys=True, ensure_ascii=False).encode("utf-8")
    return hmac.new(SECRET, payload, hashlib.sha256).hexdigest()


def _verify(data: dict) -> bool:
    if "signature" not in data:
        return False
    sig = data["signature"]
    payload = {k: v for k, v in data.items() if k != "signature"}
    expected = _sign(payload)
    return hmac.compare_digest(sig, expected)


def _get_trial_start() -> datetime:
    try:
        if _TRIAL_FILE.exists():
            return datetime.fromisoformat(_TRIAL_FILE.read_text(encoding="utf-8").strip())
    except Exception:
        pass
    now = datetime.now()
    try:
        _TRIAL_DIR.mkdir(parents=True, exist_ok=True)
        _TRIAL_FILE.write_text(now.isoformat(), encoding="utf-8")
    except Exception:
        pass
    return now


def get_license_status() -> dict:
    result = {
        "valid": False,
        "type": None,
        "message": "",
        "days_left": None,
        "machine_id": get_machine_id(),
        "issued_to": None,
    }

    if not LICENSE_FILE.exists():
        start = _get_trial_start()
        days_used = (datetime.now() - start).days
        days_left = TRIAL_DAYS - days_used
        if days_left <= 0:
            result["message"] = f"انتهت الفترة التجريبية ({TRIAL_DAYS} يوم)"
            result["type"] = "expired"
            return result
        result["valid"] = True
        result["type"] = "trial"
        result["days_left"] = days_left
        result["message"] = f"تجريبي ({days_left} يوم متبقي)"
        return result

    try:
        content = LICENSE_FILE.read_text(encoding="utf-8").strip()
        data = json.loads(content)
    except Exception as e:
        result["message"] = f"ملف الترخيص تالف: {e}"
        return result

    if not _verify(data):
        result["message"] = "التوقيع الرقمي غير صالح"
        return result

    mid_in_file = (data.get("machine_id") or "").strip()
    if mid_in_file and mid_in_file != get_machine_id():
        result["message"] = "هذا الترخيص مخصص لجهاز آخر"
        return result

    result["issued_to"] = data.get("issued_to")

    if data.get("license_type") == "lifetime":
        result["valid"] = True
        result["type"] = "lifetime"
        result["message"] = "رخصة مدى الحياة"
        return result

    expires = data.get("expires_at")
    if expires:
        exp_dt = datetime.fromisoformat(expires)
        if datetime.now() > exp_dt:
            result["message"] = f"انتهت الرخصة ({exp_dt.date()})"
            result["type"] = "expired"
            return result
        days_left = (exp_dt - datetime.now()).days
        result["valid"] = True
        result["type"] = "trial"
        result["days_left"] = days_left
        result["message"] = f"تجريبي ({days_left} يوم متبقي)"
        return result

    result["message"] = "بيانات الرخصة ناقصة"
    return result


def install_license(content: str) -> tuple:
    try:
        data = json.loads(content.strip())
    except Exception:
        return False, "صيغة غير صحيحة"

    if not _verify(data):
        return False, "التوقيع غير صالح"

    mid_in_file = (data.get("machine_id") or "").strip()
    if mid_in_file and mid_in_file != get_machine_id():
        return False, f"الترخيص لجهاز آخر (المطلوب: {mid_in_file})"

    LICENSE_FILE.write_text(content.strip(), encoding="utf-8")
    return True, "تم تفعيل الرخصة بنجاح"