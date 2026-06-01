# Line by Line — Feature Reference

## Feature Philosophy

Line by Line prioritises:

- Fast capture — the window opens immediately, input is always focused
- Low friction — no notebook required, no configuration before first use
- Local privacy — no network, no accounts, encrypted at rest
- Delayed reflection — entries are sealed until their unlock date
- Minimal interface — no visual noise, keyboard-first, terminal aesthetic

The core experience is: **write → seal → wait → reflect**

---

## Implemented Features

### 1. Fast App Launch

The window appears immediately. Key derivation (the slow operation) only runs after the user submits their passphrase, not before the window is shown.

### 2. Passphrase Vault

The app opens locked. First interaction is creating or entering a passphrase.

- First run: `SetupScreen` — create passphrase + confirm
- Subsequent runs: `LockedScreen` — enter passphrase to unlock
- Wrong passphrase: "incorrect passphrase" error, input cleared
- Key derivation runs off the UI thread; "unlocking..." shown while waiting

### 3. Default Journal

A default page-less `Journal` notebook is created on first vault setup. The user can start writing immediately — no organisation required.

### 4. Entry Input

Terminal-style prompt at the bottom of the screen.

| Key | Behaviour |
|---|---|
| `Enter` | Seal and submit |
| `Shift+Enter` | Insert newline within entry |
| `Esc` | Clear current draft |
| `Ctrl+L` | Lock vault immediately |
| `Ctrl+W` | Emergency close (saves draft as `[interrupted]`) |
| `Tab` | Cycle through command completions |
| `Page Up/Down` | Scroll entry list |

### 5. Immediate Sealing

On `Enter`:
1. Plaintext is encrypted (AES-256-GCM, random nonce)
2. A sealed preview is generated (deterministic scramble)
3. Entry saved to SQLite
4. Input cleared
5. Sealed entry appears at the bottom of the list

### 6. Sealed Preview

Preserves text shape: word lengths, spacing, newlines, capitalisation pattern, punctuation placement. Hides: actual letters, digits, words.

The scramble is deterministic — same entry looks the same every time the app opens.

### 7. Recent Entries List

The journal screen shows recent sealed entries in creation order (oldest at top, newest at bottom). Each row shows: `[timestamp]  "scrambled preview"`.

Auto-scrolls to the most recent entry on load and after each submission.

### 8. App-Enforced Unlock Dates

Each entry has an `unlock_at` timestamp. Entries are not shown in plaintext until current time ≥ `unlock_at`. The delay is configurable in `/settings`.

Default delays: 15 seconds (debug builds), 1 day (release builds).

### 9. Unlocked View (`/unlocked`)

Shows all entries in the current notebook whose unlock date has passed. Decrypts and displays plaintext. Filters to current notebook context. Keyboard-scrollable.

### 10. No Editing

Entries are immutable after submission. This is intentional — it preserves the time-capsule feel and prevents rewriting the past.

### 11. Delete and Recovery

- `/delete last` — soft-deletes the most recent entry in current context
- `/trash` — lists soft-deleted entries
- `/restore last` — recovers the most recently deleted entry
- Hard deletion not implemented in MVP (soft-delete only)

### 12. Panic Lock (`Ctrl+L`)

Clears visible plaintext, clears the input draft, zeros the vault key in memory, and returns to the locked screen immediately.

### 13. Emergency Close (`Ctrl+W`)

If there is text in the input field: encrypts it as a normal entry with `[interrupted]` appended, then locks and shuts down. Nothing is lost.

### 14. Notebooks and Pages

```
/notebooks              list all notebooks
/use <name>             switch to a notebook
/new notebook <name>    create and switch
/pages                  list pages in current notebook
/page <title>           switch to a page
/new page <title>       create and switch
```

The current context (`notebook: Journal · 14`) is displayed at the top of the journal screen. Entry count updates live.

### 15. Command System

Input starting with `/` is parsed as a command, not a journal entry. Tab cycles through completions, with a live suggestion hint displayed above the input.

All available commands: `/help`, `/unlocked`, `/notebooks`, `/use`, `/new notebook`, `/pages`, `/page`, `/new page`, `/trash`, `/restore last`, `/delete last`, `/export`, `/settings`, `/lock`.

### 16. Export (`/export`)

Decrypts all unlocked entries in the current notebook and writes them to:

```
~/Documents/LineByLine_<Notebook>_<date>.txt
```

Plaintext format with timestamps. Only entries whose `unlock_at` has passed are included.

### 17. Settings (`/settings`)

| Setting | Command | Options |
|---|---|---|
| Unlock delay | `delay <value>` | `15s` `1h` `1d` `1w` `1mo` `3mo` `1y` |
| Entry limit | `limit <n>` | Any positive integer |
| Font size | `size <value>` | `small` `medium` `large` |
| Accent colour | `color <value>` | `blue` `green` `amber` `red` `mono` or `#rrggbb` |

Settings are persisted in the vault database and applied on every unlock. Font size and accent colour update the entire UI instantly via dynamic Avalonia resources.

---

## Non-Goals (MVP)

These are explicitly out of scope:

- Cloud sync or accounts
- AI journaling assistance
- Search over encrypted entries (only over unlocked entries, future feature)
- Rich text, tags, attachments
- Mobile app
- Import from other journaling formats
- Multi-device sync
