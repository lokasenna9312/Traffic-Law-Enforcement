using System.Collections.Generic;
using Game;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public readonly struct PendingFineMoneyCharge
    {
        public readonly Entity Vehicle;
        public readonly int Amount;
        public readonly string Kind;

        public PendingFineMoneyCharge(Entity vehicle, int amount, string kind)
        {
            Vehicle = vehicle;
            Amount = amount;
            Kind = kind;
        }
    }

    public static class EnforcementFineMoneyService
    {
        private static PendingFineMoneyCharge[] s_PendingCharges = new PendingFineMoneyCharge[32];
        private static readonly object s_SyncRoot = new object();
        private static int s_Head;
        private static int s_Count;

        public static void EnqueueCharge(Entity vehicle, int amount, string kind)
        {
            if (vehicle == Entity.Null || amount <= 0)
            {
                return;
            }

            lock (s_SyncRoot)
            {
                EnsureCapacity();
                int tail = (s_Head + s_Count) % s_PendingCharges.Length;
                s_PendingCharges[tail] = new PendingFineMoneyCharge(vehicle, amount, kind);
                s_Count += 1;
            }
        }

        public static bool TryDequeue(out PendingFineMoneyCharge charge)
        {
            lock (s_SyncRoot)
            {
                if (s_Count > 0)
                {
                    charge = s_PendingCharges[s_Head];
                    s_PendingCharges[s_Head] = default;
                    s_Head = (s_Head + 1) % s_PendingCharges.Length;
                    s_Count -= 1;
                    return true;
                }
            }

            charge = default;
            return false;
        }

        public static void ClearPendingCharges()
        {
            lock (s_SyncRoot)
            {
                s_PendingCharges = new PendingFineMoneyCharge[32];
                s_Head = 0;
                s_Count = 0;
            }
        }

        private static void EnsureCapacity()
        {
            if (s_Count < s_PendingCharges.Length)
            {
                return;
            }

            PendingFineMoneyCharge[] resized = new PendingFineMoneyCharge[s_PendingCharges.Length * 2];
            for (int index = 0; index < s_Count; index += 1)
            {
                resized[index] = s_PendingCharges[(s_Head + index) % s_PendingCharges.Length];
            }

            s_PendingCharges = resized;
            s_Head = 0;
        }
    }

    public static class EnforcementBudgetUIService
    {
        public readonly struct FineIncomeEvent
        {
            public readonly long TimestampMonthTicks;
            public readonly int Amount;
            public readonly string Kind;

            public FineIncomeEvent(long timestampMonthTicks, int amount, string kind)
            {
                TimestampMonthTicks = timestampMonthTicks;
                Amount = amount;
                Kind = kind;
            }
        }

        private static readonly List<FineIncomeEvent> s_RecentFineIncomeEvents = new List<FineIncomeEvent>();

        public static int CurrentFineIncome { get; private set; }
        public static int CurrentPublicTransportLaneFineIncome { get; private set; }
        public static int CurrentMidBlockCrossingFineIncome { get; private set; }
        public static int CurrentIntersectionMovementFineIncome { get; private set; }

        public static void RecordCollectedFine(long timestampMonthTicks, int amount, string kind)
        {
            if (timestampMonthTicks < 0L || amount <= 0 || string.IsNullOrWhiteSpace(kind))
            {
                return;
            }

            s_RecentFineIncomeEvents.Add(new FineIncomeEvent(timestampMonthTicks, amount, kind));
        }

        public static void UpdateCurrentFineIncome(long currentTimestampMonthTicks)
        {
            int rollingIncome = 0;
            int publicTransportLaneIncome = 0;
            int midBlockCrossingIncome = 0;
            int intersectionMovementIncome = 0;
            long cutoffTimestamp = currentTimestampMonthTicks - EnforcementGameTime.CurrentMonthTicksPerMonth;

            for (int index = s_RecentFineIncomeEvents.Count - 1; index >= 0; index -= 1)
            {
                FineIncomeEvent entry = s_RecentFineIncomeEvents[index];
                if (currentTimestampMonthTicks > 0L && entry.TimestampMonthTicks < cutoffTimestamp)
                {
                    s_RecentFineIncomeEvents.RemoveAt(index);
                    continue;
                }

                switch (entry.Kind)
                {
                    case EnforcementKinds.PublicTransportLane:
                        publicTransportLaneIncome += entry.Amount;
                        break;
                    case EnforcementKinds.MidBlockCrossing:
                        midBlockCrossingIncome += entry.Amount;
                        break;
                    case EnforcementKinds.IntersectionMovement:
                        intersectionMovementIncome += entry.Amount;
                        break;
                }
            }

            rollingIncome = publicTransportLaneIncome + midBlockCrossingIncome + intersectionMovementIncome;
            CurrentFineIncome = rollingIncome;
            CurrentPublicTransportLaneFineIncome = publicTransportLaneIncome;
            CurrentMidBlockCrossingFineIncome = midBlockCrossingIncome;
            CurrentIntersectionMovementFineIncome = intersectionMovementIncome;
        }

        public static void LoadPersistentData(IEnumerable<FineIncomeEvent> fineIncomeEvents)
        {
            ResetPersistentData();

            if (fineIncomeEvents == null)
            {
                return;
            }

            foreach (FineIncomeEvent entry in fineIncomeEvents)
            {
                if (entry.Amount > 0 && !string.IsNullOrWhiteSpace(entry.Kind))
                {
                    s_RecentFineIncomeEvents.Add(entry);
                }
            }
        }

        public static IReadOnlyCollection<FineIncomeEvent> GetFineIncomeEventSnapshot()
        {
            return s_RecentFineIncomeEvents.ToArray();
        }

        public static void ResetPersistentData()
        {
            s_RecentFineIncomeEvents.Clear();
            CurrentFineIncome = 0;
            CurrentPublicTransportLaneFineIncome = 0;
            CurrentMidBlockCrossingFineIncome = 0;
            CurrentIntersectionMovementFineIncome = 0;
        }
    }

    public partial class EnforcementFineMoneySystem : GameSystemBase
    {
        private const int kMaxOwnershipDepth = 16;

        private CitySystem m_CitySystem;
        private BufferLookup<Resources> m_Resources;
        private ComponentLookup<PlayerMoney> m_PlayerMoneyData;
        private ComponentLookup<Owner> m_OwnerData;
        private ComponentLookup<Controller> m_ControllerData;
        private ComponentLookup<PersonalCar> m_PersonalCarData;
        private ComponentLookup<Game.Citizens.HouseholdMember> m_HouseholdMemberData;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_Resources = GetBufferLookup<Resources>();
            m_PlayerMoneyData = GetComponentLookup<PlayerMoney>();
            m_OwnerData = GetComponentLookup<Owner>(true);
            m_ControllerData = GetComponentLookup<Controller>(true);
            m_PersonalCarData = GetComponentLookup<PersonalCar>(true);
            m_HouseholdMemberData = GetComponentLookup<Game.Citizens.HouseholdMember>(true);
        }

        protected override void OnDestroy()
        {
            EnforcementFineMoneyService.ClearPendingCharges();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            long currentTimestampMonthTicks = EnforcementGameTime.IsInitialized
                ? EnforcementGameTime.CurrentTimestampMonthTicks
                : 0L;

            if (!Mod.IsEnforcementEnabled)
            {
                EnforcementFineMoneyService.ClearPendingCharges();
                EnforcementBudgetUIService.UpdateCurrentFineIncome(currentTimestampMonthTicks);
                return;
            }

            Entity city = m_CitySystem.City;
            if (city == Entity.Null)
            {
                EnforcementFineMoneyService.ClearPendingCharges();
                EnforcementBudgetUIService.UpdateCurrentFineIncome(currentTimestampMonthTicks);
                return;
            }

            m_Resources.Update(this);
            m_PlayerMoneyData.Update(this);
            m_OwnerData.Update(this);
            m_ControllerData.Update(this);
            m_PersonalCarData.Update(this);
            m_HouseholdMemberData.Update(this);

            int collectedFineIncome = 0;
            while (EnforcementFineMoneyService.TryDequeue(out PendingFineMoneyCharge charge))
            {
                int collectedAmount = ApplyCharge(city, charge);
                if (collectedAmount > 0)
                {
                    collectedFineIncome += collectedAmount;
                    if (currentTimestampMonthTicks > 0L)
                    {
                        EnforcementBudgetUIService.RecordCollectedFine(currentTimestampMonthTicks, collectedAmount, charge.Kind);
                    }
                }
            }

            EnforcementBudgetUIService.UpdateCurrentFineIncome(currentTimestampMonthTicks);

            if (collectedFineIncome > 0 && EnforcementLoggingPolicy.ShouldLogEnforcementEvents())
            {
                Mod.log.Info($"Traffic law enforcement fine income recorded. batch={collectedFineIncome}, rolling1m={EnforcementBudgetUIService.CurrentFineIncome}, busLane1m={EnforcementBudgetUIService.CurrentPublicTransportLaneFineIncome}, midBlock1m={EnforcementBudgetUIService.CurrentMidBlockCrossingFineIncome}, intersection1m={EnforcementBudgetUIService.CurrentIntersectionMovementFineIncome}, monthTicks={currentTimestampMonthTicks}");
            }
        }

        private int ApplyCharge(Entity city, PendingFineMoneyCharge charge)
        {
            if (!m_PlayerMoneyData.TryGetComponent(city, out PlayerMoney playerMoney))
            {
                return 0;
            }

            if (!TryResolvePayer(charge.Vehicle, city, out Entity payer))
            {
                return 0;
            }

            if (!m_Resources.HasBuffer(payer))
            {
                return 0;
            }

            DynamicBuffer<Resources> payerResources = m_Resources[payer];
            EconomyUtils.AddResources(Resource.Money, -charge.Amount, payerResources);
            playerMoney.Add(charge.Amount);
            EntityManager.SetComponentData(city, playerMoney);
            return charge.Amount;
        }

        private bool TryResolvePayer(Entity vehicle, Entity city, out Entity payer)
        {
            payer = Entity.Null;

            Entity rootVehicle = ResolveControllerRoot(vehicle);
            if (TryResolvePersonalCarHousehold(rootVehicle, city, out payer))
            {
                return true;
            }

            return TryResolveOwnerChain(rootVehicle, city, out payer);
        }

        private Entity ResolveControllerRoot(Entity vehicle)
        {
            Entity current = vehicle;
            for (int depth = 0; depth < kMaxOwnershipDepth; depth += 1)
            {
                if (!m_ControllerData.TryGetComponent(current, out Controller controller) ||
                    controller.m_Controller == Entity.Null ||
                    controller.m_Controller == current)
                {
                    break;
                }

                current = controller.m_Controller;
            }

            return current;
        }

        private bool TryResolvePersonalCarHousehold(Entity vehicle, Entity city, out Entity payer)
        {
            payer = Entity.Null;

            if (!m_PersonalCarData.TryGetComponent(vehicle, out PersonalCar personalCar) || personalCar.m_Keeper == Entity.Null)
            {
                return false;
            }

            if (!m_HouseholdMemberData.TryGetComponent(personalCar.m_Keeper, out Game.Citizens.HouseholdMember householdMember))
            {
                return false;
            }

            if (householdMember.m_Household == Entity.Null || householdMember.m_Household == city || !m_Resources.HasBuffer(householdMember.m_Household))
            {
                return false;
            }

            payer = householdMember.m_Household;
            return true;
        }

        private bool TryResolveOwnerChain(Entity entity, Entity city, out Entity payer)
        {
            payer = Entity.Null;
            Entity current = entity;

            for (int depth = 0; depth < kMaxOwnershipDepth && current != Entity.Null; depth += 1)
            {
                if (m_HouseholdMemberData.TryGetComponent(current, out Game.Citizens.HouseholdMember householdMember))
                {
                    current = householdMember.m_Household;
                    continue;
                }

                if (current != city && m_Resources.HasBuffer(current))
                {
                    payer = current;
                    return true;
                }

                if (!m_OwnerData.TryGetComponent(current, out Owner owner) || owner.m_Owner == Entity.Null || owner.m_Owner == current)
                {
                    break;
                }

                current = owner.m_Owner;
            }

            return false;
        }
    }
}
