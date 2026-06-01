# Line by Line — Design Specification

## Product Summary

**Line by Line** is a minimal, local-first desktop journaling app for fast private writing. The user writes one entry at a time. When they press `Enter`, the entry is immediately sealed: encrypted locally, removed from plaintext view, and shown only as a timestamp plus scrambled preview text.

The app is not just a private notes app. Its core product identity is **delayed reflection**: write now, reread later.

## Core Concept

The app solves these problems:

- Pen-and-paper journaling can be slow, physically discoverable, and inconvenient.
- Normal text files or note apps leave readable files on disk unless the user manually hides or protects them.
- Journaling can become unhelpful if the user constantly rereads, edits, spirals, or overthinks immediately after writing.

Line by Line is designed to make writing feel quick and disposable in the moment, while preserving the entries for future reflection.

## Key Product Decisions

### Security Model

Use **version-one security**:

- Local encryption.
- Password/passphrase unlock on app startup.
- App-enforced unlock dates.
- No server/API required to confirm unlocks.
- No cloud sync in the MVP.

Important limitation:

- Because the unlock timer is enforced locally by the app, it is not cryptographically impossible for a determined technical user to bypass the timer.
- Product language should not claim that entries are impossible to access before the unlock date.
- Correct wording: entries are encrypted and hidden until their unlock date **inside the app**.

### Encryption vs Hashing

Do **not** hash journal text.

A cryptographic hash is one-way and cannot be unlocked later. The app must use encryption, not hashing.

Correct flow:

```text
plaintext entry
→ encrypt using key derived from passphrase
→ store ciphertext locally
→ decrypt later after unlock date
```

### No Editing

Entries are immutable after submission.

Allowed:

- Create entry.
- Read entry after unlock date.
- Delete entry.

Not allowed:

- Edit submitted entry.
- Modify timestamp.
- Modify unlock date after submission in MVP.

This supports the time-capsule feel.

## Visual Direction

The app should feel like a minimal command-line/terminal environment.

### General Style

- Black background.
- Monospace font.
- Minimal UI chrome.
- Very low visual noise.
- No rich text formatting.
- No cards-heavy productivity-app aesthetic.
- No colourful dashboard in MVP.

Suggested fonts:

- Cascadia Mono
- Consolas
- JetBrains Mono

Suggested visual hierarchy:

```text
App title: muted off-white
Input text: off-white or muted terminal green
Timestamps: dim grey
Locked/sealed entries: dimmer than active text
Unlocked entries: brighter, but still minimal
Commands/system messages: muted grey or green
Errors: restrained red/orange only if needed
```

Do not overuse animations. The app should feel fast and stable.

## Startup UX

The app should open in seconds and feel almost instant.

Target flow:

```text
double-click app
→ black window appears immediately
→ password prompt focused
→ user enters passphrase
→ vault unlocks
→ recent sealed lines appear
```

Startup screen example:

```text
LINE BY LINE
vault locked

> unlock
passphrase:
```

Alternative simpler startup screen:

```text
LINE BY LINE
vault locked

passphrase:
```

Prioritize speed: show the window before performing expensive work.

## Main Writing UX

After unlocking, the user sees recent entries in the current notebook/page context.

Example:

```text
LINE BY LINE
vault unlocked

notebook: Journal

[2026-06-01 22:14] "ksrnv uwpe qzzx..."
[2026-06-01 22:11] "p mslg..."
[2026-06-01 22:03] "vvepl qxtm zpa..."

>
```

The input prompt is always ready. The user can immediately start typing.

### Input Rules

- `Enter` submits/seals the current entry.
- `Shift + Enter` inserts a newline inside the current entry.
- `Esc` clears the current draft input.
- `Ctrl + L` locks the app immediately.

Although the app is called Line by Line, an entry may contain multiple visual lines if the user uses `Shift + Enter`. Internally, call this object an **Entry**, not a Line.

## Sealed Preview Design

After submission, the user should see the text form but not the readable content.

Example:

```text
Original: "hello world"
Preview:  "qopke xztnw"
```

The sealed preview intentionally preserves:

- Text length.
- Spaces.
- Newlines.
- Basic capitalization pattern.
- Digits as digits.
- Punctuation placement.

The sealed preview intentionally hides:

- Actual letters.
- Actual words.
- Actual numbers.

Important: the sealed preview is an obfuscation layer, not the actual security layer. The actual security layer is encryption.

### Sealed Preview Generation Rules

Generate preview by replacing each character with a random character of the same category:

```text
lowercase letter → random lowercase letter
uppercase letter → random uppercase letter
digit → random digit
space → preserve space
newline → preserve newline
tab → preserve tab
punctuation → preserve punctuation, at least for MVP
```

Example:

```text
Original:
I feel 10x better today.

Preview:
Q jrmv 47p lxqzto hpeks.
```

The preview should be deterministic per entry so it does not change every time the app opens. Use a stable seed derived from entry metadata, such as entry ID plus a preview version. Do not use the encryption key for visual preview generation unless there is a specific reason.

## Organization Model

Use this hierarchy:

```text
Notebook
  → Page
    → Entry
      → Text
```

Internal naming:

- Use `Notebook`.
- Use `Page`.
- Use `Entry`, not `Line`, because entries can contain multiple visual lines.

Product/UI language can still use “line” because of the app name.

### Default Journal

Create a built-in default notebook:

```text
Notebook: Journal
Pages: none
Entries: direct stream
```

This default Journal allows immediate use without organization friction.

The default Journal should support entries with `page_id = null`.

Custom notebooks can have pages later:

```text
Notebook: School
  Page: CPEN stress
  Page: Co-op thoughts

Notebook: Ideas
  Page: Apps
  Page: Writing
```

## Main Screens

Start with only two screens:

1. `LockedScreen`
2. `JournalScreen`

Add more later:

3. `UnlockedEntriesScreen`
4. `NotebookBrowserScreen`
5. `SettingsScreen`

## Command System

Normal text input should create journal entries.

If input starts with `/`, parse it as a command.

Suggested commands:

```text
/help
/lock
/unlocked
/notebooks
/use Journal
/new notebook Ideas
/new page App Concepts
/delete last
/settings
```

Do not require commands for basic journaling.

## Product Voice

Recommended tagline options:

```text
Write it. Seal it. Meet it later.
```

```text
A private journal that disappears line by line.
```

```text
For thoughts you want to express now and understand later.
```

```text
Journaling without the temptation to reread.
```

Preferred positioning:

> A delayed-reflection journal.

Avoid positioning it only as a “secure notes app.”

## MVP UX Goals

The MVP should prove this loop:

```text
open app
enter passphrase
type entry
press Enter
entry disappears
sealed preview appears with timestamp
restart app
unlock again
sealed entries persist
```

The first technical goal is:

> When the app opens, the black terminal-style password screen appears immediately.
