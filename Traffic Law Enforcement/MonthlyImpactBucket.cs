using System;

namespace Traffic_Law_Enforcement
{
    [Serializable]
    public struct MonthlyImpactBucket
    {
        public long m_MonthIndex;
        public int m_PathRequestCount;
        public int m_ActualViolationCount;
        public int m_AvoidedPathCount;
        public int m_TotalFineAmount;

        public int m_PublicTransportLaneActualCount;
        public int m_MidBlockCrossingActualCount;
        public int m_IntersectionMovementActualCount;

        public int m_PublicTransportLaneFineAmount;
        public int m_MidBlockCrossingFineAmount;
        public int m_IntersectionMovementFineAmount;

        public int m_PublicTransportLaneAvoidedCount;
        public int m_MidBlockCrossingAvoidedCount;
        public int m_IntersectionMovementAvoidedCount;

        public void Reset(long monthIndex)
        {
            m_MonthIndex = monthIndex;
            m_PathRequestCount = 0;
            m_ActualViolationCount = 0;
            m_AvoidedPathCount = 0;
            m_TotalFineAmount = 0;
            m_PublicTransportLaneActualCount = 0;
            m_MidBlockCrossingActualCount = 0;
            m_IntersectionMovementActualCount = 0;
            m_PublicTransportLaneFineAmount = 0;
            m_MidBlockCrossingFineAmount = 0;
            m_IntersectionMovementFineAmount = 0;
            m_PublicTransportLaneAvoidedCount = 0;
            m_MidBlockCrossingAvoidedCount = 0;
            m_IntersectionMovementAvoidedCount = 0;
        }
    }
}
