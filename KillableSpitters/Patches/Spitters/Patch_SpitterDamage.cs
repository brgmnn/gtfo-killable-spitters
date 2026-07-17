using System.Reflection;
using System.Runtime.InteropServices;
using Gear;
using HarmonyLib;

namespace KillableSpitters.Patches.Spitters;

/// <summary>
/// Harmony taps/guards for killable spitters. All logic lives in
/// SpitterKillManager; every patch here is a thin guard or tap that defaults
/// to vanilla behavior on any error (and SpitterKillManager.Break permanently
/// reverts to vanilla after an unexpected exception).
///
/// Damage-type routing (user decision: bullets only):
/// - Bullets: tapped at BulletWeapon.BulletHit (Post_BulletHit below), the
///   single funnel for every player-side bullet damage path — main weapons
///   incl. piercing (decompile Gear/BulletWeapon.cs:425,442), shotguns
///   (Gear/Shotgun.cs:55, Gear/ShotgunSynced.cs:56), synced/bot weapons
///   (Gear/BulletWeaponSynced.cs:92) and sentries
///   (SentryGunInstance_Firing_Bullets.cs:257,290 — host-only damage). The
///   only generic IDamageable.BulletDamage dispatch in the game is
///   BulletWeapon.cs:544, so nothing bypasses this tap. Shotguns report once
///   per pellet.
/// - Melee/explosion/fire: untouched — they still pop the spitter via the
///   vanilla OnIncomingDamage path (prefixed below) but deal no health
///   damage. OnIncomingDamage funnels all damage types with no type
///   information, so it cannot serve as a bullets-only tap.
///
/// ICF WARNING — why the bullet tap is NOT a patch on
/// InfectionSpitterDamage.BulletDamage (the original hammer-crash bug): the
/// four damage forwarders BulletDamage / MeleeDamage / FireDamage /
/// ExplosionDamage all have the identical trivial body
/// `m_spitter.OnIncomingDamage(dam)` (decompile
/// InfectionSpitterDamage.cs:25-77), and IL2CPP release links fold identical
/// bodies into ONE shared native function (/OPT:ICF — legal because all four
/// only read `this` and `dam`, which sit in the same registers in every
/// signature). Detouring that function with a stub generated from
/// BulletDamage's signature meant a MELEE hit (12-arg layout) reached a stub
/// expecting BulletDamage's 9-arg layout: the stack slot the stub
/// dereferences as `Vector3* normal` holds `float staggerMulti = 1.0f`, so
/// it read address 0x3F800000 → access violation inside the marshaling stub,
/// before any managed try/catch → hard CTD. Fire/explosion damage were
/// equally unsafe. LogDamageForwarderFoldState verifies the fold assumption
/// at startup. House rule: NEVER Harmony-patch a trivially-bodied il2cpp
/// forwarder (fold risk) or an empty-bodied method like
/// PushDamage/FallDamage (empty bodies fold with every empty function in the
/// entire binary).
///
/// IL2CPP notes: the interop assembly exposes InfectionSpitter's private
/// members (fields, Update, ReceiveDamage) as public, so they are patchable /
/// readable directly — same mechanism Fix_SpitterBotAggro relies on. Beware
/// the name collision between the top-level global::InfectionSpitterDamage
/// component and the nested packet struct
/// InfectionSpitter.InfectionSpitterDamage (which Pre_ReceiveDamage takes as
/// its parameter).
/// </summary>
[HarmonyPatch]
internal static class Patch_SpitterDamage
{
    /// <summary>
    /// Bullet damage tap: reports the final post-falloff damage of every
    /// bullet the original actually applied to a spitter. BulletWeapon.BulletHit
    /// is the single funnel for all player bullet paths (class header) and has
    /// a large, unique, multi-caller static body — safe to detour, unlike the
    /// ICF-folded receiver methods.
    ///
    /// __result == doDamage &amp; flag: true iff the original called
    /// damageable.BulletDamage on this invocation, with the piercing
    /// TempSearchID dedup already applied (decompile
    /// Gear/BulletWeapon.cs:523-552) — exact parity with the old
    /// receiver-side tap. Dead/dying spitters are guarded inside
    /// ReportBulletDamage, and their pops are blocked by Pre_OnIncomingDamage.
    ///
    /// The damage recompute mirrors the original's pre-call falloff math
    /// (decompile Gear/BulletWeapon.cs:525-533). If a future build changes
    /// BulletHit's damage shaping this drifts silently — balance-only, no
    /// crash; re-diff after game updates.
    ///
    /// Ordering: the old receiver prefix reported BEFORE the pop; this
    /// postfix reports after OnIncomingDamage already ran, so a killing
    /// blow's own pop is in flight (or just completed) when the death state
    /// lands. KillSpitter cases (a)/(b) adopt exactly that pop — no visible
    /// change.
    /// </summary>
    [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
    [HarmonyPostfix]
    public static void Post_BulletHit(Weapon.WeaponHitData weaponRayData, float additionalDis, bool __result)
    {
        try
        {
            if (!__result)
                return;

            var collider = weaponRayData.rayHit.collider;
            if (collider == null)
                return;

            var spitterDamage = collider.GetComponent<global::InfectionSpitterDamage>();
            if (spitterDamage == null)
                return;

            var spitter = spitterDamage.m_spitter;
            if (spitter == null)
                return;

            // Recompute the post-falloff damage exactly like the original
            // does before the IDamageable call (Gear/BulletWeapon.cs:525-533).
            var dist = weaponRayData.rayHit.distance + additionalDis;
            var damage = weaponRayData.damage;
            var falloff = weaponRayData.damageFalloff;

            if (dist > falloff.x)
                damage *= Math.Max(1f - (dist - falloff.x) / (falloff.y - falloff.x), BulletWeapon.s_falloffMin);

            if (Weapon.SuperWeapons)
                damage *= 100f;

            SpitterKillManager.ReportBulletDamage(spitter, damage);
        }
        catch (Exception ex)
        {
            SpitterKillManager.Break(ex);
        }
    }

    /// <summary>
    /// Pop-per-hit + dead-guard for every pop source (bullets, melee,
    /// explosion, fire all funnel here — decompile
    /// InfectionSpitterDamage.cs:25-77). Vanilla gates damage pops behind a
    /// 5s cooldown (m_damageExplodeTimer, InfectionSpitter.cs:337-347); this
    /// replaces the body without it, so sustained fire keeps popping the
    /// spitter until it dies. DoExplode's own m_isExploding re-entry guard
    /// paces the pops to one per wind-up cycle, and the m_isExploding skip
    /// below also avoids re-broadcasting the vanilla explode packet for every
    /// bullet landing mid-wind-up. Pops are triggered on the shooter's client
    /// (vanilla design).
    /// </summary>
    [HarmonyPatch(typeof(InfectionSpitter), nameof(InfectionSpitter.OnIncomingDamage))]
    [HarmonyPrefix]
    public static bool Pre_OnIncomingDamage(InfectionSpitter __instance)
    {
        try
        {
            // Dead/dying spitters must not trigger further explosions.
            if (SpitterKillManager.IsDeadOrDying(__instance.m_spitterIndex))
                return false;

            // Pop already winding up — nothing to add, don't resend.
            if (__instance.m_isExploding)
                return false;

            // Vanilla body minus the 5s m_damageExplodeTimer gate.
            if (__instance.m_currentState == InfectionSpitter.eSpitterState.Retracted)
                __instance.SendSlowExplode();
            else
                __instance.SendExplode();

            return false;
        }
        catch (Exception ex)
        {
            SpitterKillManager.Break(ex);
            return true;
        }
    }

    /// <summary>
    /// C-foam kill tap: DoGetGlued runs on every peer for any foaming (local
    /// trigger via SendGlued, or the vanilla packet via ReceiveDamage —
    /// decompile InfectionSpitter.cs:349-404), giving the host a clean
    /// "became glued" edge for the foam-kill clock.
    /// </summary>
    [HarmonyPatch(typeof(InfectionSpitter), nameof(InfectionSpitter.DoGetGlued))]
    [HarmonyPostfix]
    public static void Post_DoGetGlued(InfectionSpitter __instance)
    {
        SpitterKillManager.OnSpitterGlued(__instance);
    }

    /// <summary>
    /// Dead-guard for glue: DoGetGlued's long timed retract would fight the
    /// death pop's state on a dying spitter.
    /// </summary>
    [HarmonyPatch(typeof(InfectionSpitter), nameof(InfectionSpitter.OnIncomingGlue))]
    [HarmonyPrefix]
    public static bool Pre_OnIncomingGlue(InfectionSpitter __instance)
    {
        try
        {
            return !SpitterKillManager.IsDeadOrDying(__instance.m_spitterIndex);
        }
        catch (Exception ex)
        {
            SpitterKillManager.Break(ex);
            return true;
        }
    }

    /// <summary>
    /// Drops stale vanilla explode/glue packets addressed to dead/dying
    /// spitters (decompile InfectionSpitter.cs:381-404 — ReceiveDamage would
    /// call DoExplode/DoGetGlued on the deactivated object). While a spitter
    /// is merely dying an inbound Explode is already harmless (DoExplode
    /// no-ops on m_isExploding), but pops during the dying window are unwanted
    /// anyway.
    /// </summary>
    [HarmonyPatch(typeof(InfectionSpitter), nameof(InfectionSpitter.ReceiveDamage))]
    [HarmonyPrefix]
    public static bool Pre_ReceiveDamage(InfectionSpitter.InfectionSpitterDamage data)
    {
        try
        {
            return !SpitterKillManager.IsDeadOrDying(data.spitterIndex);
        }
        catch (Exception ex)
        {
            SpitterKillManager.Break(ex);
            return true;
        }
    }

    /// <summary>
    /// Pop-completion watcher for dying spitters. Update only runs while the
    /// component is enabled, which is guaranteed during the death sequence
    /// (DoExplode and pop completion both set enabled = true). If patching
    /// this private Unity message ever fails on a game update, the deadline
    /// fallback in SpitterKillManager.ShouldBlockManagerUpdate still
    /// finalizes dying spitters (degraded: on the next ManagerUpdate tick
    /// instead of pop + grace).
    /// </summary>
    [HarmonyPatch(typeof(InfectionSpitter), nameof(InfectionSpitter.Update))]
    [HarmonyPostfix]
    public static void Post_Update(InfectionSpitter __instance)
    {
        SpitterKillManager.OnSpitterUpdatePostfix(__instance);
    }

    /// <summary>
    /// Startup diagnostic for the ICF trap documented in the class header:
    /// reads the native code pointers of the four InfectionSpitterDamage
    /// damage forwarders and logs whether they are folded into one function
    /// (expected). A warning here means the fold assumption changed on this
    /// build — revisit the routing above before anyone patches a forwarder.
    /// Log-only; never disables the feature.
    /// </summary>
    internal static void LogDamageForwarderFoldState()
    {
        try
        {
            string[] forwarders = { "BulletDamage", "MeleeDamage", "FireDamage", "ExplosionDamage" };
            var codePtrs = new Dictionary<string, IntPtr>();

            // Il2CppInterop generates one 'NativeMethodInfoPtr_<Name>_<Sig>_0'
            // static field per method on the proxy class; prefix-match so an
            // interop regeneration (signature change) can't break the lookup.
            foreach (var field in typeof(global::InfectionSpitterDamage)
                         .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.FieldType != typeof(IntPtr))
                    continue;

                var name = Array.Find(forwarders,
                    f => field.Name.StartsWith($"NativeMethodInfoPtr_{f}_", StringComparison.Ordinal));
                if (name == null)
                    continue;

                var methodInfoPtr = (IntPtr)field.GetValue(null)!;
                if (methodInfoPtr != IntPtr.Zero)
                    // Il2CppMethodInfo.methodPointer is the first field in
                    // every Unity version.
                    codePtrs[name] = Marshal.ReadIntPtr(methodInfoPtr);
            }

            if (codePtrs.Count < forwarders.Length)
            {
                Plugin.Logger.LogWarning(
                    $"[SpitterKill] Damage-forwarder fold check incomplete ({codePtrs.Count}/{forwarders.Length} resolved)");
                return;
            }

            if (codePtrs.Values.Distinct().Count() == 1)
            {
                Plugin.Logger.LogDebug(
                    "[SpitterKill] InfectionSpitterDamage damage forwarders share one native body " +
                    $"(ICF-folded at 0x{codePtrs["BulletDamage"]:X}, expected — none of them is patched)");
            }
            else
            {
                Plugin.Logger.LogWarning(
                    "[SpitterKill] Damage forwarders are NOT folded on this build — the ICF assumption " +
                    "behind the BulletHit tap no longer holds: "
                    + string.Join(", ", codePtrs.Select(kv => $"{kv.Key}=0x{kv.Value:X}")));
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"[SpitterKill] Damage-forwarder fold check failed (diagnostic only): {ex.Message}");
        }
    }
}
