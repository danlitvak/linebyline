# Line by Line — Data Model

## Conceptual Hierarchy

The app uses this hierarchy:

```text
Notebook
  → Page
    → Entry
      → Text
```

Important naming decision:

- In UI/product language, the app is called **Line by Line**.
- Internally, submitted text should be called an **Entry**, not a Line.
- Reason: the user can press `Shift + Enter` to create newlines inside one submitted unit.

## Default Journal Behavior

The app must create a built-in default notebook:

```text
Notebook: Journal
Pages: none
Entries: direct stream
```

Default Journal entries use:

```text
notebook_id = default Journal notebook ID
page_id = null
```

This allows the app to work immediately without requiring users to create notebooks or pages.

## SQLite Schema

Initial schema:

```sql
CREATE TABLE vault_meta (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    kdf_name TEXT NOT NULL,
    kdf_salt BLOB NOT NULL,
    kdf_iterations INTEGER,
    kdf_memory_kib INTEGER,
    kdf_parallelism INTEGER,
    password_check_nonce BLOB NOT NULL,
    password_check_ciphertext BLOB NOT NULL,
    created_at TEXT NOT NULL,
    schema_version INTEGER NOT NULL
);

CREATE TABLE notebooks (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    is_default INTEGER NOT NULL,
    created_at TEXT NOT NULL
);

CREATE TABLE pages (
    id TEXT PRIMARY KEY,
    notebook_id TEXT NOT NULL,
    title TEXT NOT NULL,
    created_at TEXT NOT NULL,
    FOREIGN KEY (notebook_id) REFERENCES notebooks(id)
);

CREATE TABLE entries (
    id TEXT PRIMARY KEY,
    notebook_id TEXT NOT NULL,
    page_id TEXT,
    created_at TEXT NOT NULL,
    unlock_at TEXT NOT NULL,

    encrypted_text BLOB NOT NULL,
    nonce BLOB NOT NULL,

    sealed_preview TEXT NOT NULL,
    preview_version INTEGER NOT NULL,

    is_deleted INTEGER NOT NULL DEFAULT 0,

    FOREIGN KEY (notebook_id) REFERENCES notebooks(id),
    FOREIGN KEY (page_id) REFERENCES pages(id)
);

CREATE INDEX idx_entries_created_at ON entries(created_at DESC);
CREATE INDEX idx_entries_unlock_at ON entries(unlock_at);
CREATE INDEX idx_entries_notebook_page ON entries(notebook_id, page_id);
```

## Table: vault_meta

Stores vault-level metadata and password verification data.

There should only be one row.

Fields:

```text
id
kdf_name
kdf_salt
kdf_iterations
kdf_memory_kib
kdf_parallelism
password_check_nonce
password_check_ciphertext
created_at
schema_version
```

Purpose:

- Store KDF parameters.
- Store salt needed to derive the vault key from the passphrase.
- Store encrypted password-check payload.
- Store schema version for future migrations.

Do not store:

- Plain password.
- Password hash for login.
- Raw encryption key.
- Plaintext journal content.

## Table: notebooks

Stores notebooks.

Fields:

```text
id
name
is_default
created_at
```

Rules:

- One notebook should have `is_default = 1`.
- The default notebook should be named `Journal` in MVP.
- Notebook names should be user-visible and not encrypted in MVP.

Potential future privacy improvement:

- Encrypt notebook names if metadata privacy becomes a stronger goal.

## Table: pages

Stores pages within notebooks.

Fields:

```text
id
notebook_id
title
created_at
```

Rules:

- Pages are optional.
- Default Journal can have direct entries with no pages.
- Custom notebooks may use pages for organization.

Page titles are not encrypted in MVP.

## Table: entries

Stores encrypted journal entries.

Fields:

```text
id
notebook_id
page_id
created_at
unlock_at
encrypted_text
nonce
sealed_preview
preview_version
is_deleted
```

Rules:

- `encrypted_text` stores encrypted plaintext entry.
- `nonce` stores AES-GCM nonce/IV.
- `sealed_preview` stores visual scrambled preview.
- `preview_version` allows future preview algorithm changes.
- `is_deleted` is a soft-delete flag for MVP.
- `page_id` may be null for default page-less Journal entries.

## C# Domain Models

### Notebook

```csharp
public sealed class Notebook
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsDefault { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
```

### Page

```csharp
public sealed class Page
{
    public required string Id { get; init; }
    public required string NotebookId { get; init; }
    public required string Title { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
```

### Entry

```csharp
public sealed class Entry
{
    public required string Id { get; init; }
    public required string NotebookId { get; init; }
    public string? PageId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UnlockAt { get; init; }

    public required byte[] EncryptedText { get; init; }
    public required byte[] Nonce { get; init; }

    public required string SealedPreview { get; init; }
    public required int PreviewVersion { get; init; }

    public required bool IsDeleted { get; init; }
}
```

### Vault Metadata

```csharp
public sealed class VaultMetadata
{
    public required string KdfName { get; init; }
    public required byte[] KdfSalt { get; init; }
    public int? KdfIterations { get; init; }
    public int? KdfMemoryKiB { get; init; }
    public int? KdfParallelism { get; init; }
    public required byte[] PasswordCheckNonce { get; init; }
    public required byte[] PasswordCheckCiphertext { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int SchemaVersion { get; init; }
}
```

## Entry Creation Flow

When the user submits an entry:

```text
1. Read current plaintext input from UI.
2. If empty/whitespace only, do not create entry.
3. Generate entry ID.
4. Set created_at = current time.
5. Set unlock_at based on current delay setting.
6. Generate sealed_preview from plaintext.
7. Generate encryption nonce.
8. Encrypt plaintext using vault key.
9. Insert row into entries table.
10. Clear plaintext from UI state.
11. Refresh recent entries list.
```

## Entry Read Flow

When rendering locked/recent entries:

```text
1. Query recent entries.
2. Render timestamp.
3. Render sealed_preview.
4. Do not decrypt unless entry is unlocked and user is in an unlocked/read mode.
```

When rendering unlocked entries:

```text
1. Query entries where unlock_at <= current time and is_deleted = 0.
2. Decrypt encrypted_text using vault key.
3. Render plaintext.
```

## Unlock Rules

MVP app-enforced rule:

```text
An entry can be decrypted/displayed only if current local time >= unlock_at.
```

Limitations:

- This is not cryptographically enforced.
- A determined technical user may bypass local timing checks.
- This is acceptable for version-one security.

## Sealed Preview Model

`sealed_preview` is stored as plaintext metadata.

This is acceptable for MVP because the user explicitly wants to see the shape of their text.

Information leaked by sealed preview:

```text
entry length
word lengths
spacing
newlines
capitalization pattern
punctuation
timestamp
notebook/page association
```

Information not revealed:

```text
actual words
actual letters
actual readable content
```

Preview generation should be deterministic per entry.

Recommended function signature:

```csharp
public interface ISealedPreviewGenerator
{
    string Generate(string plaintext, string entryId, int previewVersion);
}
```

## Deletion Model

Use soft delete in MVP:

```text
is_deleted = 1
```

This allows safer development and avoids accidental destructive actions.

Potential future option:

```text
/wipe deleted
```

which permanently removes soft-deleted entries.

## Settings Model: Future

Settings can be added later.

Potential table:

```sql
CREATE TABLE settings (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
```

Potential settings:

```text
default_unlock_delay
font_family
font_size
recent_entry_limit
panic_lock_shortcut_enabled
```

Do not add this until needed.

## Data Privacy Notes

Encrypted in MVP:

```text
entry plaintext
```

Not encrypted in MVP:

```text
timestamps
notebook names
page titles
sealed previews
unlock dates
delete flags
```

This matches the current product decision. If metadata privacy becomes important later, encrypt notebook/page titles and potentially replace sealed preview with a less revealing visual block.
