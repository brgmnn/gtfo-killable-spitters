using System;
using HarmonyLib;

namespace KillableSpitters.Patches;

/// <summary>
/// Makes explosions (deployer mines, disposable trip mines, grenades — anything routed through
/// DamageUtil.DoExplosionDamage[_Capsule]) actually damage an InfectionSpitter.
///
/// Vanilla forwards explosion damage correctly — InfectionSpitterDamage.ExplosionDamage calls
/// m_spitter.OnIncomingDamage (decompile InfectionSpitterDamage.cs:38-41), which the mod's
/// Patch_SpitterDamage tap turns into health-pool drain. The problem is upstream: the AoE
/// dispatcher never <i>reaches</i> that call for a spitter. Before invoking ExplosionDamage,
/// DamageUtil.DoExplosionDamage_Capsule (DamageUtil.cs:104-131) aims a line-of-sight raycast and a
/// distance-falloff term at the target's IDamageable.DamageTargetPos. Every other damageable
/// overrides DamageTargetPos to its transform.position (e.g. Dam_EnemyDamageLimb.cs:303), but
/// InfectionSpitterDamage.DamageTargetPos is a { private set; get; } auto-property that the game
/// never assigns (InfectionSpitterDamage.cs:19), so it stays Vector3.zero. The aim vector then
/// points from the blast toward world origin (0,0,0): the LoS raycast is fired the length of the
/// level, hits geometry that is not the spitter, and the ExplosionDamage call is skipped entirely.
/// (Bullets and melee hit the collider directly via BulletDamage/MeleeDamage and never consult
/// DamageTargetPos, which is why only explosions were affected.)
///
/// The fix is to give the spitter's damageable a real world position, exactly as every other
/// damageable reports. This postfix sets it once, per spitter, in AssignCourseNode — the single
/// per-spitter registration hook where the game itself first reads transform.position (it derives
/// m_position from it at InfectionSpitter.cs:161) and assigns m_spitterIndex. Spitters are static
/// wall hazards that never move, so a one-time set is stable.
///
/// m_damage.transform.position is the correct value: the explosion overlap resolves a collider to
/// its damageable via collider.GetComponent&lt;IDamageable&gt;() (DamageUtil.cs:104), so the collider
/// and m_damage share a GameObject (m_damage may be a child of the spitter — SpitterKillManager
/// relies on m_damage.gameObject differing from spitter.gameObject). Its transform.position is thus
/// precisely the collider position, identical to the value vanilla uses for damageables that do
/// not override DamageTargetPos. With it set, the LoS raycast runs blast->spitter with proper
/// occlusion and the falloff uses the real distance, so the spitter takes correctly-scaled damage.
///
/// AssignCourseNode is a public, non-trivial method, so it is not an IL2CPP identical-code-folding
/// hazard — unlike the trivial get_DamageTargetPos auto-getter, which we deliberately do not patch.
/// The write is host-independent (it just conditions the state); the explosion itself is
/// master-only (DoExplode gates on SNet.IsMaster). Despite the game's private setter, Il2CppInterop
/// exposes set_DamageTargetPos as public, so a direct assignment compiles.
///
/// Companion to Fix_SpitterMineTrigger: that patch governs mine <i>detection</i> (a spitter must
/// not self-trigger a mine); this one governs <i>damage application</i> (the resulting blast must
/// still hurt spitters).
/// </summary>
[HarmonyPatch]
internal static class Fix_SpitterExplosionDamage
{
    /// <summary>Permanently fall back to vanilla behavior if the patch ever throws.</summary>
    private static bool _broken;

    [HarmonyPatch(typeof(InfectionSpitter), nameof(InfectionSpitter.AssignCourseNode))]
    [HarmonyPostfix]
    public static void Post_AssignCourseNode(InfectionSpitter __instance)
    {
        if (_broken)
            return;

        try
        {
            var dam = __instance.m_damage;
            if (dam == null)
                return;

            // Was Vector3.zero (never assigned by the game); give it the collider's real position
            // so the explosion dispatcher's LoS raycast + falloff target the spitter, not origin.
            dam.DamageTargetPos = dam.transform.position;
        }
        catch (Exception ex)
        {
            _broken = true;
            Plugin.Logger.LogError(
                $"[SpitterExplosionDamage] Failed to set DamageTargetPos; explosions will not " +
                $"damage spitters: {ex}");
        }
    }
}
