# Changelog

See more at https://github.com/brgmnn/gtfo-killable-spitters


## Unreleased

### New

* [ExtraWeaponCustomization](https://thunderstore.io/c/gtfo/p/Dinorush/ExtraWeaponCustomization/) (EWC) compatibility — custom weapons can now hurt spitters (verified against EWC 4.12.0–4.12.6; soft dependency, no effect without EWC installed)
  * Spitters now report their real health through `GetHealthRel()` instead of the vanilla hardcoded 0, so EWC custom `Projectile`s and `DamageOverTime` ticks (and any other mod using the same "is it alive" check) no longer treat spitters as already dead
  * EWC `Explosive` blasts now damage spitters in radius (EWC's own target search only ever finds enemies, players and locks)
  * EWC `Foam` now foams spitters like a C-Foam globber hit — freezing and (by default) killing them (EWC foam never spawns a real glue projectile, and it explicitly skipped non-enemy targets)
  * Note for weapon config authors: `Trigger`s restricted to `HitEnemy`/`HitPlayer` still never match a spitter hit (spitters classify as EWC `Object` targets) — use e.g. `"Trigger": "Hit"` for effects that should also apply to spitters

### Change

* Fix: Hard crash when foaming spitters (C-Foam Launcher / glue mines) while `DoorEnemyFixUpdated` ≤ 1.1.3 is installed
  * That mod's `GlueGunProjectile.CollisionCheck` postfix throws a `NullReferenceException` on a spitter (a spitter has no `EnemyAgent`). KillableSpitters now suppresses just that foreign crash so C-Foam and glue mines keep working; the underlying bug should still be fixed upstream in DoorEnemyFixUpdated.
* Fix: Sentries using legacy detection (`Sentry_LegacyEnemyDetection`) could stop acquiring targets while a spitter was in their detection cone once spitters report health


## [v1.0.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v1.0.0) — July 21, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### New

* Damage extended to all damage sources
  * Includes fixing support for [RecurveBow](https://thunderstore.io/c/gtfo/p/hirnukuono/RecurveBow/) to damage spitters
* Split config options for C-Foam freeze duration and whether C-Foam kills spitters
  * Defaults are for C-Foam to kill spitters with a freeze duration of 0.7s

### Change

* Fix: Mines do not trigger when aimed at spitters

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.5.0...v1.0.0


## [v0.5.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.5.0) — July 16, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### Changes

* Fix: Remove GTFO-API from being bundled and rely on version from BepInEx

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.4.0...v0.5.0


## [v0.4.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.4.0) — July 16, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### Changes

* Fix: remove bundled version of AmorLib to rely on thunderstore dependency

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.3.0...v0.4.0


## [v0.3.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.3.0) — July 16, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### Changes

* Fix: Incorrect plugin version

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.2.0...v0.3.0


## [v0.2.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.2.0) — July 16, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### Changes

* Fix: Icon size too large for Thunderstore

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.1.0...v0.2.0


## [v0.1.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.1.0) — July 16, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

Initial version of killable spitters!

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/commits/v0.1.0

