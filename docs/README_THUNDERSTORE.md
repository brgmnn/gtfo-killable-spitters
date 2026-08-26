# Killable Spitters

Makes GTFO's infection spitters killable.

- **Damage them**: spitters have a health pool (default 30) drained by any
  damage — bullets, melee, explosives, or custom weapons — and pop with a full
  death explosion and gib burst when it runs out. They flash and glow hotter as
  they take damage.
- **C-foam them**: a foamed spitter dies after a configurable freeze
  (default 0.7 seconds). Turn `CfoamKillsSpitters` off to keep C-foam as a
  vanilla-style freeze that never kills.
- **Synced**: deaths are decided by the lobby host and replicated to every
  player, including late joiners. All players should install the mod; the
  host's settings decide the behavior.
- **Works with ExtraWeaponCustomization**: EWC custom projectiles, DoTs,
  explosions and foam all affect spitters. No config needed; ignored if EWC
  isn't installed. (Weapon config authors: EWC classifies spitters as *object*
  targets, so `Trigger`s restricted to `HitEnemy`/`HitPlayer` never match a
  spitter hit. Use `"Trigger": "Hit"` for effects that should also apply
  to spitters.)

Also includes spitter bug fixes:

- Spitters no longer aggro onto AI bot teammates, and lobbies with more than
  4 players are handled correctly.
- Mines no longer detonate the moment their laser lines up with a spitter —
  the beam looks past spitters and still triggers on real enemies behind them.
- Sentries using legacy detection no longer stall target acquisition while a
  spitter sits in their detection cone.
- Suppresses a hard crash when foaming a spitter while `DoorEnemyFixUpdated`
  ≤ 1.1.3 is installed (that mod's glue patch crashes on spitters).

## Configuration

Config file: `BepInEx/config/the_tavern-KillableSpitters.cfg`

| Setting                  | Section   | Default | Description |
| ------------------------ | --------- | ------- | ----------- |
| `SpitterHealth`          | `General` | `30.0`  | Health pool (drained by any damage type). Values below `1.0` are raised to `1.0`. Only the host's value applies. |
| `SpitterFreezeDuration`  | `C-Foam`  | `0.7`   | Seconds a C-foamed spitter stays frozen — before it dies (if `CfoamKillsSpitters` is on) or thaws (if off). Only the host's value applies. |
| `CfoamKillsSpitters`     | `C-Foam`  | `true`  | Whether C-foam kills spitters (no infection pop). Off keeps the vanilla freeze-only behavior. Only the host's value applies. |

## Links

Source, issues, and changelog:
https://github.com/brgmnn/gtfo-killable-spitters
