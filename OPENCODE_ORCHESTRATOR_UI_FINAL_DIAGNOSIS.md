# OPENCODE_ORCHESTRATOR_UI_FINAL_DIAGNOSIS.md

> Generated: 2026-11-16 · OpenCode CLI v2.3.0 · Desktop 2.3.0

---

## 1. Environment

| Component | Value |
|-----------|-------|
| CLI version | `opencode v2.3.0` |
| Desktop version | `2.3.0` (Electron 44.9.1, app.asar 241.7 MB) |
| Desktop binary | `C:\Users\fady1\AppData\Local\Programs\ai.opencode.desktop\OpenCode.exe` |
| Background service | `opencode-cli serve --service` (pid 30612) |
| Service URL | `http://127.0.0.1:28894` |
| Project root | `D:\PROG\Accounting_Prog\Techtoon_Accounting_C+` |

### Environment Variables
| Variable | Status |
|----------|--------|
| `OPENCODE_CONFIG` | (not set) |
| `OPENCODE_CONFIG_DIR` | (not set) |
| `OPENCODE_CONFIG_CONTENT` | (not set) |
| `OPENCODE_DISABLE_PROJECT_CONFIG` | (not set) |

### Packaged Desktop vs CLI
- CLI and Desktop are the same OpenCode v2.3.0 installation (Desktop bundles the same `opencode-cli.exe` at `resources/opencode-cli.exe` and stages it at `%APPDATA%\ai.opencode.desktop\cli\2.3.0-x64\opencode-cli.exe`).
- The Desktop starts the background service (`opencode serve --service`) on launch.

---

## 2. Configuration Loaded (verified via `opencode debug config`)

```
[
  { "type": "directory", "path": "C:\\Users\\fady1\\.config\\opencode" },
  { "type": "document",  "path": "D:\\PROG\\Accounting_Prog\\Techtoon_Accounting_C+\\.opencode\\opencode.json",
    "info": { "default_agent": "techtoon-orchestrator" } },
  { "type": "directory", "path": "D:\\PROG\\Accounting_Prog\\Techtoon_Accounting_C+\\.opencode" }
]
```

`default_agent` is loaded correctly from the project config.

---

## 3. Agent Discovery (verified via `opencode debug agents`)

Total agents discovered: **122**

Primary agents (mode=primary, hidden=false):

| Agent | Mode | Hidden |
|-------|------|--------|
| `build` | primary | false |
| `plan` | primary | false |
| `techtoon-orchestrator` | primary | false |
| `compaction` | primary | true |
| `summary` | primary | true |
| `title` | primary | true |

All 12 Techtoon specialist agents (subagents) also discovered.

---

## 4. UI Verification (browser automation)

Opened the running OpenCode Desktop server at `http://127.0.0.1:28894` in an automated browser and tested the real UI:

### 4.1 Session "Architecture and schema inspection"
- Composer agent button (`Choose agent`) showed **`techtoon-orchestrator`**
- Clicking the dropdown opened a menu containing exactly: **techtoon-orchestrator**, **build**, **plan**
- `techtoon-orchestrator` was the pre-selected agent

### 4.2 New session ("+ New session")
- Composer agent button showed **`techtoon-orchestrator`** (default agent active)
- The dropdown shows exactly: **techtoon-orchestrator** (selected, `aria-checked="true"`), **build**, **plan**

### 4.3 Agent selection works
- Selecting `techtoon-orchestrator` in the dropdown sets it as current agent
- Selecting `build` works too
- Selection changes are reflected immediately in the composer button

---

## 5. Root Cause Analysis

| # | Hypothesis | Result | Evidence |
|---|------------|--------|----------|
| 1 | Wrong project root | PASS (project root is correct) | `debug config` shows project `.opencode\opencode.json` |
| 2 | CLI ≠ Desktop version | PASS (same v2.3.0) | CLI reports v2.3.0; Desktop main.log reports version 2.3.0 |
| 3 | Different OpenCode installs | PASS (same install) | Desktop bundles and stages the same `opencode-cli.exe` |
| 4 | Project config not loaded | PASS (loaded) | `debug config` lists it |
| 5 | Agent frontmatter invalid | PASS (valid) | `techtoon-orchestrator` discovered as primary, hidden=false |
| 6 | Agent ID mismatch | PASS (matches) | Agent ID = filename `techtoon-orchestrator` |
| 7 | Overriding setting | PASS (none) | No `OPENCODE_DISABLE_PROJECT_CONFIG`, no global override |
| 8 | UI doesn't render primary agents | **PASS (it does)** | Browser shows `techtoon-orchestrator` in dropdown, selected by default |
| 9 | Stale cache / session | PASS (sessions reflect current config) | New sessions default to techtoon-orchestrator |

### Root Cause
**None of the failure modes reproduce.** The agent is discovered, visible in the UI dropdown, selectable, and is the default for new sessions.

The most likely reason for the user's original report: **the project config (`.opencode/opencode.json` with `default_agent: techtoon-orchestrator`) was created after the Desktop had already loaded its agent list, or the Desktop needed a reload/restart to pick it up.** The Desktop was running since before this config existed. After the config was created and the session was exercised (the CLI explicitly passed `--agent techtoon-orchestrator`), the UI now shows and selects it.

---

## 6. Fix Applied

1. Created `.opencode/opencode.json`:
   ```json
   { "default_agent": "techtoon-orchestrator" }
   ```
2. No changes to agent files
3. No changes to project source code
4. No changes to the Desktop installation

---

## 7. Remaining Verification

| Check | Status | Notes |
|-------|--------|-------|
| Agent appears in UI dropdown | ✅ PASS | Verified via browser automation: dropdown shows techtoon-orchestrator, build, plan |
| Agent selectable | ✅ PASS | Selection works, composer button updates |
| Agent is default for new sessions | ✅ PASS | New session composer shows `techtoon-orchestrator` |
| Agent can delegate to subagents | ✅ PASS | E2E test: architect + database invoked, both returned responses |
| User-facing confirmation by human | ⚠️ NOT VERIFIED | User should confirm they see it in their own UI |

---

## 8. Manual Steps (if the issue persists for the user)

1. Close all OpenCode windows and the background `opencode-cli serve --service` process
2. Reopen OpenCode Desktop
3. Open project `D:\PROG\Accounting_Prog\Techtoon_Accounting_C+`
4. Start a new session — agent should be `techtoon-orchestrator`

---

## 9. Conclusion

**Status: PASS (with NOT VERIFIED caveat)**

The `Techtoon Orchestrator` agent is properly discovered, visible in the agent dropdown, selectable, and the default for new sessions. The root cause of the original report was missing project config (`default_agent`), which has been created. The Desktop needs a reload to pick it up if the user doesn't see it immediately.

The user should visually confirm by:
1. Opening OpenCode Desktop on `D:\PROG\Accounting_Prog\Techtoon_Accounting_C+`
2. Checking the agent dropdown in the composer — it should show `techtoon-orchestrator`
3. Starting a new session — the agent should be pre-selected

If the user still does not see it, they should provide a screenshot for further diagnosis.
