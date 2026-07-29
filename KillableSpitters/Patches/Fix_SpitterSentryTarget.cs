using Enemies;
using GameData;
using HarmonyLib;
using UnityEngine;

namespace KillableSpitters.Patches;

/// <summary>
/// Stops legacy sentry detection from being blinded by spitters once
/// Fix_SpitterHealthRel makes them report health. Sentries with
/// ArchetypeDataBlock.Sentry_LegacyEnemyDetection scan via
/// SentryGunInstance_Detection.CheckForTargetLegacy: an OverlapSphere on
/// MASK_SENTRYGUN_DETECTION_TARGETS (= the EnemyDamagable layer — exactly
/// where spitter damage colliders live), returning on the FIRST damageable in
/// the cone with GetHealthRel() > 0. Its target condition tolerates a null
/// GetBaseAgent() (decompile SentryGunInstance_Detection.cs:220-227), so a
/// live-health spitter in the cone makes it `return null` — which reads as
/// "no target" AND stops the scan, leaving the sentry unable to acquire real
/// enemies behind or beside the spitter every frame the spitter is visible.
/// Vanilla never hit this because spitters reported 0 health.
///
/// This prefix reimplements the scan (static method with a real body — no
/// IL2CPP identical-code-folding hazard) with one change: agent-less
/// damageables never acquire and never stop the scan; the loop just keeps
/// looking, restoring "sentries ignore spitters but still see enemies past
/// them" (same intent as the mine-laser look-past in Fix_SpitterMineTrigger).
/// For health-0 damageables the reimplementation is behavior-identical to
/// vanilla (both skip), so the patch is safe regardless of whether the
/// GetHealthRel redirect installed.
///
/// The non-legacy path (CheckForTarget) walks AIG course-node enemy lists —
/// EnemyAgents only, spitters can never appear there — and needs no guard.
/// The reimplementation is a pure local read (OverlapSphere + linecasts) and
/// sends nothing, so it needs no netcode regardless of who runs it.
///
/// Decompile reference: gtfo-decompile/Modules-ASM/SentryGunInstance_Detection.cs.
/// </summary>
[HarmonyPatch]
internal static class Fix_SpitterSentryTarget
{
    /// <summary>Vanilla's extra line-of-sight probe offsets (up / left of the
    /// target position, decompile :215-217).</summary>
    private const float LosProbeOffset = 0.4f;

    /// <summary>Permanently fall back to vanilla behavior if the patch ever throws.</summary>
    private static bool _broken;

    [HarmonyPatch(typeof(SentryGunInstance_Detection),
        nameof(SentryGunInstance_Detection.CheckForTargetLegacy))]
    [HarmonyPrefix]
    public static bool Pre_CheckForTargetLegacy(
        ArchetypeDataBlock archetypeData, Transform detectionSource, ref EnemyAgent? __result)
    {
        if (_broken)
            return true;

        try
        {
            __result = CheckForTargetSkippingSpitters(archetypeData, detectionSource);
            return false;
        }
        catch (Exception ex)
        {
            _broken = true;
            Plugin.Logger.LogError(
                $"[SpitterSentryTarget] Legacy detection patch failed, reverting to vanilla behavior: {ex}");
            return true;
        }
    }

    /// <summary>
    /// Vanilla CheckForTargetLegacy with agent-less damageables (spitters)
    /// skipped instead of terminating the scan. Check order, masks, probes and
    /// first-match semantics all match vanilla.
    /// </summary>
    private static EnemyAgent? CheckForTargetSkippingSpitters(
        ArchetypeDataBlock archetypeData, Transform detectionSource)
    {
        var forward = detectionSource.forward;
        var origin = detectionSource.position;

        var colliders = Physics.OverlapSphere(
            origin, archetypeData.Sentry_DetectionMaxRange, LayerManager.MASK_SENTRYGUN_DETECTION_TARGETS);

        for (var i = 0; i < colliders.Length; i++)
        {
            var targetPos = colliders[i].transform.position;

            if (Vector3.Angle(forward, targetPos - origin) >= archetypeData.Sentry_DetectionMaxAngle)
                continue;

            if (Physics.Linecast(origin, targetPos, LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS)
                || Physics.Linecast(origin, targetPos + Vector3.up * LosProbeOffset,
                    LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS)
                || Physics.Linecast(origin, targetPos + Vector3.left * LosProbeOffset,
                    LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS))
            {
                continue;
            }

            var damageable = colliders[i].GetComponent<IDamageable>();
            if (damageable == null)
                continue;

            if (damageable.GetHealthRel() <= 0f)
                continue;

            // The one behavioral change: vanilla `return baseAgent` here even
            // when the agent is null (the spitter case) — we keep scanning.
            var enemy = damageable.GetBaseAgent()?.TryCast<EnemyAgent>();
            if (enemy == null)
                continue;

            if ((enemy.RequireTagForDetection || archetypeData.Sentry_FireTagOnly) && !enemy.IsTagged)
                continue;

            return enemy;
        }

        return null;
    }
}
