# Line by Line — Tech Stack

## Chosen Stack

**C# / .NET 7 / Avalonia 11 / SQLite**

Chosen for a fast, native-feeling desktop app without Electron or a web runtime.

---

## Runtime and Language

| | |
|---|---|
| Language | C# (.NET 7) |
| UI Framework | Avalonia 11.2 (MVVM pattern) |
| Database | SQLite via `Microsoft.Data.Sqlite` 10.x |
| Encryption | AES-256-GCM (`System.Security.Cryptography.AesGcm`) |
| Key Derivation | PBKDF2-SHA256, 300,000 iterations (`Rfc2898DeriveBytes.Pbkdf2`) |
| MVVM Toolkit | CommunityToolkit.Mvvm 8.2 (source generators) |

---

## Project Structure

```
LineByLine.App/
  Crypto/       AES-GCM, PBKDF2, SealedPreviewGenerator
  Data/         SQLite repos: EntryRepository, NotebookRepository,
                SettingsRepository, Database (schema + connection factory)
  Models/       Entry, Notebook, Page, VaultMetadata
  Services/     VaultService, SettingsService
  ViewModels/   One per screen + shared display records
  Views/        Avalonia AXAML + code-behind per screen
  Assets/       App icons (PNG + ICO + animated GIF)
```

---

## Vault File Location

```
%APPDATA%\LineByLine\linebyline.vault.db
```

SQLite WAL-mode database. The directory is created automatically on first vault setup.

---

## Cryptography Details

### Key Derivation

```
passphrase → PBKDF2-SHA256(passphrase, salt=32 bytes, iterations=300_000) → 32-byte key
```

KDF parameters stored in `vault_meta`.

### Password Verification

On vault creation, a known string (`LINE_BY_LINE_CHECK_V1`) is encrypted with the derived key. On unlock the same derivation is attempted — a `CryptographicException` (GCM tag mismatch) means wrong passphrase. The raw passphrase and derived key are never persisted.

### Entry Encryption

Each entry encrypted independently:

```
plaintext → AesGcm.Encrypt(key, nonce=12 random bytes) → ciphertext + 16-byte tag
stored as: encrypted_text = ciphertext ++ tag,  nonce stored in separate column
```

### Sealed Preview

Deterministic character-level scramble seeded by `Hash(entryId, previewVersion)`:
- Lowercase → random lowercase
- Uppercase → random uppercase
- Digit → random digit
- Punctuation / symbol → random character from fixed punctuation pool
- Whitespace, newlines → preserved

---

## Avalonia Screens

| Screen | ViewModel |
|---|---|
| First-run vault setup | `SetupScreenViewModel` |
| Vault locked | `LockedScreenViewModel` |
| Main journal | `JournalScreenViewModel` |
| Unlocked entries | `UnlockedEntriesViewModel` |
| Settings | `SettingsScreenViewModel` |

Screen routing is handled by `MainWindowViewModel.CurrentScreen` bound to a `ContentControl` + `ViewLocator`.

### Dynamic Resources

Font size and accent colour are runtime-mutable `Application.Resources`:

| Key | Default |
|---|---|
| `FontSizeBase` | `13` |
| `FontSizeTitle` | `17` |
| `FontSizeHint` | `11` |
| `AccentBrush` | `#5555aa` |

### Key Input Handling

- `Enter` and `Tab` use `RoutingStrategies.Tunnel` to intercept before `AcceptsReturn` and focus-traversal handle them
- `KeyboardNavigation.TabNavigation="Cycle"` on the journal root prevents Tab from leaving the view
- Caret restoration posted on `DispatcherPriority.Input` after Tab autocomplete

---

## SQLite Schema

```sql
vault_meta    -- KDF params + password-check payload (single row, id = 1)
notebooks     -- name, is_default, created_at
pages         -- notebook_id FK, title, created_at
entries       -- notebook_id FK, page_id FK (nullable),
               -- encrypted_text BLOB, nonce BLOB,
               -- sealed_preview TEXT, preview_version INT,
               -- unlock_at TEXT, is_deleted INT, created_at TEXT
settings      -- key TEXT PK, value TEXT
```

Indexes: `entries(created_at DESC)`, `entries(unlock_at)`, `entries(notebook_id, page_id)`.

---

## NuGet Packages

| Package | Version |
|---|---|
| `Avalonia` | 11.2.3 |
| `Avalonia.Desktop` | 11.2.3 |
| `Avalonia.Themes.Fluent` | 11.2.3 |
| `Avalonia.Diagnostics` | 11.2.3 (debug only) |
| `CommunityToolkit.Mvvm` | 8.2.2 |
| `Microsoft.Data.Sqlite` | 10.x |

---

## Performance Targets

| Operation | Target | Approach |
|---|---|---|
| Window visible | < 500ms | Show window before initialising services |
| Passphrase verify | 200–800ms | PBKDF2 on background thread |
| Entry submit | < 100ms | Encrypt + insert on background thread |
| Sealed list load | Near-instant | Indexed query, no decryption required |
