using Game;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class PublicTransportLanePermissionSystem : GameSystemBase
    {
        private EntityQuery m_AllCarsQuery;
        private EntityQuery m_ChangedCarQuery;
        private EntityQuery m_TrackedQuery;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private bool m_HasEvaluated;
        private bool m_LastEnforcementEnabled;
        private int m_LastSettingsMask;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_AllCarsQuery = GetEntityQuery(ComponentType.ReadWrite<Car>());
            m_ChangedCarQuery = GetEntityQuery(ComponentType.ReadWrite<Car>());
            m_ChangedCarQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Car>());
            m_TrackedQuery = GetEntityQuery(
                ComponentType.ReadWrite<Car>(),
                ComponentType.ReadWrite<PublicTransportLanePermissionState>());
            m_PathOwnerData = GetComponentLookup<PathOwner>();
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            RequireForUpdate(m_AllCarsQuery);
        }

        protected override void OnUpdate()
        {
            m_PathOwnerData.Update(this);
            m_TypeLookups.Update(this);
            m_CurrentLaneData.Update(this);
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

            bool enforcementEnabled = Mod.IsPublicTransportLaneEnforcementEnabled;
            if (!enforcementEnabled)
            {
                RestoreTrackedVehicles();
                m_HasEvaluated = false;
                m_LastEnforcementEnabled = false;
                return;
            }

            int settingsMask = PublicTransportLanePolicy.GetPermissionSettingsMask(settings);
            bool fullRefresh = !m_HasEvaluated || !m_LastEnforcementEnabled || settingsMask != m_LastSettingsMask;
            EvaluateQuery(fullRefresh ? m_AllCarsQuery : m_ChangedCarQuery, settings);

            m_HasEvaluated = true;
            m_LastEnforcementEnabled = true;
            m_LastSettingsMask = settingsMask;
        }

        private void EvaluateQuery(EntityQuery query, EnforcementGameplaySettingsState settings)
        {
            NativeArray<Entity> vehicles = query.ToEntityArray(Allocator.Temp);
            NativeArray<Car> cars = query.ToComponentDataArray<Car>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index++)
                {
                    EvaluateVehicle(vehicles[index], cars[index], settings);
                }
            }
            finally
            {
                vehicles.Dispose();
                cars.Dispose();
            }
        }

        private void EvaluateVehicle(Entity vehicle, Car car, EnforcementGameplaySettingsState settings)
        {
            bool hasState = EntityManager.HasComponent<PublicTransportLanePermissionState>(vehicle);
            PublicTransportLanePermissionState state = hasState
                ? EntityManager.GetComponentData<PublicTransportLanePermissionState>(vehicle)
                : default;
            CarFlags originalMask = hasState
                ? state.m_OriginalPublicTransportLaneFlags
                : (car.m_Flags & PublicTransportLanePolicy.PublicTransportLanePermissionMask);

            if (!PublicTransportLanePolicy.TryGetDesiredPermissionState(vehicle, car, settings, ref m_TypeLookups, out _, out CarFlags desiredMask))
            {
                if (hasState)
                {
                    RestoreVehicle(vehicle, car, state, removeState: true);
                }

                return;
            }

            CarFlags currentMask = car.m_Flags & PublicTransportLanePolicy.PublicTransportLanePermissionMask;
            bool emergencyActive = EmergencyVehiclePolicy.IsEmergencyVehicle(car);
            bool emergencyTransition = hasState && state.m_EmergencyActive != (emergencyActive ? (byte)1 : (byte)0);
            bool flagsChanged = currentMask != desiredMask;

            CarFlags obsoleteRelevantMask = CarFlags.UsePublicTransportLanes;
            bool obsoleteRelevantFlagsChanged =
                (currentMask & obsoleteRelevantMask) != (desiredMask & obsoleteRelevantMask);
            bool preferenceOnlyChange = flagsChanged && !obsoleteRelevantFlagsChanged;

            if (flagsChanged)
            {
                car.m_Flags = (car.m_Flags & ~PublicTransportLanePolicy.PublicTransportLanePermissionMask) | desiredMask;
                EntityManager.SetComponentData(vehicle, car);
            }

            PublicTransportLanePermissionState updatedState = new PublicTransportLanePermissionState
            {
                m_OriginalPublicTransportLaneFlags = originalMask,
                m_EmergencyActive = emergencyActive ? (byte)1 : (byte)0,
            };

            if (!hasState)
            {
                EntityManager.AddComponentData(vehicle, updatedState);
            }
            else if (!StatesEqual(state, updatedState))
            {
                EntityManager.SetComponentData(vehicle, updatedState);
            }

            if (obsoleteRelevantFlagsChanged || emergencyTransition)
            {
                string role = PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups);
                string reason = obsoleteRelevantFlagsChanged
                    ? "pt-permission-capability-changed"
                    : "emergency-state-changed";
                string extra =
                    $"currentMaskBefore={currentMask}, desiredMask={desiredMask}, originalMask={originalMask}, " +
                    $"flagsChanged={flagsChanged}, obsoleteRelevantFlagsChanged={obsoleteRelevantFlagsChanged}, " +
                    $"preferenceOnlyChange={preferenceOnlyChange}, emergencyTransition={emergencyTransition}";

                MarkPathObsolete(vehicle, car, reason, role, extra);
            }
        }

        private void RestoreTrackedVehicles()
        {
            if (m_TrackedQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            NativeArray<Entity> vehicles = m_TrackedQuery.ToEntityArray(Allocator.Temp);
            NativeArray<Car> cars = m_TrackedQuery.ToComponentDataArray<Car>(Allocator.Temp);
            NativeArray<PublicTransportLanePermissionState> states = m_TrackedQuery.ToComponentDataArray<PublicTransportLanePermissionState>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index++)
                {
                    RestoreVehicle(vehicles[index], cars[index], states[index], removeState: false);
                }
            }
            finally
            {
                vehicles.Dispose();
                cars.Dispose();
                states.Dispose();
            }

            EntityManager.RemoveComponent<PublicTransportLanePermissionState>(m_TrackedQuery);
        }

        private void RestoreVehicle(Entity vehicle, Car car, PublicTransportLanePermissionState state, bool removeState)
        {
            CarFlags currentMask = car.m_Flags & PublicTransportLanePolicy.PublicTransportLanePermissionMask;
            bool flagsChanged = currentMask != state.m_OriginalPublicTransportLaneFlags;
            if (flagsChanged)
            {
                car.m_Flags = (car.m_Flags & ~PublicTransportLanePolicy.PublicTransportLanePermissionMask) | state.m_OriginalPublicTransportLaneFlags;
                EntityManager.SetComponentData(vehicle, car);
            }

            if (flagsChanged || state.m_EmergencyActive != 0)
            {
                string role = PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups);
                string reason = flagsChanged
                    ? "restore-original-pt-permission-mask"
                    : "restore-emergency-state";
                string extra =
                    $"restoredMask={state.m_OriginalPublicTransportLaneFlags}, " +
                    $"hadTrackedEmergency={state.m_EmergencyActive != 0}";

                MarkPathObsolete(vehicle, car, reason, role, extra);
            }

            if (removeState && EntityManager.HasComponent<PublicTransportLanePermissionState>(vehicle))
            {
                EntityManager.RemoveComponent<PublicTransportLanePermissionState>(vehicle);
            }
        }

        private void MarkPathObsolete(Entity vehicle, Car car, string reason, string role, string extra)
        {
            if (!m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner))
            {
                return;
            }

            if ((pathOwner.m_State & PathFlags.Pending) != 0)
            {
                return;
            }

            if ((pathOwner.m_State & PathFlags.Obsolete) != 0)
            {
                return;
            }

            PathFlags stateBefore = pathOwner.m_State;
            pathOwner.m_State |= PathFlags.Obsolete;
            EntityManager.SetComponentData(vehicle, pathOwner);

            Entity currentLane = m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLaneData)
                ? currentLaneData.m_Lane
                : Entity.Null;

            PathObsoleteTraceLogging.Record(
                "PT_PERMISSION",
                vehicle,
                currentLane,
                stateBefore,
                pathOwner.m_State,
                reason,
                car,
                role,
                extra);
        }

        private static bool StatesEqual(PublicTransportLanePermissionState left, PublicTransportLanePermissionState right)
        {
            return left.m_OriginalPublicTransportLaneFlags == right.m_OriginalPublicTransportLaneFlags &&
                left.m_EmergencyActive == right.m_EmergencyActive;
        }
    }
}
