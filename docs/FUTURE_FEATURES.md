# Line by Line — Future Features

## Window Move / Resize Without Mouse

The app currently requires the mouse to move or resize the window.

Options:
- Go borderless (`SystemDecorations="None"`) and implement keyboard shortcuts for move and resize
- Or document it as a known limitation and leave standard OS window chrome in place

## Multi-Vault Support

Allow the user to maintain more than one separate vault file (e.g. one for personal journaling, one for work). Each vault has its own passphrase, notebooks, and entries. Switching vaults is done via a startup prompt or a `/vault` command.

## Entry Search

Add `/search <term>` that decrypts all unlocked entries in memory and returns those containing the search term. Results are shown in a read-only view. Only works on entries whose unlock date has already passed.

## Passphrase Change

Add a `/passwd` command that re-encrypts the vault check token with a new passphrase. All entry ciphertext stays the same (entries are encrypted with a derived key, not the passphrase directly — the KDF salt stays the same, only the password-check payload is re-written).

## Import

Add `/import <path>` to read a previously exported `.txt` file and re-seal entries into the vault. Useful for migrating from the export format back into the app.

## Themes

Add a `/theme` command to switch between visual themes beyond accent color:
- `dark` (current default)
- `darker` (even more reduced contrast, minimal visual noise)
- `light` (white background, dark text)

## Scheduled Unlock Notifications

When running in the background (system tray), show a notification when entries become available to read. Requires a background process or OS-level scheduled task.

## Per-Entry Unlock Delay Override

Allow the user to set a custom unlock delay per entry at write time, e.g. typing `/in 2w` before pressing Enter sets that specific entry to unlock in 2 weeks instead of the default.
