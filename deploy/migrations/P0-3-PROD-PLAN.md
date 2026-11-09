# P0-3 Production Migration — Backup & Execution Plan (APPROVAL REQUIRED)

> No production migration has been executed. This file is a plan only.

## 1. What the migration does
- File: `deploy/migrations/P0-3_money_decimal.sql`
- Converts 84 money/quantity/rate columns from `double precision` → `numeric`:
  - Money: `numeric(12,2)`
  - Rates/percents: `numeric(12,6)`
  - Quantities: `numeric(14,4)`
- Leaves 2 non-financial double columns (`match_confidence`, `confidence_score`) untouched.
- Rehearsed on local test DB (`techtoon_test`): 86 double cols → 2 left. ✅

## 2. Pre-flight (must pass)
- [ ] Neon **branch/snapshot** created (console.neon.tech → project → Branches/Migrations → Create branch from production). Record branch ID.
- [ ] Record current Fly release: `flyctl releases -a techtoon-mobile` (v10).
- [ ] Stop/reduce traffic window agreed (API deploy will restart machines).
- [ ] Verify `TECHTOON_PG_CONNECTION` / `ConnectionStrings__DefaultConnection` (Fly secrets) still point to prod Neon (unchanged).
- [ ] C# code at HEAD `7174fce` (decimal) deployed — schema change is compatible both ways (Npgsql reads numeric as decimal; double columns were read via `Convert.ToDecimal`).

## 3. Backup command (if pg_dump available)
Set `DATABASE_URL` (env only, never in repo) then:
```
pg_dump "$DATABASE_URL" -Fc -f techtoon-prod-pre-p03.dump
```
Verify:
```
pg_restore --list techtoon-prod-pre-p03.dump | Measure-Object
```
Store dump outside the repo (encrypted drive). **Do not commit.**

## 4. Execution (maintenance window)
1. Deploy code? NO — code is already decimal-compatible. Schema first is safe.
2. Run migration against prod **only** via Neon SQL editor or `psql "$DATABASE_URL" -f deploy/migrations/P0-3_money_decimal.sql` (single transaction).
3. Verify:
   ```
   SELECT count(*) FROM information_schema.columns WHERE table_schema='public' AND data_type='double precision';
   -- expected: 2 (match_confidence, confidence_score)
   ```
4. Restart Fly app: `flyctl deploy -c fly.toml` (redeploys same image with new schema compat) or `flyctl machine restart`.
5. Run smoke: `GET /health` → 200; `POST /api/auth/login` (admin) → 200; `GET /api/items` → 200 with correct `totalCount`.

## 5. Rollback
- If verification fails: roll back Neon branch (restore branch as main), then `flyctl releases rollback -a techtoon-mobile`.
- Code rollback: `git revert` the 2 decimal commits (`7b77d48`...`99368d8`) only if schema rollback fails — code supports both double and numeric via `Convert.ToDecimal`.

## 6. Approval required
- [ ] Approve backup creation (Neon branch).
- [ ] Approve maintenance window date/time.
- [ ] Approve prod execution.

**No step above has been executed. Awaiting explicit approval.**
