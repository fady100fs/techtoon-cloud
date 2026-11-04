# add_default_cash_boxes.py
from database import SessionLocal, engine
import models
from models import Account, AccountType, CashBox, CashBoxType

models.Base.metadata.create_all(bind=engine)

db = SessionLocal()

try:
    # البحث عن حساب الأصول المتداولة
    current_assets = db.query(Account).filter(Account.code == "1100").first()
    if not current_assets:
        assets_parent = db.query(Account).filter(Account.code == "1000").first()
        if assets_parent:
            current_assets = Account(
                code="1100", name="الأصول المتداولة",
                type=AccountType.ASSET, parent_id=assets_parent.id
            )
            db.add(current_assets)
            db.commit()
    
    # إنشاء الخزائن الافتراضية
    cash_boxes_data = [
        {"name": "الخزينة الرئيسية", "code": "CB001", "type": CashBoxType.CASH, "responsible": "أمين الصندوق"},
        {"name": "بنك الأهلي", "code": "BANK01", "type": CashBoxType.BANK, "responsible": "المحاسب"},
        {"name": "فودافون كاش", "code": "WALLET01", "type": CashBoxType.WALLET, "responsible": None}
    ]
    
    for box_data in cash_boxes_data:
        existing = db.query(CashBox).filter(CashBox.code == box_data["code"]).first()
        if not existing:
            account = Account(
                code=f"11{box_data['code'].zfill(4)}",
                name=box_data["name"],
                type=AccountType.ASSET,
                parent_id=current_assets.id if current_assets else None
            )
            db.add(account)
            db.flush()
            
            cash_box = CashBox(
                name=box_data["name"],
                code=box_data["code"],
                type=box_data["type"],
                account_id=account.id,
                responsible_person=box_data["responsible"],
                is_active=True
            )
            db.add(cash_box)
            print(f"✅ تم إنشاء الخزينة: {box_data['name']}")
    
    db.commit()
    print("✅ تم إنشاء الخزائن الافتراضية بنجاح!")
    
finally:
    db.close()