using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;
using KillableSpitters.Patches.Spitters;

namespace KillableSpitters;

[BepInPlugin(Name, "KillableSpitters", Version)]
[BepInProcess("GTFO.exe")]
[BepInDependency("dev.gtfomodding.gtfo-api")]
[BepInDependency("Amor.AmorLib")]
public class Plugin : BasePlugin
{
    public const string Version = "0.5.0";

    public const string Name = "the_tavern-KillableSpitters";

    public static ManualLogSource Logger { get; private set; } = new("KillableSpitters");

    /// <summary>
    /// Health pool for killable spitters (any damage type drains it).
    /// Host-authoritative: in multiplayer only the lobby host's value matters
    /// (see SpitterKillManager).
    /// </summary>
    public static float Config_SpitterHealth { get; set; }

    /// <summary>
    /// Seconds a C-foamed spitter stays frozen — before it dies (if
    /// <see cref="Config_CfoamKillsSpitters"/> is on) or thaws back to normal
    /// (if off). Host-authoritative for the kill timing.
    /// </summary>
    public static float Config_SpitterFreezeDuration { get; set; }

    /// <summary>
    /// Whether C-foam kills spitters (foamed spitter dies with the destruction
    /// burst, no infection pop). Off keeps the vanilla freeze-only behavior.
    /// Host-authoritative.
    /// </summary>
    public static bool Config_CfoamKillsSpitters { get; set; }

    public override void Load()
    {
        Logger = Log;

        var spitterHealth = Config.Bind(
            new ConfigDefinition("General", "SpitterHealth"),
            30.0f,
            new ConfigDescription("Health pool for killable spitters (drained by any damage " +
                                  "type). Only the lobby host's value applies."));

        var spitterFreezeDuration = Config.Bind(
            new ConfigDefinition("C-Foam", "SpitterFreezeDuration"),
            0.7f,
            new ConfigDescription("How many seconds a C-foamed spitter stays frozen before it " +
                                  "dies if CfoamKillsSpitters is on, otherwise before it thaws back " +
                                  "to normal. Only the lobby host's value applies to the kill timing.\n" +
                                  "Note: Base GTFO freezes spitters for 240 seconds."));

        var cfoamKillsSpitters = Config.Bind(
            new ConfigDefinition("C-Foam", "CfoamKillsSpitters"),
            true,
            new ConfigDescription("Whether C-foam kills spitters. Off keeps the vanilla freeze-only " +
                                  "behavior. Only the lobby host's value applies."));

        Config_SpitterHealth = spitterHealth.Value;
        Config_SpitterFreezeDuration = spitterFreezeDuration.Value;
        Config_CfoamKillsSpitters = cfoamKillsSpitters.Value;

        Config.Save();

        GameDataAPI.OnGameDataInitialized += SpitterKillManager.Setup;

        // Verify the ICF-fold assumption behind the bullet damage tap
        // (see Patch_SpitterDamage class header). Log-only.
        Patch_SpitterDamage.LogDamageForwarderFoldState();

        // Apply patches
        var harmony = new Harmony(Name);
        harmony.PatchAll();
    }
}
