# CRUD TEST RESULTS

## Build & Unit Tests
- `dotnet build Techtoon.sln`: **0 errors** (4 warnings CS0649).
- `dotnet test Techtoon.Tests`: **13/13 passed** (BalanceCalculator, InventorySignConvention).

## Fly API Smoke (v15 → v16 pending)
| Endpoint | v14 | v15 | v16 (متوقع بعد النشر) |
|----------|-----|-----|-----------------------|
| /health | 200 | 200 | 200 |
| POST login | 200 | 200 | 200 |
| items/parties/invoices/payments/cashboxes/expenses/kits | 200 | 200 | 200 |
| warehouses/employees | — | 200 | 200 |
| salaries | — | 400 (OrderBy) | 200 (fixed) |
| loans | — | 500 (CashBoxId shadow) | 200 (fixed) |
| Error contract 401 | {status,message} | problem+json | problem+json |

## DB (prod read-only)
- Double columns: **0** (P0-3 migration + views).
- Views: 5/5 present.
- Items count: 112 (بعد حذف "فرق" + مخزن فرعي).

## Test DB CRUD (محلي, techtoon_test)
- P0-3 rehearsal: 86 double → 2 (test) / 0 (prod).
- API integration Phase 2: 25/25 (سابقًا).

## ما لم يُختبر
- WPF UI يدوي (خارج CI).
- Writes على prod (محظور عمدًا).
