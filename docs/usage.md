# Using Superheater

Superheater installs community fixes for Steam games on Windows and Linux, including Steam Deck. File fixes replace, remove, or patch files; registry and hosts fixes modify Windows settings. Text fixes provide manual instructions.

## Install and launch

1. Download the matching archive from the [release page](https://github.com/fgsfds/Steam-Superheater/releases).
2. Extract it into a dedicated writable folder. Published builds are self-contained.
3. Run `Superheater.exe` on Windows, or make `Superheater` executable and run it on Linux.
4. Select a game and fix. Read its description, platforms, dependencies and variants.
5. Close the game before installing, then launch it through Steam.

Steam must already be installed. Registry and hosts fixes are Windows-specific; run elevated only when the selected fix requires it. Steam Deck setup starts in Desktop Mode. Run a Proton game once before installing DLL overrides. Prefix discovery checks the game's library before native, Flatpak and Snap locations. Existing user overrides are preserved; uninstall removes only newly added entries. See the [Deck guide](https://github.com/fgsfds/Steam-Superheater/wiki/Deck-install-guide).

## Installation and recovery

Keep `<game>/.superheater` while fixes are installed: it contains originals and installed records needed for uninstall. New backups use fix GUIDs; legacy records remain readable. Game updates and other mods can overwrite installed fixes, so check integrity after a game update.

File installs and updates stage the full archive/shared-fix tree before changing the game. A durable before-image journal covers affected files, backups, Proton registries and installed metadata. Cancellation or failure restores those snapshots. Unfinished journals are replayed on game discovery or retry. If recovery reports an error, preserve `.superheater`, `.superheater-transactions` and the log for investigation. Concurrent edits by other programs are outside this recovery guarantee. Post-install programs run after commit and their effects cannot be undone automatically.

Existing external files are refused rather than overwritten without an external backup mapping. New external destinations remain supported. Registry/hosts operations do not use the file-install journal.

App updates stage under `update/` beside the executable. The app closes, checks the staged executable header/checksum, swaps it and restarts with the same arguments. Replacement or launch failures restore the old executable; its `.old` backup is removed on a later startup. The staging checksum detects local corruption, not publisher authenticity.

## Data locations

| Data | Location |
| --- | --- |
| Log and crash logs | Beside the executable |
| Settings/catalog SQLite database | `Superheater.db` beside the executable |
| Installed records | `<game>/.superheater/<fix-guid>.json` |
| Original files | Subfolders of `<game>/.superheater/` |
| Pending rollback journals | `<game>/.superheater-transactions/` |
| Fix downloads | Normally beside the app; repository mode uses its configured `fixes` folder |
| Offline catalog | `db/fixes.json`, `db/news.json`, `db/data.json` |

Storage is independent of the launch directory. If the portable database does not exist, startup imports a legacy launch-directory database using SQLite backup and retains the original. An existing portable database takes precedence. Close the app before copying its database and preserve SQLite sidecars; startup cleanup does not delete them. Corrupt installed JSON is quarantined, and legacy migration retains the original record.

## Launch options

| Option | Behavior |
| --- | --- |
| `--offline` | Uses local catalogs, disables app-update checks and service mutations. Archive downloads may still use the network. |
| `--deck` | Forces Deck game-mode behavior; Linux also attempts detection. |
| `--dev` | Developer behavior, including suppressed installation counts. It is not a sandbox. |

Copy the repository's `db` folder beside the executable for an offline catalog. Source builds also search parent directories. Release archives package the executable without the catalog. Catalog browsing does not supply archives.

Backend capabilities control commands: the default GitHub backend supports uploads separately from direct catalog editing. Unsupported voting, reporting and catalog/news changes are disabled with explanations. Export entries for maintainer review when direct publishing is unavailable.

## Integrity and troubleshooting

Local, cached and downloaded archives require a valid SHA-256 unless you explicitly bypass verification. Missing hashes and mismatches fail; cancellation stops installation. Archive validation rejects traversal, rooted entries, links and conflicting destinations. Hashes are not malware scans or publisher signatures; review unexpected mismatches before bypassing them.

| Symptom | Check |
| --- | --- |
| No games | Steam libraries, mounted drives and filters. |
| Missing Proton registry | Run the game once and mount its library; look for `pfx/user.reg`. |
| Cannot install | Close the game and check app/game permissions. Some Windows fixes need elevation. |
| Missing offline catalog | Put all three JSON files under `db/`; archives are separate. |
| Recovery fails | Preserve both recovery directories and the log; do not delete backups to retry. |
| Settings seem missing | Check the database beside the executable and the retained legacy database. |

Report app version, OS, Steam installation type, game App ID, fix/version, reproduction steps and the log. Review logs for personal paths before sharing.
