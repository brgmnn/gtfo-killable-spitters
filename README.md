# Killable Spitters

![Version](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fexperimental%2Fpackage%2Fthe_tavern%2FKillableSpitters%2F&query=%24.latest.version_number&style=flat&label=version&color=%2300aaff&cacheSeconds=10800)
![Downloads](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2Fthe_tavern%2FKillableSpitters%2F&query=%24.downloads&suffix=%20downloads&style=flat&label=GTFO&color=%23c32918&cacheSeconds=10800)
![Thunderstore Likes](https://img.shields.io/thunderstore/likes/the_tavern/KillableSpitters?style=flat&cacheSeconds=10800)

A GTFO mod that makes infection spitters killable. Shoot them, melee them, blow
them up, or kill them with C-foam. Spitters explode on death with the flyer
death animation, are host-authoritative, and are synced to all players including
late joiners.

Also includes spitter bug fixes:

- Spitters no longer aggro onto AI bot teammates, and lobbies with more than
  4 players are handled correctly.
- Mines no longer detonate the moment their laser lines up with a spitter —
  the beam looks past spitters and still triggers on real enemies behind them.
- Sentries using legacy detection no longer stall target acquisition while a
  spitter sits in their detection cone.
- Suppresses a hard crash when foaming a spitter while `DoorEnemyFixUpdated`
  ≤ 1.1.3 is installed (that mod's glue patch crashes on spitters).

Works with [ExtraWeaponCustomization](https://thunderstore.io/c/gtfo/p/Dinorush/ExtraWeaponCustomization/):
EWC custom projectiles, DoTs, explosions and foam all affect spitters. No
config needed, the integration is inactive if EWC isn't installed.

## Installation

Install from Thunderstore as `the_tavern-KillableSpitters` using r2modman or
Gale.

## Configuration

Config file: `BepInEx/config/the_tavern-KillableSpitters.cfg`

| Setting                 | Section   | Default | Description                                                                                                                                                                  |
| ----------------------- | --------- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SpitterHealth`         | `General` | `30.0`  | Health pool for killable spitters (drained by any damage type). Values below `1.0` are raised to `1.0`. Only the host's value applies.                                       |
| `SpitterFreezeDuration` | `C-Foam`  | `0.7`   | Seconds a C-foamed spitter stays frozen — before it dies (if `CfoamKillsSpitters` is on) or thaws back to normal (if off). Only the host's value applies.                    |
| `CfoamKillsSpitters`    | `C-Foam`  | `true`  | Whether C-foam kills spitters (foamed spitter dies with the destruction burst, no infection pop). Off keeps the vanilla freeze-only behavior. Only the host's value applies. |
