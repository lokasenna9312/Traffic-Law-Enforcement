using Game;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public partial class PublicTransportLaneViolationApplySystem : GameSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            // Placeholder only.
            // Current develop still applies PT-lane penalties/logging/statistics
            // directly inside PublicTransportLaneViolationSystem.
        }
    }
}
