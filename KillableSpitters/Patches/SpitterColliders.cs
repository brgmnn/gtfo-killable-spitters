using Il2CppInterop.Runtime;
using UnityEngine;

namespace KillableSpitters.Patches;

/// <summary>
/// Shared spitter-collider identification, used by both the mine-trigger guard
/// (<see cref="Fix_SpitterMineTrigger"/>) and the glue-collision crash guard
/// (<see cref="Fix_SpitterGlueCollisionCrash"/>).
/// </summary>
internal static class SpitterColliders
{
    /// <summary>
    /// True iff the hit collider belongs to a spitter — i.e. its damageable is an
    /// InfectionSpitterDamage (the spitter's IDamageable, the sibling component weapons hit). The
    /// spitter's damage collider carries InfectionSpitterDamage directly, mirroring how an enemy
    /// limb collider carries Dam_EnemyDamageLimb; a ColliderMaterial indirection is handled as a
    /// fallback for colliders that reference their damageable elsewhere.
    /// </summary>
    internal static bool IsSpitter(Collider collider)
    {
        if (collider == null)
            return false;

        if (collider.GetComponent<InfectionSpitterDamage>() != null)
            return true;

        var damageable = collider.GetComponent<ColliderMaterial>()?.Damageable;
        return damageable != null && damageable.TryCast<InfectionSpitterDamage>() != null;
    }
}
