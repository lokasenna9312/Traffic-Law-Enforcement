using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct PublicTransportLaneViolation : IComponentData
    {
        public Entity m_Lane;
        public long m_StartDayTicks;
        public bool m_ExitPressureApplied;
    }
}
