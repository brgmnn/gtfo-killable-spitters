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
    public const string Version = "0.1.0";

    public const string Name = "the_tavern-KillableSpitters";

    public static ManualLogSource Logger { get; private set; } = new("KillableSpitters");

    /// <summary>
    /// Host-authoritative: in multiplayer only the lobby host's value matters
    /// (see SpitterKillManager).
    /// </summary>
    public static bool Config_KillableSpitters { get; set; }

    /// <summary>
    /// Bullet health pool for killable spitters. Host-authoritative.
    /// </summary>
    public static float Config_SpitterHealth { get; set; }

    /// <summary>
    /// Seconds after being C-foamed before a spitter dies (0 or less keeps
    /// the vanilla freeze-only behavior). Host-authoritative.
    /// </summary>
    public static float Config_SpitterGlueKillSeconds { get; set; }

    public override void Load()
    {
        Logger = Log;

        var killableSpitters = Config.Bind(
            new ConfigDefinition("Enemies", "KillableSpitters"),
            true,
            new ConfigDescription("Allow infection spitters to be shot and killed with bullets. " +
                                  "In multiplayer the lobby HOST's setting decides; clients always " +
                                  "mirror the host's spitter deaths."));

        var spitterHealth = Config.Bind(
            new ConfigDefinition("Enemies", "SpitterHealth"),
            30.0f,
            new ConfigDescription("Bullet health pool for killable spitters. Only the lobby " +
                                  "host's value applies."));

        var spitterGlueKillSeconds = Config.Bind(
            new ConfigDefinition("Enemies", "SpitterGlueKillSeconds"),
            5.0f,
            new ConfigDescription("Seconds after being C-foamed before a spitter dies (with the " +
                                  "full death explosion). 0 or less keeps the vanilla freeze-only " +
                                  "behavior. Values beyond the vanilla 240s freeze fire after the " +
                                  "foam has worn off. Only the lobby host's value applies; " +
                                  "requires KillableSpitters."));

        Config_KillableSpitters = killableSpitters.Value;
        Config_SpitterHealth = spitterHealth.Value;
        Config_SpitterGlueKillSeconds = spitterGlueKillSeconds.Value;

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
