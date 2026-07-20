# Killable Spitters

![Dynamic JSON Badge](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fexperimental%2Fpackage%2Fthe_tavern%2FKillableSpitters%2F&query=%24.latest.version_number&style=flat&label=Version&color=%2300aaff&cacheSeconds=10800)
![Dynamic JSON Badge](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2Fthe_tavern%2FKillableSpitters%2F&query=%24.downloads&suffix=%20downloads&style=flat&label=GTFO&color=%23c32918&cacheSeconds=10800)

A GTFO mod that makes infection spitters killable. Shoot them, melee them, blow
them up, or kill them with C-foam — any damage drains a shared health pool.
Deaths play a full pop + gib burst, are host-authoritative, and are synced to
all players including late joiners.

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
| `SpitterHealth`              | `30.0`  | Health pool for killable spitters (drained by any damage type). Only the host's value applies. |
| `SpitterGlueKillSeconds`     | `0.7`   | Seconds after being C-foamed before a spitter dies. `0` or less keeps the vanilla freeze-only behavior. Only the host's value applies. |
