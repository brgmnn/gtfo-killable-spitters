using System.Runtime.CompilerServices;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace KillableSpitters.Patches.Compat;

/// <summary>
/// Soft-dependency compat driver for ExtraWeaponCustomization (EWC). EWC's
/// custom damage systems bypass the vanilla IDamageable paths this mod taps,
/// and filter spitters out before ever calling one:
///  - Projectiles / DoT gate on GetBaseDamagable().GetHealthRel() > 0 — fixed
///    generically by Fix_SpitterHealthRel (no EWC patch needed).
///  - Explosions never enumerate spitters at all (EWCExplosionPatch).
///  - Foam explicitly blacklists non-agent damageables (EWCFoamPatch).
///
/// Structure rules (why this looks the way it does):
///  - The patch classes carry NO [HarmonyPatch] attributes, so
///    harmony.PatchAll() never touches EWC types — patches are applied
///    manually here, only after the EWC plugin is confirmed loaded.
///  - Each Apply and patch method keeps EWC types strictly inside method
///    bodies/signatures that are only JIT-compiled when EWC is present; this
///    driver's own signatures are EWC-free, so it is always safe to call.
///  - Each patch application is individually try/caught: an EWC refactor
///    breaks that one compat feature with a warning, never the mod. Built and
///    verified against EWC 4.12.6 (the 4.12.x line GTFriendlyO ships).
/// </summary>
internal static class EWCCompat
{
    internal const string PluginGuid = "Dinorush.ExtraWeaponCustomization";

    internal static void Init(Harmony harmony)
    {
        if (!IL2CPPChainloader.Instance.Plugins.TryGetValue(PluginGuid, out var pluginInfo))
        {
            Plugin.Logger.LogDebug("[EWCCompat] ExtraWeaponCustomization not present, compat patches skipped");
            return;
        }

        Plugin.Logger.LogInfo(
            $"[EWCCompat] ExtraWeaponCustomization {pluginInfo.Metadata.Version} detected, applying compat patches");

        TryApply(harmony, "explosion", EWCExplosionPatch.Apply);
        TryApply(harmony, "foam", EWCFoamPatch.Apply);
    }

    private static void TryApply(Harmony harmony, string name, Action<Harmony> apply)
    {
        try
        {
            ApplyIsolated(harmony, apply);
            Plugin.Logger.LogDebug($"[EWCCompat] {name} compat patch applied");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning(
                $"[EWCCompat] {name} compat patch failed (EWC version mismatch?), that feature stays " +
                $"EWC-default for spitters: {ex}");
        }
    }

    /// <summary>Keeps the delegate invocation (and with it the first JIT of
    /// the EWC-typed Apply body) inside the caller's try/catch frame.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ApplyIsolated(Harmony harmony, Action<Harmony> apply) => apply(harmony);
}
