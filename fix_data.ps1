# fix_data.ps1 - إضافة بيانات تجريبية + إصلاح سلسلة الاتصال
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Add Test Data" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1) إضافة بيانات تجريبية لقاعدة البيانات
Write-Host "`n[1/2] Adding test data to database..." -ForegroundColor Yellow

 $pyScript = @'
import psycopg2
CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
conn = psycopg2.connect(CS)
conn.autocommit = True
cur = conn.cursor()

# إنشاء جدول items إن لم يكن موجوداً
cur.execute("""CREATE TABLE IF NOT EXISTS items (
    id SERIAL PRIMARY KEY, name TEXT NOT NULL, barcode TEXT, sku TEXT,
    unit TEXT DEFAULT 'piece', sale_price NUMERIC(14,2) DEFAULT 0,
    current_stock NUMERIC(14,3) DEFAULT 0, is_active BOOLEAN DEFAULT TRUE)""")

# إضافة أصناف تجريبية
items = [
    ("Laptop Dell", "DLP001", "LAPTOP-001", "pcs", 15000, 10),
    ("Wireless Mouse", "MS002", "MOUSE-002", "pcs", 250, 50),
    ("Keyboard", "KB003", "KEYB-003", "pcs", 400, 30),
    ("Samsung Monitor", "MN004", "MON-004", "pcs", 3500, 15),
    ("Bluetooth Headset", "HP005", "HEAD-005", "pcs", 800, 20),
    ("HDMI Cable", "CB006", "HDMI-006", "m", 50, 100),
    ("Printer Ink", "INK008", "INK-008", "pcs", 350, 25),
]
for it in items:
    try:
        cur.execute("INSERT INTO items (name, barcode, sku, unit, sale_price, current_stock, is_active) VALUES (%s,%s,%s,%s,%s,%s,TRUE) ON CONFLICT DO NOTHING", it)
    except: pass
cur.execute("SELECT COUNT(*) FROM items")
print(f"  Items: {cur.fetchone()[0]}")

# إنشاء جدول parties (العملاء/الموردين)
cur.execute("""CREATE TABLE IF NOT EXISTS parties (
    id SERIAL PRIMARY KEY, name TEXT NOT NULL, type TEXT DEFAULT 'customer',
    phone TEXT, email TEXT, is_active BOOLEAN DEFAULT TRUE)""")

parties = [
    ("Ahmed Mohamed", "customer", "01001234567"),
    ("Sara Ali", "customer", "01007654321"),
    ("Tech Supplier Co", "supplier", "0223456789"),
    ("Nour Trading", "supplier", "0229876543"),
    ("Cairo Electronics", "customer", "01112345678"),
]
for p in parties:
    try:
        cur.execute("INSERT INTO parties (name, type, phone, is_active) VALUES (%s,%s,%s,TRUE) ON CONFLICT DO NOTHING", p)
    except: pass
cur.execute("SELECT COUNT(*) FROM parties")
print(f"  Parties: {cur.fetchone()[0]}")

# إنشاء جدول warehouses (المخازن)
cur.execute("""CREATE TABLE IF NOT EXISTS warehouses (
    id SERIAL PRIMARY KEY, name TEXT NOT NULL, location TEXT, is_active BOOLEAN DEFAULT TRUE)""")

warehouses = [
    ("Main Warehouse", "Cairo"),
    ("Branch 1", "Giza"),
    ("Branch 2", "Alexandria"),
]
for w in warehouses:
    try:
        cur.execute("INSERT INTO warehouses (name, location, is_active) VALUES (%s,%s,TRUE) ON CONFLICT DO NOTHING", w)
    except: pass
cur.execute("SELECT COUNT(*) FROM warehouses")
print(f"  Warehouses: {cur.fetchone()[0]}")

cur.close()
conn.close()
print("  DONE: Test data added!")
'@

 $pyPath = Join-Path $env:TEMP 'techtoon_data.py'
[System.IO.File]::WriteAllText($pyPath, $pyScript, [System.Text.UTF8Encoding]::new($false))
 $result = python $pyPath 2>&1
Write-Host $result

# 2) إصلاح سلسلة الاتصال في ViewModel
Write-Host "`n[2/2] Fixing connection string..." -ForegroundColor Yellow
 $vmPath = Join-Path $ProjectPath 'Techtoon.UI\Views\InvoiceEditViewModel.cs'
if (Test-Path $vmPath) {
    $content = Get-Content $vmPath -Raw -Encoding UTF8
    # استبدل أي return بقيمة Neon المباشرة
    $neonCs = 'return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";'
    # استبدل return defaultCs
    $content = $content -replace 'return defaultCs;', $neonCs
    # استبدل أي return داخل GetConnectionString
    $content = $content -replace 'return cs;', $neonCs
    [System.IO.File]::WriteAllText($vmPath, $content, [System.Text.UTF8Encoding]::new($true))
    Write-Host "  OK: Connection string fixed" -ForegroundColor Green
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  DONE! Now run:" -ForegroundColor Green
Write-Host "  dotnet build Techtoon.UI" -ForegroundColor White
Write-Host "  dotnet run --project Techtoon.UI" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan