namespace Traffic_Law_Enforcement
{
    public readonly struct EnforcementRecord
    {
        public readonly string Kind;
        public readonly int VehicleId;
        public readonly int LaneId;
        public readonly int FineAmount;
        public readonly string Reason;

        public string kind => Kind;
        public int vehicleId => VehicleId;
        public int laneId => LaneId;
        public int fineAmount => FineAmount;
        public string reason => Reason;

        public EnforcementRecord(string kind, int vehicleId, int laneId, int fineAmount, string reason)
        {
            Kind = kind;
            VehicleId = vehicleId;
            LaneId = laneId;
            FineAmount = fineAmount;
            Reason = reason;
        }

        public override string ToString()
        {
            return $"{Kind} | vehicle {VehicleId} | lane {LaneId} | fine {FineAmount} | {Reason}";
        }
    }
}