using System.Collections.Generic;
using Game;
using Game.Common;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class FocusedLoggingWatchedVehicleMonitorSystem : GameSystemBase
    {
        private readonly List<Entity> m_WatchedVehiclesBuffer = new List<Entity>();
        private ComponentLookup<Deleted> m_DeletedData;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_DeletedData = GetComponentLookup<Deleted>(true);
        }

        protected override void OnUpdate()
        {
            if (!FocusedLoggingService.HasWatchedVehicles)
            {
                return;
            }

            m_DeletedData.Update(this);
            m_WatchedVehiclesBuffer.Clear();
            FocusedLoggingService.CopyWatchedVehicles(m_WatchedVehiclesBuffer);

            for (int index = 0; index < m_WatchedVehiclesBuffer.Count; index += 1)
            {
                Entity vehicle = m_WatchedVehiclesBuffer[index];
                if (vehicle == Entity.Null)
                {
                    continue;
                }

                string reason = null;
                if (!EntityManager.Exists(vehicle))
                {
                    reason = "entity no longer exists";
                }
                else if (m_DeletedData.HasComponent(vehicle))
                {
                    reason = "entity marked deleted";
                }

                if (reason == null)
                {
                    continue;
                }

                FocusedLoggingService.RemoveWatchedVehicleBecauseMissing(
                    vehicle,
                    reason);
            }
        }
    }
}
