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
        private EntityQuery m_ChangedViolationQuery;
        private EntityQuery m_StatisticsQuery;

        private Entity m_EventEntity;
        private Entity m_StatisticsEntity;

        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private int m_LastActiveViolatorCount;
        private int m_LastObservedRuntimeWorldGeneration = -1;
        private bool m_HasCachedActiveViolatorCount;

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
            m_ChangedViolationQuery = GetEntityQuery(
                ComponentType.ReadOnly<PublicTransportLaneViolation>());
            m_ChangedViolationQuery.SetChangedVersionFilter(
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
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (m_LastObservedRuntimeWorldGeneration != currentGeneration)
            {
                m_LastObservedRuntimeWorldGeneration = currentGeneration;
                m_HasCachedActiveViolatorCount = false;
            }

            if (m_EventEntity == Entity.Null || !EntityManager.Exists(m_EventEntity))
            {
                if (m_EventBufferQuery.IsEmptyIgnoreFilter)
                {
                    return;
                }

                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }

            if (!ShouldApplyPublicTransportLaneIntervention())
            {
                ResetViolationApplyStateForDisabledEnforcement();
                return;
            }

            DynamicBuffer<DetectedPublicTransportLaneEvent> events =
                EntityManager.GetBuffer<DetectedPublicTransportLaneEvent>(m_EventEntity);

            if (CanSkipViolationApplyUpdate(events))
            {
                return;
            }

            TrafficLawEnforcementStatistics statistics =
                EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);

            bool statisticsChanged = false;

            if (events.Length > 0)
            {
                m_TypeLookups.Update(this);
                EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

                for (int index = 0; index < events.Length; index += 1)
                {
                    if (ApplyDetectedEvent(
                            events[index],
                            settings,
                            ref statistics))
                    {
                        statisticsChanged = true;
                    }
                }

                events.Clear();
            }

            int activeViolatorCount = GetActiveViolatorCount();
            if (statistics.m_ActivePublicTransportLaneViolatorCount != activeViolatorCount)
            {
                statistics.m_ActivePublicTransportLaneViolatorCount = activeViolatorCount;
                statisticsChanged = true;
            }

            PublishStatistics(statistics, statisticsChanged);
        }

        private static bool ShouldApplyPublicTransportLaneIntervention()
        {
            return Mod.IsPublicTransportLaneEnforcementEnabled;
        }

        private void ResetViolationApplyStateForDisabledEnforcement()
        {
            TrafficLawEnforcementStatistics statistics =
                EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(
                    m_StatisticsEntity);
            bool statisticsChanged =
                ResetActiveViolatorStatisticsForDisabledEnforcement(ref statistics);

            m_LastActiveViolatorCount = 0;
            m_HasCachedActiveViolatorCount = true;
            PublishStatistics(statistics, statisticsChanged);
            ClearDetectedEvents();
        }

        private static bool ResetActiveViolatorStatisticsForDisabledEnforcement(
            ref TrafficLawEnforcementStatistics statistics)
        {
            if (statistics.m_ActivePublicTransportLaneViolatorCount == 0)
            {
                return false;
            }

            statistics.m_ActivePublicTransportLaneViolatorCount = 0;
            return true;
        }

        private bool CanSkipViolationApplyUpdate(
            DynamicBuffer<DetectedPublicTransportLaneEvent> events)
        {
            return events.Length == 0 &&
                m_HasCachedActiveViolatorCount &&
                m_ChangedViolationQuery.IsEmptyIgnoreFilter;
        }

        private bool ApplyDetectedEvent(
            DetectedPublicTransportLaneEvent evt,
            EnforcementGameplaySettingsState settings,
            ref TrafficLawEnforcementStatistics statistics)
        {
            switch (evt.Kind)
            {
                case PublicTransportLaneEventKind.ViolationStart:
                    return ApplyViolationStart(evt, settings, ref statistics);
                case PublicTransportLaneEventKind.UsageType2:
                    RecordType2UsageObservation(evt);
                    return false;
                case PublicTransportLaneEventKind.UsageType3:
                    RecordType3UsageObservation(evt);
                    return false;
                case PublicTransportLaneEventKind.UsageType4:
                    RecordType4UsageObservation(evt);
                    return false;
                default:
                    return false;
            }
        }

        private bool ApplyViolationStart(
            DetectedPublicTransportLaneEvent evt,
            EnforcementGameplaySettingsState settings,
            ref TrafficLawEnforcementStatistics statistics)
        {
            statistics.m_PublicTransportLaneViolationCount += 1;
            string reason = PublicTransportLanePolicy.DescribeMissingPermissionReason(
                evt.Vehicle,
                settings,
                ref m_TypeLookups);

            EnforcementPenaltyService.RecordPublicTransportLaneViolation(
                evt.Vehicle,
                evt.Lane,
                reason);
            RecordViolationStartObservation(
                evt,
                statistics.m_PublicTransportLaneViolationCount,
                reason);
            return true;
        }

        private static void RecordViolationStartObservation(
            DetectedPublicTransportLaneEvent evt,
            int violationCount,
            string reason)
        {
            if (!EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(
                    evt.Vehicle))
            {
                return;
            }

            string message =
                $"Public transport lane violation #{violationCount}: vehicle={evt.Vehicle}, lane={evt.Lane}, reason={reason}";
            EnforcementLoggingPolicy.RecordEnforcementEvent(message, evt.Vehicle);
        }

        private static void RecordType2UsageObservation(
            DetectedPublicTransportLaneEvent evt)
        {
            if (!EnforcementLoggingPolicy.ShouldLogVehicleSpecificType2Usage(
                    evt.Vehicle))
            {
                return;
            }

            string message =
                $"PT-lane usage by vanilla-allowed but mod-denied vehicle (Type 2): vehicle={evt.Vehicle}, lane={evt.Lane}";
            EnforcementLoggingPolicy.RecordType2Usage(message, evt.Vehicle);
        }

        private static void RecordType3UsageObservation(
            DetectedPublicTransportLaneEvent evt)
        {
            if (!EnforcementLoggingPolicy.ShouldLogVehicleSpecificType3Usage(
                    evt.Vehicle))
            {
                return;
            }

            string message =
                $"PT-lane usage by vanilla-denied but mod-allowed vehicle (Type 3): vehicle={evt.Vehicle}, lane={evt.Lane}";
            EnforcementLoggingPolicy.RecordType3Usage(message, evt.Vehicle);
        }

        private static void RecordType4UsageObservation(
            DetectedPublicTransportLaneEvent evt)
        {
            if (!EnforcementLoggingPolicy.ShouldLogVehicleSpecificType4Usage(
                    evt.Vehicle))
            {
                return;
            }

            string message =
                $"PT-lane usage by vanilla-denied and mod-denied vehicle (Type 4): vehicle={evt.Vehicle}, lane={evt.Lane}";
            EnforcementLoggingPolicy.RecordType4Usage(message, evt.Vehicle);
        }

        private int GetActiveViolatorCount()
        {
            if (!m_HasCachedActiveViolatorCount ||
                !m_ChangedViolationQuery.IsEmptyIgnoreFilter)
            {
                m_LastActiveViolatorCount = m_ViolationQuery.CalculateEntityCount();
                m_HasCachedActiveViolatorCount = true;
            }

            return m_LastActiveViolatorCount;
        }

        private void PublishStatistics(
            TrafficLawEnforcementStatistics statistics,
            bool statisticsChanged)
        {
            if (statisticsChanged)
            {
                EntityManager.SetComponentData(m_StatisticsEntity, statistics);
            }

            EnforcementTelemetry.SetStatistics(statistics);
        }

        private void ClearDetectedEvents()
        {
            DynamicBuffer<DetectedPublicTransportLaneEvent> events =
                EntityManager.GetBuffer<DetectedPublicTransportLaneEvent>(
                    m_EventEntity);
            if (events.Length > 0)
            {
                events.Clear();
            }
        }
    }
}
