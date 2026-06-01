# Line by Line — Development Timeline

## Overview

Line by Line is a local-first desktop journaling app built on C# / .NET 7 / Avalonia / SQLite. The core concept is delayed reflection: entries are encrypted immediately on submission and only readable after a configurable unlock date. Development followed a phased handoff spec, building the core loop first and expanding outward.

---

## Phase 1 — UI Shell

**Goal:** App opens immediately to a black terminal-style window. Password screen appears. Basic navigation works.

**Built:**
- Avalonia MVVM project scaffolded with .NET 7 / Avalonia 11.2
- Black `#0d0d0d` background, Cascadia Mono / JetBrains Mono / Consolas font stack
- `LockedScreen` — passphrase input, focused on load
- `JournalScreen` — terminal prompt with `>`, placeholder sealed entries
- `Ctrl+L` panic lock from anywhere in the app
- `Enter` submits, `Esc` clears draft, `Shift+Enter` inserts newline
- `MainWindowViewModel` routing between screens

---

## Phase 2 — Local Vault

**Goal:** User can create a vault with a passphrase, and unlock it on subsequent runs.

**Built:**
- SQLite schema: `vault_meta`, `notebooks`, `pages`, `entries`, `settings`
- PBKDF2-SHA256 key derivation (300,000 iterations, 32-byte salt)
- AES-256-GCM encryption for the vault password-check payload
- First-run `SetupScreen` with create / confirm passphrase fields
- `VaultService` — `CreateVault()`, `Unlock()`, `Lock()` with key zeroing
- Default `Journal` notebook created on first run
- Vault stored at `%APPDATA%\LineByLine\linebyline.vault.db`
- Key derivation runs off the UI thread; "unlocking..." shown during wait

---

## Phase 3 — Entry Sealing

**Goal:** Write an entry, press Enter, see it sealed. Restart the app, unlock, and see the sealed preview persist.

**Built:**
- `SealedPreviewGenerator` — deterministic scramble (seeded from entry ID + preview version); letters → random letters, digits → random digits, punctuation → random punctuation, whitespace preserved
- AES-256-GCM entry encryption with per-entry random nonce
- `EntryRepository` — `InsertEntry()`, `GetRecentEntries()` filtered by notebook/page context
- `Entry` domain model with all fields from the data spec
- Entries appear newest at the bottom (typewriter order)
- Full sealed preview displayed — no truncation, wraps naturally
- `Shift+Enter` multi-line entry support via tunnel `KeyDown` interception
- Auto-scroll to most recent entry on load and new submission

---

## Phase 4 — Time Capsule

**Goal:** Entries are sealed until their unlock date. `/unlocked` shows decrypted entries after that date.

**Built:**
- `unlock_at` set per entry using the configured delay (default: 15s in debug, 1 day in release)
- `/unlocked` command navigates to `UnlockedEntriesView`
- `GetUnlockedEntries()` queries entries where `unlock_at <= now`
- AES-256-GCM decryption on unlock; entries that fail decryption are silently skipped
- `Esc` returns to journal; Page Up / Page Down for keyboard scroll
- `UnlockedEntriesView` auto-focuses its ScrollViewer so Esc is reliable
- Unlocked view shows notebook context in header

---

## Phase 5 — Organisation

**Goal:** User can create notebooks and pages, switch context, and write entries scoped to each.

**Built:**
- `NotebookRepository` — `GetAll()`, `GetByName()`, `Create()`
- `PageRepository` (within NotebookRepository) — `GetPages()`, `GetPageByTitle()`, `CreatePage()`
- Full command set in the journal prompt:
  - `/notebooks` — list with current marker
  - `/use <notebook>` — switch notebook, reset page
  - `/new notebook <name>` — create and switch
  - `/pages` — list pages in current notebook
  - `/page <title>` — switch to page
  - `/new page <title>` — create and switch
- Context display updates dynamically: `notebook: Journal · 14`
- Entry list and `/delete last` respect the current notebook/page context

---

## Polish Pass

**Goal:** Fill gaps before calling the core loop complete.

**Built:**
- `/delete last` — soft-deletes most recent entry in current context
- `/help` — prints all commands inline; clears when user starts typing
- Debug unlock delay: 15 seconds (release: 1 day)
- Unknown command error messages
- Page Up / Page Down scrolls entry list from the input field
- `Tab` autocomplete — cycles through matching commands; live suggestion hint appears as you type `/`
- `Ctrl+W` emergency close — saves in-progress draft as `[interrupted]` entry before locking and shutting down
- `KeyboardNavigation.TabNavigation="Cycle"` prevents Tab from escaping the journal view
- Explicit focus restoration after Tab to handle Avalonia focus system edge cases

---

## Future Features Phase

**Goal:** Implement settings, visual customisation, soft-delete recovery, and export.

**Built:**
- **Settings menu** (`/settings`) — command-based settings screen
  - `delay` — unlock delay (15s / 1h / 1d / 1w / 1mo / 3mo / 1y)
  - `limit` — recent entry limit
  - `size` — font size (small / medium / large) via dynamic Avalonia resources
  - `color` — accent color (blue / green / amber / red / mono)
- Dynamic font size resources (`FontSizeBase`, `FontSizeTitle`, `FontSizeHint`) — changing size updates the entire UI instantly
- Dynamic `AccentBrush` resource — changing color updates all accent elements instantly
- Settings persisted in the `settings` SQLite table; applied on every vault unlock
- Entry count shown in context header (`notebook: Journal · 14`)
- Notebook-aware `/unlocked` view — filters to current notebook
- `/trash` — lists soft-deleted entries in current context
- `/restore last` — undeletes most recently deleted entry
- `/export` — decrypts all unlocked entries in current notebook, writes to `~/Documents/LineByLine_<Notebook>_<date>.txt`
- Tab autocomplete updated with all new commands
