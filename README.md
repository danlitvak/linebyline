# Line by Line

<img src="LineByLine.App/Assets/lbl_logo_animated.gif" width="72" alt="Line by Line logo"/>

**A local-first desktop journaling app for fast, private writing.**

Write one entry at a time. Press `Enter` — it disappears. Encrypted, sealed, unreadable until its unlock date. Come back later and read what past-you had to say.

> *Write it. Seal it. Meet it later.*

---

## Motivation

Most journaling apps put the full text of your thoughts in front of you the moment you open them — which makes it easy to second-guess, re-edit, or spiral back into what you wrote. Line by Line is built around a different idea: **delayed reflection**. You write freely because you know you can't immediately reread it. The entry is sealed on submission and locked until a date you set. When that date arrives, you meet your past thoughts as a reader, not an editor.

The app is also built to be genuinely private. There is no account, no sync service, no server of any kind. Everything lives in an encrypted SQLite database on your own machine. The passphrase never leaves the device.

---

## Features

| | |
|---|---|
| **AES-256-GCM encryption** | Every entry is encrypted with a unique nonce the moment you press Enter |
| **PBKDF2-SHA256 vault** | Your passphrase derives the vault key via 300,000 iterations — never stored |
| **Sealed preview** | A deterministic scramble shows the shape of your writing without any content |
| **Configurable unlock delay** | 15 seconds to 1 year — entries stay locked until the time passes |
| **Notebooks & pages** | Organise entries into notebooks and pages, or keep everything in the default Journal |
| **Soft delete & recovery** | Entries are soft-deleted and recoverable via `/trash` and `/restore last` |
| **Export** | Dump all unlocked entries from a notebook to a plain `.txt` file |
| **Live settings** | Font size and accent colour update the entire UI instantly via dynamic resources |
| **Keyboard-first** | Tab autocomplete, Page Up/Down scroll, panic lock — no mouse required |
| **Zero network** | No telemetry, no cloud, no accounts. SQLite file on disk, that's it |

---

## Tech stack

| Layer | Choice | Why |
|---|---|---|
| Language | C# / .NET 7 | Native performance, strong crypto stdlib, type safety |
| UI framework | Avalonia 11 (MVVM) | Cross-platform desktop, not Electron, proper windowing |
| Database | SQLite via `Microsoft.Data.Sqlite` | Local, embedded, zero config, reliable |
| Encryption | `System.Security.Cryptography` AES-256-GCM | Authenticated encryption built into the runtime |
| Key derivation | PBKDF2-SHA256, 300k iterations | Standard, well-understood, built into .NET |
| MVVM toolkit | CommunityToolkit.Mvvm | Source-gen commands and properties, minimal boilerplate |

---

## Architecture

```
LineByLine.App/
├── Crypto/        Key derivation, AES-GCM encrypt/decrypt, sealed preview generation
├── Data/          SQLite repositories — entries, notebooks, pages, settings
├── Models/        Domain types — Entry, Notebook, Page, VaultMetadata
├── Services/      VaultService (lifecycle + key management), SettingsService
├── ViewModels/    One VM per screen; command parsing lives in JournalScreenViewModel
└── Views/         Avalonia AXAML — LockedScreen, SetupScreen, JournalScreen,
                   UnlockedEntriesView, SettingsScreen
```

Key design decisions:

- **Key derivation off the UI thread** — 300k PBKDF2 iterations takes ~300ms. The unlock command runs this on a background task so the window stays responsive and shows "unlocking..."
- **Deterministic sealed preview** — the scramble is seeded from the entry ID and preview version, so it looks the same every time the app opens without storing any additional state
- **Vault key zeroed on lock** — `Array.Clear` on the in-memory key bytes on every lock path (Ctrl+L, Ctrl+W, lock command, window close)
- **Dynamic Avalonia resources** — font size and accent colour are stored as `Application.Resources` entries; updating them at runtime re-renders the whole UI without a restart
- **Tunnel KeyDown for Enter/Tab** — the TextBox intercepts `AcceptsReturn` before bubble events fire, so Enter-to-submit and Tab-to-complete use `RoutingStrategies.Tunnel` to intercept before the control handles them

---

## Getting started

### Requirements

- Windows 10 / 11
- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)

### Run from source

```bash
git clone https://github.com/danlitvak/linebyline.git
cd linebyline/LineByLine.App
dotnet run
```

### Build self-contained release

```bash
dotnet publish LineByLine.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

---

## Usage

**First launch:** create a passphrase. This is the only key to your vault — there is no recovery mechanism if you forget it.

**Writing:** type at the `>` prompt and press `Enter` to seal. Use `Shift+Enter` for multi-line entries.

### Keyboard shortcuts

| Key | Action |
|---|---|
| `Enter` | Seal and submit the current entry |
| `Shift+Enter` | Insert a newline inside the current entry |
| `Esc` | Clear the current draft |
| `Tab` | Cycle through command completions |
| `Page Up / Down` | Scroll the entry list |
| `Ctrl+L` | Lock the vault immediately |
| `Ctrl+W` | Emergency close — saves draft as `[interrupted]`, locks, exits |

### Commands

Type any of the following at the `>` prompt and press `Enter`.

**Navigation**

| Command | Description |
|---|---|
| `/unlocked` | Read entries whose unlock date has passed |
| `/notebooks` | List all notebooks |
| `/use <name>` | Switch to a notebook |
| `/new notebook <name>` | Create a new notebook and switch to it |
| `/pages` | List pages in the current notebook |
| `/page <title>` | Switch to a page |
| `/new page <title>` | Create a new page and switch to it |

**Entries**

| Command | Description |
|---|---|
| `/delete last` | Soft-delete the most recent entry |
| `/trash` | List soft-deleted entries |
| `/restore last` | Recover the most recently deleted entry |
| `/export` | Export all unlocked entries in this notebook to a `.txt` file |

**App**

| Command | Description |
|---|---|
| `/settings` | Open the settings screen |
| `/lock` | Lock the vault |
| `/help` | Print all available commands |

### Settings

Open `/settings` and type a command at the `>` prompt.

| Command | Options | Default |
|---|---|---|
| `delay <value>` | `15s` `1h` `1d` `1w` `1mo` `3mo` `1y` | `1d` |
| `limit <n>` | Any positive integer | `30` |
| `size <value>` | `small` `medium` `large` | `medium` |
| `color <value>` | `blue` `green` `amber` `red` `mono` or any `#rrggbb` hex | `blue` |

Settings are persisted in the vault database and applied automatically on every unlock.

---

## Data & privacy

The vault lives at:

```
%APPDATA%\LineByLine\linebyline.vault.db
```

**What is encrypted:** entry plaintext (AES-256-GCM, unique nonce per entry).

**What is stored in plaintext:** timestamps, notebook names, page titles, sealed previews, unlock dates, soft-delete flags.

**Losing your passphrase means losing your entries.** The key is never stored — it is derived fresh from your passphrase on every unlock. There is no server-side recovery.

To back up your vault, copy the `.vault.db` file.

---

## Limitations

- **Unlock timing is app-enforced, not cryptographic.** A technically capable user with direct database access could read ciphertext before the unlock date. The app makes casual access impossible, not adversarial access.
- **Windows-first.** Avalonia supports macOS and Linux, but the app has only been packaged and tested on Windows.
- **No multi-device sync.** The vault is a local file. You can copy it manually between machines, but there is no sync mechanism.

---

## Licence

MIT — see [LICENSE](LICENSE) if present, or treat as open for personal and educational use.
