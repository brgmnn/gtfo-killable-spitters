# Killable Spitters

Makes GTFO's infection spitters killable.

- **Shoot them**: spitters have a bullet health pool (default 30) and pop with
  a full death explosion and gib burst when it runs out. They flash and glow
  hotter as they take damage.
- **C-foam them**: a foamed spitter dies after a configurable delay
  (default 5 seconds).
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
| `SpitterHealth`          | `30.0`  | Bullet health pool. Only the host's value applies. |
| `SpitterGlueKillSeconds` | `0.7`   | Seconds after being C-foamed before a spitter dies. `0` or less keeps the vanilla freeze-only behavior. Only the host's value applies. |

## Links

Source, issues, and changelog:
https://github.com/brgmnn/gtfo-killable-spitters
