using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.Properties.Shared.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using HarmonyLib;
using KillableSpitters.Patches.Spitters;

namespace KillableSpitters.Patches.Compat;

/// <summary>
/// Makes ExtraWeaponCustomization foam (the Foam weapon property) foam
/// spitters. EWC never flies a real GlueGunProjectile — it attaches glue
/// through its own FoamActionManager — so the vanilla
/// AddGlue → OnIncomingGlue path this mod taps never fires. Worse, spitters
/// are filtered out twice before any foam logic runs: a spitter hit classifies
/// as DamageType.Object (agent-less damageable, EWC DamageType.cs:137-146),
/// and Foam both blacklists Object at the trigger level (Foam.cs:47 via
/// SetValidTriggers, applied to each trigger in Effect.VerifyTriggers) and
/// skips Object contexts at runtime (Foam.cs:59-60) — its enemy branch
/// hard-casts Dam_EnemyDamageLimb, so the blacklist is load-bearing as
/// written.
///
/// Two patches, applied manually by EWCCompat (no [HarmonyPatch] attributes —
/// see EWCCompat structure rules). TriggerApply is patched BEFORE the ctor so
/// a partial failure can never leave the blacklist relaxed with nothing
/// consuming the resulting contexts:
///  - TriggerApply prefix: applies the spitter reaction for Object contexts
///    whose damageable is a spitter, then lets the original run untouched (it
///    skips those same contexts, so nothing double-applies). The reaction is
///    vanilla glue: InfectionSpitter.OnIncomingGlue() → replicated SendGlued →
///    DoGetGlued on every peer → this mod's existing freeze/kill clock
///    (SpitterKillManager.OnSpitterGlued), identical to a C-foam globber hit.
///    Vanilla spitters fully freeze on ANY glue volume, so any positive foam
///    amount glues — EWC's per-enemy foam accumulation deliberately does not
///    apply (a spitter has no Dam_EnemyDamageBase glue meter to fill).
///  - Foam ctor postfix: clears the Object bit from the ctor blacklist (a
///    private Effect field) and from the already-verified default trigger, so
///    spitter hit contexts actually reach TriggerApply. JSON-deserialized
///    triggers pick the reduced blacklist up via Effect.VerifyTriggers.
///    Lock/door hits also start flowing as contexts, but the original loop
///    still skips every Object context itself, so their behavior is unchanged.
/// </summary>
internal static class Fix_EWCFoam
{
    /// <summary>Permanently fall back to EWC-default behavior if a patch ever throws.</summary>
    private static bool _broken;

    private static bool _applied;

    internal static void Apply(Harmony harmony)
    {
        if (_applied)
            return;

        var triggerApply = AccessTools.Method(typeof(Foam), nameof(Foam.TriggerApply))
            ?? throw new MissingMethodException("EWC Foam.TriggerApply not found");
        var ctor = AccessTools.Constructor(typeof(Foam), Type.EmptyTypes)
            ?? throw new MissingMethodException("EWC Foam constructor not found");

        // Resolve the private blacklist field up front so a rename fails the
        // whole apply (EWCCompat warns) instead of breaking mid-game.
        _ = Cache.BlacklistType;

        harmony.Patch(triggerApply, prefix: new HarmonyMethod(typeof(Fix_EWCFoam), nameof(Pre_TriggerApply)));
        harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(Fix_EWCFoam), nameof(Post_FoamCtor)));

        _applied = true;
    }

    /// <summary>EWC-typed reflection handles, resolved lazily on first touch
    /// (only ever from code paths that already require EWC).</summary>
    private static class Cache
    {
        internal static readonly AccessTools.FieldRef<Effect, DamageType> BlacklistType =
            AccessTools.FieldRefAccess<Effect, DamageType>("_blacklistType");
    }

    public static void Post_FoamCtor(Foam __instance)
    {
        if (_broken)
            return;

        try
        {
            Cache.BlacklistType(__instance) &= ~DamageType.Object;

            // The default BulletLanded trigger was created and verified inside
            // the ctor, so it already carries the Object blacklist — clear it
            // there too (each trigger keeps its own accumulated BlacklistType).
            var coordinator = __instance.Trigger;
            if (coordinator == null)
                return;

            foreach (var trigger in coordinator.Activate.Triggers)
            {
                if (trigger is IDamageTypeTrigger typeTrigger)
                    typeTrigger.BlacklistType &= ~DamageType.Object;
            }
        }
        catch (Exception ex)
        {
            _broken = true;
            Plugin.Logger.LogError(
                $"[EWCFoam] Blacklist relax failed, EWC foam no longer affects spitters: {ex}");
        }
    }

    public static void Pre_TriggerApply(Foam __instance, List<TriggerContext> triggerList)
    {
        if (_broken)
            return;

        try
        {
            HashSet<IntPtr>? gluedSpitters = null;

            foreach (var tContext in triggerList)
            {
                if (tContext.context is not WeaponHitDamageableContextBase damContext)
                    continue;

                if ((damContext.DamageType & DamageType.Object) == 0)
                    continue;

                var damageable = damContext.Damageable?.TryCast<InfectionSpitterDamage>();
                if (damageable == null)
                    continue;

                // A spitter exposes several colliders and EWC accumulates one
                // context per hit, so a shotgun / burst lands K contexts on one
                // spitter. One OnIncomingGlue per spitter per apply — each call
                // is a replicated SendGlued (packet + foam sound on every peer).
                gluedSpitters ??= new HashSet<IntPtr>();
                if (!gluedSpitters.Add(damageable.Pointer))
                    continue;

                var spitter = damageable.m_spitter;
                if (spitter == null || SpitterKillManager.IsDeadOrDying(spitter.m_spitterIndex))
                    continue;

                // Foam strength scales like EWC's own sizeMod; a spitter only
                // cares that SOME foam landed (vanilla full-freeze semantics).
                var sizeMod = tContext.triggerAmt * (__instance.IgnoreFalloff ? 1f : damContext.Falloff);
                if (Math.Max(__instance.Amount, __instance.BubbleAmount) * sizeMod <= 0f)
                    continue;

                spitter.OnIncomingGlue();
            }
        }
        catch (Exception ex)
        {
            _broken = true;
            Plugin.Logger.LogError(
                $"[EWCFoam] Spitter foam apply failed, EWC foam no longer affects spitters: {ex}");
        }
    }
}
