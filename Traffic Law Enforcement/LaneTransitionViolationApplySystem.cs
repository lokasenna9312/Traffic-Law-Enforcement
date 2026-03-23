using Game;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public partial class LaneTransitionViolationApplySystem : GameSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            // Placeholder only.
            // Current develop still applies lane-transition penalties/logging/statistics
            // directly inside LaneTransitionViolationSystem.
        }
    }
}
