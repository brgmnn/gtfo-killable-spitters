using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using KillableSpitters.Patches.Spitters;

namespace KillableSpitters.Patches;

/// <summary>
/// Makes InfectionSpitterDamage.GetHealthRel() report a killable spitter's real
/// health instead of the vanilla hardcoded 0 (decompile
/// InfectionSpitterDamage.cs:57). With this mod a spitter HAS health, and the
/// stub is now a lie that breaks third-party mods: ExtraWeaponCustomization
/// (and anything else using the common "GetBaseDamagable().GetHealthRel() > 0"
/// liveness idiom) classifies every spitter as already dead and refuses to
/// damage it (EWC: EWCProjectileHitbox.ShouldDamage:395 kills custom
/// projectiles, DOTController.Alive:119 purges DoTs before their first tick).
///
/// ICF WARNING — why this is NOT a Harmony patch: `GetHealthRel() => 0.0f` is
/// a trivially-bodied method, and IL2CPP release links fold identical bodies
/// into one shared native function (see the Patch_SpitterDamage class header
/// for the original hammer-crash CTD this caused). A Harmony/native detour of
/// that shared body would fire for every folded "return 0f" sibling in the
/// binary with mismatched argument layouts. House rule: never detour a
/// trivially-bodied il2cpp method.
///
/// Safe mechanism — redirect only this method's METADATA and this class's
/// DISPATCH TABLE, never touching the shared native body:
///  - write InfectionSpitterDamage.GetHealthRel's own Il2CppMethodInfo
///    .methodPointer (each method keeps a distinct MethodInfo struct even when
///    bodies fold). Covers every managed/interop call: Il2CppInterop proxies
///    dispatch via il2cpp_object_get_virtual_method + il2cpp_runtime_invoke,
///    both of which read the resolved MethodInfo's methodPointer (verified
///    against this game's interop assembly — the IDamageable proxy body).
///  - rewrite VirtualInvokeData.methodPtr in InfectionSpitterDamage's own
///    class vtable for every slot backed by that MethodInfo. Covers native
///    game-code interface call sites (e.g. sentry legacy detection), which
///    call the cached vtable methodPtr directly.
/// Hardcoded native direct calls to the folded body are not covered — audited:
/// vanilla never direct-calls GetHealthRel on a spitter (all other callers are
/// player-damage reads). The one vanilla interface-dispatch consumer that can
/// see a live spitter — sentry legacy detection — no longer reaches the vtable
/// at all: Fix_SpitterSentryTarget replaces that scan with managed code that
/// calls GetHealthRel through the interop proxy (the methodPointer path). The
/// vtable rewrite stays as defense in depth for any other native interface
/// call site, so a "0 slots rewritten" warning below is informational.
///
/// The replacement (HealthRelThunk) is called from native code with the
/// il2cpp instance-method ABI (this, MethodInfo*). It must never throw and
/// must not call back into il2cpp — it only does managed dictionary lookups
/// against SpitterKillManager's pointer map (IL2CPP's Boehm GC never moves
/// objects, so raw native pointers are stable keys). Anything unknown, dead,
/// over capacity or broken reports the vanilla 0.
///
/// Failure mode: any validation mismatch or exception during Install aborts
/// with a warning BEFORE any pointer is written, leaving vanilla behavior
/// (writes are raw pointer stores that cannot throw; the scan that can throw
/// happens first).
/// </summary>
internal static unsafe class Fix_SpitterHealthRel
{
    /// <summary>Native signature il2cpp invokes vtable/invoker targets with:
    /// (this, MethodInfo*) -> float for a parameterless instance method.</summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate float HealthRelFn(IntPtr thisPtr, IntPtr methodInfoPtr);

    /// <summary>Roots the marshaled thunk delegate for the process lifetime —
    /// its native stub is only pinned while the delegate is alive.</summary>
    private static HealthRelFn? _thunk;

    private static bool _warnedThunkFailure;

    /// <summary>True once the redirect is live.</summary>
    internal static bool Installed { get; private set; }

    /// <summary>
    /// Installs the redirect. Called once from Plugin.Load (the il2cpp domain
    /// is fully loaded before BepInEx plugins run). Idempotent via Installed.
    /// </summary>
    internal static void Install()
    {
        if (Installed)
            return;

        try
        {
            var klass = Il2CppClassPointerStore<global::InfectionSpitterDamage>.NativeClassPtr;
            if (klass == IntPtr.Zero)
            {
                WarnAborted("class pointer unavailable");
                return;
            }

            // Force class init so the vtable is built before we read it.
            IL2CPP.il2cpp_runtime_class_init(klass);

            var methodPtr = IL2CPP.il2cpp_class_get_method_from_name(klass, "GetHealthRel", 0);
            if (methodPtr == IntPtr.Zero)
            {
                WarnAborted("GetHealthRel not found on InfectionSpitterDamage");
                return;
            }

            var method = UnityVersionHandler.Wrap((Il2CppMethodInfo*)methodPtr);

            // Validate everything the game update could silently change before
            // writing a single pointer.
            var name = Marshal.PtrToStringAnsi(method.Name);
            if (name != "GetHealthRel")
            {
                WarnAborted($"resolved method is named '{name}'");
                return;
            }

            if (method.ParametersCount != 0)
            {
                WarnAborted($"unexpected parameter count {method.ParametersCount}");
                return;
            }

            var returnType = IL2CPP.il2cpp_method_get_return_type(methodPtr);
            var returnTypeName = returnType != IntPtr.Zero
                ? Marshal.PtrToStringAnsi(IL2CPP.il2cpp_type_get_name(returnType))
                : null;
            if (returnTypeName != "System.Single")
            {
                WarnAborted($"unexpected return type '{returnTypeName}'");
                return;
            }

            // Scan phase (may throw, nothing written yet): find every vtable
            // slot backed by this exact MethodInfo — the interface slot(s) for
            // IDamageable.GetHealthRel. The shared folded body itself may back
            // any number of OTHER slots via different MethodInfos; those are
            // deliberately untouched.
            var klassStruct = UnityVersionHandler.Wrap((Il2CppClass*)klass);
            var vtable = (VirtualInvokeData*)klassStruct.VTable;
            int vtableCount = klassStruct.VtableCount;
            var slots = new List<int>();

            if (vtable != null)
            {
                for (var i = 0; i < vtableCount; i++)
                {
                    if ((IntPtr)vtable[i].method == methodPtr)
                        slots.Add(i);
                }
            }

            var originalBody = method.MethodPointer;

            _thunk = HealthRelThunk;
            var thunkPtr = Marshal.GetFunctionPointerForDelegate(_thunk);

            // Write phase (raw pointer stores, cannot throw).
            foreach (var slot in slots)
                vtable![slot].methodPtr = thunkPtr;
            method.MethodPointer = thunkPtr;

            Installed = true;

            Plugin.Logger.LogDebug(
                $"[SpitterHealthRel] GetHealthRel redirected (original body 0x{originalBody:X}, " +
                $"{slots.Count} vtable slot(s) of {vtableCount} rewritten)");

            if (slots.Count == 0)
            {
                // Interop-driven calls (EWC) still work via the MethodInfo
                // write; only hardcoded native interface call sites would miss.
                Plugin.Logger.LogWarning(
                    "[SpitterHealthRel] No vtable slot referenced GetHealthRel's MethodInfo — native " +
                    "interface dispatch keeps the vanilla 0 (managed/interop callers are still redirected)");
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning(
                $"[SpitterHealthRel] Install failed, spitters keep the vanilla GetHealthRel() = 0 " +
                $"(EWC projectiles/DoTs will ignore spitters): {ex}");
        }
    }

    private static void WarnAborted(string reason)
    {
        Plugin.Logger.LogWarning(
            $"[SpitterHealthRel] Install aborted ({reason}), spitters keep the vanilla " +
            "GetHealthRel() = 0 (EWC projectiles/DoTs will ignore spitters)");
    }

    /// <summary>
    /// The redirected GetHealthRel body. Native callback: never throws, no
    /// il2cpp re-entry, no allocations on the lookup path.
    /// </summary>
    private static float HealthRelThunk(IntPtr thisPtr, IntPtr methodInfoPtr)
    {
        try
        {
            return SpitterKillManager.GetHealthRelForDamagePtr(thisPtr);
        }
        catch (Exception ex)
        {
            WarnThunkOnce(ex);
            return 0f;
        }
    }

    private static void WarnThunkOnce(Exception ex)
    {
        if (_warnedThunkFailure)
            return;

        _warnedThunkFailure = true;

        try
        {
            Plugin.Logger.LogWarning(
                $"[SpitterHealthRel] Health lookup failed, reporting vanilla 0: {ex.Message}");
        }
        catch
        {
            // Never let anything escape into native code.
        }
    }

    /// <summary>
    /// One-shot per-level visibility that the redirect actually took, called
    /// from SpitterKillManager once the level's pointer map is registered:
    /// invokes GetHealthRel through the interop proxy (the same dispatch EWC
    /// uses) and logs the value — expected 1 for an undamaged live spitter.
    /// </summary>
    internal static void LogSmokeCheck(global::InfectionSpitterDamage damage)
    {
        if (!Installed)
            return;

        try
        {
            Plugin.Logger.LogDebug(
                $"[SpitterHealthRel] Smoke check: GetHealthRel() = {damage.GetHealthRel()} " +
                "(expected > 0 for a live spitter)");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"[SpitterHealthRel] Smoke check failed: {ex.Message}");
        }
    }
}
