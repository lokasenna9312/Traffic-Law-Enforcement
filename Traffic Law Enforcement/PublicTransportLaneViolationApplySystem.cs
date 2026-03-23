using Game;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class PublicTransportLaneViolationApplySystem : GameSystemBase
    {
        private EntityQuery m_EventBufferQuery;
        private EntityQuery m_ViolationQuery;
        private EntityQuery m_StatisticsQuery;

        private Entity m_EventEntity;
        private Entity m_StatisticsEntity;

        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EventBufferQuery = GetEntityQuery(
                ComponentType.ReadOnly<PublicTransportLaneEventBufferTag>(),
                ComponentType.ReadWrite<DetectedPublicTransportLaneEvent>());

            if (m_EventBufferQuery.IsEmptyIgnoreFilter)
            {
                m_EventEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<PublicTransportLaneEventBufferTag>(m_EventEntity);
                EntityManager.AddBuffer<DetectedPublicTransportLaneEvent>(m_EventEntity);
            }
            else
            {
                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }

            m_ViolationQuery = GetEntityQuery(
                ComponentType.ReadOnly<PublicTransportLaneViolation>());

            m_StatisticsQuery = GetEntityQuery(
                ComponentType.ReadWrite<TrafficLawEnforcementStatistics>());

            if (m_StatisticsQuery.IsEmptyIgnoreFilter)
            {
                m_StatisticsEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(
                    m_StatisticsEntity,
                    default(TrafficLawEnforcementStatistics));
            }
            else
            {
                m_StatisticsEntity = m_StatisticsQuery.GetSingletonEntity();
            }

            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
        }

        protected override void OnUpdate()
        {
            if (m_EventEntity == Entity.Null || !EntityManager.Exists(m_EventEntity))
            {
                if (m_EventBufferQuery.IsEmptyIgnoreFilter)
                {
                    return;
                }

                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }

            if (!Mod.IsPublicTransportLaneEnforcementEnabled)
            {
                TrafficLawEnforcementStatistics statistics =
                    EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);

                bool statisticsChanged = false;

                if (statistics.m_ActivePublicTransportLaneViolatorCount != 0)
                {
                    statistics.m_ActivePublicTransportLaneViolatorCount = 0;
                    statisticsChanged = true;
                }

                if (statisticsChanged)
                {
                    EntityManager.SetComponentData(m_StatisticsEntity, statistics);
                }

                EnforcementTelemetry.SetStatistics(statistics);

                DynamicBuffer<DetectedPublicTransportLaneEvent> events =
                    EntityManager.GetBuffer<DetectedPublicTransportLaneEvent>(m_EventEntity);
                if (events.Length > 0)
                {
                    events.Clear();
                }

                return;
            }

            m_TypeLookups.Update(this);

            DynamicBuffer<DetectedPublicTransportLaneEvent> events =
                EntityManager.GetBuffer<DetectedPublicTransportLaneEvent>(m_EventEntity);

            TrafficLawEnforcementStatistics statistics =
                EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);

            bool statisticsChanged = false;

            if (events.Length > 0)
            {
                EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

                for (int index = 0; index < events.Length; index += 1)
                {
                    DetectedPublicTransportLaneEvent evt = events[index];

                    switch (evt.Kind)
                    {
                        case PublicTransportLaneEventKind.ViolationStart:
                        {
                            statistics.m_PublicTransportLaneViolationCount += 1;
                            statisticsChanged = true;

                            string reason = PublicTransportLanePolicy.DescribeMissingPermissionReason(
                                evt.Vehicle,
                                settings,
                                ref m_TypeLookups);

                            string message =
                                $"Public transport lane violation #{statistics.m_PublicTransportLaneViolationCount}: vehicle={evt.Vehicle}, lane={evt.Lane}, reason={reason}";

                            EnforcementPenaltyService.RecordPublicTransportLaneViolation(
                                evt.Vehicle,
                                evt.Lane,
                                reason);

                            EnforcementLoggingPolicy.RecordEnforcementEvent(message);
                            break;
                        }

                        case PublicTransportLaneEventKind.UsageType2:
                        {
                            string message =
                                $"PT-lane usage by vanilla-allowed but mod-denied vehicle (Type 2): vehicle={evt.Vehicle}, lane={evt.Lane}";
                            EnforcementLoggingPolicy.RecordType2Usage(message);
                            break;
                        }

                        case PublicTransportLaneEventKind.UsageType3:
                        {
                            string message =
                                $"PT-lane usage by vanilla-denied but mod-allowed vehicle (Type 3): vehicle={evt.Vehicle}, lane={evt.Lane}";
                            EnforcementLoggingPolicy.RecordType3Usage(message);
                            break;
                        }

                        case PublicTransportLaneEventKind.UsageType4:
                        {
                            string message =
                                $"PT-lane usage by vanilla-denied and mod-denied vehicle (Type 4): vehicle={evt.Vehicle}, lane={evt.Lane}";
                            EnforcementLoggingPolicy.RecordType4Usage(message);
                            break;
                        }
                    }
                }

                events.Clear();
            }

            int activeViolatorCount = m_ViolationQuery.CalculateEntityCount();
            if (statistics.m_ActivePublicTransportLaneViolatorCount != activeViolatorCount)
            {
                statistics.m_ActivePublicTransportLaneViolatorCount = activeViolatorCount;
                statisticsChanged = true;
            }

            if (statisticsChanged)
            {
                EntityManager.SetComponentData(m_StatisticsEntity, statistics);
            }

            EnforcementTelemetry.SetStatistics(statistics);
        }
    }
}