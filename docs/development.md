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
dotnet test --project src/Tests.Unit.Sequential/Tests.Unit.Sequential.csproj --no-build
```

| Project | Scope and side effects |
| --- | --- |
| `Tests.Core` | Shared fixtures/resources; not a test suite. |
| `Tests.Unit` | Fake-backed unit tests; 63 passed in final validation. |
| `Tests.Unit.Sequential` | Filesystem, registry, parsing, downloads, and API integration tests; collections are serialized. Tests can use network services and a dedicated HKCU `Layers_test` registry key. |
| `Tests.Database` | Live catalog/archive checks, MinIO listing, and a real remote upload. Not a hermetic unit suite. |

Fixtures use unique temporary roots. Hosts tests edit a copy and explicitly skip without Windows administrator rights; symbolic-link tests skip without link privileges. Documents-placeholder coverage does not write user documents. Registry tests use the dedicated HKCU `Layers_test` key. The full sequential suite reports 85 passed and two privilege skips on the validation host. Local catalog schema and GUID checks run without network access.

External tests have `ServiceRead` and `ServiceWrite` traits. The database workflow selects read-only checks on trusted pushes or manual dispatch, skips PR events, and uses MinIO secrets for listing. Normal PR CI validates local catalogs without those secrets. Upload tests require `SUPERHEATER_ALLOW_SERVICE_WRITES=1` and never run in normal CI. Review `db/data.json` destinations before opting in:

```powershell
# Read-only service checks can download large archives and need MinIO credentials.
dotnet test --project src/Tests.Database/Tests.Database.csproj --no-build --filter-trait Category=ServiceRead
# An intentional real upload requires both this variable and test selection.
$env:SUPERHEATER_ALLOW_SERVICE_WRITES = '1'
dotnet test --project src/Tests.Database/Tests.Database.csproj --no-build --filter-trait Category=ServiceWrite
Remove-Item Env:SUPERHEATER_ALLOW_SERVICE_WRITES
```

The upload guard was verified without sending an upload. Final clean build: zero errors, 648 warnings versus the original 653-warning baseline. Deprecated UI API, fixture ownership and serialization documentation were addressed without broad analyzer suppression; further warning cleanup remains incremental maintenance.

## Publish

Both profiles produce self-contained, untrimmed, single-file x64 applications. Explicit output directories avoid ambiguity in the profiles' relative `PublishDir`:

```powershell
dotnet publish src/Avalonia.Desktop/Avalonia.Desktop.csproj -p:PublishProfile=Windows -c Release -o artifacts/publish/win-x64
dotnet publish src/Avalonia.Desktop/Avalonia.Desktop.csproj -p:PublishProfile=Linux -c Release -o artifacts/publish/linux-x64
```

After publishing a different runtime, run `dotnet restore` again before a default `dotnet build --no-restore`; the shared assets file may only contain the last published runtime (NETSDK1047).

The Windows profile also enables ReadyToRun. Local publishing does not perform SignPath signing. `publish.ps1` is a convenience script using shared output paths; the release workflow signs Windows output and packages the executables into platform-specific ZIP files.

Release jobs run on master for either manual dispatch or a push whose head message contains `[release]`. Other branches and ordinary pushes skip. Both accepted paths sign Windows builds and publish a prerelease: manual dispatch is a real release, not a dry run. Local condition checks do not validate hosted signing credentials.

## Dependency maintenance

```powershell
dotnet list package --outdated
dotnet list package --vulnerable --include-transitive
```

Edit central versions, restore, build, run the affected test suites, and publish both targets when runtime/UI dependencies change. Keep related Microsoft and Avalonia core packages aligned. Independent packages such as Avalonia DataGrid need not share the core package version. Do not enable prerelease updates implicitly.

`nuget.config` clears inherited feeds and selects nuget.org. Dependabot checks NuGet and GitHub Actions weekly. Windows/Linux PR jobs have read-only repository permissions. Vulnerability reports describe known advisories at query time, not a complete security assessment.

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

A typical installation flows from `MainViewModel` through `FixManager` into a per-type installer. File installations stage the complete shared-fix tree, then journal affected files and metadata before mutation. Per-game locking serializes operations; failures restore snapshots and unfinished journals recover on discovery/retry. Post-install programs run after commit. `InstalledFixesProvider` persists the resulting record in the game's `.superheater` folder. That installed state is separate from the SQLite catalog/settings cache.

Providers coordinate catalog/game state and cache access. Preserve their synchronization when changing them; installed-state writes flush temporary records before replacement and cache publication; corrupt records are quarantined, and legacy originals are retained. Offline catalog branches live in providers and the API's data loader. `Result` and `Result<T>` carry expected failures across layers, although several operations still allow exceptions to escape.

SQLite migrations live under `src/Database.Client/Migrations`. Add new migrations with compatible EF Core tooling; do not rewrite applied migrations. Context and DI use `PortableDatabase.ConnectionString`, an absolute path beside the executable. Startup acquires ownership before mutations, imports a legacy launch-directory database only when the portable database is absent, and leaves SQLite sidecars intact.

## Contributing catalog entries

The authoritative schema is in `Common.Axiom/Entities/Fixes` and its source-generated JSON contexts. File, registry, hosts, and text entries are distinct types. Preserve existing GUIDs when updating a fix, set the game App ID and supported platforms, and explain dependencies and variants.

For file fixes, `Sha256` is the archive SHA-256 value; `MD5` is a legacy obsolete field. Record the actual byte size and upstream source. Variant names correspond to folders inside an archive. Review every replacement, deletion, patch, configuration path, and `RunAfterInstall` action. Some paths support absolute destinations or `{documents}`; they must not be treated as harmless game-relative paths without review.

Use the editor to prepare/import entries and export the catalog. `EditorModel.SaveFixesJsonAsync` also generates `fixes.md` beside the JSON and may fetch missing size/hash metadata. Review both generated outputs. The default GitHub backend does not support direct database mutation; uploads use a separate configured endpoint and must not be assumed to publish an entry immediately. Submit reviewed changes through the repository's contribution process.

Keep secrets out of catalog files, code, logs, and commits. The [review plan](review-plan.md) separates current behavior from proposed fixes and gives acceptance tests for each change.
