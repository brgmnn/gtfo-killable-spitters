# Killable Spitters

![Dynamic JSON Badge](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fexperimental%2Fpackage%2Fthe_tavern%2FKillableSpitters%2F&query=%24.latest.version_number&style=flat&label=Version&color=%2300aaff&cacheSeconds=10800)
![Dynamic JSON Badge](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2Fthe_tavern%2FKillableSpitters%2F&query=%24.downloads&suffix=%20downloads&style=flat&label=GTFO&color=%23c32918&cacheSeconds=10800)

A GTFO mod that makes infection spitters killable. Shoot them, melee them, blow
them up, or kill them with C-foam. Spitters explode on death with the flyer
death animation, are host-authoritative, and are synced to all players including
late joiners.

Also includes a spitter targeting fix: spitters no longer aggro onto AI bot
teammates, and lobbies with more than 4 players are handled correctly.

## Installation

Install from Thunderstore as `the_tavern-KillableSpitters` using r2modman or
Gale.

## Configuration

Config file: `BepInEx/config/the_tavern-KillableSpitters.cfg`

| Setting (section `General`) | Default | Description                                                                                                                                                                  |
| --------------------------- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SpitterHealth`             | `30.0`  | Health pool for killable spitters (drained by any damage type). Only the host's value applies.                                                                               |
| `SpitterFreezeDuration`     | `0.7`   | Seconds a C-foamed spitter stays frozen — before it dies (if `CfoamKillsSpitters` is on) or thaws back to normal (if off). Only the host's value applies.                    |
| `CfoamKillsSpitters`        | `true`  | Whether C-foam kills spitters (foamed spitter dies with the destruction burst, no infection pop). Off keeps the vanilla freeze-only behavior. Only the host's value applies. |
