using System.Runtime.InteropServices;

namespace KillableSpitters.Events;

/// <summary>
/// NetworkAPI payload for reporting damage dealt to an InfectionSpitter.
/// Sent by the client that dealt the damage directly to the session master, who
/// owns the authoritative health ledger (see SpitterKillManager).
///
/// SpitterIndex is InfectionSpitter.m_spitterIndex — assigned in registration
/// order during deterministic, seed-driven level generation, so the same index
/// resolves to the same spitter on every peer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SpitterDamageEvent
{
    public ushort SpitterIndex { get; set; }

    public float Damage { get; set; }
}
