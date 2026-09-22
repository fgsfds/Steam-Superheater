# agent.md

Instructions for AI coding agents working in the **Superheater** repository.

## Project overview

Steam Superheater is a cross-platform (Windows + Linux / Steam Deck) desktop app that
installs and manages must-have fixes and patches for Steam games: (ultra)widescreen
fixes, crash workarounds, unofficial patches, source ports, and no-intro fixes. Fixes
come in three flavours: file fixes (replace/delete/patch files in the game folder),
registry fixes (add/change registry values), and hosts fixes (edit the Windows hosts
file). It also ships an editor for user-submitted fixes.

- UI: **Avalonia** desktop app, MVVM via **CommunityToolkit.Mvvm**.
- Runtime: **.NET 10**, nullable enabled, implicit usings, file-scoped namespaces.
- Persistence: **SQLite** via EF Core (`Superheater.db`).
- Networking: `HttpClient` against GitHub raw JSON and S3; fixes archives are verified
  with SHA-256 (the legacy `MD5` field is obsolete).
- Content DB: versioned JSON files under `db/` (`fixes.json`, `news.json`, `data.json`).

## Build, run, test

The projects target **.NET 10** (`net10.0`); CI installs the `10.0.x` SDK. Central package
management is on (`Directory.Packages.props`); never hard-code package versions in a
`.csproj`.

```pwsh
dotnet restore
dotnet build                          # builds Superheater.slnx
dotnet run --project src/Avalonia.Desktop
```

`global.json` selects the **Microsoft.Testing.Platform** runner, so use the
`--project` form (not a bare project path). Tests are split into projects by category,
with shared fixtures in `Tests.Core`:

```pwsh
# pure unit tests — no real filesystem/network/OS (Windows + Linux CI)
dotnet test --project ./src/Tests.Unit/Tests.Unit.csproj --no-build
# tests that touch real storage/network/OS and must run sequentially (Windows + Linux CI)
dotnet test --project ./src/Tests.Unit.Sequential/Tests.Unit.Sequential.csproj --no-build
# database integrity + MinIO (needs MINIO_ACCESS_KEY / MINIO_SECRET_KEY)
dotnet test --project ./src/Tests.Database/Tests.Database.csproj --no-build
```

- `Tests.Unit` and `Tests.Unit.Sequential` run in the normal CI pass on Windows and Linux.
- `Tests.Database` hits real external services (GitHub/S3/MinIO) or requires an elevated
  shell, so it is expected to fail without the MinIO secrets / admin rights; CI runs it
  separately (on `db/**` changes).
- `Tests.Core` is a non-test library holding the shared `Helpers` and test resources; the
  test projects reference it.
- Always build and run the affected test project after a change. There is no separate
  lint command: analyzers run on build.

## Repository layout

```
src/
  Api.Axiom         API request/response message contracts and IApiInterface.
  Api.Client        API implementations (GitHubApiInterface, ServerApiInterface).
  Common.Axiom      Domain entities, enums, Result/Result<T>, JSON contexts, helpers,
                    release provider. Shared layer, no dependency on the UI.
  Common.Client     Config, file/archive/download/upload tools, fix tools
                    (installer/updater/uninstaller/checker), providers, DI helpers.
  Database.Client   EF Core DatabaseContext + DbEntities + Migrations.
  Avalonia.Desktop  App entry point, DI composition root, views, view models, styles.
  Tests.Core        Shared test helpers and resources (non-test library).
  Tests.Unit        xunit v3 pure unit tests.
  Tests.Unit.Sequential  xunit v3 tests that touch real storage/network/OS.
  Tests.Database    xunit v3 database-integrity and MinIO tests.
db/                 fixes/news/data JSON databases shipped with the app.
```

Project references:

```
Api.Axiom          -> Common.Axiom
Common.Axiom       -> (none)
Database.Client    -> Common.Axiom
Api.Client         -> Api.Axiom, Common.Client
Common.Client      -> Api.Axiom, Common.Axiom, Database.Client
Avalonia.Desktop   -> Api.Client, Common.Client
Tests.Core         -> Common.Client
Tests.Unit         -> Tests.Core, Api.Client, Common.Client
Tests.Unit.Sequential -> Tests.Core, Api.Client, Common.Client
Tests.Database     -> Tests.Core, Api.Client, Common.Client
```

Do not introduce a cycle that reverses these.

## Architecture essentials

### Composition root and DI

`src/Avalonia.Desktop/App.axaml.cs` is the entry point. `App.LoadBindings()` builds a
`ServiceCollection` through the `With*()` extension-method helpers
(`WithLogging`, `WithCommon`, `WithDatabase`, `WithProviders`, `WithModels`,
`WithViewModels`, `WithApi`) and resolves services from the built `ServiceProvider`.
Views receive their view models through `IViewModelsFactory`
(`src/Avalonia.Desktop/ViewModels/ViewModelsFactory.cs`) instead of a service locator.

- Register new services in the owning `With*()` helper — the `*Bindings` classes
  (`ModelsBindings`, `ViewModelsBindings`, `CommonBindings`, `ProvidersBindings`,
  `ApiBindings`, `DatabaseBindings`, `LoggingBindings`) — not in `App`.
- Design mode swaps in `ConfigProviderFake` and the `*ProviderFake` implementations via
  `WithProviders(Design.IsDesignMode)`.
- `--dev`, `--offline`, and `--deck` are the runtime modes selected in `Program.Main`.
  Keep every branch working.

### Domain model

- Fixes derive from `BaseFixEntity` (`FileFixEntity`, `RegistryFixEntity`,
  `HostsFixEntity`) with matching `BaseInstalledFixEntity` state objects.
- `FixManager` is the coordination layer: it dispatches install/uninstall/update/check to
  the per-type tools and persists installed state through `IInstalledFixesProvider`.
- Games are `GameEntity`; the fix->game relation is by numeric `GameId`.
- Settings/upvotes/hidden tags/sources live in `IConfigProvider` (backed by the DB), which
  raises `ParameterChangedEvent` so view models can react.

### Providers and data flow

Providers (`FixesProvider`, `GamesProvider`, `InstalledFixesProvider`, `NewsProvider`,
`AppReleasesProvider`) are the stateful coordination layer between the domain model and
the UI. They own caches guarded by a `SemaphoreSlim`; preserve that locking. `IApiInterface`
(`GitHubApiInterface` online, `ServerApiInterface` for the self-hosted backend) is the
network boundary. Offline mode currently branches inside providers/API on
`ClientProperties.IsOfflineMode` and reads the local `db/*.json` files.

### Persistence

- The DB file is `Superheater.db`. `DatabaseContext.OnConfiguring` configures SQLite.
- Migrations live in `src/Database.Client/Migrations` and are applied on startup.
  Add migrations with EF Core tooling; never edit an already-applied migration.
- Startup cleanup in `App.Cleanup()` deletes `*.old`, `*.temp`, `.update`, the `update/`
  folder, and stale `*.db-wal` / `*.db-shm` files.

### Error handling

Operations that can fail return `Result` / `Result<T>` (`Common.Axiom`) with a `ResultEnum`
and message rather than throwing across layer boundaries. Use exceptions for programmer
errors and unexpected states, and log user-facing failures through `ILogger`.

## Code conventions (mandatory)

- **XML documentation** on public members follows the existing style (`<summary>`,
  `<param>`, `<returns>`, `<inheritdoc />`). `GenerateDocumentationFile` is on.
- **Discard intentionally-unused results** with `_ =`, e.g. `_ = services.WithCommon();`,
  `_ = sb.Append(...)`, `_ = cache.Remove(x);`.
- Use `var` everywhere; never spell out the type when it is apparent.
- File-scoped namespaces only, matching folder structure.
- Allman braces, 4 spaces, **CRLF** line endings, no trailing whitespace.
- Naming: interfaces start with `I`; types and non-field members are PascalCase; private
  fields are `_camelCase`.
- Mark new classes `sealed` by default. Only leave a class unsealed when it is a
  base/abstract type actually designed to be inherited.
- Async: suffix `Async`, accept/propagate `CancellationToken` where the surrounding API
  does, and use `ConfigureAwait(false)` in library code (Common.Axiom, Common.Client,
  Api.Axiom, Api.Client, Database.Client) as the existing code does.
- Prefer modern C# the codebase already uses: collection expressions (`[...]`), pattern
  matching, `required`/`init`, records, `readonly` collections, raw strings, local
  functions.
- `AnalysisMode` is `All`. Do not blanket-disable analyzer rules; if a suppression is
  genuinely required, use a narrowly scoped `#pragma warning disable` with a reason, or
  add the specific rule to `.editorconfig` with justification.
- Do not add code comments unless they explain non-obvious intent; the codebase relies on
  XML docs instead of inline narration.

## Testing conventions

Tests are split into projects by category, with shared fixtures in `Tests.Core`:

- `src/Tests.Core` — non-test library with the shared `Helpers` and test resources
  (`Resources/`). Referenced by every test project; do not add tests here.
- `src/Tests.Unit` — pure unit tests that do not touch the real filesystem, network, OS
  state (hosts/registry), or MinIO. Use fakes (`ConfigProviderFake`, `*ProviderFake`,
  `Moq`, stubs). Runs in the normal CI pass on Windows and Linux.
- `src/Tests.Unit.Sequential` — tests that touch real storage/network/OS (install/uninstall
  against a game folder, temp files, the real hosts file/registry, GitHub/S3) and must run
  sequentially (`xunit.runner.json` sets `parallelizeTestCollections: false`). Runs in the
  normal CI pass.
- `src/Tests.Database` — database-integrity and MinIO tests; needs
  `MINIO_ACCESS_KEY` / `MINIO_SECRET_KEY`. Runs separately (on `db/**` changes).

- One test class per subject, `sealed`, named `<Type>Tests`, namespace matching the project
  (`Tests.Unit`, `Tests.Unit.Sequential`, `Tests.Database`).
- Use xunit `[Fact]`/`[Theory]`, `[InlineData]`, and `Moq` for dependencies.
- Test observable behavior through public APIs; avoid testing private members.
- Put a test in the lowest category that can run it: prefer `Tests.Unit`; use
  `Tests.Unit.Sequential` when it touches real storage/network/OS; use `Tests.Database`
  only when it needs MinIO or validates `db/*.json`.
- Do not use `[Collection("Sync")]`; the sequential project serializes via
  `xunit.runner.json`.

## Guardrails

- Never hard-code package versions; edit `Directory.Packages.props`.
- Never commit secrets, tokens, or the MinIO credentials.
- Do not change `Directory.Build.props` shared properties (TFM, version, nullable,
  analyzer mode) as a shortcut for a local compile fix.
- Keep `db/*.json` valid: they are validated by the database tests and CI.
- When adding a fix type, update the matching enum, entity, installed-entity, installer/
  updater/uninstaller, and `FixManager` dispatch; do not special-case logic in view models.
- Prefer editing existing files and following neighboring patterns over creating new
  abstractions.
- Never commit, amend, push, or open pull requests unless the user explicitly asks.
- Commit messages start with a capital letter and use `Fixed` / `Added` / `Updated`
  rather than `Fix` / `Add` / `Update`.
