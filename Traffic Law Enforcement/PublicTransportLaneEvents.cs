using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public enum PublicTransportLaneEventKind : byte
    {
        None = 0,
        ViolationStart = 1,
        UsageType2 = 2,
        UsageType3 = 3,
        UsageType4 = 4,
        ViolationEnd = 5,
    }

    public struct DetectedPublicTransportLaneEvent : IBufferElementData
    {
        public Entity Vehicle;
        public Entity Lane;
        public PublicTransportLaneEventKind Kind;
    }

    public struct PublicTransportLaneEventBufferTag : IComponentData
    {
    }
}
