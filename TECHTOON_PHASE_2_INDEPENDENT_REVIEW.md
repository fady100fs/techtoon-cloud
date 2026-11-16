# TECHTOON_PHASE_2_INDEPENDENT_REVIEW.md

> Date: 2026-11-16 · Review type: Independent architecture / security / database / sync / accounting / QA / feasibility audit of Phase 1 & Phase 2 outputs · Mode: READ-ONLY (no source, DB, config, or design file was modified) · Method: OpenCode multi-agent orchestration (primary agent → `techtoon-orchestrator` subagent → 7 specialist agents in two parallel waves).
>
> Specialists invoked: techtoon-architect · techtoon-database · techtoon-security · techtoon-sync · techtoon-accounting · techtoon-qa · techtoon-code-review — **all 7 completed, none failed.**
>
> Status key used below: PASS / FAIL / NOT VERIFIED / BLOCKED / REQUIRES APPROVAL. "INFERENCE" = deduction, not direct verification. Citations use each specialist's finding IDs ([A]=architect, [DB]=database, [Sec]=security, [Sync]=sync, [Acct]=accounting, [QA]=QA, [CR]=code-review).

---

## 0. Executive Summary

**Verdict: FAIL — Phase 2 is NOT approved for Phase 3.** The multi-tenant design is a coherent *target architecture sketch*, but it is (a) unimplementable as written on the current codebase, (b) unsafe at its highest-risk seams (restore, offline clock, key management, RLS), (c) materially inaccurate in factual counts and cross-document tables, and (d) 0% implemented in every high-risk category. Overall score: **38 / 100** (see TECHTOON_PHASE_2_FINAL_SIGNOFF.md).

The current product (single company, WPF + ASP.NET Core API + Neon PostgreSQL) works today. The Phase-2 multi-tenant program is effectively a **new product requiring a re-architecture**, and it was presented as a documentation exercise with a self-granted "PASS (Design Complete)" sign-off in `TECHTOON_PHASE_2_FINAL_REPORT.md` line 151.

---

## 1. Files Under Review — Verification

All 19 required documents exist, are tracked in git, and were read by at least one delegated specialist:

| # | File | State |
|---|---|---|
| 1 | TECHTOON_DISCOVERY_REPORT.md | reviewed |
| 2 | MULTI_TENANT_ARCHITECTURE.md | reviewed |
| 3 | LOCAL_DATABASE_ISOLATION.md | reviewed |
| 4 | LOCAL_USER_ACCESS_CONTROL.md | reviewed |
| 5 | DEVICE_SECURITY_DESIGN.md | reviewed |
| 6 | SESSION_SECURITY_DESIGN.md | reviewed |
| 7 | BACKUP_AND_REPLICA_POLICY.md | reviewed |
| 8 | OFFLINE_AUTHORIZATION_POLICY.md | reviewed |
| 9 | SYNC_ENGINE_ARCHITECTURE.md | reviewed |
| 10 | TENANT_SECURITY_ARCHITECTURE.md | reviewed |
| 11 | ARCHITECTURE_DECISIONS.md | reviewed (12 ADRs) |
| 12 | PHASE_2_REVIEW_CHECKLIST.md | reviewed |
| 13 | DATABASE_TENANT_CLASSIFICATION.md | reviewed |
| 14 | DATABASE_SCHEMA_PROPOSAL.md | reviewed |
| 15 | MIGRATION_STRATEGY.md | reviewed |
| 16 | SECURITY_THREAT_MODEL.md | reviewed |
| 17 | ACCOUNTING_INTEGRITY_DESIGN.md | reviewed |
| 18 | MULTI_TENANT_TEST_STRATEGY.md | reviewed |
| 19 | TECHTOON_PHASE_2_FINAL_REPORT.md | reviewed |

Additional consulted artifacts: `deploy/migrations/schema_full.sql`, `deploy/migrations/P7_permissions.sql`, production dump/migration scripts (P0–P7), `Techtoon.Api` JWT/Idempotency middleware, `Techtoon.UI/Services/{AuthService,BackupService,RoleService,InvoiceAppService}`, `Techtoon.Mobile/pubspec.yaml`, `Techtoon.Streamlit` DB connection code, test suite inventory.

No conflicting copies of these design documents exist in the repository. Note: several of the original Phase-2 docs are untracked-but-present in earlier stages were verified tracked (git ls-files) for all 19.

---

## 2. Architecture Review (techtoon-architect)

### 2.1 The keystone finding: Desktop↔Neon direct connection contradicts the documented design
Documents depict `Desktop → API → Neon` (`TECHTOON_PHASE_2_FINAL_REPORT.md §4.1`; `MULTI_TENANT_ARCHITECTURE.md §1.1, §8.7, §10.3` "No Neon connection string in client"). Reality:
- `Techtoon.UI` opens raw `NpgsqlConnection` in ~94 files (~341 `new` call-sites; ~669 raw SQL strings; DPAPI-encrypted Neon credential).
- `Techtoon.Streamlit` connects **directly** to Neon via `psycopg2` (`app.py`, `pages/cash_boxes.py`) — **documented in no Phase-2 file**.
- `Techtoon.Api` has **zero** references to `TenantId`/`tenant_id`.
- `TENANT_SECURITY_ARCHITECTURE.md §5.1` itself lists "Desktop DB password | DPAPI-encrypted" — security doc contradicts architecture docs and reality.
- **RLS is thereby defeated by construction**: an Npgsql connection never sets `app.current_tenant_id`, so RLS yields 0 rows (or a permissive policy would expose all tenants). [A §1.1, §1.2; CR §A(4)]

### 2.2 The design is greenfield for a single-tenant codebase
Tenants/memberships/local-DBs/outbox tables: 0 occurrences in `schema_full.sql` and 0 in `Techtoon.Api`. The "33–53 day" estimate is not credible against ~94 raw-SQL files with no repository seam. Architecture is defensible for a future hosted SaaS; **over-scoped for the current single-company deployment; the mismatch is neither quantified nor gated anywhere.** [A §1.2; CR §E]

### 2.3 Scalability posture
Shared-DB per-row tenancy + ~100 new FKs + ~200 indexes on 86 tables is workable only for a handful of tenants; current integrity is app-level (4–5 FKs, 0 RLS). [A §1.3]

### 2.4 Duplicated / drifted designs (verified)
- audit_trail DDL ×3; sync_outbox/conflicts/log DDL ×2–3; local_users DDL ×3; local_databases ×3; 15-step migration sequence ×3.
- Conflict-resolution rule table ×4 — content drifted (only SYNC/MULTI include the Settings-LWW exception and Accounting-period-manual rule; BACKUP is prose).
- Offline parameters ×4 (MULTI §7.1: 4 params; SESSION §4.2: 3; OFFLINE §2: 6; ADR-7: 1).
- Backup state machine ×3 (MTA §4.1: 7 states incl. Offline; BACKUP §1.1: 6; LOCAL_DATABASE §6.1: 6).
- Fingerprint formulas ×2 different (`LOCAL_DATABASE_ISOLATION §1.2` vs `§2.5`) — see ADR-2.
- Threat models ×2 overlapping (`TENANT_SECURITY §1`: 14 threats vs `SECURITY_THREAT_MODEL.md`: 33, different totals); sessions vs local_sessions naming; SI- test ID collision. [A §3–4]

### 2.5 Cross-document factual errors (representative, verified)
- FINAL_REPORT: "17 design documents" vs its own 18-file index; "12 ADRs (4 approved, 8 need approval)" vs `ARCHITECTURE_DECISIONS.md` (3 APPROVED / 9 NEEDS APPROVAL); "60 tenant-owned tables" (§4.1) vs "TenantId on all 86" (§5).
- MULTI_TENANT §12 lists only 10 ADRs.
- Classification: claims "86 tables classified" yet omits 9 real tables, double-counts 2; arithmetic nets 84/77 unique.
- PROPOSAL: "New Tables (10)" heading lists 13; DISCOVERY says 10; FINAL_REPORT says 13; §2.1 `ALTER TABLE invoices ADD created_at` fails (column exists, `schema_full.sql:783-784`); "Modified tables 86" in the same doc as "all 60".
- MIGRATION M2 says "all 86" while PROPOSAL §2.1/§3.2 verify 60.
- `sessions refresh_token` stored **plaintext VARCHAR(500)** — a security regression vs current hashing (`AuthAppService` HashToken). [A §4 A/B/D/E/J/U]

### 2.6 Secrecy / approval audit
**Exemption C1 — a Neon password embedded in 13 files — is excluded from every remediation list, ADR, and approval point in Phase 2.** MULTI §10.3 ("No Neon connection string in client") contradicts `TENANT_SECURITY §5.1` and reality. [A §4-K; Sec §9; CR §B.9]

### 2.7 Architecture strengths (acknowledged, not sufficient)
ADR-9/10 additive defaults; ADR-3 backup≠replica concept; ADR-6 manual-for-financial instinct; discovery report's risk realism; the classification attempt (which exposed the 60%/68-real split). [A §7]

---

## 3. Database Review (techtoon-database)

### 3.1 The 85 vs 86 difference — RESOLVED
- `deploy/migrations/schema_full.sql` contains **exactly 85 `CREATE TABLE IF NOT EXISTS`** (1674 lines, direct count), **5 views** (lines 1601, 1609, 1619, 1640, 1659), **84 sequences, 85 PKs, 4 FKs** (lines 1595–1598), **0 unique/check constraints, 142 `timestamp without time zone`, 0 TenantId/is_deleted/version columns**.
- **86 = the live `information_schema` BASE TABLE count = 85 dump + `role_permissions`** (created by `P7_permissions.sql:23` and re-created defensively by `RoleService.EnsureSchema()` at startup — `Techtoon.UI/Services/RoleService.cs`). `_efmigrationshistory` exists nowhere (no EF migrations) — never counted, live absence NOT VERIFIED.
- Classification's "86" is a **third, fictitious number**: 86 rows → 84 unique → 76 real + 8 proposed-NEW (audit_trail, local_memberships, local_sessions, local_users, role_permissions, sync_conflicts, sync_log, sync_outbox). Docs mix 85 and 86 interchangeably (CLOSED_LOOP/OPENCODE reports use 85; Phase-2 docs use 86). [DB §1]

### 3.2 Classification is materially wrong
**0 of 85 tables have TenantId.** Decomposition: 59 unique tenant-owned rows (after removing duplicates `custody_increase_requests` #30/#52 and double-classified `expense_categories`) + 7 real reference rows + 5 system-global (roles, users, user_permissions, app_settings, reporting_settings) + 1 local (user_devices) + 1 sync (idempotency_keys — semantic collision: HTTP-scope, not local-sync) + 3 audit (app_audit_logs, audit_log, login_attempts) = **76 real tables + 9 real tables entirely omitted** (`bank_reconciliations, bank_statement_import_logs, bank_statement_import_templates, bank_statement_lines, bank_statements, inventory_movements, reconciliation_adjustments, reconciliation_matches, shift_closings` — all tenant business data, read by `v_stock_balance`/`v_party_ledger`). **True tenant-scoped count ≈ 68 tables**, not 60, not 86. Proposed-NEW tables are entirely unclassified in the doc. [DB §2.2–2.3]

### 3.3 Migration strategy — not safe as written
- P4_loans_closed_loop.sql FKs are absent from the dump — consistent with its DDL having **failed on live DBs** (orphan rows) while psql continued — **live FK count NOT VERIFIED (5–8 possible)**.
- M4/M7 NOT NULL take ACCESS EXCLUSIVE without NOT VALID→VALIDATE / CREATE INDEX CONCURRENTLY.
- M11 "Add FK constraints" will very likely abort on orphans (P4 precedent).
- **M13 (RLS) = certain outage** — `app.current_tenant_id` appears 0 times in code; `current_setting(...)` without `missing_ok` hard-errors; no FORCE RLS; desktop connects directly.
- PROPOSAL DDL is not idempotent and not executable as written (`invoices.created_at` already exists).
- Rollback §4.1 is lossy (drops tenant_id/version/is_deleted/deleted_*) and contradicts `MULTI_TENANT_TEST_STRATEGY.md DM-5` ("rollback… data restored").
- MIGRATION_STRATEGY never references `BACKUP_AND_REPLICA_POLICY.md` verification gates; dump mandated but backup verification is never gated into M-phases; no downtime/lock-timeout plan; `techtoon_test` migration would violate the doc's own approval gates.
- Both `neondb` and `techtoon_test` would get **different "Default Tenant" GUIDs** — silent replica fork. [DB §4; CR §D]

### 3.4 Integrity today
4 FKs in dump (`subscription_periods→invoices SET NULL`/`→payments SET NULL`/`→subscriptions CASCADE`; `subscriptions→parties RESTRICT`) + `role_permissions_role_id_fkey` (P7) = "5 FKs" cited everywhere. No unique constraints in dump → duplicate prevention (invoice_number) unenforceable at DB level. [DB §3.2]

---

## 4. Security Review (techtoon-security)

### 4.1 Tenant isolation — NOT ADEQUATE as designed (do not approve ADR-1 as written)
- **F-01 (Critical):** the only RLS example (`TENANT_SECURITY §3.3`, `DATABASE_SCHEMA_PROPOSAL §2.5`) uses `USING(...)` with **no `WITH CHECK`** → INSERTs not tenant-checked → cross-tenant write path RLS does not stop.
- **F-02 (Critical):** no `FORCE ROW LEVEL SECURITY`; no dedicated non-owner role; if the app connects as table owner (current pattern), RLS is silently void.
- **F-03 (Critical):** pooled connections + session-level `SET app.current_tenant_id` bleed context across borrowers; transaction-scoped `set_config(..., true)` never specified. Neon pooler caveat never addressed.
- **F-04 (High):** RLS shown for `invoices` only; 8 "global reference" tables (`currencies, categories, …, roles/role_permissions/users/user_permissions`) get optional/NULL TenantId → NULL-invisible break or `OR tenant_id IS NULL` making the **permission system itself visible cross-tenant**.
- **F-05 (High):** the 5 views/triggers/reporting unverified for tenant filtering (no `security_barrier`, no filter spec).
- **F-06 (High):** TenantId source ambiguity — `TENANT_SECURITY §3.1` "from request header/body" vs `SESSION §3.3/§5.1` JWT `tid` vs `MULTI §6.2` post-JWT selection. A multi-tenant user may act on any tenant via header — **no doc states which wins or enforces equality with `tid`**.
- **F-07/F-08 (Med):** JWT alg confusion risk (no alg pinning/iss/aud/rotation); `perms` baked at issuance; revocation kills sessions but permission changes don't reload per request. [Sec §1]

### 4.2 Local DB isolation — NOT ADEQUATE as designed
- **F-13 (Critical):** **DatabaseFingerprint is incoherent for a mutable file** — `§1.2` vs `§2.5` give two different formulas; SQLite/PG file size changes on every write; creation-time unstable under copy; never states where the reference fingerprint lives for offline open; attacker-computable inputs → checksum, not secret; **false-lockout guaranteed on routine writes**.
- **F-14 (High):** encryption at rest is "optional" today (`BACKUP §3.1` says "if enabled"; `LOCAL_DATABASE §7.4` says ALL backups; reality: plaintext dumps verified in code).
- **F-15 (High):** key storage contradictions — "Stored in OS keychain" vs "Key derived from password + device fingerprint" in the same sentence; KDF from public device properties ≈ zero entropy beyond password; SQLCipher PBKDF2/Argon2 parameters unspecified; DPAPI = same-user decrypt.
- **F-16 (High):** every identity factor (LocalDatabaseId, TenantId, InstallationId, fingerprint, session tokens) is a copyable file — **full clone = a second working replica inside the 72h offline window**.
- **F-18 (Med):** outbox events carry no signature/MAC — stolen unlocked laptop pushes forged financial documents under victim identity on reconnect. [Sec §2]

### 4.3 Device identity — INADEQUATE
- **F-20 (High):** fingerprint = SHA-256(MachineName+OS+CPU+MAC?+InstallationId) — public, low-entropy, settable; no secret/TPM/Secure Enclave.
- **F-21 (High):** **no per-device random credential at enrollment anywhere** — "device spoofing (T22/T11) mitigation is ceremonial".
- **F-22 (Med):** no re-enrollment/drift flow — OS patch changes OSVersion → mass false lockout.
- **F-23 (Med):** single admin approval enrolls a rogue device — no dual control.
- **F-24 (Med):** `device_tenant_links` — "Link permissions override device permissions" unexplained; no target-tenant countersign. [Sec §3]

### 4.4 Sessions — PARTIALLY ADEQUATE
Online model strong on paper (15-min access + 7-day single-use rotating refresh + reuse→revoke-all + device binding). Gaps: **F-27 (Med)** concurrent-refresh race → false-positive mass revocation (no grace window); **F-28 (Med)** tokens stored **plaintext** (`SESSION §1.1`) — security regression vs current hashing; **F-29 (Low)** sessions table location unspecified and absent from Phase-2 new-tables list. [Sec §4]

### 4.5 Offline Authorization — INADEQUATE FOR WRITE PRIVILEGE
- **F-34 (Critical):** **clock rollback defeats the 72h window completely** — expiry enforced against device wall clock; `last verification timestamp` stored in client-writable state; no monotonic clock, no secure floor, no rollback lock — not even in the threat model (no T34).
- **F-35 (High):** `OfflineValidityHours`/`ReverificationRequired` are tenant-configurable with **no floor/cap/dual approval** — one admin sets hours→∞ and kills the only revocation-propagation control.
- **F-36 (Med):** `OfflineGracePeriodMinutes=30` semantics undefined (writes? reads?).
- **F-37 (Med):** revoked user keeps working ≤72h; pending transactions rejected on reconnect with **silent destruction** — "rejected" recoverability NOT VERIFIED. [Sec §5]

### 4.6 Backups — NOT ADEQUATE / NOT VERIFIED
- **F-40 (High):** backup encryption "if enabled" vs "ALL backups encrypted" — contradiction; reality: plaintext.
- **F-41 (Critical):** **no key-loss/escrow/recovery anywhere** — keys in this-machine keychain; laptop dies ⇒ all backups undecryptable (contradicts no-data-loss principle); threat model lacks key-loss entirely.
- **F-42 (Critical):** **no binding between backup TenantId and restoring tenant** — admin with another tenant's `.enc` file + key (or key-adjacent access) can restore it; single biggest cross-tenant path.
- **F-43 (Med):** fingerprint = integrity, not authenticity.
- **F-45 (High):** stale-restore replay bounded only by `idempotency_keys` retention — **retention unspecified**; stock=additive ⇒ double-counted inventory; LWW resurrects deleted rows.

### 4.7 Cross-tenant paths (15 enumerated; open ones)
missing RLS/WITH CHECK/FORCE; forged-JWT alg confusion; header-vs-JWT tid; NULL-TenantId globals; reports/exports; backup cross-tenant restore (F-42); stale replica restore (F-45); **emergency access read = single System Owner, audit-only, no tenant consent (F-55)**; multi-tenant device links; conflict-LWW replay; rogue-device enrollment; **F-56 (Med):** restored stale DBs resurrect soft-deleted rows. [Sec §8]

### 4.8 Threat-model arithmetic wrong / completeness fails
Claimed 5 Critical / 12 High / 10 Medium / 3 Low vs actual **8 Critical / 15 High / 10 Medium / 0 Low** (both in `SECURITY_THREAT_MODEL §2/§6` and `FINAL_REPORT §2.2/§4.3`). **Zero Repudiation and zero DoS threats; 6 of 18 critical scenarios unmodeled** (cross-tenant admin; cloud-admin vs local DB; sync-conflict integrity; migration-phase; restore-replay; key loss). STRIDE structure absent. [Sec §9]

### 4.9 Single-admin approval stack (12 actions, all unapproved-dual)
emergency access read-all; backup restore+activation; offline-policy kill switch; cross-tenant device linking; rogue-device enrollment; god-mode restore permissions; cross-tenant backup decrypt; device reactivation; tenant suspend; superuser audit deletion. **Audit ≠ approval.** [Sec §10]

---

## 5. Sync Engine Review (techtoon-sync)

### 5.1 Paths
`SYNC §1.1` diagram shows `Neon ←→ Mobile`, contradicting every other doc and code (`Techtoon.Mobile api_service.dart` → `https://techtoon-api.fly.dev`). **Desktop bypasses the API entirely (direct Npgsql)** — outbox/push would be dead code. **Mobile sync path undefined** (PWA: no local DB/outbox; `local_database_id NOT NULL` vs mobile devices ⇒ **mobile can never write to the outbox as specified**; receive-only is INFERENCE). No path for Streamlit/managerial Neon writes into outbox. `since=<timestamp>` pull cursor. [Sync §1]

### 5.2 Outbox
- `gen_random_uuid()` = PostgreSQL-only vs SQLite-local (ADR-11) — dialect/placement unstated.
- Atomicity locally GOOD (§10.1). Worker unspecified (lease? single instance? Windows service?); max-100 batch vs all-or-nothing contradiction unresolved; **poison messages: infinite retry, no dead-letter**; retry backoff names parameters never given; no retention/cleanup; UNIQUE has no device_id/replica_id. [Sync §2]

### 5.3 Idempotency
- **"idempotency_keys (existing)" is factually wrong** — table exists (dump:657, PK `key`, `expires_at NOT NULL`, HTTP-scope, 24h TTL, `IdempotencyMiddleware`, claim-before-execute) but **must not be reused for sync EventIds** (divergent semantics, mandatory TTL).
- **No DDL/TTL for sync idempotency anywhere**; no natural keys wired into apply flow ⇒ **double-submit duplicates invoices**; cross-device same-operation undetected; apply+store-key atomicity unspecified. [Sync §3]

### 5.4 Versioning
- Pure counter cannot distinguish descendant from sibling (A applies v6′, B's v6′ sibling overwrites without review — "V > cloud → apply").
- Equal-version "different data" comparator undefined; no row-level CAS (`WHERE version=@expected`); derived balances have no post-sync recalculation. [Sync §4]

### 5.5 Conflict boundary — coverage table wrong
- **Only ~10 of 60 tenant-owned tables have a strategy; ~50 inherit nothing.**
- `journal_entries/journal_lines` — financial — absent from all conflict tables.
- `parties.opening_balance` (balance-affecting) exposed to silent LWW.
- `stock`: "Sum (additive)" unimplementable (stock_levels is absolute; inventory_movements unclassified; no oversell; `stock_levels` carries Version AND additive strategy — mutually exclusive); schema also lacks `version` columns entirely today (M-backfill required).
- **Deletion conflicts unaddressed** (delete-vs-delete; LWW resurrection of deleted customers); conflict pauses only the entity — no storm cap/SLA; "merge" offered for financial with zero semantics. [Sync §5; Acct §3]

### 5.6 Long-offline recovery
designed: 72h validity; re-verify on reconnect; revoked⇒reject; schema gate; new identity. **Missing:** unbounded unpaginated `since` pull; unbounded 100%-manual conflict storm; no local schema-upgrade path on reconnect; **NO full-resync/bootstrap/snapshot exists** (`sync_type='full'` vestigial); **revoked-user reconnect silently discards queued financial transactions** — violates the project's own no-data-loss rule. [Sync §6]

### 5.7 End-to-end duplicate prevention / financial loss
- Sync-path retries OK only if F3.x fixed; non-sync API writes ✗; double-tap ✗; journal/payment/stock replay ✗.
- **Restore discards the pre-restore outbox ⇒ un-synced confirmed transactions vanish permanently by design** — no export/rescue/warning. [Sync §8]
- Financial aggregates not atomic across outbox events ⇒ invoice can land without journal/COGS; no saga; derived balances never recalculated; numbering collision. [Sync §8; Acct §4]

### 5.8 Overall
Skeleton correct (outbox+idempotency+versioning+manual-financial); **as specified it cannot be implemented safely**; ADR-5/6 correctly motivated, not engineered; do not approve Phase 2 without correcting the listed items. [Sync §Overall]

---

## 6. Accounting Integrity Review (techtoon-accounting)

- **Effects documented locally, never extended to sync.** Sale = Dr AR / Cr Revenue (+tax), Dr COGS / Cr Inventory ✓ (`ACCOUNTING_INTEGRITY §2.1`; matches code). No doc states records push/apply as one atomic unit ⇒ **cloud can apply COGS without revenue, or revenue without COGS**. [Acct §1.1]
- **Duplicate revenue:** transport-level retry dedup PASS-by-design (untested); **document-level FAIL** — no natural keys for journal_entries/journal_lines/inventory_movements/cost_history/COGS; invoice numbering decision open (`ACCOUNTING §5.1` — "or global with tenant prefix" parenthetical, no ADR); `UNIQUE(tenant_id, invoice_number)` never specified ⇒ (TenantId, InvoiceNumber) backstop would reject one device's invoice instead of preventing duplicates. [Acct §1.2]
- **Edit/delete/cancel:** ADR-8 soft delete correct in principle (NEEDS APPROVAL) — but **reversal mechanics unspecified**: does soft-delete post a reversal of journal/COGS/inventory/party-balance effects? Formula `PartyBalance = Opening + Receipts − Payments + Sales − Purchases` never states the `IsDeleted=FALSE` filter; existing code contradicts the design (hard `DELETE` in `InvoiceAppService.DeleteInvoiceAsync`; cancel flips movement type without reversing journals). [Acct §1.3]
- **Weighted-average costing — NOT PRESERVED: Python cumulative-over-history (`services.py`) vs .NET moving-weighted (P1.5, `InvoiceAppService.cs`) are mutually inconsistent; `items` = LWW ⇒ one device's cost update silently discarded; `cost_history` unclassified (no rule/natural key) ⇒ ledger double-count risk**; the design "COGS calculation … Unchanged" claim is unverifiable. **FAIL.** [Acct §3]
- **Financial/reference boundary not drawn at table level:** explicit assignments cover only ~5 entity types; **~35 financial tenant-owned tables unassigned** (journal lines, expenses, loans, salaries, budgets…); **no classification criterion anywhere** (monetary/legal/audit effect?); `BACKUP §7.1` "local > cloud: apply" rule precedes the manual-review sentence — implementer applying it literally auto-applies stale financial payloads. [Acct §5]
- **Delete/restore re-posting — UNSAFE:** pending outbox re-pushed on activation; idempotency retention unspecified; no "restore must reverse the delete" rule; cancelled-document resurrection via straight local>cloud branch. [Acct §6]
- **Overclaims audit — 12 found**, headline: FINAL_REPORT "PASS (Design Complete)" = checklist pass, not integrity verification; AI-13/AI-10 tests only exist as 0%-coverage plans; ADR-5/6/8 are the integrity backbone and all are NEEDS APPROVAL. [Acct §8]

**Verdict: duplicate-revenue transport PASS-by-design / document FAIL · COGS FAIL · weighted-average FAIL · financial-conflict partially sound · delete/restore UNSAFE · mid-flow atomicity UNDEFINED/CONTRADICTORY · Phase 3 NOT APPROVED from accounting-integrity standpoint.** Minimum corrective set: 10 items (see TECHTOON_PHASE_2_FINAL_SIGNOFF.md §Remediation). [Acct §9–10]

---

## 7. QA / Test Strategy Review (techtoon-qa)

- 12 categories / **104 designed cases, 0% implemented**. Tenant isolation TI-1..10 (8 Critical), LDI, UP, SI (session), DA, OA (offline), BR, **sync-idempotency SI-1..5 — collides with Session-Isolation SI prefix**, conflicts SC-1..7, accounting AI-1..15 (13 Critical), migration DM-1..6, recovery R-1..4.
- **Missing test types:** RLS-bypass battery (no SET-LOCAL/NULL/owner-role/pin tests), **key-rotation/loss** (nothing anywhere), **clock-tamper test** (none — defeats the 72h), cross-tenant FK tests, per-tenant numbering tests, restore→replay tests, oversell/negative-stock tests, partial-batch-failure tests, two-device/network-partition harness.
- **The 156 figure is reproducible**: 146 attribute occurrences (144 [Fact] + 2 [Theory]) + 12 InlineData rows ⇒ **156 executable cases**. **Zero tests match tenant|rls|sync|outbox|replica|offline|device|fingerprint.** "156/156 passing" NOT VERIFIED. E2E env (`techtoon_test`) contradicts the TestDb guard (substring `techtoon_test` refuses). ~60 vacuous `if (!TestDb.Available) return;` paths inflate "pass". E2E ~50% / Integration ~70% unverifiable (no coverage artifact). DISCOVERY "36 test files" wrong (actual 27 tracked).
- **Test strategy itself misleads**: §6/§7 claims read as if tests exist. [QA §1–5]

**Verdict: FAIL as a Phase-3 gate; PASS as a baseline design once metrics honestly restated and the missing cases added.** [QA §6]

---

## 8. Implementation Feasibility (techtoon-code-review)

Every one of the 10 required infrastructures is **0% present (verified)**: (1) sync tables/interceptor; (2) tenant JWT claim (`JwtTokenService` emits sub/name/role/device_id/permission — no tid); (3) TenantId columns (0); (4) RLS (0); (5) SQLCipher/SQLite package in any .csproj (9 projects — 0); (6) local/offline user management (desktop = cloud `users` via Npgsql, online-only; **.NET offline hits = 0**); (7) device fingerprint (`user_devices` = refresh-token store only); (8) backup encryption (`BackupService` = plaintext, no AES/RSA/ProtectedData); (9) offline auth (0; `AccountingPeriodGuard` fails **open**); (10) conflict-resolution UI (none). [CR §A]

- **Unsupported assumptions:** desktop is a WPF privileged DB client (~94 files, 669 raw SQL, 91 hard DELETEs vs 10 in API); Streamlit psycopg2 direct; dual paradigm (raw-SQL app services + EF Core controllers) ⇒ no interceptor seam; **EF Core migrations = 0** (raw P0–P7 scripts); "saved by tooling" (ADR-9) false.
- **Breaking-functionality risks:** RLS enable ⇒ desktop instantly locks out; ADR-8 vs existing hard deletes ⇒ silent disappearance; `AccountingPeriodGuard` fail-open worsens offline; two DbContexts (`Techtoon.Data\TechtoonDbContext.cs` empty shell vs registered one) ⇒ tenant filters may land on unused context; `GenerateInvoiceNumberAsync` advisory lock not replay-safe; `SchemaMaintenanceService` self-healing vs tenant sequences. [CR §C]
- **Unstated dependencies:** SQLCipher bundle + EF SQLite provider; JWT rotation + theft-reuse detection absent; Workers/HA on Fly.io single machine; Neon autosuspend vs poller; DPAPI Windows-only; realistic effort **3–6× the 33–53 day estimate**. [CR §E]
- **ADR-11 options verdict:** Option 1 (local PostgreSQL) ≈ lower code-rewrite risk but **makes file-fingerprint design unimplementable** and redistributes PostgreSQL to customer machines. Option 2 (SQLite+SQLCipher) = full SQL-dialect rewrite of ~94 files + dual EF providers; "Consistent API" claim false. Option 3 rejected correctly. **ADR-11 and ADR-2 must be decided together.** [CR §F]
- **25 absent design requirements** (headline §A/CR §G): local-first desktop mode; outbox seam; sync worker + push/pull endpoints; tenant middleware + tid claim; RLS; revocation propagation; devices/QR/UI; fingerprint verification; backup encryption + activation state machine; ReplicaId validation; local users/sessions; conflict UI; audit_trail v2; oversell; tenant-selection UX; cross-DB fixtures; symmetric Pg→SQLite if chosen.

**Verdict: NOT implementable as written on this codebase. Phase 2 is a valid target architecture and an invalid implementation plan for this codebase. Do not progress to Phase 3.** [CR §Overall]

---

## 9. Critical Scenarios — Summary of 18 (detailed in FINAL_SIGNOFF §3)

9 scenarios are CRITICAL-severity, 9 HIGH, none below HIGH: **4 (file copy), 5 (old backup), 6 (long offline), 10 (offline expiry/clock rollback), 11 (mid-flow sync failure), 14 (stock oversell), 16 (cross-tenant), 17 (key loss), 18 (stale replica restore).** Every scenario is 0% implemented; **no scenario is safe today.** QA mapping: COVERED-by-design 6–8, PARTIAL 7, **MISSING 3 (6, 11, 17)**.

---

## 10. Aggregate Findings (Section E of orchestrator report — ranked inventory)

**CRITICAL 20 · HIGH 30 · MEDIUM 25 · LOW 11** (86 register items on file — full register in TECHTOON_PHASE_2_RISK_REGISTER.md).

Headline critical classes:
1. Desktop/Streamlit hold the production Neon credential; Phase-2 isolation is void against it (docs contradict each other — MULTI §10.3 vs TENANT §5.1 — and reality).
2. Exemption C1 (Neon password ×13 files) excluded from all remediation/ADRs.
3. Restore destroys the pre-restore outbox ⇒ un-synced confirmed transactions vanish by design.
4. Financial aggregates not atomic across outbox events.
5. No key-loss/escrow/recovery for backup/local keys.
6. Backups not bound to restoring tenant (F-42).
7. 72h offline window defeated by clock rollback (no defense, no T34).
8. Offline policy admin kill-switch (hours=∞ / ReverificationRequired=No) uncapped.
9. Stock "additive sum" unimplementable; inventory_movements unclassified; oversell silent.
10. ~35 financial tables + inventory_movements + cost_history unassigned strategies; journal_entries absent from conflict tables.
11. RLS missing WITH CHECK / FORCE / non-owner role; pooler bleed; current_setting unset ⇒ lockout or leak — M13 = certain outage.
12. MIGRATION rollout locks/drops (M4/M7/M11) without NOT VALID/CONCURRENTLY; lossy rollback contradicts DM-5.
13. Idempotency "(existing)" is HTTP-scoped TTL-24h — sync replay window; sync idempotency table absent.
14. Desktop↔API topology absent ⇒ ADR-1 unexecutable.
15. Version=1 backfill makes replay look fresh.
16. Fingerprint-of-mutable-file — ADR-2 unimplementable (two different formulas in the same doc).
17. Two DbContexts — tenant filters may land on the unused context.
18. 9 real tables unclassified ⇒ cutover leaves them without tenant_id ⇒ silent tenant leak.
19. Cross-tenant restore / emergency-access read without tenant consent or dual control.
20. **All high-risk categories 0% implemented; ~60 vacuous pass-paths; "156/156 passing" NOT VERIFIED.**

---

## 11. Sign-off Answers to the Review Mandate (Summary — full text in FINAL_SIGNOFF.md)

1. Implementable? **No (as written).** 2. Without data loss? **Unverifiable today; conditional for rehearsed ci only.** 3. Current functionality preserved? **No as written.** 4. Isolation clear and secure? **Model yes; mechanisms no.** 5. Sync sufficient? **No.** 6. Offline adequate? **No.** 7. Decisions to change before Phase 3? **Yes — nine ADRs adjust/defer + six new ADRs required.** 8. Points requiring redesign? **Yes — ten listed.** 9. Which decisions the user may approve (as adjusted)? **ADR-1/3/5/6/7/8/12 + principle-level B-1/B-6/B-7/B-8.** 10. Which must remain pending? **ADR-2, ADR-11, B-2/B-3/B-9, anything touching neondb, Phase 3 itself.** 11. Overall score **38/100** (informational only). 12. Phase 3? **DO NOT PROCEED.**

---

## 12. What happens next (user decisions only)

This review produced no file modifications to design documents, code, or data. Corrections are *proposed* in the register/ADR review but **not applied**, per mandate. The user must decide: (a) whether to approve the adjusted ADR set; (b) whether to authorize the C1 remediation; (c) whether to commission the Phase-2 repair + re-review; (d) whether to retract the FINAL_REPORT "PASS (Design Complete)". Until then every blocker stands.

*Report 1 of 5 · TECHTOON Phase 2 · 2026-11-16*
