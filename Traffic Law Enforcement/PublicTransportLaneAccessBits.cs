using System;

namespace Traffic_Law_Enforcement
{
    [Flags]
    public enum PublicTransportLaneAccessBits : byte
    {
        None = 0,

        // Vanilla-side facts
        VanillaAllowsAccess = 1 << 0,
        VanillaPrefersLanes = 1 << 1,

        // Mod-side facts
        ModAllowsAccess = 1 << 2,
        ModPrefersLanes = 1 << 3,

        // Identity / derived-context helpers
        IsRoadPublicTransport = 1 << 4,
        PermissionChangedByMod = 1 << 5,

        Reserved6 = 1 << 6,
        Reserved7 = 1 << 7,
    }
}