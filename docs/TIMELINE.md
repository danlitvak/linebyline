# Line by Line — Development Timeline

## Overview

Line by Line is a local-first desktop journaling app built on C# / .NET 7 / Avalonia 11 / SQLite. The core concept is delayed reflection: entries are encrypted on submission and only readable after a configurable unlock date. Development followed a phased spec, building the core loop first and expanding outward.

---

## Phase 1 — UI Shell

**Goal:** App opens immediately to a black terminal-style window. Password screen appears. Basic navigation works.

**Built:**
- Avalonia 11 MVVM project scaffolded with .NET 7
- Black `#0d0d0d` background, Cascadia Mono / JetBrains Mono / Consolas font stack
- `LockedScreen` — passphrase input, auto-focused on load
- `JournalScreen` — terminal prompt with `>`, placeholder sealed entries
- `Ctrl+L` panic lock from anywhere in the app
- `Enter` submits, `Esc` clears draft, `Shift+Enter` inserts newline
- `MainWindowViewModel` routing between screens

---

## Phase 2 — Local Vault

**Goal:** User can create a vault with a passphrase and unlock it on subsequent runs.

**Built:**
- SQLite schema: `vault_meta`, `notebooks`, `pages`, `entries`, `settings`
- PBKDF2-SHA256 key derivation (300,000 iterations, 32-byte salt)
- AES-256-GCM for the vault password-check payload
- First-run `SetupScreen` with create / confirm passphrase fields
- `VaultService` — `CreateVault()`, `Unlock()`, `Lock()` with vault key zeroing on lock
- Default `Journal` notebook created on first run
- Vault stored at `%APPDATA%\LineByLine\linebyline.vault.db`
- Key derivation runs off the UI thread; "unlocking..." indicator shown during wait

---

## Phase 3 — Entry Sealing

**Goal:** Write an entry, press Enter, see it sealed. Restart, unlock, sealed preview persists.

**Built:**
- `SealedPreviewGenerator` — deterministic scramble seeded from entry ID + preview version; letters → random letters, digits → random digits, punctuation → random punctuation, whitespace preserved
- AES-256-GCM entry encryption with per-entry random nonce
- `EntryRepository` — `InsertEntry()`, `GetRecentEntries()` filtered by notebook/page context
- `Entry` domain model
- Entries appear newest at the bottom (typewriter order)
- Full sealed preview displayed without truncation, wraps naturally
- `Shift+Enter` multi-line support via `RoutingStrategies.Tunnel` KeyDown interception
- Auto-scroll to most recent entry on load and new submission

---

## Phase 4 — Time Capsule

**Goal:** Entries stay sealed until their unlock date. `/unlocked` shows decrypted entries after that date.

**Built:**
- `unlock_at` set per entry using the configured delay (15s in debug builds, 1 day in release)
- `/unlocked` command navigates to `UnlockedEntriesView`
- `GetUnlockedEntries()` queries `unlock_at <= now`
- AES-256-GCM decryption on read; entries that fail decryption are silently skipped
- `Esc` returns to journal; Page Up / Page Down keyboard scroll
- `UnlockedEntriesView` auto-focuses its `ScrollViewer` so Esc is reliable
- Unlocked view shows notebook context in header

---

## Phase 5 — Organisation

**Goal:** User can create notebooks and pages, switch context, write entries scoped to each.

**Built:**
- `NotebookRepository` — `GetAll()`, `GetByName()`, `Create()`
- `PageRepository` (within `NotebookRepository`) — `GetPages()`, `GetPageByTitle()`, `CreatePage()`
- Full organisation command set: `/notebooks`, `/use`, `/new notebook`, `/pages`, `/page`, `/new page`
- Context display updates dynamically: `notebook: Journal · 14`
- Entry list and all commands respect the current notebook/page context

---

## Polish Pass

**Goal:** Fill UX gaps before calling the core loop complete.

**Built:**
- `/delete last` — soft-deletes most recent entry in current context
- `/help` — prints all commands inline; clears when user starts typing
- Unknown command error messages
- Page Up / Page Down scrolls entry list from the input field
- `Tab` autocomplete — cycles through matching commands; live suggestion hint appears as you type `/`
- `Ctrl+W` emergency close — saves in-progress draft as `[interrupted]` before locking and shutting down
- `KeyboardNavigation.TabNavigation="Cycle"` prevents Tab from escaping the journal view
- Explicit focus restoration after Tab to handle Avalonia focus system edge cases

---

## Future Features Phase

**Goal:** Settings, visual customisation, soft-delete recovery, export.

**Built:**
- **Settings screen** (`/settings`) — command-based interface
  - `delay` — unlock delay (15s / 1h / 1d / 1w / 1mo / 3mo / 1y)
  - `limit` — recent entry limit (any positive integer)
  - `size` — font size (`small` / `medium` / `large`) via dynamic Avalonia resources
  - `color` — accent colour (named presets or any `#rrggbb` hex value)
- Dynamic font size resources (`FontSizeBase`, `FontSizeTitle`, `FontSizeHint`) — changing size updates the entire UI instantly without a restart
- Dynamic `AccentBrush` resource — colour change re-renders all accent elements live
- Settings persisted in the `settings` SQLite table; applied on every vault unlock
- Entry count shown in context header
- Notebook-aware `/unlocked` view — filters to the current notebook
- `/trash` — lists soft-deleted entries in current context
- `/restore last` — recovers the most recently deleted entry
- `/export` — decrypts all unlocked entries in the current notebook, writes to `~/Documents/LineByLine_<Notebook>_<date>.txt`
- Tab autocomplete covers all commands including the new ones

---

## Finalisation

**Goal:** Polish, branding, keyboard improvements, and portfolio readiness.

**Built:**
- Custom app logo (`lbl_logo.png` / `.ico`) — animated GIF variant used in README
- `.ico` generated from PNG and wired into `ApplicationIcon` in the csproj
- Settings screen updated: each row now shows its command syntax in a third column
- Hex color input — `color #rrggbb` accepted in addition to named palette
- Explored borderless window with custom title bar, drop shadow, and rounded corners (`SystemDecorations="None"`, `TransparencyLevelHint="Transparent"`, `BoxShadow`) — reverted in favour of standard Windows chrome for reliability
- README rewritten for portfolio: motivation, architecture notes, design decisions, clean usage guide
- `TIMELINE.md` and `FUTURE_FEATURES.md` brought up to date
- All changes committed and pushed to GitHub

---

## CI/CD — GitHub Actions

**Goal:** Automate compile verification on every change and produce a downloadable Windows build on every release, without any manual build steps.

**Built:**
- **CI pipeline** (`.github/workflows/ci.yml`) — triggers on every push and pull request to `main` (plus manual `workflow_dispatch`). Checks out the repo, sets up the .NET 7 SDK via `actions/setup-dotnet@v4`, restores dependencies, and builds `LineByLine.App` in Release configuration on `ubuntu-latest`. Any push that breaks the build fails the check, so `main` always compiles.
- **Release pipeline** (`.github/workflows/release.yml`) — triggers on pushing a version tag (`v*`). Runs on `windows-latest`, publishes a **self-contained, single-file `win-x64` executable** (`-p:PublishSingleFile=true --self-contained`), renames it to `LineByLine-<tag>-win-x64.exe`, and attaches it to an auto-created GitHub Release (`softprops/action-gh-release@v2`) with generated release notes. Users download one `.exe` and run it — no .NET install required.
- **Least-privilege permissions** — the release job declares `permissions: contents: write` so the workflow token can publish releases, nothing more.
- **Download badge** in the README (shields.io `github/v/release`) that tracks the latest release automatically.
- Publish output directories (`publish/`, `dist/`) added to `.gitignore` so build artifacts never get committed.

**Verified:** ran the exact publish command locally to confirm it produces a working ~74 MB single-file exe, then cut the first real release (`v0`) — the workflow built and uploaded `LineByLine-v0-win-x64.exe` on GitHub's runner end-to-end.

**Why it matters:** this is a complete CI/CD setup — continuous integration guarding every commit and an automated, reproducible release pipeline that ships a ready-to-run binary from a single `git tag`. It demonstrates the build → verify → package → distribute lifecycle that production software depends on.
