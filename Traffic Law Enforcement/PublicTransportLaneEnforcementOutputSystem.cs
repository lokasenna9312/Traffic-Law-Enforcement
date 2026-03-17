using Game;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public class PublicTransportLaneEnforcementOutputSystem : GameSystemBase
    {
        private EntityQuery m_ViolationQuery;
        private EntityQuery m_StatisticsQuery;
        private Entity m_StatisticsEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ViolationQuery = GetEntityQuery(
                ComponentType.ReadOnly<PublicTransportLaneViolation>());
            m_StatisticsQuery = GetEntityQuery(ComponentType.ReadWrite<TrafficLawEnforcementStatistics>());
            if (m_StatisticsQuery.IsEmptyIgnoreFilter)
            {
                m_StatisticsEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(m_StatisticsEntity, default(TrafficLawEnforcementStatistics));
            }
            else
            {
                m_StatisticsEntity = m_StatisticsQuery.GetSingletonEntity();
            }
        }

        protected override void OnUpdate()
        {
            TrafficLawEnforcementStatistics statistics = EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);
            int activeViolatorCount = Mod.IsEnforcementEnabled ? m_ViolationQuery.CalculateEntityCount() : 0;
            if (statistics.m_ActivePublicTransportLaneViolatorCount != activeViolatorCount)
            {
                statistics.m_ActivePublicTransportLaneViolatorCount = activeViolatorCount;
                EntityManager.SetComponentData(m_StatisticsEntity, statistics);
            }

            EnforcementTelemetry.SetStatistics(statistics);
        }
    }
}
