# Changelog

See more at https://github.com/brgmnn/gtfo-killable-spitters


## [v1.2.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v1.2.0) — September 14, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### New

* Host C-Foam settings are now synced to every player, including late joiners
  * The host's `CfoamKillsSpitters` and `SpitterFreezeDuration` are replicated to all peers, so foamed spitters behave identically for all players
* `SpitterHealth` values below `1.0` now log a startup warning (they were already raised to `1.0`)

### Change

* Fix: If the mod hit an internal error and disabled itself, spitters kept popping on every hit and could never die
* Fix: [ExtraWeaponCustomization](https://thunderstore.io/c/gtfo/p/Dinorush/ExtraWeaponCustomization/) `Foam` on multi-hit weapons (shotguns, bursts) foamed the same spitter once per pellet
  * Each pellet sent its own glue packet and played its own foam sound on every player. Now once per spitter per shot
* Fix: [ExtraWeaponCustomization](https://thunderstore.io/c/gtfo/p/Dinorush/ExtraWeaponCustomization/) `Explosive` blasts ignored EWC's per-shot falloff, so spitters took full damage at ranges where enemies took reduced damage
* Fix: The spitter health-glow sync was sent per hit on the game's reliable-ordered channel
  * Under sustained fire it could delay damage reports and death sync behind purely cosmetic packets. It now uses the non-critical channel
* Fix: A rare IL2CPP garbage-collection race while hiding a dying spitter's model could permanently disable the mod for the session
* The [DoorEnemyFixUpdated](https://thunderstore.io/c/gtfo/p/Dinorush/DoorEnemyFixUpdated/) ≤ 1.1.3 crash workaround now only arms itself when another mod actually patches `GlueGunProjectile.CollisionCheck`
  * Without one, unexpected errors are surfaced instead of silently hidden. When the workaround cannot identify what the glue hit, it now logs the full exception at error level
  * A "no other patch present" answer is re-checked on the next error, so a mod that patches `CollisionCheck` lazily is never missed
* The glue dead-guard moved from `InfectionSpitter.OnIncomingGlue` (a one-line forwarder, an identical-code-folding hazard) to `DoGetGlued`
* Dependency: [AmorLib](https://thunderstore.io/c/gtfo/p/Amorously/AmorLib/) pin bumped from 1.2.2 to 1.2.7
  * The plugin now declares a minimum AmorLib version of 1.2.2
* The built DLL's assembly version now matches the mod version (was `1.0.0.0`)

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v1.1.0...v1.2.0


## [v1.1.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v1.1.0) — July 29, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### New

* [ExtraWeaponCustomization](https://thunderstore.io/c/gtfo/p/Dinorush/ExtraWeaponCustomization/) (EWC) compatibility. Custom weapons can now hurt spitters
  * EWC custom `Projectile`s and `DamageOverTime` ticks no longer treat spitters as already dead
  * EWC `Explosive` blasts now damage spitters in radius
  * EWC `Foam` now foams spitters like the base game C-Foam Launcher

### Change

* Fix: Hard crash when foaming spitters (C-Foam Launcher / glue mines) while [DoorEnemyFixUpdated](https://thunderstore.io/c/gtfo/p/Dinorush/DoorEnemyFixUpdated/) ≤ 1.1.3 is installed
  * Fix `NullReferenceException` crash so C-Foam and glue mines keep working; the underlying bug should still be fixed upstream in DoorEnemyFixUpdated.
* Fix: Sentries using legacy detection could stop acquiring targets while a spitter was in their detection cone

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v1.0.0...v1.1.0


## [v1.0.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v1.0.0) — July 22, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### New

* Damage extended to all damage sources
  * Includes fixing support for [RecurveBow](https://thunderstore.io/c/gtfo/p/hirnukuono/RecurveBow/) to damage spitters
* Split config options for C-Foam freeze duration and whether C-Foam kills spitters
  * Defaults are for C-Foam to kill spitters with a freeze duration of 0.7s

### Change

* Fix: Mines do not trigger when aimed at spitters

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.5.0...v1.0.0


## [v0.5.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.5.0) — July 17, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### Changes

* Fix: Remove GTFO-API from being bundled and rely on version from BepInEx

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.4.0...v0.5.0


## [v0.4.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.4.0) — July 17, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### Changes

* Fix: remove bundled version of AmorLib to rely on thunderstore dependency

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.3.0...v0.4.0


## [v0.3.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.3.0) — July 17, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### Changes

* Fix: Incorrect plugin version

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.2.0...v0.3.0


## [v0.2.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.2.0) — July 17, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

### Changes

* Fix: Icon size too large for Thunderstore

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/compare/v0.1.0...v0.2.0


## [v0.1.0](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/v0.1.0) — July 17, 2026

<!-- Release notes generated using configuration in .github/release.yml at main -->

Initial version of killable spitters!

**Full Changelog**: https://github.com/brgmnn/gtfo-killable-spitters/commits/v0.1.0

