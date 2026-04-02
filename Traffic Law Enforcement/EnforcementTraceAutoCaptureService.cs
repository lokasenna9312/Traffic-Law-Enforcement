using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal static class EnforcementTraceAutoCaptureService
    {
        public const string PtLaneFamily = "ptLane";
        public const string MidBlockFamily = "midBlock";
        public const string IntersectionFamily = "intersection";

        private static readonly FamilyTraceState s_PtLaneState = new FamilyTraceState();
        private static readonly FamilyTraceState s_MidBlockState = new FamilyTraceState();
        private static readonly FamilyTraceState s_IntersectionState = new FamilyTraceState();
        private static int s_CurrentRuntimeWorldGeneration = int.MinValue;

        public static void Reset()
        {
            s_CurrentRuntimeWorldGeneration = int.MinValue;
            ResetState(s_PtLaneState, int.MinValue);
            ResetState(s_MidBlockState, int.MinValue);
            ResetState(s_IntersectionState, int.MinValue);
        }

        public static void NotifyRuntimeWorldGenerationChanged(int runtimeWorldGeneration)
        {
            EnsureCurrentGeneration(runtimeWorldGeneration);
        }

        public static void FlushAll()
        {
            FlushFamilySummary(PtLaneFamily, s_PtLaneState);
            FlushFamilySummary(MidBlockFamily, s_MidBlockState);
            FlushFamilySummary(IntersectionFamily, s_IntersectionState);
        }

        public static void RecordScan(string family)
        {
            FamilyTraceState state = GetFamilyState(family);
            state.Scanned += 1;
        }

        public static void RecordCandidate(string family, Entity vehicle)
        {
            FamilyTraceState state = GetFamilyState(family);
            state.Candidates += 1;
            if (state.HasLoggedFirstCandidate)
            {
                return;
            }

            state.HasLoggedFirstCandidate = true;
            Mod.log.Info(
                $"[ENFORCEMENT_TRACE] family={family} runtimeWorldGeneration={state.RuntimeWorldGeneration} " +
                $"phase=first-candidate vehicle={vehicle}");
        }

        public static void RecordIllegalCandidate(string family, Entity vehicle)
        {
            FamilyTraceState state = GetFamilyState(family);
            state.IllegalCandidates += 1;
            if (state.HasLoggedFirstIllegal)
            {
                return;
            }

            state.HasLoggedFirstIllegal = true;
            Mod.log.Info(
                $"[ENFORCEMENT_TRACE] family={family} runtimeWorldGeneration={state.RuntimeWorldGeneration} " +
                $"phase=first-illegal vehicle={vehicle}");
        }

        public static void RecordApplied(string family, Entity vehicle, int appliedPenalty)
        {
            FamilyTraceState state = GetFamilyState(family);
            state.Applied += 1;
            if (state.HasLoggedFirstApplied)
            {
                return;
            }

            state.HasLoggedFirstApplied = true;
            Mod.log.Info(
                $"[ENFORCEMENT_TRACE] family={family} runtimeWorldGeneration={state.RuntimeWorldGeneration} " +
                $"phase=first-applied vehicle={vehicle} appliedPenalty={appliedPenalty}");
        }

        private static FamilyTraceState GetFamilyState(string family)
        {
            EnsureCurrentGeneration(EnforcementSaveDataSystem.RuntimeWorldGeneration);

            switch (family)
            {
                case PtLaneFamily:
                    return s_PtLaneState;

                case MidBlockFamily:
                    return s_MidBlockState;

                case IntersectionFamily:
                    return s_IntersectionState;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(family), family, "Unknown enforcement trace family.");
            }
        }

        private static void EnsureCurrentGeneration(int runtimeWorldGeneration)
        {
            if (s_CurrentRuntimeWorldGeneration == runtimeWorldGeneration)
            {
                return;
            }

            if (s_CurrentRuntimeWorldGeneration != int.MinValue)
            {
                FlushAll();
            }

            s_CurrentRuntimeWorldGeneration = runtimeWorldGeneration;
            ResetState(s_PtLaneState, runtimeWorldGeneration);
            ResetState(s_MidBlockState, runtimeWorldGeneration);
            ResetState(s_IntersectionState, runtimeWorldGeneration);
        }

        private static void FlushFamilySummary(string family, FamilyTraceState state)
        {
            if (!HasMeaningfulActivity(state))
            {
                return;
            }

            Mod.log.Info(
                $"[ENFORCEMENT_TRACE] family={family} runtimeWorldGeneration={state.RuntimeWorldGeneration} " +
                $"phase=scan-summary scanned={state.Scanned} candidates={state.Candidates} " +
                $"illegalCandidates={state.IllegalCandidates} applied={state.Applied}");
        }

        private static bool HasMeaningfulActivity(FamilyTraceState state)
        {
            return state.RuntimeWorldGeneration != int.MinValue &&
                (state.Scanned > 0 ||
                 state.Candidates > 0 ||
                 state.IllegalCandidates > 0 ||
                 state.Applied > 0);
        }

        private static void ResetState(FamilyTraceState state, int runtimeWorldGeneration)
        {
            state.RuntimeWorldGeneration = runtimeWorldGeneration;
            state.Scanned = 0;
            state.Candidates = 0;
            state.IllegalCandidates = 0;
            state.Applied = 0;
            state.HasLoggedFirstCandidate = false;
            state.HasLoggedFirstIllegal = false;
            state.HasLoggedFirstApplied = false;
        }

        private sealed class FamilyTraceState
        {
            public int RuntimeWorldGeneration = int.MinValue;
            public int Scanned;
            public int Candidates;
            public int IllegalCandidates;
            public int Applied;
            public bool HasLoggedFirstCandidate;
            public bool HasLoggedFirstIllegal;
            public bool HasLoggedFirstApplied;
        }
    }
}
