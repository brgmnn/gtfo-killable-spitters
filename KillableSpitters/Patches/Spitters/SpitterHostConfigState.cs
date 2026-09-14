using System.Runtime.InteropServices;

namespace KillableSpitters.Patches.Spitters;

/// <summary>
/// The host's C-foam settings, replicated to every peer once per level
/// (AmorLib StateReplicator, ID SpitterKillManager.CONFIG_REPLICATOR_ID —
/// the slot right after the death-state shards, same lifecycle).
///
/// Why this exists: CfoamKillsSpitters and SpitterFreezeDuration are
/// host-authoritative, but both are consumed on EVERY peer — the non-lethal
/// freeze override writes a per-peer local timer, and the lethal path must
/// leave that timer at the vanilla 240s on every peer or vanilla thaws the
/// spitter (m_isGlued cleared at m_stayInTimer &lt; 5) before the replicated
/// death lands, turning the silent foam death into a full infection pop on
/// that peer only. Reading each peer's own config there desyncs the moment
/// two configs differ, so the host publishes its values and peers use those.
/// Until the state has arrived (IsPublished false) a peer leaves glue vanilla.
///
/// Struct size: 1 + 1 + 4 = 6 bytes (padded to 8), far under AmorLib's
/// smallest payload bucket. The flags are bytes, not bools: Marshal.SizeOf
/// treats bool as a 4-byte BOOL and AmorLib sizes payloads with Marshal.SizeOf.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SpitterHostConfigState
{
    private byte _published;

    private byte _cfoamKills;

    /// <summary>Seconds a foamed spitter stays frozen before it dies
    /// (CfoamKills) or thaws (otherwise). Finite and &gt;= 0.</summary>
    public float FreezeDuration;

    /// <summary>False for the pre-publish default the replicator is created
    /// with; true once the host's SetState has landed.</summary>
    public readonly bool IsPublished => _published != 0;

    public readonly bool CfoamKills => _cfoamKills != 0;

    /// <summary>Snapshot of this process's config, for the host to publish.</summary>
    public static SpitterHostConfigState FromHostConfig()
    {
        var freeze = Plugin.Config_SpitterFreezeDuration;

        return new SpitterHostConfigState
        {
            _published = 1,
            _cfoamKills = Plugin.Config_CfoamKillsSpitters ? (byte)1 : (byte)0,
            FreezeDuration = float.IsFinite(freeze) ? Math.Max(0f, freeze) : 0f,
        };
    }
}
