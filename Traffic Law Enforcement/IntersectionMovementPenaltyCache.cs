using System.Collections.Generic;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public static class IntersectionMovementPenaltyCache
    {
        // 0 = unknown, 1 = illegal, 2 = legal
        private static readonly Dictionary<ulong, byte> s_LanePairLegalityCache =
            new Dictionary<ulong, byte>(4096);

        private static EntityManager s_EntityManager;
        private static int s_CachedIntersectionFine;
        private static int s_SettingsRevision;
        private static ulong s_WorldRevision;

        public static int CurrentFine => s_CachedIntersectionFine;

        public static void RefreshContext(
            EntityManager entityManager,
            int intersectionFine,
            int settingsRevision,
            ulong worldRevision)
        {
            bool mustClear =
                s_CachedIntersectionFine != intersectionFine ||
                s_SettingsRevision != settingsRevision ||
                s_WorldRevision != worldRevision ||
                !s_EntityManager.Equals(entityManager);

            if (mustClear)
            {
                Clear();
            }

            s_EntityManager = entityManager;
            s_CachedIntersectionFine = intersectionFine;
            s_SettingsRevision = settingsRevision;
            s_WorldRevision = worldRevision;
        }

        public static void Clear()
        {
            s_LanePairLegalityCache.Clear();
            s_CachedIntersectionFine = 0;
            s_SettingsRevision = 0;
            s_WorldRevision = 0;
            s_EntityManager = default;
        }

        public static bool TryIsIllegal(Entity sourceLane, Entity targetLane)
        {
            if (sourceLane == Entity.Null || targetLane == Entity.Null)
            {
                return false;
            }

            ulong key = MakeLanePairKey(sourceLane, targetLane);

            if (s_LanePairLegalityCache.TryGetValue(key, out byte cached))
            {
                return cached == 1;
            }

            if (!s_EntityManager.Exists(sourceLane) || !s_EntityManager.Exists(targetLane))
            {
                s_LanePairLegalityCache[key] = 2;
                return false;
            }

            bool illegal = IntersectionMovementPolicy.TryGetIllegalIntersectionMovement(
                s_EntityManager,
                sourceLane,
                targetLane,
                out _,
                out _);

            s_LanePairLegalityCache[key] = illegal ? (byte)1 : (byte)2;

            if (s_LanePairLegalityCache.Count > 65536)
            {
                s_LanePairLegalityCache.Clear();
            }

            return illegal;
        }

        private static ulong MakeLanePairKey(Entity sourceLane, Entity targetLane)
        {
            return ((ulong)(uint)sourceLane.Index << 32) | (uint)targetLane.Index;
        }
    }
}
