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

Also includes a spitter targeting fix: spitters no longer aggro onto AI bot
teammates, and lobbies with more than 4 players are handled correctly.

## Configuration

Config file: `BepInEx/config/the_tavern-KillableSpitters.cfg` (section
`General`)

| Setting                  | Default | Description |
| ------------------------ | ------- | ----------- |
| `SpitterHealth`          | `30.0`  | Health pool (drained by any damage type). Only the host's value applies. |
| `SpitterFreezeDuration`  | `0.7`   | Seconds a C-foamed spitter stays frozen — before it dies (if `CfoamKillsSpitters` is on) or thaws (if off). Only the host's value applies. |
| `CfoamKillsSpitters`     | `true`  | Whether C-foam kills spitters (no infection pop). Off keeps the vanilla freeze-only behavior. Only the host's value applies. |

## Links

Source, issues, and changelog:
https://github.com/brgmnn/gtfo-killable-spitters
