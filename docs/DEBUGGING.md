# Line by Line — Debugging Notes

Real production bugs found after the first public release, and how they were
diagnosed and fixed. Both were reported by a tester running the downloaded
Windows `.exe` on their own machine — an environment that never appears during
local development — and both were diagnosed from Windows Event Viewer crash
logs (`.evtx`) without ever touching the failing machine.

These are written up postmortem-style: symptom, investigation, root cause, fix,
verification, and the takeaway.

---

## Postmortem 1 — App closes instantly: missing native library

**Release:** `v1.1`
**Symptom:** A tester downloaded `LineByLine-v1.1-win-x64.exe` from the GitHub
release and it closed immediately on launch. They exported their Windows event
logs and sent them over.

### Investigation

The export was a full Application/System log — **116,512 events** of pure noise.
Filtering to events whose message mentioned the app narrowed it to six, of which
the useful pair was a `.NET Runtime` event (ID **1026**, unhandled exception)
and an `Application Error` (ID **1000**).

The `Application Error` only blamed `KERNELBASE.dll` with exception code
`0xe0434352` — which is just "a .NET exception escaped." That's the *symptom*,
not the cause. The real story was in the `.NET Runtime` 1026 event:

```
System.TypeInitializationException: The type initializer for
'SkiaSharp.SKImageInfo' threw an exception.
 ---> System.DllNotFoundException: Dll was not found.
   at SkiaSharp.SkiaApi.sk_colortype_get_default_8888()
   at Avalonia.Skia.PlatformRenderInterface..ctor(...)
   at LineByLine.App.Program.Main(String[] args)
```

The app died while Avalonia was initialising its Skia renderer, because the
native `libSkiaSharp.dll` could not be loaded.

The next step was to reproduce the *artifact*, not the app. Re-running the exact
release publish command locally and listing the output revealed the problem:

```
LineByLine.App.exe      73.79 MB
libSkiaSharp.dll         8.98 MB   <- left loose, next to the exe
av_libglesv2.dll         4.23 MB   <- "
e_sqlite3.dll            1.71 MB   <- "
libHarfBuzzSharp.dll     1.53 MB   <- "
```

`-p:PublishSingleFile=true` does **not** embed native libraries by default — it
leaves them as loose DLLs beside the executable. The release workflow copied and
uploaded **only `LineByLine.App.exe`**. The tester therefore received an exe with
no renderer library, and it crashed the instant Avalonia tried to draw.

It never failed in development because running from the build output (or via
`dotnet run`) always has those four DLLs sitting right next to the binary.

### Root cause

Single-file publish keeps native libraries external by default; the release
pipeline shipped the exe alone.

### Fix

Add to the project file so the native libraries are bundled inside the single
executable and self-extracted at runtime:

```xml
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
```

The publish output collapsed to a single 90 MB `.exe` with **zero loose DLLs**.

### Verification

Re-ran the publish and confirmed the output folder contained only the exe (plus
a `.pdb`), i.e. the native libraries were now embedded.

### Takeaway

- **"Self-contained" is not the same as "single-file-complete."** Bundling the
  .NET runtime does not bundle native dependencies into one file.
- **Test the artifact you actually ship**, ideally on a clean machine — not the
  build tree, which is contaminated with loose dependencies.
- **Read the exception chain inwards.** The OS-level `Application Error` was a
  dead end; the inner `DllNotFoundException` was the answer.

---

## Postmortem 2 — First-run crash: reading the database before it exists

**Release:** `v1.2` (the fix for Postmortem 1)
**Symptom:** The same tester downloaded `v1.2`. It got further — past the
renderer — but still crashed on launch. A second, much smaller event export
arrived.

### Investigation

`v1.2` getting past Skia was itself a useful signal: fix #1 had worked. The new
`.NET Runtime` 1026 event told a completely different story:

```
Microsoft.Data.Sqlite.SqliteException: SQLite Error 1: 'no such table: settings'.
   at LineByLine.App.Data.SettingsRepository.Get(String key)
   at LineByLine.App.Services.SettingsService.get_Transparency()
   at LineByLine.App.ViewModels.MainWindowViewModel..ctor(VaultService, SettingsService)
   at LineByLine.App.App.OnFrameworkInitializationCompleted()
```

The stack pointed straight at the cause. `MainWindowViewModel`'s **constructor**
read the persisted `transparency` setting, which queries the `settings` table.
But the constructor runs at application startup — *before* the user has unlocked
or created a vault. The database schema (including the `settings` table) is only
created by `EnsureSchema`, which runs inside `CreateVault` and `Unlock`. On a
brand-new machine with no vault yet, that table simply does not exist.

This was a regression introduced by the transparency feature. It never appeared
in development because the developer's machine already had a vault — so the
`settings` table had existed for weeks. The bug was only reachable by a user
opening the app for the very first time.

### Root cause

Persisted settings were read in a view-model constructor that executes before
the database schema is guaranteed to exist.

### Fix

Move the read out of the constructor and into `GoToJournal`, which only runs
after the vault is unlocked or set up (i.e. after `EnsureSchema` and `ApplyAll`):

```csharp
// Constructor: no DB access — schema may not exist yet.
public void GoToJournal()
{
    _settings.ApplyAll();
    _isTransparent = _settings.Transparency > 0;  // safe: schema now exists
    CurrentScreen = new JournalScreenViewModel(this, _vault, _settings);
}
```

### Verification

Rather than trust a recompile, the failure condition was reproduced directly:
launch the build with an **empty `%APPDATA%`** to simulate a first-time user with
no vault.

- Before the fix: instant crash (the tester's exact failure).
- After the fix: the app ran and reached the passphrase setup screen.

Shipped as `v1.3`.

### Takeaway

- **Initialization order is a real source of bugs.** "What state is guaranteed
  to exist at this point in the lifecycle?" is a question worth asking for any
  code that runs at startup.
- **Your development environment hides first-run bugs.** Existing local state
  (a database, a config file, a logged-in session) masks anything that only
  breaks on a clean install.
- **Reproduce the user's environment, not your own.** Simulating an empty
  `%APPDATA%` turned an unreproducible field crash into a five-second local test.
- **Project rule going forward:** only touch the `settings` table *after* unlock
  or setup — never from a constructor that runs at startup.

---

## Skills this exercise exercised

- Reading production crash telemetry (`.evtx`) with no debugger and no access to
  the failing machine.
- Filtering signal from a six-figure pile of unrelated events.
- Distinguishing a symptom (OS fault in `KERNELBASE.dll`) from a root cause
  (managed exception deeper in the stack), and reading an exception chain inward
  to its inner cause.
- Understanding the tools beneath the app: the .NET single-file bundler's
  treatment of native libraries, Avalonia's renderer startup, and the SQLite
  schema lifecycle.
- Reproducing failures deterministically — rebuilding the shipped artifact and
  simulating a clean user environment — instead of guessing.
- Verifying each fix against the *exact* condition that failed before declaring
  it done.
- Iterative release discipline under a CI/CD pipeline: `v1.1 → v1.2 → v1.3`,
  each tag a tested, downloadable build.
