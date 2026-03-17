using System;

namespace Traffic_Law_Enforcement
{
    [Flags]
    public enum LaneMovement : byte
    {
        None = 0,
        Forward = 1 << 0,
        Left = 1 << 1,
        Right = 1 << 2,
        UTurn = 1 << 3,
    }
}