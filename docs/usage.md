# Using Superheater

Superheater installs community fixes for Steam games on Windows and Linux, including Steam Deck. File fixes can replace, remove, or patch files; registry and hosts fixes modify Windows settings. Text fixes provide instructions instead of an automatic installation.

## Install and launch

1. Download the matching `win-x64` or `linux-x64` archive from the [release page](https://github.com/fgsfds/Steam-Superheater/releases).
2. Extract it into a dedicated folder that you can write to. Avoid sharing this folder with unrelated archives or temporary files: the app has cleanup commands that operate on files in its folder.
3. On Windows, run `Superheater.exe`. On Linux, make `Superheater` executable if necessary (`chmod u+x Superheater`), then run `./Superheater` from its folder.
4. Select an installed game and a fix. Read the description, supported platforms, dependencies, and any variant choices before installing.
5. Close the game, install the fix, and launch the game through Steam as usual.

Published builds are self-contained; building from source requires the .NET SDK. Steam must already be installed. Registry and hosts fixes are Windows-specific; some fixes require administrator rights. Run normally unless the selected fix requires elevation.

For Steam Deck, start in Desktop Mode to extract and configure the app. Launch a Proton game at least once before installing a fix that needs Wine DLL overrides. The current override implementation expects the default Steam compatibility-data location; alternate libraries and Flatpak installations need particular care. See the existing [Deck installation guide](https://github.com/fgsfds/Steam-Superheater/wiki/Deck-install-guide) for the launcher workflow.

## Backups, updates, and removal

Superheater stores original files and installed-fix metadata under `.superheater` inside the game's installation folder. Keep this directory while any fixes are installed. Uninstalling uses that metadata and those backups to remove the fix and restore original files.

Game updates, Steam file verification, and other mods may overwrite patched files. Check the selected fix after a game update and use the available update or reinstall workflow as appropriate. Backups are not a transaction journal: a failed installation can leave partial changes. Preserve the game folder's `.superheater` directory and the log before attempting recovery.

An app update is downloaded into the app folder and staged under `update/`; a subsequent launch applies it. For now, launch from the app's own folder: the update marker and SQLite database use relative paths in some code paths. If an update fails, keep the existing executable and any `.old` copy until you have a working installation.

## Data and file locations

| Data | Current location |
| --- | --- |
| Log | `Superheater.log` beside the executable |
| Crash log | Timestamped `.crashlog` beside the executable, when a log is available |
| Settings, catalog cache, hidden tags, votes, sources | `Superheater.db` in the process working directory |
| Installed-fix records | `<game>/.superheater/<fix-guid>.json` |
| Original-file backups | Subfolders of `<game>/.superheater/` |
| Downloaded fix archives | Normally beside the app; local repository mode uses its configured `fixes` folder |
| Local catalog | `db/fixes.json`, `db/news.json`, and `db/data.json` |

The process working directory and executable directory can differ, especially with shortcuts or `dotnet run`. This is a known issue tracked in the [review plan](review-plan.md). Close the app before copying its database, and preserve SQLite sidecar files with it when present. Do not delete the database as a first troubleshooting step: it also contains settings.

## Launch options and offline use

| Option | Behavior |
| --- | --- |
| `--offline` | Loads local catalog data and disables app-update checks. It does not guarantee that all operations avoid the network. |
| `--deck` | Forces Steam Deck game-mode behavior; Linux also attempts automatic detection. |
| `--dev` | Enables developer behavior, including suppressing installation-count reporting. This is not a dry-run or a sandbox. |

For a portable offline catalog, copy the repository's `db` directory next to the executable. The app also searches parent directories of the executable and current directory, up to eight levels, to support running from source. Release archives currently package the executable, not the catalog.

Offline catalog browsing does not supply fix archives. A missing archive can still cause an attempted download; prepare the required files separately. Uploads and other service-backed features require their services. The default GitHub API implementation does not implement voting, reporting, statistics, or direct database changes; those operations currently return errors.

## Integrity and trust

Superheater does not ask for Steam credentials. Downloaded fix archives can be checked against the catalog's SHA-256 hash. This detects a mismatch when verification completes; it is not a malware scan, signature, or guarantee that the catalog or fix is trustworthy.

The current implementation has exceptions: local file URLs bypass verification, a hash mismatch can be overridden, and cancelling a hash check can report success. Some UI text still calls the SHA-256 check “MD5.” These are documented defects in the [review plan](review-plan.md), not security guarantees. Prefer a verified archive and investigate unexpected mismatches before installing.

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| No games found | Steam installation and library availability; mounted external drives; game/tag/platform filters. |
| Missing Proton compatibility data | Run the game once; confirm the default Steam prefix exists. Alternate-library prefix resolution is a known limitation. |
| Hash mismatch | The upstream archive may have changed. Report the game, fix version, and log rather than assuming the replacement is safe. |
| Cannot write or install | Close the game and check permissions for both the app folder and game folder. Some Windows fix types require elevation. |
| Catalog unavailable offline | Put all three JSON files under `db/` beside the executable. Fix archives are separate. |
| Controls remain disabled after an error | Save the log and restart the app. Exception-safe operation cleanup is planned. Preserve backups before retrying installation. |
| Settings appear to reset between launches | Check whether the launcher changed the working directory and therefore selected another `Superheater.db`. |

For a bug report, include the app version, OS, Steam installation type, game App ID, fix name/version, exact steps, and `Superheater.log`. Review logs for personal paths or other private data before sharing. Include a crash log if one was created.
