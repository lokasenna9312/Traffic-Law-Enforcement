using Game;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public partial class PublicTransportLaneReconcileSystem : GameSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            // Placeholder only.
            // Current develop still evaluates PT-lane state directly in
            // PublicTransportLaneViolationSystem and does not yet require
            // separate reconcile logic for correctness.
        }
    }
}
