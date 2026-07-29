using UnityEngine;

namespace KillableSpitters.Patches;

/// <summary>
/// Shared collider→spitter resolution for the spitter patches.
/// </summary>
internal static class SpitterColliders
{
    /// <summary>
    /// Resolves the hit collider's damageable as an InfectionSpitterDamage (the spitter's
    /// IDamageable, the sibling component weapons hit). The spitter's damage collider carries
    /// InfectionSpitterDamage directly, mirroring how an enemy limb collider carries
    /// Dam_EnemyDamageLimb; a ColliderMaterial indirection is handled as a fallback for colliders
    /// that reference their damageable elsewhere.
    /// </summary>
    internal static bool TryGetDamage(Collider collider, out InfectionSpitterDamage damage)
    {
        damage = null!;

        if (collider == null)
            return false;

        damage = collider.GetComponent<InfectionSpitterDamage>();
        if (damage != null)
            return true;

        var damageable = collider.GetComponent<ColliderMaterial>()?.Damageable;
        if (damageable != null)
            damage = damageable.TryCast<InfectionSpitterDamage>()!;

        return damage != null;
    }

    /// <summary>True iff the hit collider belongs to a spitter.</summary>
    internal static bool IsSpitter(Collider collider) => TryGetDamage(collider, out _);
}
