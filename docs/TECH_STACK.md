# Line by Line — Tech Stack

## Chosen Stack

Use **C# + .NET + Avalonia + SQLite**.

This stack was chosen because the app should feel like a fast, serious desktop application rather than a browser-wrapped web app. The user specifically does not want to use Tauri + React again.

## Goals

Primary technical goals:

- Fast startup.
- Windows-first desktop experience.
- Local encrypted storage.
- No cloud dependency.
- Minimal terminal-style UI.
- Clean architecture suitable for later expansion.
- Avoid React/Tauri.

Target UX performance:

```text
App window appears: near-instant / under 1 second if possible
Password verification: acceptable if 200–800 ms
Recent entries load: near-instant for normal journal sizes
```

## Runtime and Language

Recommended:

```text
Language: C#
Runtime: .NET 8 or newer
UI Framework: Avalonia
Database: SQLite
Encryption: AES-GCM
Key Derivation: Argon2id preferred, PBKDF2 acceptable for MVP
Packaging: Windows executable/installer
```

## Project Structure

Recommended clean architecture:

```text
LineByLine/
  LineByLine.App/        Avalonia UI
  LineByLine.Core/       Domain models + business rules
  LineByLine.Storage/    SQLite database/repositories
  LineByLine.Crypto/     Encryption, key derivation, password checks
```

For faster early prototyping, it is acceptable to start with one Avalonia project and split into these projects after the core loop works.

## Project Responsibilities

### LineByLine.App

Responsibilities:

- Avalonia UI.
- Window lifecycle.
- Keyboard shortcuts.
- Terminal-style input component.
- State transitions between locked/unlocked screens.
- Rendering recent sealed entries.
- Rendering unlocked entries later.

Should not contain raw cryptographic logic.

### LineByLine.Core

Responsibilities:

- Domain models.
- Entry creation rules.
- Notebook/page hierarchy logic.
- Unlock-date rules.
- No-editing rule.
- Command parsing interfaces.

Should not know about Avalonia UI details.

### LineByLine.Storage

Responsibilities:

- SQLite connection management.
- Schema creation/migrations.
- Notebook/page/entry persistence.
- Query recent entries.
- Query unlocked entries.

### LineByLine.Crypto

Responsibilities:

- Passphrase-based key derivation.
- Vault password verification.
- Entry encryption.
- Entry decryption.
- Nonce generation.
- Secure random generation.

## Startup Performance Strategy

Do not block window creation with expensive work.

On app launch:

```text
1. Create Avalonia window.
2. Show LockedScreen immediately.
3. Focus passphrase input.
4. Then initialize lightweight services if needed.
```

After passphrase submission:

```text
1. Load vault metadata.
2. Derive encryption key from passphrase.
3. Verify passphrase using encrypted vault check.
4. Load recent sealed entries.
5. Render JournalScreen.
```

Key derivation is intentionally slow. This cost should happen only during unlock, not before the window appears.

## UI Framework: Avalonia

Avalonia gives the app a proper desktop identity without using React/Tauri/Electron.

Use Avalonia for:

- Window rendering.
- Input handling.
- Terminal-style layout.
- Keyboard shortcuts.
- Cross-platform potential, though MVP should be Windows-first.

Suggested initial UI components:

```text
MainWindow
LockedScreen
JournalScreen
TerminalInput
EntryList
StatusLine
```

## Database: SQLite

Use SQLite because:

- Local file-based database.
- Fast.
- Reliable.
- No server required.
- Good fit for small structured data.

Recommended access options:

- `Microsoft.Data.Sqlite` for direct SQL control.
- Avoid heavy ORM initially unless the project grows.

SQLite file location should be app-data based, not the project folder.

Potential Windows location:

```text
%APPDATA%/LineByLine/linebyline.vault.db
```

Do not place the vault in an obvious plain-text location like the Desktop or Documents folder by default.

## Cryptography

### Encryption

Use authenticated encryption.

Recommended:

```text
AES-256-GCM
```

Alternative:

```text
XChaCha20-Poly1305 via a trusted library
```

Do not invent a custom encryption scheme.

### Key Derivation

Preferred:

```text
Argon2id
```

Acceptable for MVP:

```text
PBKDF2-HMAC-SHA256 with high iteration count
```

Reason:

- A password/passphrase is not itself a secure encryption key.
- The app must derive a strong key from the passphrase using a KDF.
- KDF parameters should be stored in `vault_meta`.

### Password Verification

Do not store the password or raw key.

Instead, store an encrypted known value in `vault_meta`.

Unlock flow:

```text
passphrase
→ derive key using stored KDF salt/settings
→ try to decrypt password_check_ciphertext
→ if successful, vault unlocked
→ if failed, wrong passphrase
```

### Entry Encryption Flow

On entry submit:

```text
plaintext entry
→ generate nonce
→ encrypt with vault key using AES-GCM
→ generate sealed preview
→ save encrypted_text, nonce, sealed_preview, timestamps
→ clear plaintext from UI state
```

### Security Limitations

MVP does not protect against:

- Malware.
- Keyloggers.
- Admin-level access.
- Someone modifying the local app to bypass unlock timing.
- Someone viewing the screen while the user types.

MVP does protect against casual file browsing and accidental discovery of readable journal files.

## Important Packages to Consider

Potential NuGet packages:

```text
Avalonia
Avalonia.Desktop
Microsoft.Data.Sqlite
System.Security.Cryptography
```

For Argon2id, choose a reputable maintained package after review. If the package choice slows development, start with PBKDF2 and isolate KDF logic behind an interface so Argon2id can replace it later.

Suggested interfaces:

```csharp
public interface IKeyDeriver
{
    byte[] DeriveKey(string passphrase, byte[] salt, KeyDerivationSettings settings);
}

public interface IEntryEncryptor
{
    EncryptedPayload Encrypt(string plaintext, byte[] key);
    string Decrypt(EncryptedPayload payload, byte[] key);
}
```

## Build Milestones

### Milestone 1: Shell + UI Speed

Goal:

```text
App opens immediately to black terminal-style locked screen.
```

Tasks:

- Create Avalonia app.
- Set dark/black window.
- Add monospace text.
- Add focused passphrase input.
- Add basic locked/unlocked UI state, even before real crypto.

### Milestone 2: Local Vault

Goal:

```text
Create vault, unlock vault, persist entries.
```

Tasks:

- Add SQLite database.
- Add `vault_meta` table.
- Add first-run vault creation.
- Add password verification.
- Add default Journal notebook.

### Milestone 3: Entry Sealing

Goal:

```text
Type entry → press Enter → encrypted entry saved → sealed preview shown.
```

Tasks:

- Add entry input behavior.
- Add `Enter` submit.
- Add `Shift + Enter` newline.
- Add AES-GCM encryption.
- Add sealed preview generation.
- Add recent entry query.

### Milestone 4: Time Capsule

Goal:

```text
Entries reveal only after unlock date inside app.
```

Tasks:

- Add `unlock_at` logic.
- Add default delay setting.
- Add `/unlocked` command.
- Add decryption only if current time >= `unlock_at`.

### Milestone 5: Organization

Goal:

```text
Add notebooks and pages while keeping default Journal frictionless.
```

Tasks:

- Add notebook commands.
- Add page commands.
- Add current notebook/page context.
- Keep Journal as page-less default stream.

## Development Command

Initial project creation:

```bash
dotnet new install Avalonia.Templates
dotnet new avalonia.app -o LineByLine
cd LineByLine
dotnet run
```

## Development Principle

Build the core loop before adding polish:

```text
unlock → write → seal → persist → restart → unlock → see sealed preview
```

Do not start with settings, notebooks, export, AI features, cloud sync, or elaborate navigation.
