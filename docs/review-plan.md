# App review and improvement plan

Reviewed 2026-11-05. This is a source/configuration review plus dependency build/test validation, not an interactive UI or Linux runtime audit. Findings below describe the original review. Status notes identify implemented work; other proposals remain open.

## Completed dependency refresh

All versions below were resolved from configured NuGet sources, using stable releases.

| Packages | Before | After |
| --- | --- | --- |
| Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent | 12.1.3 | 12.1.5 |
| Microsoft.EntityFrameworkCore.Design / Sqlite | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.DependencyInjection / Http / Logging / Logging.Abstractions / Logging.Debug | 10.0.12 | 10.0.14 |
| System.IO.Hashing | 10.0.12 | 10.0.14 |
| Octopus.Octodiff | 2.0.549 | 2.0.551 |
| Microsoft.NET.Test.Sdk | 18.10.1 | 18.11.0 |
| Moq | 4.20.72 | 4.21.0 |
| xunit.runner.visualstudio | 3.1.5 | 4.0.1 |
| xunit.v3 | 3.2.2 | 4.0.2 |

Other central package versions were already current in the NuGet report. No framework migration was needed. The xUnit package/adapter major updates build and pass the executed suites without source changes.

Validation on Windows x64, .NET SDK 10.0.403:

- Restore and solution build succeeded: 0 errors, 653 warnings.
- Windows and Linux Release publish profiles both succeeded on the Windows host. A subsequent default no-restore build needed `dotnet restore` to restore the Windows runtime assets after Linux publishing; the final build then succeeded.
- Unit suite: 49 passed.
- Sequential suite excluding `HostsFixTests`: 22 passed. The excluded fixture writes to hard-coded `C:\games\test game\`; it was not run on this host.
- NuGet's post-update outdated and vulnerability reports returned no listed updates or known vulnerable packages, including transitive packages in the vulnerability query.
- The database/service suite was not run: it includes a real remote upload and a MinIO credential requirement.
- Linux runtime behavior and interactive UI behavior remain unverified.

Warnings include missing XML documentation, disposable ownership, and an obsolete Avalonia `TextBox.Watermark` property. No pre-update warning baseline was collected, so the total is not attributed to the updates.

## Prioritized findings

Priorities: P1 = integrity, security, or recovery work to do first; P2 = correctness/reliability; P3 = maintenance. Effort is relative (S/M/L), not a delivery estimate. Source links identify methods; line numbers are intentionally omitted so the plan survives edits.

### 1. P1 — Validate archive destinations before any mutation (M)

**Status:** Archive inventory and extraction now share preflight validation for relative paths, containment, links, duplicate destinations, and file/directory conflicts. Variant removal uses an ordinal leading-prefix slice. New installations prepare the archive before moving originals or removing backups. ZIP/7z and installer regression tests cover these paths. Filesystem symlink coverage runs where link creation is permitted; it is explicitly skipped otherwise. Full update rollback and protection from concurrent filesystem/archive replacement remain part of items 3 and 6.

**Evidence:** [ArchiveTools](../src/Common.Client/FilesTools/ArchiveTools.cs), `UnpackArchiveAsync` and `GetListOfFilesInArchive`, combine archive entry names with output paths without a canonical containment check. The resulting inventory also feeds backup operations. Variant selection uses prefix matching, while removal uses `Replace`, which can strip repeated directory segments beyond the leading prefix.

**Impact:** Crafted archive names can escape the intended extraction root; nested names matching the variant prefix can be rewritten incorrectly. This is an application-level validation gap, not a claim that a specific live catalog archive is malicious.

**Plan:** Preflight all entries and the backup inventory with one validated path mapping. Reject rooted/traversing entries, check canonical destinations against the selected extraction root, define symlink/reparse-point handling, and remove only the leading variant prefix with ordinal comparison. Preserve explicitly supported external install roots without allowing an archive to escape them.

**Acceptance:** Temporary-directory fixtures cover `../`, absolute paths, both separators, sibling-prefix collisions, links, repeated variant names, and normal ZIP/7z archives. Rejection must leave both destination and backups untouched.

### 2. P1 — Make verification cancellation fail closed (S–M)

**Status:** Implemented cancellation/error propagation and required SHA-256 checks for local, cached, and downloaded archives. Only an explicit bypass skips verification, including for cached files. Missing/malformed hashes return a hash error; hash failures preserve their diagnostic. Update preflight runs before uninstall, and the UI keeps the update path when retrying with a bypass. Regression tests cover cancelled checks (before and after verification starts), unchanged originals, local/cache policy, downloads, missing files, and update failures. Legacy parameter renaming is deferred as a mechanical cleanup; the user-facing warning now says SHA-256.

**Evidence:** [FileFixInstaller](../src/Common.Client/FixTools/FileFix/FileFixInstaller.cs), `CheckFileHashAsync`, catches `OperationCanceledException` and returns success. `CheckAndDownloadFileAsync` immediately accepts local file URLs; a downloaded hash failure returns the download result's message instead of the hash error. Existing cached files can be checked with a null expected hash when the skip flag is used.

**Plan:** Distinguish verified, cancelled, missing-hash, and explicitly bypassed verification. Return `Cancelled` on cancellation and propagate the hash error message. Apply an explicit policy to local files and absent hashes; remove accidental null-hash behavior. Rename legacy MD5 parameters/UI copy in a separate mechanical step after behavioral tests.

**Acceptance:** Test pre-cancelled and mid-stream cancellation, mismatches, missing hashes, cached archives, local file URLs, and explicit override. Cancellation must not proceed to installation or report successful verification.

### 3. P1 — Stage installations and preserve recovery data (L)

**Evidence:** [FileFixInstaller](../src/Common.Client/FixTools/FileFix/FileFixInstaller.cs), `InstallFixAsync`, moves/deletes original files before download and verification complete. `CreateAndGetBackupFolder` deletes an existing backup directory named from the fix name. Shared fixes are installed before the parent finishes. [FixManager](../src/Common.Client/FixTools/FixManager.cs) persists installed state only after installation.

**Impact:** A network/hash/patch failure can leave a game partially modified without a completed installed record; retrying can destroy the previous backup.

**Plan:** Download, validate, and stage first. Use stable fix identity and an operation journal; retain existing backups until commit. Record each mutation, roll it back on failure, and handle parent/shared-fix dependencies as one recoverable operation. Define update and retry semantics before implementation.

**Acceptance:** Fault injection at download, backup, extract, patch, shared-fix install, and metadata save boundaries. Original files and existing backups must survive failure; restart recovery and retry must be deterministic.

### 4. P1 — Acquire startup ownership before cleanup and SQLite access (M)

**Status:** Implemented ownership and writable-folder checks before update, logging, cleanup or migrations. Cleanup now targets only the known executable backup and leaves SQLite recovery sidecars/unrelated files intact. Tests cover competing ownership, failed write probes, release/reacquisition, and scoped cleanup.

**Evidence:** [App](../src/Avalonia.Desktop/App.axaml.cs), `Run`, calls `Cleanup` and migrates the database before checking write access or acquiring the mutex. `Cleanup` deletes all matching `.old`, `.temp`, `.db-wal`, and `.db-shm` files in the executable directory. It compares an absolute enumerated filename with the relative `.update` marker.

**Impact:** A second launch can touch files owned by the first; SQLite recovery sidecars and unrelated files can be removed. The intended marker match is ineffective for a full path.

**Plan:** Acquire single-instance ownership before update/cleanup/database mutations, including the update branch in `Program.Main`. Check permissions before writes. Let SQLite manage recovery sidecars. Limit cleanup to app-owned staging artifacts and dispose/release ownership on all exits.

**Acceptance:** Run two processes, simulate an interrupted SQLite write, launch in a read-only location, and place unrelated `.old`/`.temp` files beside the app. State and unrelated files must remain intact; the second process must exit before mutations.

### 5. P1 — Persist installed state atomically and preserve corrupt records (M)

**Status:** Implemented serialized reads/writes, flushed temporary-file replacement before cache publication, failure-safe removal, and structured legacy migration with preserved originals. Corrupt records are quarantined rather than deleted. Tests cover failed writes/removal, migration, corruption, and concurrent snapshots.

**Evidence:** [InstalledFixesProvider](../src/Common.Client/Providers/InstalledFixesProvider.cs) mutates `_cache` before disk writes/deletes succeed. `UpdateCacheAsync` deletes JSON that still fails deserialization after legacy rewriting. Read initialization uses a semaphore, but writes and cached snapshots do not share that protection.

**Plan:** Write to a temporary record and atomically replace it, then update the cache under consistent synchronization. Commit cache removal only after successful deletion. Quarantine unreadable records with diagnostics instead of deleting uninstall evidence. Make legacy migration structured and preserve the original.

**Acceptance:** Simulate permission failures, interrupted writes, corrupt/legacy JSON, and concurrent read/write operations. Cache and disk must agree after failures; unreadable originals must remain recoverable.

### 6. P2 — Unify storage paths and make self-update recoverable (M)

**Evidence:** [DatabaseBindings](../src/Common.Client/DI/DatabaseBindings.cs) and [DatabaseContext](../src/Database.Client/DatabaseContext.cs) use a relative SQLite path. [AppUpdateInstaller](../src/Common.Client/AppUpdateInstaller.cs) creates `.update` relative to the current directory, while [Program](../src/Avalonia.Desktop/Program.cs) checks beside the executable. `InstallUpdate` moves away the old executable before checking/replacing the staged executable, with no rollback.

**Plan:** Centralize app paths, define portable-mode storage explicitly, and migrate/discover existing databases without silently losing settings. Validate the staged executable and integrity metadata before swapping; restore the old executable on failure. Preserve launch flags and working directory across restart.

**Acceptance:** Launch from a different directory; test missing/corrupt staged output, a locked executable, and failures between moves. The app must retain a launchable version and use one consistent database/marker location.

### 7. P2 — Make operation cleanup exception-safe in view models (M)

**Evidence:** [MainViewModel](../src/Avalonia.Desktop/ViewModels/MainViewModel.cs), the installation helper, subscribes progress and sets `LockButtons` before dependency dialogs/operations, but cleanup occurs only on the normal path. [EditorViewModel](../src/Avalonia.Desktop/ViewModels/Editor/EditorViewModel.cs), `UploadFixAsync`, also subscribes before validation and can return without unsubscribing. Cancellation-token sources are replaced without disposal.

**Plan:** Put lock/progress/subscription cleanup in `finally`; create and dispose a per-operation cancellation source before exposing Cancel. Extract a small operation coordinator only where it removes duplication. Marshal filesystem-watcher notifications to the UI thread and unsubscribe configuration events during disposal in `SettingsViewModel`.

**Acceptance:** Declined dependencies, validation failures, exceptions, and cancellation all restore controls and remove handlers. A second operation must not duplicate progress notifications or call a stale cancellation source.

### 8. P2 — Harden download resume and early-failure handling (M)

**Status:** Implemented bounded download retries, open-ended ranges with If-Range identity, Content-Range/length validation, deliberate restart on HTTP 200, integer byte accounting, and cleanup covering initial failures/cancellation. Tests cover interrupted streams, range/ETag mismatches, ignored ranges, 416, and preservation of existing destination files.

**Evidence:** [FilesDownloader](../src/Common.Client/FilesTools/FilesDownloader.cs) constructs the resume range with `contentLength` as an inclusive end (the last byte is length minus one), and does not validate the returned `Content-Range` or final file length. Initial HTTP access and temp-file creation occur before the cleanup/error-handling block.

**Plan:** Request an open-ended resume range or the correct inclusive endpoint; validate start, total length, and resource identity before appending. Handle ignored ranges deliberately. Cover the entire operation with cancellation/error cleanup and reset progress on every exit. Use integer byte counts for accounting.

**Acceptance:** Stub HTTP responses for interrupted transfers, wrong offsets, changed ETags, 200/206/416 responses, missing lengths, initial cancellation, and filesystem errors. Never publish an incomplete or concatenated file as successful.

### 9. P2 — Fix file-picker URI conversion and editor cloning (S)

**Status:** Implemented checked local-file URI conversion for both exports and complete editable metadata cloning, including nested shared fixes. Unit tests cover encoded spaces/Unicode, rejected remote/relative URIs, and independent clone collections.

**Evidence:** [EditorViewModel](../src/Avalonia.Desktop/ViewModels/Editor/EditorViewModel.cs) and [NewsViewModel](../src/Avalonia.Desktop/ViewModels/NewsViewModel.cs) pass `file.Path.AbsolutePath` to filesystem APIs. Encoded spaces and Windows drive paths need native-path conversion. [FileFixEntity.Clone](../src/Common.Axiom/Entities/Fixes/FileFix/FileFixEntity.cs) omits `Changelog` and `IsDisabled`.

**Plan:** Prefer Avalonia storage streams, or a checked native local path when the operation also generates sibling files. Preserve all editable clone fields and independent collections.

**Acceptance:** Export to paths containing spaces, Unicode, and Windows drive prefixes; handle cancellation/non-local storage. Cloning must retain disabled state/changelog without aliasing mutable collections.

### 10. P2 — Resolve Proton prefixes through the discovered Steam libraries (M)

**Evidence:** [SteamTools](../src/Common.Client/SteamTools.cs) discovers several Steam layouts, but [FileFixInstaller](../src/Common.Client/FixTools/FileFix/FileFixInstaller.cs) hard-codes `~/.local/share/Steam/steamapps/compatdata/<id>/pfx/user.reg`. Wine override insertion assumes the section exists. `GetAcfsList` assumes every listed library has a readable `steamapps` directory.

**Plan:** Resolve compatibility data from actual library/game metadata and account for supported package layouts. Handle missing DLL-override sections and duplicate values explicitly. Skip unavailable libraries with useful diagnostics rather than failing the whole scan.

**Acceptance:** Fixtures for default Steam, Flatpak/Snap layouts, secondary libraries, missing mounts, no override section, and pre-existing override values. Include a Steam Deck runtime smoke test before release.

### 11. P2 — Align UI actions with backend capabilities (M)

**Evidence:** [GitHubApiInterface](../src/Api.Client/GitHubApiInterface.cs) returns empty-message errors for votes, reports, statistics, and direct database mutations; it is the backend selected in [ApiBindings](../src/Api.Client/DI/ApiBindings.cs). Offline mode is distributed across providers and does not block all networking.

**Plan:** Expose backend capabilities, disable unavailable actions with an explanation, and define whether offline means catalog-only or strict no-network mode. Keep upload capability separate from catalog editing. Provide actionable errors instead of empty notifications.

**Acceptance:** Exercise GitHub/server/offline capability combinations with fakes. Unavailable commands cannot execute; supported commands remain usable; strict offline mode, if chosen, issues no HTTP requests.

### 12. P3 — Make builds, tests, and releases easier to maintain (M)

**Evidence:** [HostsFixTests](../src/Tests.Unit.Sequential/HostsFixTests.cs) has a hard-coded game path; [DatabaseTests](../src/Tests.Database/DatabaseTests.cs) mixes read-only validation with upload behavior and uses early platform returns. [release.yml](../.github/workflows/release.yml) declares manual dispatch but gates all jobs on push. [build-and-test.yml](../.github/workflows/build-and-test.yml) only triggers on push. [nuget.config](../nuget.config) inherits machine sources.

**Plan:** Isolate fixtures under unique temporary directories; split pure catalog validation, read-only service checks, and explicit upload tests. Add PR build/test coverage. Make manual release semantics deliberate and test both event paths. Consider explicit package sources and dependency automation. Reduce analyzer warnings by category, beginning with disposal and async correctness, then XML docs and deprecated Avalonia APIs; do not broadly suppress them.

**Acceptance:** Normal tests need no production service writes or arbitrary disk paths. Fork PRs build without secrets. Release dry runs show intended jobs for each trigger. Warning counts fall without changing analyzer policy.

## Current implementation validation

- Items 1–2 have separate commits, each including tests and documentation.
- The sequential suite currently reports 52 passed and 1 explicit symlink-privilege skip, excluding the original hard-coded-path hosts fixture.
- Items 3–12 remain open; initial archive preflight is not a transaction or protection against concurrent local filesystem changes.

## Implementation sequence

1. Add isolated regression fixtures, then implement archive validation and verification semantics (items 1–2).
2. Fix startup ownership and persistence (4–5), then unify paths and self-update recovery (6).
3. Implement staged installation/rollback (3), using the validated archive and persistence primitives.
4. Address UI cleanup, downloads, exports, and Steam prefix resolution (7–10), in independently reviewable changes.
5. Align backend/offline UX and improve CI/release/test organization (11–12). Start the test-isolation portion of 12 immediately if needed for safe regression coverage.

For each change: run focused regression tests, the unit suite, relevant sequential tests on Windows/Linux, and a manual failure/recovery scenario where applicable. Runtime and packaging changes require both publish targets. Do not use the live catalog or a real game installation as the first test fixture.
