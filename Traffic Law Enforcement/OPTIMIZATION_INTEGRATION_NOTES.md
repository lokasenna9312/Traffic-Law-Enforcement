# Optimization integration notes (draft)

This patch adds the new helper/event/cache/profile files only. It does **not** fully patch the existing files in-place. I kept it this way because several existing-file edits depend on your local decompiled game DLL signatures, and I did not compile or validate the result from here.

## Existing files to update manually

### `Traffic Law Enforcement/Mod.cs`
Add/update the system order so the new systems run in this shape:

- `VehicleTrafficLawProfileSystem` before `PublicTransportLanePermissionSystem`
- `VehicleTrafficLawProfileSystem` before `CarNavigationSystem`
- `PublicTransportLaneReconcileSystem` after `PublicTransportLanePermissionSystem`
- `PublicTransportLaneReconcileSystem` before `PublicTransportLaneViolationSystem`
- `PublicTransportLaneViolationApplySystem` after `EnforcementGameTimeSystem`
- `PublicTransportLaneViolationSystem` after `PublicTransportLaneViolationApplySystem`
- `IntersectionMovementPenaltyCacheSystem` before `CarNavigationSystem`
- `LaneTransitionViolationApplySystem` after `LaneTransitionViolationSystem`

### `Traffic Law Enforcement/VehicleUtilsPatches.cs`
Replace path-request-time PT permission evaluation with `VehicleTrafficLawProfile` consumption.

What changes:
- remove `PathfindingMoneyPenaltySystem` / `PublicTransportLaneVehicleTypeLookups.Create(...)` from `SetupPathfindPrefix`
- read `VehicleTrafficLawProfile` from the owner vehicle
- set `RuleFlags.ForbidPrivateTraffic` based on `profile.m_DesiredPublicTransportLaneMask`

### `Traffic Law Enforcement/PublicTransportLanePermissionSystem.cs`
Refactor this system to consume `VehicleTrafficLawProfile` instead of recomputing `TryGetDesiredPermissionState(...)` per vehicle.

What changes:
- add `ComponentLookup<VehicleTrafficLawProfile>`
- use `profile.m_DesiredPublicTransportLaneMask`
- add batched full-refresh processing via `NativeList<Entity>`

### `Traffic Law Enforcement/PublicTransportLaneViolationSystem.cs`
Convert to a detector-only system.

What changes:
- create or query the singleton entity tagged with `PublicTransportLaneEventBufferTag`
- move fine/log/statistics work to `PublicTransportLaneViolationApplySystem`
- on detection, append `DetectedPublicTransportLaneEvent` entries

### `Traffic Law Enforcement/PublicTransportLaneViolationApplySystem.cs`
Create this new system from the draft discussed earlier.

What it should do:
- consume `DetectedPublicTransportLaneEvent`
- increment PT violation count
- call `EnforcementPenaltyService.RecordPublicTransportLaneViolation(...)`
- emit usage logs for type2/type3/type4
- update active violator count from `PublicTransportLaneViolation` query count

### `Traffic Law Enforcement/PublicTransportLaneReconcileSystem.cs`
Create this new system from the draft discussed earlier.

What it should do:
- reconcile profile-changed vehicles that did not change lane
- clean stale PT violation/type states from vehicles that no longer have `VehicleTrafficLawProfile`

### `Traffic Law Enforcement/LaneTransitionViolationSystem.cs`
Convert this to detector-only, and append `DetectedLaneTransitionViolation` to a shared event buffer.

### `Traffic Law Enforcement/LaneTransitionViolationApplySystem.cs`
Create this new system from the draft discussed earlier.

What it should do:
- consume `DetectedLaneTransitionViolation`
- apply fines/logs/statistics for mid-block and intersection movement

### `Traffic Law Enforcement/IntersectionMovementPathfindPatches.cs`
Refactor the postfix so the hot path only:
- extracts source/target lane
- reads `IntersectionMovementPenaltyCache.CurrentFine`
- calls `IntersectionMovementPenaltyCache.TryIsIllegal(...)`
- applies the money-weighted penalty

### `Traffic Law Enforcement/EnforcementPolicyImpactService.cs`
Replace:
- `s_PathRequestEvents`
- `s_ActualViolationEvents`
- `s_AvoidedRerouteEvents`

with:
- `MonthlyImpactBucket[]`

and refactor:
- `RecordPathRequest()`
- `RecordActualViolation(...)`
- `RecordAvoidedReroute(...)`
- `GetRollingWindowSnapshot()`

to bucket-based aggregation.

### `Traffic Law Enforcement/EnforcementTelemetry.cs`
Replace:
- `Dictionary<string, Dictionary<int, List<long>>> s_ViolationTimestamps`

with:
- `Dictionary<string, Dictionary<int, RepeatOffenderCounter>> s_ViolationCounters`

and refactor:
- `RegisterViolationTimestamp(...)`
- `GetRecentViolationCount(...)`

to month-counter logic.

### `Traffic Law Enforcement/EnforcementFineMoneySystem.cs`
Keep the payer-batching optimization only. Do **not** apply the partial-collection logic.

What changes:
- drain queue
- resolve payer once per vehicle with a cache
- aggregate totals in `Dictionary<Entity, int> payerTotals`
- apply one debit per payer
- keep the same semantic behavior as before (no insufficient-funds checks)

## Local checks you should do before compile

1. Verify `IntersectionMovementPolicy.TryGetIllegalIntersectionMovement(...)` signatures in your local decompiled DLLs.
2. Verify `Resources` buffer element field names only if you later add insufficient-funds logic.
3. Grep for stale references:
   - `s_PathRequestEvents`
   - `s_ActualViolationEvents`
   - `s_AvoidedRerouteEvents`
   - `s_ViolationTimestamps`
   - `TrimQueue`
   - `PathfindingMoneyPenaltySystem` in `VehicleUtilsPatches.cs`
   - `World.DefaultGameObjectInjectionWorld` in `IntersectionMovementPathfindPatches.cs`
