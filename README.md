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

## Configuration

Config file: `BepInEx/config/the_tavern-KillableSpitters.cfg`

| Setting (section `General`)  | Default | Description |
| ---------------------------- | ------- | ----------- |
| `SpitterHealth`              | `30.0`  | Bullet health pool for killable spitters. Only the host's value applies. |
| `SpitterGlueKillSeconds`     | `0.7`   | Seconds after being C-foamed before a spitter dies. `0` or less keeps the vanilla freeze-only behavior. Only the host's value applies. |
