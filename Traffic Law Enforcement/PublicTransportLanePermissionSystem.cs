using Game;
using Game.Net;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;
using System.Collections.Generic;

namespace Traffic_Law_Enforcement
{
    public partial class PublicTransportLanePermissionSystem : GameSystemBase
    {
        private const int kVehiclesPerFrame = 512;

        private EntityQuery m_AllCarsQuery;
        private EntityQuery m_ChangedCarQuery;
        private EntityQuery m_TrackedQuery;
        private EntityQuery m_TrackedWithoutProfileQuery;
        private EntityQuery m_PendingExitQuery;
        private BufferLookup<CarNavigationLane> m_NavigationLaneData;
        private ComponentLookup<ConnectionLane> m_ConnectionLaneData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private ComponentLookup<VehicleTrafficLawProfile> m_ProfileData;
        private ComponentLookup<PublicTransportLanePermissionState> m_PermissionStateData;
        private ComponentLookup<PublicTransportLanePendingExit> m_PendingExitData;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private NativeList<Entity> m_PendingRefreshVehicles;
        private HashSet<Entity> m_ProcessedThisFrame;
        private ComponentLookup<Car> m_CarData;
        private int m_RefreshCursor;
        private int m_LastObservedRuntimeWorldGeneration = -1;
        private bool m_HasEvaluated;
        private bool m_LastEnforcementEnabled;
        private int m_LastSettingsMask;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_AllCarsQuery = GetEntityQuery(ComponentType.ReadWrite<Car>());
            m_PendingExitQuery = GetEntityQuery(
                ComponentType.ReadWrite<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<PublicTransportLanePendingExit>());
            m_CarData = GetComponentLookup<Car>(true);
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
            m_NavigationLaneData = GetBufferLookup<CarNavigationLane>(true);
            m_ConnectionLaneData = GetComponentLookup<ConnectionLane>(true);
            m_ChangedCarQuery = GetEntityQuery(ComponentType.ReadWrite<Car>());
            m_ChangedCarQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Car>());
            m_TrackedQuery = GetEntityQuery(
                ComponentType.ReadWrite<Car>(),
                ComponentType.ReadWrite<PublicTransportLanePermissionState>());
            m_TrackedWithoutProfileQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<Car>(),
                    ComponentType.ReadWrite<PublicTransportLanePermissionState>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<VehicleTrafficLawProfile>(),
                },
            });
            m_PathOwnerData = GetComponentLookup<PathOwner>();
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_ProfileData = GetComponentLookup<VehicleTrafficLawProfile>(true);
            m_PermissionStateData = GetComponentLookup<PublicTransportLanePermissionState>(true);
            m_PendingExitData = GetComponentLookup<PublicTransportLanePendingExit>(true);
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
            m_PendingRefreshVehicles = new NativeList<Entity>(Allocator.Persistent);
            m_ProcessedThisFrame = new HashSet<Entity>();
            RequireForUpdate(m_AllCarsQuery);
        }

        protected override void OnDestroy()
        {
            if (m_PendingRefreshVehicles.IsCreated)
            {
                m_PendingRefreshVehicles.Dispose();
            }

            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            m_CarData.Update(this);
            m_CarLaneData.Update(this);
            m_PathOwnerData.Update(this);
            m_CurrentLaneData.Update(this);
            m_ProfileData.Update(this);
            HandleRuntimeWorldReload();
            m_TypeLookups.Update(this);
            m_PermissionStateData.Update(this);
            m_PendingExitData.Update(this);
            m_NavigationLaneData.Update(this);
            m_ConnectionLaneData.Update(this);
            m_EdgeLaneData.Update(this);

            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;
            bool enforcementEnabled = Mod.IsPublicTransportLaneEnforcementEnabled;

            if (!enforcementEnabled)
            {
                RestoreTrackedVehicles();
                if (!m_PendingExitQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<PublicTransportLanePendingExit>(m_PendingExitQuery);
                }
                ClearPendingRefresh();
                m_HasEvaluated = false;
                m_LastEnforcementEnabled = false;
                return;
            }

            int settingsMask = PublicTransportLanePolicy.GetPermissionSettingsMask(settings);
            bool fullRefreshRequested =
                !m_HasEvaluated ||
                !m_LastEnforcementEnabled ||
                settingsMask != m_LastSettingsMask;

            if (fullRefreshRequested && m_PendingRefreshVehicles.Length == 0)
            {
                BuildPendingRefreshList();
            }

            if (m_PendingRefreshVehicles.Length > 0)
            {
                ProcessRefreshBatch();

                if (m_PendingRefreshVehicles.Length == 0)
                {
                    m_HasEvaluated = true;
                    m_LastEnforcementEnabled = true;
                    m_LastSettingsMask = settingsMask;
                }

                return;
            }

            BeginSteadyStateEvaluation();
            EvaluateQueryDeduplicated(m_ChangedCarQuery);
            EvaluatePendingExitVehiclesDeduplicated();
            RestoreVehiclesMissingProfileDeduplicated();

            m_HasEvaluated = true;
            m_LastEnforcementEnabled = true;
            m_LastSettingsMask = settingsMask;
        }

        private void HandleRuntimeWorldReload()
        {
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (m_LastObservedRuntimeWorldGeneration == currentGeneration)
            {
                return;
            }

            m_LastObservedRuntimeWorldGeneration = currentGeneration;

            ClearPendingRefresh();
            m_ProcessedThisFrame.Clear();
            m_HasEvaluated = false;
            m_LastEnforcementEnabled = false;
            m_LastSettingsMask = 0;

            if (!m_PendingExitQuery.IsEmptyIgnoreFilter)
            {
                EntityManager.RemoveComponent<PublicTransportLanePendingExit>(m_PendingExitQuery);
            }

            if (!m_TrackedQuery.IsEmptyIgnoreFilter)
            {
                EntityManager.RemoveComponent<PublicTransportLanePermissionState>(m_TrackedQuery);
            }

            Mod.log.Info(
                $"[SAVELOAD] PublicTransportLanePermissionSystem runtime reset: generation={currentGeneration}");
        }

        private bool IsPublicOnlyLane(Entity laneEntity)
        {
            return laneEntity != Entity.Null &&
                m_CarLaneData.TryGetComponent(laneEntity, out CarLane laneData) &&
                (laneData.m_Flags & Game.Net.CarLaneFlags.PublicOnly) != 0;
        }

        private bool IsCommittedToImmediatePublicTransportEntry(Entity vehicle, Entity currentLaneEntity)
        {
            if (!m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes))
            {
                return false;
            }

            for (int index = 0; index < navigationLanes.Length; index += 1)
            {
                Entity nextLane = navigationLanes[index].m_Lane;
                if (nextLane == Entity.Null || nextLane == currentLaneEntity)
                {
                    continue;
                }

                if (IsPublicOnlyLane(nextLane))
                {
                    return true;
                }

                if (m_ConnectionLaneData.HasComponent(nextLane))
                {
                    continue;
                }

                if (m_EdgeLaneData.HasComponent(nextLane) &&
                    m_CarLaneData.HasComponent(nextLane))
                {
                    return false;
                }

                return false;
            }

            return false;
        }

        private void RemovePendingExitIfPresent(Entity vehicle)
        {
            if (m_PendingExitData.HasComponent(vehicle))
            {
                EntityManager.RemoveComponent<PublicTransportLanePendingExit>(vehicle);
            }
        }

        private void BuildPendingRefreshList()
        {
            ClearPendingRefresh();

            NativeArray<Entity> vehicles = m_AllCarsQuery.ToEntityArray(Allocator.Temp);
            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    m_PendingRefreshVehicles.Add(vehicles[index]);
                }
            }
            finally
            {
                vehicles.Dispose();
            }
        }

        private void ClearPendingRefresh()
        {
            if (m_PendingRefreshVehicles.IsCreated)
            {
                m_PendingRefreshVehicles.Clear();
            }

            m_RefreshCursor = 0;
        }

        private void ProcessRefreshBatch()
        {
            int end = System.Math.Min(
                m_RefreshCursor + kVehiclesPerFrame,
                m_PendingRefreshVehicles.Length);

            for (int index = m_RefreshCursor; index < end; index += 1)
            {
                Entity vehicle = m_PendingRefreshVehicles[index];
                if (!m_CarData.TryGetComponent(vehicle, out Car car))
                {
                    continue;
                }

                EvaluateVehicle(vehicle, car);
            }

            m_RefreshCursor = end;

            if (m_RefreshCursor >= m_PendingRefreshVehicles.Length)
            {
                ClearPendingRefresh();
            }
        }

        private void BeginSteadyStateEvaluation()
        {
            m_ProcessedThisFrame.Clear();
        }

        private void EvaluateQueryDeduplicated(EntityQuery query)
        {
            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }

            NativeArray<Entity> vehicles = query.ToEntityArray(Allocator.Temp);
            NativeArray<Car> cars = query.ToComponentDataArray<Car>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    Entity vehicle = vehicles[index];
                    if (!m_ProcessedThisFrame.Add(vehicle))
                    {
                        continue;
                    }

                    EvaluateVehicle(vehicle, cars[index]);
                }
            }
            finally
            {
                vehicles.Dispose();
                cars.Dispose();
            }
        }

        private void EvaluatePendingExitVehiclesDeduplicated()
        {
            if (m_PendingExitQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            NativeArray<Entity> vehicles = m_PendingExitQuery.ToEntityArray(Allocator.Temp);
            NativeArray<Car> cars = m_PendingExitQuery.ToComponentDataArray<Car>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    Entity vehicle = vehicles[index];
                    if (!m_ProcessedThisFrame.Add(vehicle))
                    {
                        continue;
                    }

                    EvaluateVehicle(vehicle, cars[index]);
                }
            }
            finally
            {
                vehicles.Dispose();
                cars.Dispose();
            }
        }

        private void RestoreVehiclesMissingProfileDeduplicated()
        {
            if (m_TrackedWithoutProfileQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            NativeArray<Entity> vehicles = m_TrackedWithoutProfileQuery.ToEntityArray(Allocator.Temp);
            NativeArray<Car> cars = m_TrackedWithoutProfileQuery.ToComponentDataArray<Car>(Allocator.Temp);
            NativeArray<PublicTransportLanePermissionState> states =
                m_TrackedWithoutProfileQuery.ToComponentDataArray<PublicTransportLanePermissionState>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    Entity vehicle = vehicles[index];
                    if (!m_ProcessedThisFrame.Add(vehicle))
                    {
                        continue;
                    }

                    RestoreVehicle(vehicle, cars[index], states[index], removeState: true);
                }
            }
            finally
            {
                vehicles.Dispose();
                cars.Dispose();
                states.Dispose();
            }
        }

        private void EvaluateVehicle(Entity vehicle, Car car)
        {
            bool hasState = m_PermissionStateData.TryGetComponent(
                vehicle,
                out PublicTransportLanePermissionState state);

            CarFlags originalMask = hasState
                ? state.m_OriginalPublicTransportLaneFlags
                : (car.m_Flags & PublicTransportLanePolicy.PublicTransportLanePermissionMask);

            byte immediateEntryGraceConsumed = hasState
                ? state.m_ImmediateEntryGraceConsumed
                : (byte)0;

            if (!m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile profile) ||
                profile.m_ShouldTrack == 0)
            {
                RemovePendingExitIfPresent(vehicle);

                if (hasState)
                {
                    RestoreVehicle(vehicle, car, state, removeState: true);
                }

                return;
            }

            PublicTransportLaneAccessBits accessBits = profile.m_PublicTransportLaneAccessBits;

            bool modAllowsAccess =
                PublicTransportLanePolicy.ModAllowsAccess(accessBits);

            bool modPrefersLanes =
                PublicTransportLanePolicy.ModPrefersLanes(accessBits);

            bool vanillaAllowsAccess =
                PublicTransportLanePolicy.VanillaAllowsAccess(accessBits);

            bool hasCurrentLane = m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLaneData);
            Entity currentLaneEntity = hasCurrentLane
                ? currentLaneData.m_Lane
                : Entity.Null;

            bool currentLaneIsPublicOnly = IsPublicOnlyLane(currentLaneEntity);
            bool currentLaneIsConnection =
                hasCurrentLane &&
                (currentLaneData.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0;

            bool currentLaneStillInExitCorridor =
                currentLaneIsPublicOnly || currentLaneIsConnection;

            bool hasPendingExit = m_PendingExitData.TryGetComponent(
                vehicle,
                out PublicTransportLanePendingExit pendingExit);

            bool permissionBeingRevoked = !modAllowsAccess;

            if (!permissionBeingRevoked)
            {
                immediateEntryGraceConsumed = 0;
            }

            bool currentlyHasPublicTransportLaneFlag =
                (car.m_Flags & CarFlags.UsePublicTransportLanes) != 0;

            bool immediatePublicTransportEntryPlanned =
                permissionBeingRevoked &&
                currentlyHasPublicTransportLaneFlag &&
                !currentLaneStillInExitCorridor &&
                IsCommittedToImmediatePublicTransportEntry(vehicle, currentLaneEntity);

            bool continueImmediateEntryGrace =
                hasPendingExit &&
                pendingExit.m_HasLeftPublicTransportLane == 0 &&
                immediatePublicTransportEntryPlanned;

            bool bootstrapImmediateEntryGrace =
                !hasPendingExit &&
                immediateEntryGraceConsumed == 0 &&
                immediatePublicTransportEntryPlanned;

            bool shouldGrantPendingExitGrace =
                permissionBeingRevoked &&
                currentlyHasPublicTransportLaneFlag &&
                (
                    currentLaneStillInExitCorridor ||
                    continueImmediateEntryGrace ||
                    bootstrapImmediateEntryGrace
                );

            if (shouldGrantPendingExitGrace)
            {
                if (!hasPendingExit)
                {
                    pendingExit = new PublicTransportLanePendingExit
                    {
                        m_LaneWhenGraceGranted = currentLaneEntity,
                        m_HasLeftPublicTransportLane = 0,
                    };

                    EntityManager.AddComponentData(vehicle, pendingExit);

                    MarkPathObsolete(
                        vehicle,
                        car,
                        "pt-pending-exit-grace-granted",
                        PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups),
                        $"currentLane={currentLaneEntity}, originalMask={originalMask}, engineUseFlag={(car.m_Flags & CarFlags.UsePublicTransportLanes) != 0}, modAllowsAccess={modAllowsAccess}");
                }

                if (bootstrapImmediateEntryGrace)
                {
                    immediateEntryGraceConsumed = 1;
                }
            }
            else if (hasPendingExit)
            {
                if (!permissionBeingRevoked)
                {
                    EntityManager.RemoveComponent<PublicTransportLanePendingExit>(vehicle);
                }
                else if (currentLaneStillInExitCorridor)
                {
                }
                else if (pendingExit.m_HasLeftPublicTransportLane == 0)
                {
                    pendingExit.m_HasLeftPublicTransportLane = 1;
                    EntityManager.SetComponentData(vehicle, pendingExit);

                    MarkPathObsolete(
                        vehicle,
                        car,
                        "pt-pending-exit-safe-lane-reached",
                        PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups),
                        $"currentLane={currentLaneEntity}, graceGrantedLane={pendingExit.m_LaneWhenGraceGranted}, originalMask={originalMask}, engineUseFlag={(car.m_Flags & CarFlags.UsePublicTransportLanes) != 0}, modAllowsAccess={modAllowsAccess}");
                }
                else
                {
                    EntityManager.RemoveComponent<PublicTransportLanePendingExit>(vehicle);
                }
            }

            bool emergencyActive = profile.m_EmergencyVehicle != 0;
            bool emergencyTransition =
                hasState && state.m_EmergencyActive != (emergencyActive ? (byte)1 : (byte)0);

            bool oldModAllowsAccess = hasState &&
                PublicTransportLanePolicy.ModAllowsAccess(state.m_PublicTransportLaneAccessBits);

            bool oldModPrefersLanes = hasState &&
                PublicTransportLanePolicy.ModPrefersLanes(state.m_PublicTransportLaneAccessBits);

            bool obsoleteRelevantFlagsChanged =
                hasState &&
                oldModAllowsAccess != modAllowsAccess;

            bool preferenceOnlyChange =
                hasState &&
                oldModAllowsAccess == modAllowsAccess &&
                oldModPrefersLanes != modPrefersLanes;

            PublicTransportLanePermissionState updatedState = new PublicTransportLanePermissionState
            {
                m_OriginalPublicTransportLaneFlags = originalMask,
                m_EmergencyActive = emergencyActive ? (byte)1 : (byte)0,
                m_ImmediateEntryGraceConsumed = immediateEntryGraceConsumed,
                m_PublicTransportLaneAccessBits = accessBits,
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
                    $"vanillaAllowsAccess={vanillaAllowsAccess}, modAllowsAccess={modAllowsAccess}, " +
                    $"modPrefersLanes={modPrefersLanes}, obsoleteRelevantFlagsChanged={obsoleteRelevantFlagsChanged}, " +
                    $"preferenceOnlyChange={preferenceOnlyChange}, emergencyTransition={emergencyTransition}, " +
                    $"engineUseFlag={(car.m_Flags & CarFlags.UsePublicTransportLanes) != 0}, " +
                    $"enginePreferFlag={(car.m_Flags & CarFlags.PreferPublicTransportLanes) != 0}";

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
            NativeArray<PublicTransportLanePermissionState> states =
                m_TrackedQuery.ToComponentDataArray<PublicTransportLanePermissionState>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
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

        private void RestoreVehicle(
            Entity vehicle,
            Car car,
            PublicTransportLanePermissionState state,
            bool removeState)
        {
            bool hadModPermissionChange =
                PublicTransportLanePolicy.PermissionChangedByMod(
                    state.m_PublicTransportLaneAccessBits);

            bool hadTrackedEmergency =
                state.m_EmergencyActive != 0;

            if (hadModPermissionChange || hadTrackedEmergency)
            {
                string role = PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups);
                string reason = hadModPermissionChange
                    ? "clear-mod-pt-policy-state"
                    : "restore-emergency-state";
                string extra =
                    $"hadModPermissionChange={hadModPermissionChange}, " +
                    $"hadTrackedEmergency={hadTrackedEmergency}";

                MarkPathObsolete(vehicle, car, reason, role, extra);
            }

            if (removeState && m_PermissionStateData.HasComponent(vehicle))
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

        private static bool StatesEqual(
            PublicTransportLanePermissionState left,
            PublicTransportLanePermissionState right)
        {
            return left.m_OriginalPublicTransportLaneFlags == right.m_OriginalPublicTransportLaneFlags &&
                left.m_EmergencyActive == right.m_EmergencyActive &&
                left.m_ImmediateEntryGraceConsumed == right.m_ImmediateEntryGraceConsumed &&
                left.m_PublicTransportLaneAccessBits == right.m_PublicTransportLaneAccessBits;
        }
    }
}