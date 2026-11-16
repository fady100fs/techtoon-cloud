# TECHTOON_PHASE_2_ADR_REVIEW.md

> Date: 2026-11-16 · Scope: the 12 ADRs in `ARCHITECTURE_DECISIONS.md` · Method: orchestration review by techtoon-architect (primary), with security/database/sync/accounting/code-review inputs · Constraint: **no ADR status is changed here. Only the user may approve.** ADR-4, ADR-9, ADR-10 are APPROVED (unchanged). The other nine are NEEDS APPROVAL (unchanged).

Per decision: proposed rationale · alternatives · pros/cons · impact on current system/data · security risks · ops cost · fit · specialist recommendation. Where a specialist recommends adjustment, both the **Original** and the **Adjusted** proposal are presented. Findings are labeled [A]=architect, [Sec], [Sync], [Acct], [DB], [CR], [QA].

---

## ADR-1 — TenantId + Row Level Security
**Status: NEEDS APPROVAL · Specialist rec: KEEP WITH ADJUSTMENTS (do NOT approve as written). Support: [A §ADR-1] [Sec F-01..F-04] [DB M13] [CR §A.4]**

- **Proposed:** Option 2 — shared DB, TenantId column on tenant tables, RLS policies as enforcement + app-level filtering as first layer.
- **Alternatives (documented):** separate DB per tenant; separate schema per tenant; app-level only. **Ignored:** Neon branches per tenant (PITR included), per-tenant Neon projects/tiering, schema-per-tenant.
- **Pros:** one schema/migrations; cost-efficient; defense-in-depth; single ops runbook.
- **Cons (admitted + unadmitted):** RLS overhead; SPOF; per-tenant scale-out hard. **Unadmitted:** `current_setting` breaks under pooling without transaction-scoped `set_config(..., is_local=true)`; **incompatible with desktop's direct Npgsql**; misconfig = silent leak or full lockout; every new table silently regresses without a policy+test gate.
- **Impact on current system/data:** 68 tables × TenantId backfill + NOT NULL + ~200 indexes + ~100 FKs; existing docs' literal example fails (`invoices.created_at` already exists); desktop locked out if enabled as written.
- **Security risks:** cross-tenant read on any unpolicied table (violates isolation); NULL-globals exposure (permission system visible to all); C1 credential voids everything.
- **Ops cost:** policy lifecycle per schema change; harder debugging (0-row reports, no errors).
- **Fit:** right end-state for a hosted SaaS; premature & unverifiable for today's single-company system.
- **Verdict: KEEP WITH ADJUSTMENTS.** **Adjusted:** TenantId keep. App-level mandatory primary enforcement; RLS = defense-in-depth **gated on** (a) desktop no longer holds production DB credentials (topology ADR), (b) CI fails build if any tenant-owned table lacks a policy + isolation test, (c) `FORCE RLS` + dedicated non-owner role + transaction-scoped set_config; per-table decision for the NULL-TenantId globals; add Neon-branch option explicitly to the approval packet; **do not run M13 before application cutover**; enforce TI-1..10 isolation tests on `techtoon_test_ci` first.

## ADR-2 — LocalDatabaseId + Fingerprint
**Status: NEEDS APPROVAL · Specialist rec: RECONSIDER (re-issue adjusted). [A §ADR-2] [Sec F-13/F-16] [CR §F]**

- **Proposed:** Option 3 — file-level OS permissions + DB-level LocalDatabaseId + SHA-256 fingerprint of the local file, verified on every open.
- **Alternatives:** file-only; DB-level-only. **Ignored:** SQLCipher-only + ACL; cloud replica registry as sole authority (BACKUP §5 already validates ReplicaId — partially redundant); HMAC-of-a-manifest/sentinel row instead of hashing a mutable file.
- **Pros:** tamper evidence; defense in depth. **Cons:** complexity; open-time O(file) cost. **Unadmitted (fatal):** fingerprinting a **mutable file** can never verify; two different formulas exist (`LOCAL_DATABASE §1.2` vs `§2.5`); size/hash changes on every write; creation-time unstable under copy (restore workflows); reference fingerprint's offline location unspecified; algorithm public ⇒ checksum, not secret; false lockouts on routine writes deactivate the feature by frustration.
- **Impact:** none live; design unimplementable as specified.
- **Security risks:** false-reject DoS; bypass via complete clone (**DB+config+profile ⇒ second working replica ≤72h offline**); collision claims never analyzed.
- **Ops cost:** high; fingerprint lifecycle undefined on legit rotation/restore.
- **Fit:** wrong mechanism for a live file; right mechanism for immutable backup artifacts.
- **Verdict: RECONSIDER.** **Adjusted:** keep LocalDatabaseId as immutable identity. **Drop live-file fingerprint verification.** Identity binding = (a) InstallationId vs `local_databases.installation_id`, (b) ReplicaId checked vs cloud registry on every sync (already designed), (c) SQLCipher passphrase required to open. Keep fingerprint **only for immutable backup artifacts**; optional HMAC-of-manifest if tamper evidence is wanted.

## ADR-3 — Backup ≠ Replica, Explicit Activation
**Status: NEEDS APPROVAL · Specialist rec: KEEP WITH ADJUSTMENTS. [A §ADR-3] [Sec F-40..F-45] [Sync F8.2] [Acct]**

- **Proposed:** Option 2 — separate states; restore enters `RestoredPendingActivation`; activation forges a new identity (LocalDatabaseId/ReplicaId/InstallationId); unique ReplicaId; admin approval.
- **Alternatives:** Backup=Replica; auto-activation. **Ignored:** Neon PITR/branch restore for cloud-side; unified restore/device-pairing flow; dual approval for sync-enabling activation.
- **Pros:** prevents auto-sync of stale restored data; strong recovery story; audit trail. **Cons:** admin intervention; slower recovery; complex 7-step activation; two near-identical state tables.
- **Impact:** recovery UX changes; identity regen cost.
- **Security risks:** backup key adjacent to backup (theft ⇒ no confidentiality); activation approver singleton (abuse window); **restore inherits pending outbox ⇒ re-push of acknowledged events** (retention gap); backup content readable pre-activation; single-person cross-tenant restore possible (F-42).
- **Ops cost:** high (identity regen, conflict matrix, tests).
- **Fit:** right shape for a local-file DB world that doesn't exist yet; for Neon, the restore problem is largely branch/PITR.
- **Verdict: KEEP WITH ADJUSTMENTS.** **Adjusted:** keep backup≠replica + activation-creates-new-identity as principles. Scope the machinery to local DBs (post-ADR-11 decision). For cloud/Neon use branch-based PITR verification restores. Unify the 3 state tables (offline-row inconsistency). **Dual approval to activate a backup into a syncing replica**; restore path must quarantine the inherited outbox + require financial-diff review before first sync (**currently absent**); idempotency-key retention ≥ backup retention; encryption-by-default with tenant-bound keys.

## ADR-4 — No Master Password; Emergency Access with Audit
**Status: APPROVED (unchanged). Specialist rec: KEEP (approved as-is). [A §ADR-4] [Sec A1/F-55]**

- **Proposed (APPROVED):** no backdoor; emergency access = reason + duration (≤24h) + audit; no data modification without explicit permission.
- **Alternatives:** master password (rejected — backdoor by definition ✓); no emergency access (unrecoverable product). **Ignored:** break-glass dual control; server-side auto-revocation; alert fan-out.
- **Pros:** minimal surface; auditable; compatible with zero-trust. **Cons:** single-operator trust; audit can be paper. **Risks:** no text on who sees the alert, which channel, what latency ⇒ quiet backdoor with paperwork.
- **Impact/fit:** none today; correct shape for financial.
- **Verdict: KEEP.** *Future revision recommended (NOT applied, status untouched): dual-approval on financial tenants; server-enforced 24h revocation; alert fan-out to tenant admin + security channel.*

## ADR-5 — Sync: Outbox + Idempotency + Versioning
**Status: NEEDS APPROVAL · Specialist rec: KEEP WITH ADJUSTMENTS. [A §ADR-5] [Sync §Overall] [Acct §4] [Sec F-45] [CR §B.2]**

- **Proposed:** outbox table + EventId idempotency ("reuse existing `idempotency_keys`") + `version` column on every tenant table.
- **Alternatives:** direct sync; full event sourcing; **buy PowerSync/ElectricSQL/WatermelonDB (never mentioned)**.
- **Pros:** reliable delivery; no duplicates on retry; conflict detection; additive simplicity. **Cons (admitted):** complexity; outbox growth; write overhead. **Unadmitted:** stock-sum rule; invoice numbering undefined (open parenthetical, no ADR); wall-clock ordering under clock skew; no backoff ceiling; no retention; 100-row all-or-nothing vs non-blocking contradiction; "version>cloud → apply" defeats lineage detection; **per-tenant stock/balance must be derived, not added**.
- **Impact:** requires ADR-11; Version NOT NULL backfill on 68 tables.
- **Security:** new sync attack surface; unsigned payloads; financial data at rest in outbox (ties to ADR-11).
- **Ops cost:** second-highest; monitoring/alerting mandatory; Fly.io single shared-cpu machine, no HA worker; Neon autosuspend vs poller.
- **Fit:** correct spine; unimplementable before ADR-11 + numbering + stock-derivation decisions.
- **Verdict: KEEP WITH ADJUSTMENTS.** **Adjusted:** keep the spine; (a) sync **movements/events only** — derive stock/balances (never additive-sum derived values); (b) per-tenant number allocation (server-reserved blocks or tenant prefix) — **new ADR required**; (c) HLC/lamport or server-arrival ordering — drop wall-clock primary ordering; (d) outbox retention (ack+Nd purge) + backoff ceiling + dead-letter; (e) **per-event commit, not whole-batch rollback** — failed events park + alert; (f) rejected-on-revocation ⇒ **quarantine + admin review, never silent reject**; (g) ADR-5 appendix: buy-vs-build on PowerSync/ElectricSQL; (h) financial document = one atomic event/envelope or saga; **sync idempotency in a dedicated table, tenant-scoped** — do not reuse `idempotency_keys`.

## ADR-6 — Conflicts: Manual Financial / LWW Reference / Sum Stock
**Status: NEEDS APPROVAL · Specialist rec: KEEP WITH ADJUSTMENTS. [A §ADR-6] [Sync F5] [Acct §5] [QA]**

- **Proposed:** Option 3 — manual for financial, LWW for reference, additive for stock.
- **Alternatives:** LWW-all (rejected — loses money data); manual-all (impractical). **Ignored:** field-level three-way merge; auto-merge with per-field flags; quorum review for high-value docs.
- **Pros:** right domain instinct — humans own money. **Cons:** "admin may not review promptly" (unmitigated; no SLA/escalation). **Table-level coverage absent** — ~10 of 60 covered; ~50 inherit nothing; `journal_entries/journal_lines` absent from every conflict table; `parties.opening_balance` exposed to LWW; `accounts` unassigned; financial boundary criterion never written; BACKUP §7.1 "local>cloud ⇒ apply" executes **before** the manual-review sentence.
- **Stock rule defective everywhere:** sum-of-nothing; `stock_levels` absolute vs `inventory_movements`; oversell impossible to express; `stock_levels` Version + additive strategy is contradictory.
- **Ops cost:** medium; depends on review discipline; needs `sync.conflicts.*` permission keys (none defined) + conflict UI (doesn't exist).
- **Fit:** right instinct; under-specified; defective stock rule.
- **Verdict: KEEP WITH ADJUSTMENTS.** **Adjusted:** keep manual for financial — **extend to ALL financial tables** (lines, payments, invoice_payments, journals + lines, expenses, transfers, loans/installments, petty cash, salaries, bad-debt, fixed assets, budgets, cost_history, inventory_movements, stock_*, warehouse_transfers) with a written classification criterion (*monetary/legal/audit effect ⇒ manual*). Replace LWW-reference with **field-level merge + explicit notification**, fallback LWW confined to `settings`. **Replace sum-stock with movements-synced derived levels.** Add conflict SLA + stale-conflict alert + bulk path; define `sync.conflicts.view/resolve` permissions + tests; canonicalize the four duplicated rule tables; restore-path financial review mandatory; prohibit "merge" for financial docs (or mandate full re-derivation).

## ADR-7 — Offline Auth, 72h Default Configurable
**Status: NEEDS APPROVAL · Specialist rec: KEEP WITH ADJUSTMENTS. [A §ADR-7] [Sec F-34..F-37] [Sync F6]**

- **Proposed:** default 72h validity, configurable per tenant; warn at 12h; re-verify on reconnect.
- **Alternatives:** no offline access (rejected); fixed 72h (rejected for flexibility). **Ignored:** 24h default with opt-up; monotonic lease (server-signed expires_at floor); hardware-bound offline key.
- **Critical flaws unaddressed anywhere:** device clock rollback (no threat entry T34); admin kill-switch (hours=∞ / ReverificationRequired=No, no floor/cap/dual approval); parameter tables disagree ×3 docs (MULTI 4 · SESSION 3 · OFFLINE 6 · ADR-7 1); suspension semantics contradictory (OFFLINE §4 ❌ vs MTA §4.1 ✅); grace 30min undefined; revoked-on-reconnect ⇒ silent destruction of queued financial work.
- **Impact/fit:** zero value today (product is cloud-only); correct feature later; 72h on a stolen, unencrypted laptop is long.
- **Security risks:** see [Sec audit F-34/F-35]; **the primary enforcement is a writable wall-clock — unbounded bypass**.
- **Ops cost:** low (once honest controls exist).
- **Fit:** keep direction; harden controls.
- **Verdict: KEEP WITH ADJUSTMENTS.** **Adjusted:** keep configurable; **default 24h** (tenant opt-up to 72h with signed acknowledgment); **server-signed expiry + persisted monotonic `last_server_time`** — offline session invalid if device time < last known server time; **hard cap 168h; ReverificationRequired non-disableable (or dual-approval to weaken)**; clock-rollback added to the threat model (T34); offline financial writes capped (no payouts/transfers offline regardless of window); unify the parameter table in ONE doc; revoked-on-reconnect ⇒ quarantine + review — never silent reject; define write/read scope for the 30-minute grace.

## ADR-8 — Soft Delete for Financial Records
**Status: NEEDS APPROVAL · Specialist rec: KEEP WITH ADJUSTMENTS. [A §ADR-8] [Acct §1.3/§6] [DB]**

- **Proposed:** IsDeleted/DeletedAt/DeletedBy on financial tables; no hard delete; sync propagates.
- **Alternatives:** hard delete (rejected — compliance); archive tables (rejected). **Ignored:** use existing `status='cancelled'` + filtered views as the read layer; **do NOT soft-delete inventory_movements (append-only ledger)**.
- **Cons (unadmitted):** **every query + the 5 reporting views must filter IsDeleted=FALSE** — views not covered by any doc; ~100 raw-SQL sites unbudgeted (~94 desktop files); **accounting reports silently wrong post-migration**; formulas change; no retention/purge; pre-sync hard-deleted rows can't be resurrected anyway.
- **Impact:** **largest correctness risk in Phase 3** — absent from MIGRATION_STRATEGY entirely.
- **Security risks:** LWW undelete of reference tombstones; resurrected deleted docs on restore (F-56).
- **Fit:** right concept; must be engineered, not just declared.
- **Verdict: KEEP WITH ADJUSTMENTS.** **Adjusted:** soft-delete only non-cancellable financial records; keep `status='cancelled'` where already used; **mandatory migration phase to recreate the 5 reporting views + grep-based raw-SQL audit (~100 call sites) with IsDeleted=FALSE**; global EF query filter if EF remains on the write path; retention policy; **do NOT soft-delete inventory movements**; soft-delete dominates conflicts and LWW; restore-of-financial-delete disallowed or mandatory full reversal; formulas amended to state IsDeleted=FALSE.

## ADR-9 — Additive Migrations Only
**Status: APPROVED (unchanged). Specialist rec: KEEP. [A §ADR-9] [DB §4]**

- **Proposed (APPROVED):** additive-only; no destructive changes; correct default for production accounting DBs.
- **Tensions:** MIGRATION §4.1 rollback **drops** columns/tables (appropriate for rollback; never labeled as deliberate exception); §2.3 default-local-DB uses placeholder fingerprint `'default-fingerprint'` (**a fake Active row; if ever synced, it corrupts primaries** — must be revoked/bootstrap state, never synced); §2.4 assigns ALL rows to it (single-writer assumption).
- **Ops/fit:** correct and stays correct.
- **Verdict: KEEP.** (Noting: the rollback phase is a rollback exception, not a violation — it must remain documented as reversible-only, backed by verified backup. No status change.)

## ADR-10 — No Production Migration Without Approval
**Status: APPROVED (unchanged). Specialist rec: KEEP. [A §ADR-10] [DB §4] [CR §D]**

- **Proposed (APPROVED):** manual migration by explicit approval; no auto-migrate on startup.
- **Alternatives:** auto-migrate (rejected ✓); migration tool with approval step (rejected as "overkill" — weak rejection; a checked-in runner + dual sign-off gives near-zero-cost auditability).
- **Risks:** forgotten migrations → drift; **no CI gate compares deploy/migrations vs live Neon** (P0–P7 applied ad hoc; P4 half-failed silently — dump evidence).
- **Fit:** correct for this team.
- **Verdict: KEEP.** (Recommended future revision — NOT applied: migration runner + dual sign-off + deployment/migration freshness check; statuses stay APPROVED.)

## ADR-11 — SQLite + SQLCipher for Local DB
**Status: NEEDS APPROVAL · Specialist rec: RECONSIDER (defer until ADR/Topology decision). [A §ADR-11] [CR §F] [Sec F-17]**

- **Proposed:** Option 2 — SQLite + SQLCipher — citing lighter footprint for single-user and **falsely** "Consistent API (Npgsql → Microsoft.Data.Sqlite)".
- **Alternatives:** Option 1 local PostgreSQL (highest dialect fidelity; rejected without measurement); Option 3 LocalDB (rejected correctly). **Ignored crucially:** buy PowerSync/ElectricSQL/WatermelonDB; topology decision first (missing topology ADR — ADR-11 silently mandates local-first + outbox + removal of the desktop direct-Neon path); mobile offline (PWA cannot host SQLCipher; never claimed); fleet cutover plan.
- **Environment reality:** whole WPF codebase is PostgreSQL dialect (NOW(), ::date casts, JSONB, gen_random_uuid, ILIKE, 5 views); **~94 raw-SQL files with no repository seam**; EF Core dual providers Npgsql/SQLite unbudgeted.
- **SQLCipher specifics:** requires SQLitePCLRaw.bundle_e_sqlcipher; keys in OS keychain; rotation drill undefined; not applicable to the PWA.
- **Security:** file at-rest encryption needed either way. Option 1 gives no file-level encryption (needs pgcrypto or filesystem EFS/BitLocker); both options need passphrase policy.
- **Ops cost:** Option 2 = rewrite + support burden; Option 1 = heavier install/service management on every client PC.
- **Fit:** cannot decide before topology.
- **Verdict: RECONSIDER (do NOT approve).** **Adjusted:** **DEFER this ADR.** File the missing topology ADR (direct-Neon removal vs API-mediated vs local-first) with cost + fleet cutover plan; then evaluate in order: (1) **PowerSync/ElectricSQL (buy)**; (2) SQLite+SQLCipher (build, after a full SQL-dialect audit + rewrite budget); (3) local PostgreSQL (max parity, highest ops, kills file fingerprint — acceptable). Decide mobile offline separately. ADR-5 stays blocked until this resolves.

## ADR-12 — Tenant Selection at Login
**Status: NEEDS APPROVAL · Specialist rec: KEEP WITH ADJUSTMENTS. [A §ADR-12] [Sec F-06]**

- **Proposed:** Option 2 — dropdown on the login screen (before entering the app).
- **Alternatives:** single tenant per user (rejected — excludes legitimate multi-tenant); selection after login (rejected). **Ignored:** conditional dropdown (auto-select when count==1); remember-last-tenant; in-app switch re-minting the JWT; block entirely when device status ≠ Active; reject before mint when pending.
- **Pros:** explicit context; less wrong-tenant waste. **Cons:** every current customer is single-tenant (100% of the user base pays the cost); dropdown alone doesn't solve wrong-tenant actions (title-bar company name does — already specified); no pending-device rule; header↔JWT tid binding unspecified.
- **Ops cost:** minimal either way.
- **Fit:** adjust the UX, keep the principle.
- **Verdict: KEEP WITH ADJUSTMENTS.** **Adjusted:** zero extra step for single-membership users (auto-select); dropdown only when ≥2 active memberships; persist last-used; in-app switch re-mints the token; title bar always shows the active company (already partially present); block selection while device pending; **tid claim enforced per-request server-side** (pairs with ADR-1 adjustment).

---

## Coupling map (read before approving anything)

- **ADR-11 → ADR-5 → ADR-7** — all blocked on the missing topology ADR.
- **ADR-1 → topology ADR** (desktop must not hold Neon credentials).
- **ADR-2 ↔ ADR-11** — fingerprint design is unimplementable under Option 1/PostgreSQL; decide together or drop fingerprint.
- **ADR-8 → MIGRATION_STRATEGY** — missing phase for the 5 views + raw SQL audit.
- **ADR-6 → classification criterion doc** — missing for ~50 tables.
- **No ADR exists for** (a) topology, (b) reporting-view + soft-delete read strategy, (c) per-tenant invoice/payment numbering, (d) inventory/stock sync semantics, (e) idempotency/retention policy, (f) backup key escrow/recovery. **These must be filed before Phase 3 sign-off.**

## Status discipline

| ADR | Status (unchanged) | Specialist recommendation |
|---|---|---|
| ADR-1 | NEEDS APPROVAL | KEEP WITH ADJUSTMENTS (gated) |
| ADR-2 | NEEDS APPROVAL | RECONSIDER |
| ADR-3 | NEEDS APPROVAL | KEEP WITH ADJUSTMENTS |
| ADR-4 | APPROVED | KEEP |
| ADR-5 | NEEDS APPROVAL | KEEP WITH ADJUSTMENTS |
| ADR-6 | NEEDS APPROVAL | KEEP WITH ADJUSTMENTS |
| ADR-7 | NEEDS APPROVAL | KEEP WITH ADJUSTMENTS (24h default) |
| ADR-8 | NEEDS APPROVAL | KEEP WITH ADJUSTMENTS |
| ADR-9 | APPROVED | KEEP |
| ADR-10 | APPROVED | KEEP |
| ADR-11 | NEEDS APPROVAL | RECONSIDER (defer) |
| ADR-12 | NEEDS APPROVAL | KEEP WITH ADJUSTMENTS |

**No status was changed in this review. Approvals are a user decision delivered through the sign-off path (TECHTOON_PHASE_2_USER_APPROVAL_GUIDE.md).**

*Report 2 of 5 · TECHTOON Phase 2 · 2026-11-16*
