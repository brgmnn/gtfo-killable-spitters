# Killable Spitters

A GTFO mod that makes infection spitters killable. Shoot them down with bullets
or kill them with C-foam. Deaths play a full pop + gib burst, are
host-authoritative, and are synced to all players including late joiners.

Also includes a spitter targeting fix: spitters no longer aggro onto AI bot
teammates, and lobbies with more than 4 players are handled correctly.

## Installation

Install from Thunderstore as `the_tavern-KillableSpitters` using r2modman or
Gale. Dependencies (installed automatically): `BepInEx-BepInExPack_GTFO` and
`Amorously-AmorLib`. GTFO-API is bundled inside the package.

> [!WARNING]
> Do not run this mod alongside **AutogenRundown** (v1.0.6 or later). Autogen
> already includes killable spitters; running both double-applies the patches.

## Configuration

Config file: `BepInEx/config/the_tavern-KillableSpitters.cfg`

| Setting (section `General`)  | Default | Description |
| ---------------------------- | ------- | ----------- |
| `SpitterHealth`              | `30.0`  | Bullet health pool for killable spitters. Only the host's value applies. |
| `SpitterGlueKillSeconds`     | `0.7`   | Seconds after being C-foamed before a spitter dies. `0` or less keeps the vanilla freeze-only behavior. Only the host's value applies. |

## Development

```bash
dotnet build KillableSpitters.sln
```

Always build the solution, not the csproj (the project relies on
`$(SolutionDir)`/`$(SolutionName)`).

Local setup: download and extract
[BepInExPack_GTFO](https://thunderstore.io/c/gtfo/p/BepInEx/BepInExPack_GTFO/)
into `deps/BepInEx-BepInExPack_GTFO/` (the folder is gitignored). Game interop
assemblies and referenced plugin DLLs are committed under
`KillableSpitters/interop/` and `KillableSpitters/plugins/`.

The build output is assembled in `build/` as a ready-to-zip Thunderstore
package. On Windows debug builds, the output is also copied into the
`modding-killable-spitters` profile of both r2modman and Gale (create a profile
with exactly that name) under `BepInEx/plugins/the_tavern-KillableSpitters/`.

## Releasing

1. Bump the version in `KillableSpitters/Plugin.cs`, `thunderstore.toml`, and
   `manifest.json` (keep all three in sync).
2. Push to `main` — the `Build .NET` workflow produces the
   `KillableSpitters_<version>` artifact.
3. Create a GitHub release tagged `v<version>`.
4. Run the **Release (Github Artifacts)** workflow to attach the build zip to
   the release.
5. Run the **Release (Thunderstore)** workflow to publish to Thunderstore.
6. Optionally run **Generate CHANGELOG.md** to regenerate the changelog from
   the GitHub releases.

Required repository secrets: `REPO_ADMIN_TOKEN` (releases + changelog commits)
and `THUNDERSTORE_TOKEN` (Thunderstore publish).

## Acknowledgements

Extracted from [AutogenRundown](https://github.com/brgmnn/autogen-rundown),
where the feature was originally developed.
