using System;
using HarmonyLib;

namespace KillableSpitters.Patches;

/// <summary>
/// Neutralizes a hard crash that occurs when a C-Foam / glue-mine blob collides with a spitter while
/// a downstream mod postfixes GlueGunProjectile.CollisionCheck. A spitter's damageable
/// InfectionSpitterDamage is an IGlueTarget whose GlueTargetEnemyAgent and GetBaseAgent() are
/// hardcoded null (decompile InfectionSpitterDamage.cs:126,53) because a spitter is not an
/// EnemyAgent; DoorEnemyFixUpdated &lt;= 1.1.3 dereferences that without a guard in its
/// GluePatches.Post_Collision postfix and throws a NullReferenceException. Because that throw is
/// inside the il2cpp-&gt;managed trampoline there is no managed catch above it, so the game hard
/// crashes on any C-Foam / glue-mine use once foaming a spitter is worthwhile — which is exactly what
/// KillableSpitters makes it, by letting C-Foam kill spitters.
///
/// Vanilla CollisionCheck (decompile GlueGunProjectile.cs:425-436) is pure physics plus field writes
/// and cannot itself NRE, and KillableSpitters never postfixes it, so a NullReferenceException
/// surfacing here is always a foreign postfix. A Harmony Finalizer wraps the whole patch chain and
/// receives that exception regardless of mod load order; we swallow it only for the spitter case and
/// re-throw anything else unchanged. The real fix belongs upstream in DoorEnemyFixUpdated.
///
/// CollisionCheck has a large, unique body (not an ICF-foldable forwarder), so patching it is safe
/// per the mod's identical-code-folding rule.
/// </summary>
[HarmonyPatch]
internal static class Fix_SpitterGlueCollisionCrash
{
    /// <summary>One-time log so a repeatedly-firing suppression doesn't spam the console.</summary>
    private static bool _logged;

    [HarmonyPatch(typeof(GlueGunProjectile), nameof(GlueGunProjectile.CollisionCheck))]
    [HarmonyFinalizer]
    public static Exception? Finalize_CollisionCheck(GlueGunProjectile __instance, Exception? __exception)
    {
        // Pass through the no-exception path and anything that isn't the class we neutralize.
        if (!(__exception is NullReferenceException))
            return __exception;

        bool hitSpitter;
        try
        {
            hitSpitter = SpitterColliders.IsSpitter(__instance.m_projLastRayHitCollider);
        }
        catch
        {
            hitSpitter = true; // field unreadable: prefer suppressing over letting the game crash
        }

        if (!hitSpitter)
            return __exception; // an NRE on a non-spitter glue collision isn't ours — preserve it

        if (!_logged)
        {
            _logged = true;
            Plugin.Logger.LogWarning(
                "[SpitterGlueCollisionCrash] Suppressed a downstream NullReferenceException from a " +
                "GlueGunProjectile.CollisionCheck postfix on a glue-vs-spitter collision (e.g. " +
                "DoorEnemyFixUpdated <= 1.1.3). That is the other mod's bug; KillableSpitters is " +
                "neutralizing it so C-Foam / glue-mines keep working.");
        }

        return null; // swallow
    }
}
