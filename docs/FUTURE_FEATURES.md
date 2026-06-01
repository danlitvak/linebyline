# Line by Line — Roadmap

Features considered for future versions, in rough priority order.

---

## High priority

### Passphrase Change

Add a `/passwd` command that re-derives the vault key with a new passphrase and rewrites the `vault_meta` password-check payload. Entry ciphertext is unaffected — entries are encrypted with the derived key, not the passphrase directly.

### Entry Search

Add `/search <term>` that decrypts all unlocked entries in memory and returns those whose plaintext contains the search term. Results shown in a read-only view. Only operates on entries whose `unlock_at` has already passed.

### Per-Entry Unlock Delay Override

Allow typing `/in 2w` or `/in 3mo` before pressing `Enter` to set a custom unlock delay for that specific entry, overriding the global default.

---

## Medium priority

### Multi-Vault Support

Allow maintaining more than one separate vault (e.g. personal and work). Each vault has its own passphrase, notebooks, and entries. Switching vaults via a startup picker or a `/vault` command.

### Themes

Add a `/theme` command:
- `dark` — current default
- `darker` — reduced contrast, more minimal
- `light` — white background, dark text

### Scheduled Unlock Notifications

When running in the background (system tray), show a Windows notification when entries become readable. Requires a background agent or scheduled task.

### Import

Add `/import <path>` to read a previously exported `.txt` file and re-seal its entries back into the vault with a new `unlock_at`.

---

## Lower priority / polish

### Backup Reminder

On vault unlock, if the vault file has not been copied/backed up in over 30 days, show a subtle one-time reminder.

### Resize Without Mouse

The standard Windows title bar currently handles resize. If the app ever moves to a custom chrome, add keyboard-driven resize (`Win+Arrow` works natively, but edge dragging would need explicit handling).

### Entry Word Count

Show a live character or word count while typing, in the hint bar area.

### Configurable Timestamp Format

Allow the user to choose between `yyyy-MM-dd HH:mm` (default) and shorter or locale-aware formats in settings.
