# Development and architecture

Read [AGENTS.md](../AGENTS.md) for coding conventions. Package versions are centralized in [Directory.Packages.props](../Directory.Packages.props); shared build settings are in [Directory.Build.props](../Directory.Build.props).

## Build and run

Install a .NET 10 SDK and Git. `global.json` selects Microsoft.Testing.Platform but does not pin an SDK version. CI requests `10.0.x`; the dependency refresh was validated with SDK 10.0.403 on Windows x64.

Run from the repository root:

```powershell
dotnet restore
dotnet build --no-restore
dotnet run --project src/Avalonia.Desktop -- --offline
```

The desktop project defaults to `win-x64`. On Linux, use its explicit configuration:

```sh
dotnet run --project src/Avalonia.Desktop -c Debug-Linux -- --offline
```

Build outputs are under `artifacts/`. Running the app writes state: offline mode is not a sandbox. See [file locations and launch options](usage.md). Local catalog discovery can find the repository's `db/` directory from build output directories.

## Tests

Build before using `--no-build`. With the runner selected in `global.json`, use `--project`, not a positional project path. Pass runner options directly to `dotnet test`; inspect them with `--help` (without an extra `--` separator).

```powershell
dotnet test --project src/Tests.Unit/Tests.Unit.csproj --no-build
dotnet test --project src/Tests.Unit.Sequential/Tests.Unit.Sequential.csproj --no-build --filter-not-class Tests.Unit.Sequential.HostsFixTests
```

| Project | Scope and side effects |
| --- | --- |
| `Tests.Core` | Shared fixtures/resources; not a test suite. |
| `Tests.Unit` | Fake-backed unit tests; 49 passed during this refresh. |
| `Tests.Unit.Sequential` | Filesystem, registry, parsing, downloads, and API integration tests; collections are serialized. Tests can use network services and a dedicated HKCU `Layers_test` registry key. |
| `Tests.Database` | Live catalog/archive checks, MinIO listing, and a real remote upload. Not a hermetic unit suite. |

`HostsFixTests` uses a copied hosts fixture, **not** the Windows hosts file, but its game directory is hard-coded to `C:\games\test game\`. The filtered command above avoids that write on a developer machine; 22 tests passed with this exclusion. Run the full project in a disposable Windows environment until the fixture is isolated. CI currently runs the full sequential project on Windows and Linux. Platform guards sometimes return early instead of reporting a skipped test, so a green Linux run does not establish Windows registry/hosts coverage.

The database suite runs separately for `db/**` changes and manual dispatch. `LooseFilesTest` needs `MINIO_ACCESS_KEY` and `MINIO_SECRET_KEY`; `UploadFixTest` uploads `Resources/test_fix.zip` to the configured service even though it sets offline mode. Only run it against an intentionally selected service environment. The suite is Windows-gated; administrator access is not a blanket prerequisite for its current tests.

```powershell
# External-service suite; review destinations and credentials first.
dotnet test --project src/Tests.Database/Tests.Database.csproj --no-build
```

Build analyzers are enabled; there is no separate lint command. The refresh build succeeded with 653 warnings. Treat this as a baseline to reduce, not a reason to disable analyzers globally.

## Publish

Both profiles produce self-contained, untrimmed, single-file x64 applications. Explicit output directories avoid ambiguity in the profiles' relative `PublishDir`:

```powershell
dotnet publish src/Avalonia.Desktop/Avalonia.Desktop.csproj -p:PublishProfile=Windows -c Release -o artifacts/publish/win-x64
dotnet publish src/Avalonia.Desktop/Avalonia.Desktop.csproj -p:PublishProfile=Linux -c Release -o artifacts/publish/linux-x64
```

After publishing a different runtime, run `dotnet restore` again before a default `dotnet build --no-restore`; the shared assets file may only contain the last published runtime (NETSDK1047).

The Windows profile also enables ReadyToRun. Local publishing does not perform SignPath signing. `publish.ps1` is a convenience script using shared output paths; the release workflow signs Windows output and packages the executables into platform-specific ZIP files.

The release workflow currently requires a push to `master` whose head commit message contains `[release]`. Although `workflow_dispatch` is declared, its jobs require a push event and therefore skip manual dispatch. Releases are created as prereleases. See the [review plan](review-plan.md) before changing this behavior.

## Dependency maintenance

```powershell
dotnet list package --outdated
dotnet list package --vulnerable --include-transitive
```

Edit central versions, restore, build, run the affected test suites, and publish both targets when runtime/UI dependencies change. Keep related Microsoft and Avalonia core packages aligned. Independent packages such as Avalonia DataGrid need not share the core package version. Do not enable prerelease updates implicitly.

`nuget.config` adds nuget.org but does not clear inherited sources. Machine-level feeds may therefore appear in package reports. Reproducible source configuration and CI coverage are follow-up work in the review plan. NuGet vulnerability reports describe known advisories at query time, not a complete security assessment.

## Architecture

| Layer | Responsibility |
| --- | --- |
| `Common.Axiom` | Domain entities, result types, serialization contexts, helpers, release discovery. |
| `Api.Axiom` | API interface and request/response contracts. References `Common.Axiom`. |
| `Database.Client` | SQLite EF Core context, entities, migrations. References `Common.Axiom`. |
| `Common.Client` | Providers, configuration, Steam discovery, installation tools, files, models, DI registrations. References the contracts and database layers. |
| `Api.Client` | GitHub and server API implementations. References API contracts and `Common.Client`. |
| `Avalonia.Desktop` | Composition root, views, view models, controls and styles. References client layers. |

`App.LoadBindings()` registers services through the `With*` extensions and builds the container with scope/build validation. `IViewModelsFactory` supplies view models to views. `ApiBindings.WithApi()` currently selects `GitHubApiInterface`; having `ServerApiInterface` in the tree does not make it the active backend.

A typical installation flows from `MainViewModel` through `FixManager` into a per-type installer. File installations download/check archives, back up affected paths, extract or patch files, and optionally apply Wine overrides or run a post-install file. `InstalledFixesProvider` persists the resulting record in the game's `.superheater` folder. That installed state is separate from the SQLite catalog/settings cache.

Providers coordinate catalog/game state and cache access. Preserve their synchronization when changing them; installed-state writes are not currently transactional. Offline catalog branches live in providers and the API's data loader. `Result` and `Result<T>` carry expected failures across layers, although several operations still allow exceptions to escape.

SQLite migrations live under `src/Database.Client/Migrations`. Add new migrations with compatible EF Core tooling; do not rewrite applied migrations. Both default context configuration and DI currently use the relative connection string `Data Source=Superheater.db`.

## Contributing catalog entries

The authoritative schema is in `Common.Axiom/Entities/Fixes` and its source-generated JSON contexts. File, registry, hosts, and text entries are distinct types. Preserve existing GUIDs when updating a fix, set the game App ID and supported platforms, and explain dependencies and variants.

For file fixes, `Sha256` is the archive SHA-256 value; `MD5` is a legacy obsolete field. Record the actual byte size and upstream source. Variant names correspond to folders inside an archive. Review every replacement, deletion, patch, configuration path, and `RunAfterInstall` action. Some paths support absolute destinations or `{documents}`; they must not be treated as harmless game-relative paths without review.

Use the editor to prepare/import entries and export the catalog. `EditorModel.SaveFixesJsonAsync` also generates `fixes.md` beside the JSON and may fetch missing size/hash metadata. Review both generated outputs. The default GitHub backend does not support direct database mutation; uploads use a separate configured endpoint and must not be assumed to publish an entry immediately. Submit reviewed changes through the repository's contribution process.

Keep secrets out of catalog files, code, logs, and commits. The [review plan](review-plan.md) separates current behavior from proposed fixes and gives acceptance tests for each change.
