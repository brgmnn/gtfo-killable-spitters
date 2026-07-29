using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.Properties.Effects.Hit.Explosion;
using HarmonyLib;
using KillableSpitters.Patches.Spitters;
using UnityEngine;

namespace KillableSpitters.Patches.Compat;

/// <summary>
/// Makes ExtraWeaponCustomization explosions damage spitters. EWC does not use
/// vanilla DamageUtil explosions (so the DamageTargetPos fix in
/// Fix_SpitterExplosionDamage never applies to them); its own target search in
/// ExplosionManager.DoExplosionDamage enumerates AIG course-node enemies
/// (EnemyAgents only), players, and — only with DamageLocks — an OverlapSphere
/// on the Dynamic layer. Spitter damage colliders are agent-less objects on
/// the EnemyDamagable layer, so no search path can ever return one
/// (EWC ExplosionManager.cs:70-79; the AgentType "lock" damage branch that
/// WOULD handle them at :201-206 is unreachable).
///
/// This postfix sweeps the blast radius for spitter colliders and routes the
/// damage through the vanilla IDamageable.ExplosionDamage forwarder — i.e.
/// into InfectionSpitter.OnIncomingDamage and this mod's existing funnel
/// (pop + host health report, per-hit clamp included), exactly like a vanilla
/// explosion hit. Damage mirrors EWC's own falloff for enemy targets
/// (distance.MapInverted(InnerRadius, Radius, MaxDamage, MinDamage, Exponent)
/// * triggerAmt — EWC ExplosionManager.cs:116-118, replicated locally rather
/// than calling EWC internals), with the same world-geometry line-of-sight
/// gate vanilla explosions use.
///
/// DoExplosionDamage runs exactly once per explosion, on the client that owns
/// the shot (gated upstream on the locally-managed owner; EWC's sync path
/// receives through separate Internal_Receive* methods that never re-enter
/// it), so the existing shooter-reports-to-host netcode needs no changes.
///
/// Applied manually by EWCCompat only when EWC is loaded — no [HarmonyPatch]
/// attributes here (see EWCCompat structure rules).
/// </summary>
internal static class Fix_EWCExplosion
{
    /// <summary>Permanently fall back to EWC-default behavior if the postfix ever throws.</summary>
    private static bool _broken;

    private static bool _applied;

    internal static void Apply(Harmony harmony)
    {
        if (_applied)
            return;

        var target = AccessTools.Method(typeof(ExplosionManager), "DoExplosionDamage")
            ?? throw new MissingMethodException("EWC ExplosionManager.DoExplosionDamage not found");

        harmony.Patch(target,
            postfix: new HarmonyMethod(typeof(Fix_EWCExplosion), nameof(Post_DoExplosionDamage)));

        _applied = true;
    }

    public static void Post_DoExplosionDamage(Vector3 position, Explosive explosiveBase, float triggerAmt)
    {
        if (_broken)
            return;

        try
        {
            // Mirror the original's early-outs (its FriendlyRadius-only case
            // deals no enemy damage, so neither do we).
            var radius = explosiveBase.Radius;
            if (radius <= 0f)
                return;

            var maxDamage = explosiveBase.MaxDamage;
            var minDamage = explosiveBase.MinDamage;
            if (maxDamage == 0f && minDamage == 0f)
                return;

            var colliders = Physics.OverlapSphere(position, radius, LayerManager.MASK_EXPLOSION_TARGETS);
            HashSet<IntPtr>? hitSpitters = null;

            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];

                if (!SpitterColliders.TryGetDamage(collider, out var damageable))
                    continue;

                // A spitter can expose several colliders — one damage report each.
                hitSpitters ??= new HashSet<IntPtr>();
                if (!hitSpitters.Add(damageable.Pointer))
                    continue;

                var spitter = damageable.m_spitter;
                if (spitter == null || SpitterKillManager.IsDeadOrDying(spitter.m_spitterIndex))
                    continue;

                var point = collider.bounds.ClosestPoint(position);
                if (Physics.Linecast(position, point, LayerManager.MASK_EXPLOSION_BLOCKERS))
                    continue;

                var damage = MapInverted(
                    Vector3.Distance(position, point),
                    explosiveBase.InnerRadius, radius, maxDamage, minDamage, explosiveBase.Exponent);
                damage *= triggerAmt;

                if (damage <= 0f)
                    continue;

                damageable.ExplosionDamage(damage, position, Vector3.zero);
            }
        }
        catch (Exception ex)
        {
            _broken = true;
            Plugin.Logger.LogError(
                $"[EWCExplosion] Explosion sweep failed, EWC explosions no longer damage spitters: {ex}");
        }
    }

    /// <summary>EWC's NumExtensions.MapInverted, replicated so the compat
    /// doesn't reach into EWC internals: full damage inside the inner radius,
    /// scaling to minDamage at the outer radius (optionally exponential).</summary>
    private static float MapInverted(
        float distance, float innerRadius, float radius, float maxDamage, float minDamage, float exponent)
    {
        if (innerRadius == radius)
            return distance < innerRadius ? maxDamage : minDamage;

        var range = radius - innerRadius;
        var t = Math.Clamp(distance - innerRadius, 0f, range);

        if (exponent != 1f)
            return (float)Math.Pow((range - t) / range, exponent) * (maxDamage - minDamage) + minDamage;

        return (range - t) / range * (maxDamage - minDamage) + minDamage;
    }
}
