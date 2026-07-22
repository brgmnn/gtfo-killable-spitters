using System;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace KillableSpitters.Patches;

/// <summary>
/// Stops trip mines / deployed mines from being <i>triggered</i> by an InfectionSpitter on
/// their own. All three mine variants — the mine-deployer mine, the disposable trip mine and
/// the C-foam trip mine — are a single MineDeployerInstance driven by one shared detection
/// component, MineDeployerInstance_Detect_Laser. Its UpdateDetection SphereCasts along the laser
/// and detonates the instant the nearest hit is on the EnemyDamagable layer. A spitter's damage
/// collider (InfectionSpitterDamage) sits on that layer, so a mine whose beam lines up with a
/// spitter detonates the moment it is placed — with killable spitters in play that is an easy,
/// frustrating way to end a run (the mine goes off in the player's face).
///
/// This prefix leaves vanilla behavior completely intact except in the one case that matters:
/// when the beam's nearest target is a spitter. It then looks <i>past</i> the spitter(s) — a
/// spitter is a fixed object on the line, but enemies cross the beam wherever their path
/// intersects it, often farther out than the spitter — and:
///   - if any non-spitter enemy is on the beam before a wall occludes it, it lets vanilla trigger
///     (the resulting explosion legitimately also damages the spitter, which is wanted);
///   - if the beam sees <i>only</i> spitter(s), it suppresses the trigger and just keeps the laser
///     length in sync via the public UpdateDetectionRange, as vanilla would for a non-trigger hit.
///
/// This is deliberately look-past rather than a simpler "nearest hit is a spitter -> ignore":
/// because a stationary spitter is often closer than where an enemy actually crosses the laser, a
/// nearest-only skip would leave the mine unable to fire at enemies behind the spitter, breaking
/// "they can still trigger if there's some other enemy that would normally trigger them".
///
/// Detection only runs on the network master (MineDeployerInstance.FixedUpdate gates on
/// SNet.IsMaster), so this is host-authoritative and needs no netcode. UpdateDetection has a real,
/// non-trivial body, so it is not an IL2CPP identical-code-folding hazard.
///
/// Look-past uses only the single-hit Physics.SphereCast overload the game itself uses (a proven
/// interop signature) rather than SphereCastAll, advancing the ray origin past each spitter.
/// Unity's SphereCast ignores colliders the sphere already overlaps at its start, so stepping past
/// a spitter cannot re-hit the same spitter.
///
/// Decompile reference: gtfo-decompile/Modules-ASM/MineDeployerInstance_Detect_Laser.cs.
/// </summary>
[HarmonyPatch]
internal static class Fix_SpitterMineTrigger
{
    /// <summary>Sphere radius vanilla UpdateDetection casts with.</summary>
    private const float BeamRadius = 0.1f;

    /// <summary>
    /// Safety bound on the look-past loop (spitters stepped over on one beam). Real beams cross at
    /// most one or two spitters; the cap only guards against a pathological collider arrangement.
    /// </summary>
    private const int MaxLookPastSteps = 8;

    /// <summary>Permanently fall back to vanilla behavior if the patch ever throws.</summary>
    private static bool _broken;

    [HarmonyPatch(typeof(MineDeployerInstance_Detect_Laser),
        nameof(MineDeployerInstance_Detect_Laser.UpdateDetection))]
    [HarmonyPrefix]
    public static bool Pre_UpdateDetection(MineDeployerInstance_Detect_Laser __instance)
    {
        if (_broken)
            return true;

        try
        {
            // Only the modes vanilla acts on; for anything else vanilla just early-returns.
            var mode = __instance.m_core.Mode;
            if (mode != eStickyMineMode.Explode && mode != eStickyMineMode.Alarm)
                return true;

            if (__instance.m_maxLineDistance <= 0f)
                return true;

            var align = __instance.m_lineRendererAlign;
            var origin = align.position;
            var dir = align.forward;
            var maxDist = __instance.m_maxLineDistance;
            var scanMask = __instance.m_scanMask;
            var enemyMask = __instance.m_enemyMask;

            // Replicate vanilla's nearest-hit probe to decide whether we need to intervene at all.
            if (!Physics.SphereCast(origin, BeamRadius, dir, out var nearest, maxDist, scanMask))
                return true;                                    // clear beam -> vanilla
            if (!IsOnMask(nearest.collider, enemyMask))
                return true;                                    // wall nearest -> vanilla (no trigger)
            if (!IsSpitter(nearest.collider))
                return true;                                    // real enemy nearest -> vanilla triggers

            // Nearest target is a spitter. Look past it for any non-spitter enemy that is not
            // occluded by a wall.
            if (HasNonSpitterEnemy(origin, dir, maxDist, scanMask, enemyMask))
                return true;                                    // real enemy on the beam -> let vanilla trigger

            // Only spitter(s) on the beam: suppress the trigger, but keep the laser's visual length
            // in sync (vanilla would set it to the nearest hit distance for a non-triggering hit;
            // it never reaches that code for an enemy-layer hit because it triggers first).
            if (nearest.distance != __instance.DetectionRange)
                __instance.UpdateDetectionRange(nearest.distance);
            return false;
        }
        catch (Exception ex)
        {
            _broken = true;
            Plugin.Logger.LogError(
                $"[SpitterMineTrigger] Detection patch failed, reverting to vanilla behavior: {ex}");
            return true;
        }
    }

    /// <summary>
    /// Walks the beam from <paramref name="origin"/>, stepping over spitter hits, and reports
    /// whether a non-spitter enemy is reachable before a wall blocks the line.
    /// </summary>
    private static bool HasNonSpitterEnemy(
        Vector3 origin, Vector3 dir, float remaining, int scanMask, LayerMask enemyMask)
    {
        for (var step = 0; step < MaxLookPastSteps && remaining > 0.01f; step++)
        {
            if (!Physics.SphereCast(origin, BeamRadius, dir, out var hit, remaining, scanMask))
                return false;                                   // nothing more on the beam
            if (!IsOnMask(hit.collider, enemyMask))
                return false;                                   // wall -> everything beyond is occluded
            if (!IsSpitter(hit.collider))
                return true;                                    // a real enemy is on the beam

            // Spitter: advance just past it and keep looking. The small epsilon guarantees forward
            // progress; SphereCast then ignores this spitter because the sphere now overlaps it.
            var advance = hit.distance + 0.2f;
            origin += dir * advance;
            remaining -= advance;
        }

        return false;
    }

    private static bool IsOnMask(Collider collider, LayerMask mask)
        => collider != null && (mask.value & (1 << collider.gameObject.layer)) != 0;

    /// <summary>
    /// True iff the hit collider belongs to a spitter — i.e. its damageable is an
    /// InfectionSpitterDamage (the spitter's IDamageable, the sibling component weapons hit). The
    /// spitter's damage collider carries InfectionSpitterDamage directly, mirroring how an enemy
    /// limb collider carries Dam_EnemyDamageLimb; a ColliderMaterial indirection is handled as a
    /// fallback for colliders that reference their damageable elsewhere.
    /// </summary>
    private static bool IsSpitter(Collider collider)
    {
        if (collider == null)
            return false;

        if (collider.GetComponent<InfectionSpitterDamage>() != null)
            return true;

        var damageable = collider.GetComponent<ColliderMaterial>()?.Damageable;
        return damageable != null && damageable.TryCast<InfectionSpitterDamage>() != null;
    }
}
