# KillableSpitters — Code Review

_Review date: 2026-07-20_

A full read-through of the `KillableSpitters/` source (Plugin, patches, manager,
visuals, death FX, replication state, events).

## Overall assessment

The codebase is well-engineered and heavily defensive. The IL2CPP hazards are
handled with real care:

- ICF identical-code-folding is avoided (the damage tap sits on the unique-bodied
  `OnIncomingDamage`, never the folded forwarders).
- Boehm GC races on boxed interop structs are hardened (shared long-lived
  gradient, by-value capture, retry-and-skip).
- Session-lifetime pooled FX are restored (mandatory, deferred).
- Death replication is index-stable and shard-bounded, with double-kill
  serialization, late-join/recall reconcile, and an "adopt the in-flight pop"
  death sequence.

No crashes or replication-correctness bugs were found. The shard/bit math,
double-kill guard, recall/revive path, and pop-adoption sequencing all check out.
Findings below are edge cases and design trade-offs, ranked by materiality.

---

## Bugs / correctness gaps

### 1. `Break()`'s "revert to vanilla" is incomplete — _low severity_

`SpitterKillManager.Break()` (`SpitterKillManager.cs:499`) is documented as
reverting to vanilla behaviour, but `Pre_OnIncomingDamage`
(`Patch_SpitterDamage.cs:88`) has **no `_broken`/`IsBroken` guard**. After a Break
it still fully replaces `OnIncomingDamage` — popping the spitter on *every* hit
(the vanilla 5s `m_damageExplodeTimer` cooldown stays removed) while
`ReportDamage` no-ops. Result: a broken feature leaves spitters **un-killable but
popping more aggressively than vanilla** — arguably worse than vanilla.

**Fix:** expose an `IsBroken` accessor and `return true` (run original) at the top
of `Pre_OnIncomingDamage`.

_(The dead-guards `Pre_OnIncomingGlue` / `Pre_ReceiveDamage` also read a
non-cleared `_dead` set after a Break, but those spitters are already deactivated,
so the residual effect is nil.)_

### 2. Timer-based FX tint can leak past its restore — _low severity, cosmetic_

Damaged (non-death) pops register a ~3s **timer** restore via
`TintPop(..., untilFinalize:false)` and are **not** added to `_tintedBySpitter`
(`SpitterVisuals.cs:332-335`). `TickRestores` is driven **only** from the
`InfectionSpitter.Update` postfix.

If that spitter then dies via C-foam (case (c), which does not re-tint `m_fx`) or a
silent recall, and no other spitter is actively `Update`-ing (player walked away),
the tint on the **pooled, session-lifetime, projectile-shared** `FX_InfectionSpit`
system persists until the next spitter pops or level cleanup. That pool is shared
with infectious projectiles (per the comment at `SpitterVisuals.cs:33-35`), so a
projectile hit in the window would splash tinted.

**Fix:** always append to `_tintedBySpitter[index]` in `TintPop` so
`FinalizeSpitter → RestoreForSpitter` cleans it up regardless of anchor mode.

---

## Pitfalls

### 3. C-foam freeze now depends on each peer's *local* config (mismatched-lobby desync)

The recent config split made `OnSpitterGlued` (`SpitterKillManager.cs:439-457`)
branch on the **local** `Config_CfoamKillsSpitters` / `Config_SpitterFreezeDuration`.
Previously clients returned early and always showed the vanilla freeze until the
host's death; now a client with `CfoamKillsSpitters=false` actively shortens its
own `m_stayInTimer`.

In a lobby where the host is `ON` but a client is `OFF`, the client thaws early,
the spitter briefly reactivates, then the host's replicated death lands. The
**kill** stays host-authoritative, but the **freeze visual** no longer does.

**Recommendation:** document a "keep configs consistent across the lobby" caveat.
(Per-peer freeze *length* was an accepted trade-off; the sharper edge is the
*branch selection* being driven by local config.)

### 4. Pooled-FX tint aliasing across two dying spitters — _very low severity_

`m_fx` is a pooled instance; its `ParticleSystem.GetInstanceID()`s recur across
spitters. If spitter A tints system X (finalize-anchored), A's FX returns to pool,
B acquires the same instance and tints X, then A finalizes —
`RestoreForSpitter(A)` restores/removes X, ending B's tint early. No crash (the
missing-key path in `RestoreSystem` is safe), just a cosmetic early-restore.

### 5. Fragile game-version couplings

- `SpitterDeathFx.cs:39` hardcodes `FlyerEnemyId = 42`; a datablock-id shift
  silently downgrades to sound-only (there is a warn-once + fallback, so it fails
  soft).
- `Fix_SpitterBotAggro.cs:142` relies on `flag5 | flag4 && …` precedence
  (`flag5 | (flag4 && …)`). The comment says it matches vanilla IL, but it is the
  classic precedence trap and must be re-diffed on every game update. Consider an
  explicit parenthesization once vanilla's intent is confirmed.

---

## Design choices worth reconsidering

### 6. Any exception permanently kills the feature for the whole session

`_broken` is never reset — `OnBuildDone` early-returns on it
(`SpitterKillManager.cs:987`) and `ClearRuntimeState` (`:1072`) does not clear it.
One fluke exception on level 1 disables killable spitters until the game restarts.
The same applies to `Fix_SpitterBotAggro._broken`, `SpitterVisuals._visualsBroken`,
and `SpitterDeathFx._broken`.

Given how conservative the per-path try/catch already is, resetting these flags at
`OnBuildDone` would make the feature **self-heal per level** with no real downside.

### 7. No master enable toggle

`CfoamKillsSpitters` gates C-foam, but there is no config to disable the
**bullet/melee** kill while keeping the bot-aggro fix (which many players would
want independently). Consider a top-level `SpittersAreKillable` bool.

---

## Improvements

### 8. Health-broadcast rate

`BroadcastHealth` fires from `AccumulateDamage` on **every** non-killing hit
(`SpitterKillManager.cs:587`). At the default 30 HP, spitters die in 1–3 hits so
it is fine, but with a high `SpitterHealth` or a high-RoF weapon it becomes one
network send per bullet per spitter.

Coalescing (only broadcast when the rounded fraction changes, or throttle to
~5–10 Hz) would cut traffic with no visible difference. It is purely cosmetic, so
an unreliable channel would also be appropriate.

### 9. Doc nits

- `Plugin.cs:61` still says "the ICF-fold assumption behind the **bullet damage
  tap**" — stale since the tap moved to `OnIncomingDamage`.
- `SpitterVisuals` mentions `m_fxLight` in one place while `FinalizeSpitter`
  correctly notes the R6 wind-up light no longer exists in the current build —
  minor internal inconsistency.

---

## Priority summary

| # | Finding | Severity | Suggested action |
|---|---------|----------|------------------|
| 1 | `Break()` doesn't fully revert `OnIncomingDamage` | Low (correctness/contract) | Guard `Pre_OnIncomingDamage` on `IsBroken` |
| 6 | `_broken` is session-permanent | Low (robustness) | Reset break flags at `OnBuildDone` |
| 3 | Per-peer C-foam config desync | Low (MP consistency) | Document lobby-config caveat |
| 2 | Timer FX tint leak | Low (cosmetic) | Always track tints by spitter |
| 8 | Per-hit health broadcast | Improvement (perf) | Coalesce/throttle broadcasts |
| 7 | No master kill toggle | Feature gap | Add `SpittersAreKillable` |
| 4 | Pooled-FX tint aliasing | Very low (cosmetic) | Accept or key by pool epoch |
| 5 | Version-coupled constants/precedence | Low (fragility) | Re-diff on game updates |
| 9 | Stale comments | Trivial | Tidy |

The highest-value changes are **#1** (honour the vanilla-revert contract), **#6**
(self-heal `_broken` per level), and **#3** (document/handle the per-peer C-foam
config).
