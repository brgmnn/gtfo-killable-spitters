using HarmonyLib;

namespace KillableSpitters.Patches;

/// <summary>
/// Neutralizes a hard crash that occurs when a C-Foam / glue-mine blob collides with a spitter while
/// a downstream mod postfixes GlueGunProjectile.CollisionCheck. A spitter's damageable
/// InfectionSpitterDamage is an IGlueTarget whose GlueTargetEnemyAgent and GetBaseAgent() are
/// hardcoded null (decompile InfectionSpitterDamage.cs:126 and :53) because a spitter is not an
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
/// Armed only when needed: the suppression checks once (lazily, on the first NRE, by which time
/// every plugin has loaded) whether any OTHER mod actually has a Harmony patch on CollisionCheck.
/// With none present an NRE here is something genuinely new and is surfaced unchanged rather than
/// hidden behind this workaround.
///
/// CollisionCheck has a large, unique body (not an ICF-foldable forwarder), so patching it is safe
/// per the mod's identical-code-folding rule.
///
/// There is deliberately no _broken kill-switch here: the finalizer already fails safe in both
/// directions (re-throw foreign exceptions, suppress the spitter case), and disabling it on an
/// error would reintroduce the crash it exists to prevent.
/// </summary>
[HarmonyPatch]
internal static class Fix_SpitterGlueCollisionCrash
{
    /// <summary>One-time log so a repeatedly-firing suppression doesn't spam the console.</summary>
    private static bool _logged;

    /// <summary>One-time log for the conservative fallback when the collider probe itself fails.</summary>
    private static bool _loggedProbeFailure;

    /// <summary>Whether some other mod has a patch on CollisionCheck. Null until first needed.</summary>
    private static bool? _foreignPatchPresent;

    [HarmonyPatch(typeof(GlueGunProjectile), nameof(GlueGunProjectile.CollisionCheck))]
    [HarmonyFinalizer]
    public static Exception? Finalize_CollisionCheck(GlueGunProjectile __instance, Exception? __exception)
    {
        // Pass through the no-exception path and anything that isn't the class we neutralize.
        if (!(__exception is NullReferenceException))
            return __exception;

        // Nobody else patches CollisionCheck: this NRE isn't the known foreign-postfix bug, and
        // vanilla can't throw it, so it must surface — never hide an unknown crash.
        if (!ForeignPatchPresent())
            return __exception;

        bool hitSpitter;
        try
        {
            hitSpitter = SpitterColliders.IsSpitter(__instance.m_projLastRayHitCollider);
        }
        catch (Exception ex)
        {
            hitSpitter = true; // field unreadable: prefer suppressing over letting the game crash

            if (!_loggedProbeFailure)
            {
                _loggedProbeFailure = true;
                Plugin.Logger.LogError(
                    "[SpitterGlueCollisionCrash] Could not identify the glue collision target; " +
                    "suppressing this and every later NullReferenceException on " +
                    "GlueGunProjectile.CollisionCheck conservatively — a real crash may be hidden " +
                    $"behind this until the probe works again: {ex}");
            }
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

    /// <summary>
    /// True when a Harmony patch owned by anyone but this mod sits on CollisionCheck. Resolved
    /// once; if the inspection itself fails the historical fail-safe (armed) applies.
    /// </summary>
    private static bool ForeignPatchPresent()
    {
        if (_foreignPatchPresent is { } known)
            return known;

        bool present;
        try
        {
            var original = AccessTools.Method(typeof(GlueGunProjectile), nameof(GlueGunProjectile.CollisionCheck));
            var owners = (original != null ? Harmony.GetPatchInfo(original)?.Owners : null)
                ?.Where(owner => owner != Plugin.Name)
                .ToList() ?? new List<string>();

            present = owners.Count > 0;
            Plugin.Logger.LogDebug(present
                ? "[SpitterGlueCollisionCrash] Foreign CollisionCheck patch owner(s): " +
                  $"{string.Join(", ", owners)} — spitter NRE suppression armed"
                : "[SpitterGlueCollisionCrash] No foreign CollisionCheck patch — spitter NRE suppression disarmed");
        }
        catch (Exception ex)
        {
            present = true;
            Plugin.Logger.LogWarning(
                "[SpitterGlueCollisionCrash] Could not inspect CollisionCheck patches, arming the " +
                $"suppression conservatively: {ex.Message}");
        }

        _foreignPatchPresent = present;
        return present;
    }
}
