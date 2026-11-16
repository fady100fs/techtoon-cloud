# TECHTOON_PHASE_2_RISK_REGISTER.md

> Date: 2026-11-16 · Scope: ALL Phase 1/Phase 2 design outputs · Method: aggregation of the seven specialist findings (architect, database, security, sync, accounting, QA, code-review) into a single classified register · Convention: each risk = ID · statement · evidence/source · severity. **No design file was edited; corrections are proposals only.**

Totals: **CRITICAL 20 · HIGH 30 · MEDIUM 25 · LOW 11 (86 items).**

---

## A. CRITICAL (20)

| # | Risk | Evidence / Source |
|---|---|---|
| C1-R1 | Desktop (WPF, ~94 files, DPAPI-encrypted Neon credential) and Streamlit (`psycopg2` direct) hold the production DB master credential — every Phase-2 isolation claim is void against it; architecture docs (MULTI §10.3 "No Neon connection string in client") and security docs (TENANT §5.1 "Desktop DB password \| DPAPI-encrypted") contradict each other and reality. No topology ADR exists. | [A §1.1] [CR §B.1] [Sec §9/C1] |
| C2-R2 | Exemption **C1** — Neon password embedded in **13 files** — is excluded from every remediation list, ADR, and approval point in Phase 2. Residual risk = total read/write of production, undermining every "zero-trust" claim. | [A §4-K] [Sec §9] [CR §B.9] |
| C3-R3 | **Restore permanently destroys un-synced confirmed transactions**: the pre-restore DB is discarded with its `local_databases` identity and its outbox is ignored; there is no export, rescue, or explicit operator warning in the restore flow. Contradicts the project's no-data-loss rule. | [Sync F8.2/G-19] [Acct §6] |
| C4-R4 | **Financial aggregates not atomic across outbox events**: invoice can land without journal/COGS/movements (per-entity outbox; §7.2 non-blocking). §10.3 single-transaction batch contradicts §7.2/§4.1 — unresolved. No saga/compensation. | [Sync F8.4/G-17] [Acct §1.1] |
| C5-R5 | **No key-loss/escrow/recovery story** for backup/local encryption keys anywhere — keys live on the lost machine's keychain; loss = all backups undecryptable (contradicts "no data loss"); threat model has no key-loss entry; no test exists. | [Sec F-41] [QA §scenario 17] [A §ADR-3] |
| C6-R6 | **Backup restore is not bound to the target tenant** — an operator/admin holding another tenant's `.enc` file plus a key restores foreign data; encryption is the only control and can be optional; no tenant-binding/dual approval in the activation flow. | [Sec F-42/A2/A8] |
| C7-R7 | **Offline Authorization (72h) defeated by clock rollback** — expiry enforced on device wall clock; monotonic counter / server floor / rollback lock absent; not modeled (**no T34**). Principal enforcement of the whole offline model is advisory. | [Sec F-34] [A §ADR-7] [QA §5] |
| C8-R8 | **Offline-policy kill switch**: `OfflineValidityHours` / `ReverificationRequired` tenant-configurable with **no floor, cap, or dual approval**; hours→∞ / ReverificationRequired=No silently removes the only control propagating user revocation; not audited as a policy-class. | [Sec F-35/A3] |
| C9-R9 | **Stock "additive sum" unimplementable**; `inventory_movements` absent from classification entirely; no oversell guard; negative stock accepted silently; `stock_levels` carries Version + additive strategy (mutually exclusive); "sum" has no delta payload defined. | [Sync F5.6/G-13] [Acct §3.2] [A §ADR-5/6] |
| C10-R10 | **~35 financial tables + `inventory_movements` + `cost_history` have NO conflict strategy**; `journal_entries/journal_lines` absent from all conflict tables (a financial *state* document); no classification criterion written → ADR-6 cannot be applied consistently by future authors. | [Sync F5.1/F5.2/G-12] [Acct §5] [A §ADR-6] |
| C11-R11 | **RLS broken as designed**: `USING` without `WITH CHECK`; no FORCE RLS; no non-owner role; no transaction-scoped `set_config(...,'true')`; `current_setting` unset hard-errors (0 code references) ⇒ **M13 = certain outage**, or leak via fallback. | [Sec F-01..F-04] [DB §4-M13] [A §ADR-1] |
| C12-R12 | **Migration rollout unsafe as written**: M4/M7 NOT NULL without NOT VALID/CONCURRENTLY; M11 FK will likely abort on orphans (P4 evidence: FKs missing from dump); lossy drop-based rollback contradicts DM-5 "rollback → data restored". | [DB §4] |
| C13-R13 | **Idempotency "(existing idempotency_keys)" factually wrong** — HTTP-scope, mandatory 24h TTL, wrong columns; sync replay window; sync idempotency table/DDL absent; natural keys never wired into apply flow ⇒ **transport-level works, operation-level fails**. | [Sync F3.1/F3.2/F3.6] [CR §B.2] [Acct §1.2] |
| C14-R14 | **Tenant isolation RLS + direct-desktop Npgsql are mutually exclusive end-states**; no topology ADR; enabling the design breaks every current desktop workstation — Phase 2 and current product per the design are incompatible as written. | [A §1.1/ADR-1] [CR §A.4] |
| C15-R15 | **Version=1 backfill** makes replayed/restored events look fresh; sibling-vs-descendant ambiguity undetected; conflict resolver may misapply stale payloads (double-edit v6′ over v5 applies without review). | [Sync F4.3] [CR §C] |
| C16-R16 | **Fingerprint of a mutable file — ADR-2 mechanism unimplementable**; two different formulas in the same doc (§1.2 vs §2.5); size changes every write; no offline reference location; attacker-computable ⇒ false lockouts guaranteed + useless as secret. | [Sec F-13] [A §ADR-2] |
| C17-R17 | **Two DbContexts** (`Techtoon.Data\TechtoonDbContext.cs` empty shell vs registered `Techtoon.Data\Context\TechtoonDbContext.cs`) — tenant filters land on the unused one; desktop silently diverges from API views. | [CR §C] |
| C18-R18 | **9 real tables unclassified** (`bank_*`, `inventory_movements`, `reconciliation_*`, `shift_closings`) ⇒ no tenant_id/is_deleted/indexes at cutover ⇒ **silent tenant leak through the gaps**; `expense_categories` double-classified; classification's "86/84/76/60/68" figures all inconsistent. | [DB §2.2] |
| C19-R19 | **Cross-tenant restore + emergency-access read as single person**: backup restore god-mode (`settings.restore`) one-admin, no tenant binding; emergency access = one System Owner cross-tenant read, audit-only, no tenant consent — ADR-4 sound in isolation, scope-weak as shipped. | [Sec §8 §10] |
| C20-R20 | **All high-risk categories 0% implemented**; ~60 vacuous `if (!TestDb.Available) return;` paths; "156/156 passing" NOT VERIFIED; 0 tests match tenant/sync/offline/replica/device/fingerprint; E2E "~50%" unsubstantiated. | [QA §3] |

## B. HIGH (30)

| # | Risk | Evidence / Source |
|---|---|---|
| H1 | Conflict-rule table duplicated ×4 with drift (Settings-LWW absent in two) | [A §3] |
| H2 | Offline parameter table ×4 inconsistent (MULTI 4 vs SESSION 3 vs OFFLINE 6 vs ADR-7 1) — grace 30min unexplained | [A §3] [Sec F-36] |
| H3 | Backup state machine ×3 inconsistent (Offline state missing in BACKUP/LOCAL_DB) | [A §3] |
| H4 | `users`/`user_permissions` treated as global while design assumes tenant roles; permission system not tenant-scoped in schema | [Sec F-04] |
| H5 | 5 reporting views + ~100 raw-SQL sites silently include soft-deleted rows after ADR-8; views never recreated | [A §ADR-8] [Acct §8] |
| H6 | `sessions`/`local_sessions` refresh tokens stored **plaintext** — regression vs current HashToken; naming drift; sessions table absent from new-tables list and location unspecified | [A §4-J] [Sec F-28/F-29] |
| H7 | Threat-model counts wrong (claimed 5/12/10/3 vs actual **8/15/10/0**); same in FINAL_REPORT §2.2/§4.3 | [Sec F-57] [A §4-B] |
| H8 | Device fingerprint low-entropy/spoofable; no per-device secret at enrollment; no drift/re-enrollment flow; patch-Tuesday mass lockout | [Sec F-20/F-21/F-22] |
| H9 | Emergency access single-operator cross-tenant read; no dual control, alert channel, or countersign | [A §ADR-4] [Sec F-55/A1] |
| H10 | Backup restore/activate = one admin god-mode; no tenant binding; no dual control anywhere | [Sec F-42/F-50/A2/A6] |
| H11 | Revoked-user reconnect **silently discards queued financial work** (audited, irrecoverable, unapprovable) — "rejected" is unrecoverable | [Sync F6.7] [Sec F-37] |
| H12 | `idempotency_keys` retention unspecified; stale restore replay double-applies sum-stock and undoes deletes under LWW | [Sec F-45] [Acct §6] |
| H13 | LWW on `parties.opening_balance` shifts every derived balance silently; audit-only fallback | [Acct §2/§5] [Sync F5.3] |
| H14 | Derived balances never recalculated post-sync — silent corruption of all reports/AR/AP | [Sync F4.7/F8.5] |
| H15 | Version sibling-vs-descendant ambiguity — "V > cloud → apply" overwrites good edits | [Sync F4.3] |
| H16 | Equal-version "different data" comparator undefined — missed conflicts or false positives | [Sync F4.4] |
| H17 | No row-level CAS (`WHERE version=@expected`) — TOCTOU between validate and apply | [Sync F4.5] |
| H18 | Pull sync `since=wall-clock` unpaginated, no cursor, no failure recovery; unbounded conflict storm; 100% manual, no bulk path | [Sync F6.1/F6.3/G-14] |
| H19 | Mobile absent from sync actor model — outbox `local_database_id NOT NULL` vs mobile NULL; unspecified write path; flow diagram wrong (F-M1) | [Sync F1.1/F1.3] [A §4-V] |
| H20 | `invoice_number` has no UNIQUE constraint; natural-key dedup unusable; numbering decision open (§5.1 parenthetical; no ADR) | [DB §3.4] [Acct §1.2] |
| H21 | BACKUP §7.1 rule ordering — "local newer ⇒ apply" executes before manual-review override: restore auto-applies stale financial payloads | [Acct §5] |
| H22 | Rejected on revocation events have no dead-letter/quarantine/review path | [Sync F2.7] |
| H23 | `UNIQUE(tenant_id, entity_type, entity_id, version)` lacks device/replica — legitimate cross-replica pushes collide server-side | [Sync F2.9] |
| H24 | Migration "Default Tenant" GUIDs fork between `neondb` and `techtoon_test` — silent replica fork post-migration | [CR §D] |
| H25 | MIGRATION before-backup not verified; unencrypted pre-migration production dump exposure; no SLO/downtime plan | [DB §4] [CR §D] |
| H26 | RestoredPendingActivation read-only ACL unspecified — restored financial data semi-exposed pre-activation | [Sec F-46] |
| H27 | No `sync.conflicts.*` permission keys — any authenticated user can touch conflicted financial data | [A §ADR-6] |
| H28 | Coverage claims inflated (~50% E2E with zero E2E cases; ~70% integration without artifact; DISCOVERY "36 files" actual 27; "156" accurate but "156/156 passing" not) | [QA §3] [DB §6] |
| H29 | SI- test-ID collision (Session Isolation vs Sync Idempotency); envs inconsistent (5 vs 7 in strategy chapters) | [QA §1] [A §3] [DB §6.1] |
| H30 | Three timestamp strategies (none used / mandated TZ / mixed) — all 142 columns naive; PROPOSAL mandates TZ | [DB §5-C6] [A §4-E] |

## C. MEDIUM (25)

| # | Risk | Evidence / Source |
|---|---|---|
| M1 | Sessions vs local_sessions naming; location unspecified; sessions table absent from new-tables list | [A §4-J] [Sec F-29] |
| M2 | Secret-inventory contradictions (MULTI §10.3 vs TENANT §5.1 vs THREAT §3.1 vs reality) | [A §3/K] |
| M3 | FINAL_REPORT factual counts wrong (17/18/19 docs; 4 vs 3 approved; 60 vs 86 TenantId); pressures Phase 3 | [A §4-A/U] [DB §5-C11] [Acct §8-6] |
| M4 | Migration pre-check "Expected: 0 orphans" unsubstantiated; only invoices→parties checked (P4 evidence of partial failure) | [DB §4] |
| M5 | `sync_outbox` DDL PG-only (`gen_random_uuid()`) vs SQLite-local unstated — ADR-11 destabilizes semantics | [Sync F2.1] |
| M6 | Missing `WITH CHECK`/`missing_ok`/`is_local` in both DDL examples — operational RLS details absent | [Sec F-01/F-03] [DB §4-M13] |
| M7 | `expense_categories` double-classified contradictory; currencies/settings "⚠️ Optional" vs M4 forces NOT NULL-all | [DB §2.2/§5-C10] |
| M8 | Backup metadata verified pre-restore against itself (self-consistent file+key) — no binding to tenant/era | [Sec F-42/F-43] |
| M9 | v_party_ledger/v_stock_balance break if views recreated wrong post-soft-delete | [DB §5] |
| M10 | Empty second DbContext — latent DI ambiguity in App.xaml.cs | [CR §A bonus] |
| M11 | TestDb guard substring match fragile (`techtoon_test_prod`-like names pass) | [QA §4] |
| M12 | Role seeding/ON CONFLICT vs tenant-scoped memberships — roles' tenancy destiny undecided | [CR §C.6] |
| M13 | `idempotency_keys` HTTP semantics reuse collision risk (user_id/endpoint columns wrong for tenant sync) | [DB §5-C17] [Sync F3.1] |
| M14 | "11 architecture documents" duplicated lists vs 18–19 files | [A §3] |
| M15 | Phase-2 reports overstate verification (FINAL_REPORT "Design Complete PASS" with 9/12 ADRs pending incl. integrity backbone) | [Acct §8] |
| M16 | `role_permissions` treated as existing, dump lacks it; live FK state 5–8, NOT VERIFIED | [DB §3.3] |
| M17 | Recoverability of "rejected" offline transactions NOT VERIFIED; notifications to payees/drivers absent | [Sec F-37] [Sync F6.7] |
| M18 | Refresh-concurrency race → false-positive mass revocation (no overlap grace window) | [Sec F-27] |
| M19 | `parties` LWW wholesale silently overwrites credit_limit/phone/tax flags (audit-only) | [Sync F5.3] |
| M20 | `accounts` (chart of accounts) has no conflict strategy — silent GL master overwrites | [Sync F5.4] |
| M21 | Legacy `backup_to_local.py`/`s3_backup.py`/`local_mirror.py` unaudited — unencrypted external copies may persist | [DB §6.2] |
| M22 | 12h offline warning vs 72h window vs 24h alert escalation ladder undefined | [Sync F6.10] |
| M23 | Per-tenant numbering absent — duplicate `invoice_number` possible TODAY (no UNIQUE in dump) | [DB §3.4] [Acct §1.2] |
| M24 | BACKUP §7.3 outbox-in-backup/restore conflicts only cover copied rows; restored set ⊆ cloud-set unchecked | [Acct §6-4] |
| M25 | WAL/continuous-backup claims likely infeasible from app against Neon serverless | [A §5-12] |

## D. LOW (11)

| # | Risk | Evidence / Source |
|---|---|---|
| L1 | `RestoredPendingActivation` read-only ACL unspecified (who reads restored PII?) | [Sec F-46] |
| L2 | Temp password delivery unspecified (out-of-band?) | [Sec F-54] |
| L3 | Doc arithmetic drift: "18" vs "19" vs "17" files; MULTI §12 lists 10 of 12 ADRs | [A §4-A] [DB §5-C16] [L-doc counts] |
| L4 | SI- ID collision (cosmetic + functional) | [QA §1] [A §3] |
| L5 | LocalDatabaseId in schema adds origin column to cloud tables (device-topology inference leakage) | [A §4-V] |
| L6 | `audit_trail` SERIAL vs UUID mix; `created_by INT → users(id)` vs `local_users(id)` inconsistent targets | [DB §5-C14/C15] |
| L7 | SCHEMA §1 header "New Tables (10)" lists 13 (cosmetic) | [DB §5-C1] |
| L8 | MULTI §12 "Required (11 files)" vs FINAL_REPORT 18 files | [DB §5-C16] |
| L9 | Backup `Encrypted (yes/no)` boolean only — no KDF/algorithm fields surfaced | [A §ADR-3] |
| L10 | Audit 90-day retention + cold archive — no ADR (retention policy chosen silently) | [A §5-8] |
| L11 | Permission resolution "union minus denies" precedence ambiguity (docs differ) | [A §5-9] [Sec F-49] |

---

## E. Document Contradiction Cross-Reference Summary

| Topic | Contradicting values | Sources |
|---|---|---|
| Approval count | "4 approved / 8 pending" (FINAL_REPORT) vs **3 approved / 9 pending** (ARCHITECTURE_DECISIONS.md) | [A §4-A] [DB §5-C11] |
| Document count | "17" (FINAL_REPORT body) vs "18" (same index) vs "19 files" (actual) | [A §4-A] [Acct §8-6] |
| Table count | 85 (dump) vs 86 (live=85+role_permissions) vs **84/76/60/68** classification figures | [DB §1–2] |
| New tables | "10" (header) vs 13 listed (same doc) vs DISCOVERY 10 | [DB §5-C1] |
| Modified tables | "all 86" (MIGRATION M2) vs "60" (PROPOSAL §3.2) | [DB §4/A §4-U] |
| FKs | 4 (dump) vs "5" (doc claim=P7) vs 5–8 live NOT VERIFIED | [DB §3.2–3.3] |
| Timestamps | none w/ TZ (dump) vs "mandatorily TZ" (PROPOSAL) | [DB §5-C6] |
| Sessions | sessions vs local_sessions; PLAINTEXT refresh tokens | [A §4-J] |
| Offline params | 4 vs 3 vs 6 vs 1; grace 30min undefined | [A §3] |
| Backup states | 7 vs 6 vs 6 (Offline row inconsistent) | [A §3] |
| Severities | 5/12/10/3 claimed vs 8/15/10/0 actual | [Sec F-57] |
| E2E env | `techtoon_test` participant (strategy) vs TestDb guard substring (code) | [QA §4] |
| Classification coverage | "86 tables classified" vs **9 omitted** | [DB §2.2] |
| Sign-off | "PASS (Design Complete)" vs **9/12 ADRs pending** | [Acct §8] |
| Idempotency | "reuse existing idempotency_keys" vs HTTP-scoped TTL-24h reality | [Sync F3.1] |
| Stock | "Sum (additive)" everywhere vs unimplementable + no oversell | [Acct §3] |

---

## F. Unverified-but-Claimed Items (must be labelled, not accepted)

1. "156/156 tests passing" — reproducible count, unverified pass rate.
2. E2E ~50% / Integration ~70% coverage — no artifact.
3. "100% coverage goal" — unmeasured.
4. Live `information_schema` table count = 86 — single-time dump evidence; live state NOT VERIFIED.
5. Live FK count 5–8 — P4 partial-application evidence; never rechecked.
6. Current production Neon password scope (C1) — 13 files identified; authoritative list not re-audited under this review (read-only, no editor opened).
7. `_efmigrationshistory` absence in live — assumed 0.
8. Backup `BACKUP_AND_REPLICA` activation drill by some admin — designed, never executed.
9. Weighted-average costing parity Python vs .NET — code-read only; runtime behavior NOT VERIFIED on real data.
10. "0 RLS in production" — inference from absence of any DDL/code match; no catalog query performed (read-only).

---

## G. Remediation priority order (proposal — not executed, per mandate)

1. **Retract "PASS (Design Complete)"** — root political blocker for any honest re-baseline.
2. **Exemption C1 rotation** — 13 files credential exposure; requires explicit user authorization.
3. **Topology ADR** (Desktop/Streamlit/API ↔ Neon credential removal).
4. **Classification re-baseline** → corrected 68-table tenant list; re-run DDL; fix `invoices.created_at` failure; idempotent deploy scripts.
5. **Repair RLS design** (WITH CHECK, FORCE, non-owner role, SET LOCAL) gated behind #3; M13 forbidden before.
6. **Mandatory backup verification gate** (+escrow for keys) wired into every M-phase.
7. **Redesign restore flow**: outbox quarantine at activation + financial-diff review gate + post-restore derived-balance re-derivation.
8. **ADR adjustments per TECHTOON_PHASE_2_ADR_REVIEW.md**; new ADRs (topology, numbering, stock, retention, escrow, view strategy).
9. **Rebuild threat model** (STRIDE, T34–T36, recount severities).
10. **Rebuild test strategy** + implement an isolated `techtoon_ci` harness + 18 scenario tests; require ≥1 EXECUTED security/sync/offline test before any sign-off.
11. **Conflict classification criterion** written + applied to all tenant tables; canonical rule table single-sourced.
12. **Financial-document atomic envelope** (saga or one-event-per-document) + natural keys + CAS + dead-letter.

*Report 3 of 5 · TECHTOON Phase 2 · 2026-11-16*
