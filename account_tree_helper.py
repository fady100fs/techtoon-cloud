# account_tree_helper.py
from __future__ import annotations
from sqlalchemy.orm import Session
import models


def _has_active_field():
    """يكتشف هل نموذج Account يحتوي على حقل is_active أو active."""
    cols = {c.name for c in models.Account.__table__.columns}
    if "is_active" in cols:
        return "is_active"
    if "active" in cols:
        return "active"
    return None


def build_account_tree_options(db: Session) -> dict:
    """قائمة حسابات شجرية تعرض الآباء والأبناء بكل الأنواع."""
    q = db.query(models.Account)
    active_field = _has_active_field()
    if active_field:
        q = q.filter(getattr(models.Account, active_field) == True)
    rows = q.order_by(models.Account.code).all()
    
    by_id = {a.id: a for a in rows}
    children = {}
    roots = []
    for a in rows:
        if getattr(a, "parent_id", None) and a.parent_id in by_id:
            children.setdefault(a.parent_id, []).append(a)
        else:
            roots.append(a)
    
    out = {}
    def walk(a, depth):
        prefix = "    " * depth + ("\u2514\u2500 " if depth > 0 else "")
        code = getattr(a, "code", "") or ""
        name = getattr(a, "name", "") or ""
        out[a.id] = f"{prefix}{code} - {name}"
        for ch in children.get(a.id, []):
            walk(ch, depth + 1)
    
    for r in roots:
        walk(r, 0)
    return out
