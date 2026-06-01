# Line by Line — Important App Features

## Feature Philosophy

Line by Line should prioritize:

- Fast capture.
- Low friction.
- Local privacy.
- Delayed reflection.
- Minimal interface.
- No editing after submission.

Avoid turning the MVP into a general notes app.

The core experience is:

```text
write → seal → wait → reflect
```

## MVP Features

### 1. Fast App Launch

The app must open quickly.

Expected behavior:

```text
User opens app
→ black terminal-style window appears immediately
→ password/passphrase field is focused
```

Do not perform heavy startup work before showing the window.

### 2. Password/Passphrase Unlock

The app opens locked.

The first interaction is unlocking the local vault with a password/passphrase.

Preferred product wording:

```text
passphrase
```

Reason:

- A passphrase is stronger than a short PIN.
- The passphrase derives the encryption key.

PIN support can be considered later, but the MVP should use a full passphrase.

### 3. First-Run Vault Creation

If no vault exists, the app should enter setup mode:

```text
LINE BY LINE
new vault

create passphrase:
confirm passphrase:
```

After successful creation:

- Create vault metadata.
- Create default Journal notebook.
- Enter unlocked Journal screen.

### 4. Default Journal

The app includes a default page-less notebook:

```text
Journal
```

This is the main place where users can immediately write entries.

The user should not be forced to create a notebook or page before journaling.

### 5. Line-by-Line Entry Input

User writes into a terminal-style prompt.

Rules:

```text
Enter         submits/seals entry
Shift+Enter   adds newline inside current entry
Esc           clears current draft
Ctrl+L        locks app
```

An entry may contain multiple visual lines, but it is submitted as one entry.

### 6. Immediate Sealing

When the user presses `Enter`:

```text
1. Plaintext is encrypted.
2. Plaintext disappears from the input area.
3. Sealed preview appears in recent entries list.
4. Timestamp is visible.
5. Entry cannot be edited.
```

The user should feel that the thought has been sealed immediately.

### 7. Sealed Preview

After submission, show timestamp plus scrambled preview.

Example:

```text
[2026-06-01 22:14] "gqvso yxldm, zkb p cnvj wlzf cz..."
```

The preview preserves text shape but hides readable content.

Preserve:

```text
spaces
newlines
punctuation
capitalization pattern
digit positions
rough length
```

Hide:

```text
actual words
actual letters
actual numbers
```

### 8. Recent Entries List

After unlocking, show recent entries.

Recent list should include:

```text
timestamp
sealed preview
lock/unlock status if useful
```

Example:

```text
[2026-06-01 22:14] "ksrnv uwpe qzzx..." unlocks 2026-07-01
[2026-06-01 22:11] "p mslg..." unlocks 2026-07-01
[2026-06-01 22:03] "vvepl qxtm zpa..." unlocks 2026-07-01
```

Keep display compact.

### 9. App-Enforced Unlock Dates

Each entry has an `unlock_at` timestamp.

Before `unlock_at`:

- Show sealed preview.
- Do not show plaintext.

After `unlock_at`:

- Entry can be read from unlocked view.

MVP does not need API/server verification.

### 10. Unlocked View

Command:

```text
/unlocked
```

Shows entries whose unlock date has passed.

Example:

```text
UNLOCKED

[2026-05-01 23:12]
I felt overwhelmed today, but I still kept going.

[2026-05-02 18:40]
I think I need to stop comparing myself so much.
```

Plaintext should only be visible in this mode or equivalent read mode.

### 11. No Editing

Once submitted, an entry cannot be edited.

This is an intentional product decision, not a missing feature.

Reason:

- Preserves the time-capsule concept.
- Prevents perfectionism/editing loops.
- Keeps the data model simpler and more trustworthy.

### 12. Delete Entry

Allow deletion.

MVP can support:

```text
/delete last
```

or later:

```text
/delete <entry-id>
```

Use soft delete initially.

### 13. Panic Lock

Keyboard shortcut:

```text
Ctrl + L
```

Expected behavior:

```text
clear visible plaintext
clear input draft
return to locked screen
remove vault key from active app state
```

This is important to the app’s privacy feel.

## Organization Features

### 14. Notebooks

Users can create notebooks later.

Example:

```text
/new notebook Ideas
/use Ideas
```

Notebooks help organize entries without changing the core writing loop.

### 15. Pages

Users can create pages inside notebooks later.

Example:

```text
/new page App Concepts
```

Default Journal does not require pages.

### 16. Current Context

The UI should show current notebook/page context.

Example:

```text
notebook: Ideas / page: App Concepts
```

If in default Journal:

```text
notebook: Journal
```

## Command Features

Input beginning with `/` should be treated as a command.

Initial useful commands:

```text
/help
/lock
/unlocked
/delete last
```

Later commands:

```text
/notebooks
/use <notebook>
/new notebook <name>
/new page <title>
/settings
```

Normal text input should never require a command.

## Settings Features

Do not build full settings UI first.

Eventually support:

```text
default unlock delay
font size
font family
recent entry count
preview style
```

Default unlock delay should be configurable later.

Possible options:

```text
1 day
1 week
1 month
3 months
1 year
custom
```

## Security Features

### Must Have in MVP

- Local encrypted entries.
- Passphrase-derived vault key.
- Authenticated encryption.
- No plaintext journal files.
- No cloud sync.
- No logs containing user text.
- Clear plaintext UI state after submission.
- Clear active key on lock.

### Should Avoid in MVP

- AI analysis of journal entries.
- Cloud sync.
- Accounts.
- Sharing.
- Export.
- Search over plaintext.
- Rich text.
- Tags.
- Attachments.

These can undermine the simplicity and privacy of the concept.

## Non-Goals for MVP

Do not build these initially:

```text
mobile app
cloud accounts
server-side unlock enforcement
AI journaling assistant
mood analytics
calendar integration
import/export
rich text editor
search
tags
media attachments
multi-device sync
```

## Suggested Development Order

### Phase 1: UI Shell

Build:

```text
Avalonia window
black terminal style
locked screen
passphrase input
unlocked journal screen mock
```

Done when:

```text
App opens fast and looks like Line by Line.
```

### Phase 2: Vault Setup

Build:

```text
first-run vault creation
SQLite database creation
vault_meta table
default Journal notebook
password verification
```

Done when:

```text
User can create and unlock a local vault.
```

### Phase 3: Entry Sealing

Build:

```text
entry input
Enter submit
Shift+Enter newline
sealed preview generation
encryption
save entry
recent entries render
```

Done when:

```text
User can write entries, restart app, unlock, and see persisted sealed previews.
```

### Phase 4: Time Capsule

Build:

```text
unlock_at field
app-enforced unlock check
/unlocked command
decrypt and show readable entries after unlock date
```

Done when:

```text
Entries remain sealed until their unlock_at timestamp has passed.
```

### Phase 5: Organization

Build:

```text
notebook commands
page commands
current context display
entry creation inside selected notebook/page
```

Done when:

```text
User can use default Journal or organize entries into notebooks/pages.
```

## Most Important Implementation Rule

Build the smallest complete version of the core loop first:

```text
unlock → write → seal → persist → restart → unlock → see sealed preview
```

Everything else is secondary.
