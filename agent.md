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
- Networking: `HttpClient` against GitHub raw JSON and S3; fixes archives are downloaded
  with SHA-256/MD5 verification.
- Content DB: versioned JSON files under `db/` (`fixes.json`, `news.json`, `data.json`).

## Build, run, test

.NET SDK is pinned to the `10.0.x` band. Central package management is on
(`Directory.Packages.props`); never hard-code package versions in a `.csproj`.

```pwsh
dotnet restore
dotnet build                          # builds Superheater.slnx
dotnet run --project src/Avalonia.Desktop
```

`global.json` selects the **Microsoft.Testing.Platform** runner, so use the
`--project` form (not a bare project path):

```pwsh
dotnet test --project ./src/Tests/Tests.csproj --no-build
```

Test filtering uses MTP/xunit v3 trait syntax (not the VSTest `--filter "Category~…"`):

```pwsh
# everything except the database-integrity tests
dotnet test --project ./src/Tests/Tests.csproj --no-build --filter-not-trait "Category=Database"
# only the database-integrity tests (needs MINIO_ACCESS_KEY / MINIO_SECRET_KEY)
dotnet test --project ./src/Tests/Tests.csproj --no-build --filter-trait "Category=Database"
```

- `Tests` is the only test project. Pure unit tests run in the normal pass; tests that
  touch external services, the network, or the real Windows hosts file are marked
  `[Trait("Category", "Database")]` and run separately.
- The `Category=Database` tests hit real external services (GitHub/S3) or require an
  elevated shell, so they are expected to fail without the MinIO secrets / admin rights;
  CI runs them separately (on push only).
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
                    (installer/updater/uninstaller/checker), providers, bindings.
  Database.Client   EF Core DatabaseContext + DbEntities + Migrations.
  Avalonia.Desktop  App entry point, DI composition root, views, view models, styles.
  Tests             xunit v3 test project.
db/                 fixes/news/data JSON databases shipped with the app.
```

Project references:

```
Api.Axiom          -> (none)
Common.Axiom       -> (none)
Database.Client    -> Common.Axiom
Api.Client         -> Api.Axiom, Common.Axiom, Common.Client
Common.Client      -> Api.Axiom, Common.Axiom, Database.Client
Avalonia.Desktop   -> Api.Client, Common.Client
Tests              -> Api.Client, Common.Client
```

Do not introduce a cycle that reverses these.

## Architecture essentials

### Composition root and DI

`src/Avalonia.Desktop/App.axaml.cs` is the entry point. `App.LoadBindings()` populates a
`ServiceCollection` through the `Load(container, isDesigner)` helpers
(`ModelsBindings`, `ViewModelsBindings`, `CommonBindings`, `ProvidersBindings`,
`ApiBindings`) and resolves everything through the static `BindingsManager.Provider`.

- Register new services in the owning `*Bindings.Load` helper, not in `App`.
- Design mode swaps in `ConfigProviderFake` and the `*ProviderFake` implementations.
- `--dev`, `--offline`, and `--deck` are the runtime modes selected in `Program.Main`.
  Keep every branch working.

> NOTE: replacing the static `BindingsManager` service locator with `With*()` extension
> methods and instance-based view models is planned; until then, follow the existing
> `BindingsManager` pattern when adding services.

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
- **Discard intentionally-unused results** with `_ =`, e.g. `_ = services.WithConfig();`,
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

- One test class per subject, `sealed`, named `<Type>Tests`, namespace `Tests`.
- Use xunit `[Fact]`/`[Theory]`, `[InlineData]`, and `Moq` for dependencies.
- Mark tests that touch external services/network with `[Trait("Category", "Database")]`.
- Test observable behavior through public APIs; avoid testing private members.
- Do not add network/disk-dependent tests to the non-database set; isolate with fakes
  (`ConfigProviderFake`, `*ProviderFake`, stubs).

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
