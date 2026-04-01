using System;

namespace Traffic_Law_Enforcement
{
    [Flags]
    public enum PublicTransportLaneAccessBits : byte
    {
        None = 0,

        // Effective allow-state facts, retained for save compatibility and
        // diagnostics. Use CanUsePublicTransportLane(...) for runtime checks.
        EffectiveVanillaAllowsAccess = 1 << 0,
        VanillaPrefersLanes = 1 << 1,

        EffectiveModAllowsAccess = 1 << 2,
        ModPrefersLanes = 1 << 3,

        // Identity / derived-context helpers
        IsRoadPublicTransport = 1 << 4,
        PermissionChangedByMod = 1 << 5,

        // Configured allow-state before any emergency-duty override is applied.
        VanillaAllowsAccess = 1 << 6,
        ModAllowsAccess = 1 << 7,
    }
}
