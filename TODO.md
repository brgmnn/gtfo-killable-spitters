# TODO

Deferred work and known limitations. Fixed items get removed, not checked off.

## Release

- [ ] Cut v1.1.1. `main` has shipped-relevant changes since `v1.1.0`: the
      AmorLib pin moved 1.2.2 → 1.2.7 (the published 1.1.0 package was compiled
      against AmorLib 1.2.1), the packaged `CHANGELOG.md` is truncated relative
      to the live v1.1.0 release notes (re-run the changelog workflow first),
      plus the September 2026 bug-fix batch. Bump `Plugin.cs`, `manifest.json`
      and `thunderstore.toml` together (`./update-dependencies.py
      --check-versions` verifies, and CI runs it). Delete the merged
      `origin/deps/thunderstore-updates` branch.

## Tooling

- [ ] Extend `update-dependencies.py` to also refresh the checked-in reference
      DLLs in `KillableSpitters/plugins/` when it bumps a pin — the metadata
      files and the compile references currently drift apart silently
      (AmorLib sat at 1.2.1 while the manifest pinned 1.2.7 until Aug 2026).
- [ ] Record / pin the ExtraWeaponCustomization version the compat layer is
      built against. `plugins/ExtraWeaponCustomization.dll` is 4.12.6 and that
      fact lives only in an `EWCCompat.cs` comment; the dependency script
      iterates `[package.dependencies]` only, so EWC drift is never flagged
      (the workspace EWC source is already 4.13.2).
- [ ] Local `deps/` bootstrap. `deps/` is populated only by the CI-only
      `.github/actions/thunderstore-download.js`, so a fresh clone cannot
      build, and the action names zips without a version so a stale extraction
      never self-corrects (local `deps/` holds BepInExPack 3.2.1 against a
      3.2.2 pin). Share one download routine between the workflow and a local
      script, and stamp the version into the zip name.
- [ ] Add a `[build]` section (icon/readme/outdir/copy) to `thunderstore.toml`
      so `tcli build` works locally; today only the CI `gen-manifest` +
      `tcli publish --file` path is supported.
- [ ] `build.yml` runs `gen-manifest` but discards the result — the checked-in
      `manifest.json` is what ships. Either diff the two and fail on mismatch,
      or drop `manifest.json` from git and package the generated one.
- [ ] `update-dependencies.yml`: when the no-op guard trips, the PR branch is
      left on a stale base and never rebased; there is no `concurrency:`
      group, so a manual dispatch can race the weekly cron; `--check` never
      runs on PRs.
- [ ] `.github/actions/thunderstore-download.js` regex assumes exactly two
      hyphens and a numeric version; a non-matching dependency string throws
      an unhelpful `TypeError` on the destructure instead of naming the entry.
- [ ] `KillableSpitters.csproj` derives `AssemblyName` and all reference paths
      from `$(SolutionName)`/`$(SolutionDir)` — building the csproj directly
      (not via the `.sln`) silently breaks. Consider hardcoding or guarding.
- [ ] `.editorconfig` sets `IDE0005` to error but the csproj never enables
      `EnforceCodeStyleInBuild`, so unused usings never fail CI.
- [ ] `icon.afdesign` (12.3 MB) is tracked in plain git — consider Git LFS or
      moving it out of the repo. Likewise the 126 `KillableSpitters/interop/`
      DLLs (~95% of repo bytes) are regenerable from the BepInExPack CI already
      downloads, and only a handful are actually referenced; the `*.dll` glob
      needs two explicit `Remove`s to work at all.
- [ ] `README.md` and `docs/README_THUNDERSTORE.md` are two hand-maintained
      copies with differently worded config tables; the EWC `"Trigger": "Hit"`
      author note and "all players should install the mod" exist only in the
      Thunderstore one, and neither mentions the in-code note that base GTFO
      freezes spitters for 240 s.

## Mod behavior

- [ ] Config values are snapshotted once in `Plugin.Load()` (`Plugin.cs`) —
      BepInEx config reloads have no effect until restart, and the `Config_*`
      static setters are publicly mutable by other plugins. Also clamp once at
      load (`float.IsFinite`, min 1 health, warn on very high health — with
      the vanilla 5 s pop cooldown removed a 200 HP spitter is a sustained
      infection/noise emitter) instead of the three site-local
      `Math.Max(1f, …)` in `SpitterKillManager`: `Math.Max(1f, NaN)` is NaN
      and kills on the first hit.
- [ ] No level epoch on `SpitterDamageEvent` / `SpitterHealthEvent`:
      `SpitterIndex` restarts at 0 every level, so a report in flight during a
      level transition lands on the next level's spitter with the same index.
      Narrow window; a `byte LevelEpoch` bumped at `OnBuildDone` and checked in
      the handlers closes it.
- [ ] `ReconcileDeadSpitters` runs at `OnEnterLevel`, which precedes a late
      joiner's recall, so a kill deferred during recall ("not resolvable yet")
      is never retried. Sweep from the `Update` postfix on a dirty flag.
- [ ] `HostMarkDead` indexes `_currentStates[shard]` with no capacity guard;
      every current caller guards, a future one would `Break()`.
- [ ] `_broken` latches in the `Fix_*` patches trip on transient nulls and
      never reset (`Fix_SpitterMineTrigger` reads `align.position`, and vanilla
      itself can leave `m_lineRendererAlign` null) — one bad mine prefab
      disables that fix for the whole session. Consider a per-level reset.
- [ ] `Fix_SpitterBotAggro` and `Fix_SpitterSentryTarget` return `false`
      unconditionally on every tick, silencing any later-sorted peer-mod prefix
      on `ManagerUpdate` / `CheckForTargetLegacy`. A "no bots in
      `PlayerAgentsInLevel` → run original" fast path would limit exposure.
- [ ] `Fix_SpitterMineTrigger` re-runs vanilla's `SphereCast` and then returns
      `true` in the common no-spitter case, so vanilla casts again: two casts
      per mine per `FixedUpdate` on the host.
- [ ] `Fix_SpitterHealthRel` keeps no copy of the original `methodPointer`
      (no `Uninstall`), and reads `Il2CppClassPointerStore<T>.NativeClassPtr`
      without forcing the interop type's class ctor — handled by `WarnAborted`,
      but the symptom would be silent loss of EWC projectile/DoT support.
- [ ] `SpitterDeathState.ShardIndex` is transmitted and never checked.

## Accepted limitations (documented in code, revisit if they start to matter)

- Host migration is out of scope (`SpitterKillManager`).
- A late joiner sees the default glow until the spitter's next hit
  (`SpitterVisuals`).
- The `DoorEnemyFixUpdated` ≤ 1.1.3 glue-crash suppression
  (`Fix_SpitterGlueCollisionCrash`) papers over a foreign bug; the real fix
  belongs upstream in DoorEnemyFixUpdated.
- Pooled pop FX reuse can un-anchor a death-pop tint if the same pooled
  instance is handed to another spitter's pop mid-burst (`SpitterVisuals`,
  cosmetic only).
