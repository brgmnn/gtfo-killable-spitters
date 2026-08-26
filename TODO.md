# TODO

Deferred work and known limitations. Fixed items get removed, not checked off.

## Tooling

- [ ] Extend `update-dependencies.py` to also refresh the checked-in reference
      DLLs in `KillableSpitters/plugins/` when it bumps a pin — the metadata
      files and the compile references currently drift apart silently
      (AmorLib sat at 1.2.1 while the manifest pinned 1.2.7 until Aug 2026).
- [ ] Add a `[build]` section (icon/readme/outdir/copy) to `thunderstore.toml`
      so `tcli build` works locally; today only the CI `gen-manifest` +
      `tcli publish --file` path is supported.
- [ ] `KillableSpitters.csproj` derives `AssemblyName` and all reference paths
      from `$(SolutionName)`/`$(SolutionDir)` — building the csproj directly
      (not via the `.sln`) silently breaks. Consider hardcoding or guarding.
- [ ] `icon.afdesign` (12.3 MB) is tracked in plain git — consider Git LFS or
      moving it out of the repo.

## Mod behavior

- [ ] Config values are snapshotted once in `Plugin.Load()` (`Plugin.cs`) —
      BepInEx config reloads have no effect until restart, and the `Config_*`
      static setters are publicly mutable by other plugins.

## Accepted limitations (documented in code, revisit if they start to matter)

- Host migration is out of scope (`SpitterKillManager`).
- A late joiner sees the default glow until the spitter's next hit
  (`SpitterVisuals`).
- The `DoorEnemyFixUpdated` ≤ 1.1.3 glue-crash suppression
  (`Fix_SpitterGlueCollisionCrash`) papers over a foreign bug; the real fix
  belongs upstream in DoorEnemyFixUpdated.
