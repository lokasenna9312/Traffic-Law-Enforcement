# Center-Line Access Investigation Notes

This note summarizes the reverse-engineering and mod-side investigation done around vehicles crossing the center line to enter or leave parking, garages, and service-building access lanes.

It is written to be usable as either:

- a repository note for future implementation work, or
- a draft for a GitHub Discussion post.

## Scope

The investigation focused on this question:

> Are repeated center-line crossings near parking buildings and service buildings happening because the mod is failing to detect them, or because current route deterrence is too weak or too indirect?

## What Is Confirmed

### 1. Detection is not the main failure

The mod already detects actual illegal lane transitions after they happen.

Relevant mod code:

- [Traffic Law Enforcement/LaneTransitionViolationSystem.cs](../Traffic%20Law%20Enforcement/LaneTransitionViolationSystem.cs)
- [Traffic Law Enforcement/RoutePenaltyRerouteLoggingSystem.cs](../Traffic%20Law%20Enforcement/RoutePenaltyRerouteLoggingSystem.cs)

Confirmed behaviors:

- opposite-flow same-segment mid-block crossings are detected,
- parking-access ingress is detected,
- garage-access ingress is detected,
- building/service access connection ingress is detected,
- illegal egress from access origins back into lanes without side-access permission is detected.

This means the core problem is not primarily “the mod misses the violation”.

### 2. Existing pathfinding pressure is generic, not maneuver-specific

Relevant mod code:

- [Traffic Law Enforcement/PathfindingMoneyPenaltySystem.cs](../Traffic%20Law%20Enforcement/PathfindingMoneyPenaltySystem.cs)
- [Traffic Law Enforcement/VehicleUtilsPatches.cs](../Traffic%20Law%20Enforcement/VehicleUtilsPatches.cs)

Current pathfinding pressure is applied indirectly through shared cost axes such as lane-cross, unsafe turn, and u-turn related costs.

What is not currently present:

- no explicit dedicated pathfinding cost for `parking-access`,
- no explicit dedicated pathfinding cost for `garage-access`,
- no explicit dedicated pathfinding cost for `building/service access connection`,
- no explicit dedicated pathfinding cost for illegal egress from those accesses.

So even when the mod later fines the vehicle, the selected route may still have been acceptable to vanilla pathfinding because the violation category was never modeled directly at route-selection time.

### 3. Detailed subtype logging is now in place

The logging was refined so that different access cases are visible instead of being collapsed into a single generic reason.

Relevant mod code:

- [Traffic Law Enforcement/LaneTransitionViolationSystem.cs](../Traffic%20Law%20Enforcement/LaneTransitionViolationSystem.cs)
- [Traffic Law Enforcement/RoutePenaltyRerouteLoggingSystem.cs](../Traffic%20Law%20Enforcement/RoutePenaltyRerouteLoggingSystem.cs)

Examples of the current categories:

- `parking-access`
- `garage-access`
- `parking-connection`
- `building/service access connection`
- `illegal-egress:parking-origin`
- `illegal-egress:garage-origin`
- `illegal-egress:building-service-access-connection-origin`

In observed logs so far, `parking-access` has been the dominant subtype.

## Vanilla Reverse-Engineering Findings

### 4. `VehicleUtils.ValidateParkingSpace` is a real post-selection invalidation hook

Relevant vanilla file examined:

- `VehicleUtils.ValidateParkingSpace(...)` in the decompiled `VehicleUtils.cs`

Important observed behavior:

- it inspects `CarNavigationLane` planned navigation lanes,
- it can set `pathOwner.m_State |= PathFlags.Obsolete`,
- it validates real parking lanes,
- it validates garage lanes,
- it validates parking-flagged connection lanes,
- it uses the planned route rather than waiting for the actual illegal transition to happen.

This makes it a strong hook for invalidating bad planned routes before the vehicle completes the maneuver.

### 5. Planned: patch `ValidateParkingSpace` for illegal planned parking ingress

Planned mod code (design only; not yet implemented in the current `Traffic Law Enforcement` sources as of 2026‑03):

- (planned) [Traffic Law Enforcement/VehicleUtilsPatches.cs](../Traffic%20Law%20Enforcement/VehicleUtilsPatches.cs)

A future patch is intended to mark the route obsolete when all of the following are true:

- the planned route includes a future parking-target lane,
- the approach lane is a road edge/car lane,
- the approach lane lacks side-access permission,
- the target is one of:
  - a parking lane,
  - a garage lane,
  - a parking-flagged connection lane.

If implemented, this would allow the mod to invalidate some illegal planned center-line crossings before execution, instead of reacting only after an illegal transition occurs.

### 6. Generic service-building access does appear in planned navigation, but not as `ParkingSpace`

Relevant vanilla file examined:

- decompiled `CarNavigationSystem.cs`

Important observed behavior in `FillNavigationPaths`:

- generic `ConnectionLane` segments are added into `navigationLanes`,
- only `ConnectionLaneFlags.Parking` gets the `CarLaneFlags.ParkingSpace` tag,
- generic building/service access connections do not get that parking tag.

This matters because the current `ValidateParkingSpace`-based patch discovers its target transition by scanning for the first planned lane marked `ParkingSpace`.

Result:

- parking/garage/parking-connection ingress is patchable with the current approach,
- generic building/service access ingress is not covered by the current patch,
- generic building/service access egress is also not covered by the current patch.

## What This Means Architecturally

### 7. Parking-style validation and generic service-access validation are not the same problem

`ValidateParkingSpace` is attractive because it is cheap and route-oriented, but its naming is not misleading by accident: it is centered on parking-like destinations.

It is likely useful for:

- parking spaces,
- garages,
- parking connections.

It is not yet proven to be the right hook for:

- generic building/service access connections,
- egress from service-building access back into a road lane.

### 8. A second independent avoidance system is still undesirable

One important design constraint from the implementation discussion was to avoid stacking two conceptually separate avoidance systems on top of each other.

In practice, that means the preferred approaches remain:

- explicit route-selection invalidation for the targeted maneuver class, or
- explicit maneuver-aware path cost,

and not:

- generic fines plus a second broad reroute trigger that duplicates the same policy at another layer.

## Best Current Hypothesis For Service-Building Access

### 9. The next likely hook is not another parking helper, but a car-navigation transition point

The most promising open direction is to find a point in vanilla car navigation where:

- a generic building/service access `ConnectionLane` has already entered `navigationLanes`,
- the source road lane is still known,
- the route can still be marked obsolete before the vehicle executes the access maneuver.

The strongest current candidate area is the vanilla car navigation loop in the decompiled `CarNavigationSystem.cs`, especially around:

- `FillNavigationPaths`,
- navigation-lane consumption and promotion into `currentLane`,
- places where invalid paths already force `PathFlags.Obsolete`.

### 10. Egress may require a separate rule from ingress

Illegal egress from service-building access appears to be a different problem from illegal ingress.

Ingress question:

- “Should this road lane be allowed to enter this access lane?”

Egress question:

- “Should this access-origin lane be allowed to merge back into this road lane?”

The mod already knows how to classify this after the fact in [Traffic Law Enforcement/LaneTransitionViolationSystem.cs](../Traffic%20Law%20Enforcement/LaneTransitionViolationSystem.cs), but a pre-execution hook for egress has not yet been confirmed.

## Current Status Summary

### Implemented

- actual violation detection is working,
- subtype logging is more precise,
- parking/garage/parking-connection planned illegal ingress can now be invalidated through the `ValidateParkingSpace` patch,
- the current build has already been locally deployed for testing.

### Not Yet Implemented

- planned invalidation for generic service-building ingress,
- planned invalidation for generic service-building egress,
- a true maneuver-specific pathfinding cost for service-building center-line crossings.

### Rejected Or Reverted During Investigation

- globally adding the mid-block penalty to `m_ParkingCost` was tested conceptually and then removed, because it was too broad and not specific to illegal center-line access.

## Suggested Next Steps

1. Continue reverse-engineering vanilla `CarNavigationSystem` for a pre-execution hook that sees generic service access transitions with both source and target lanes still available.
2. If that fails, investigate a lower-level pathfinding hook only if it can discriminate service access specifically enough to avoid broad side effects.
3. Keep parking/garage invalidation and generic service-building invalidation as separate implementation tracks unless a shared hook is proven to cover both cleanly.

## Discussion Draft Version

If this is reposted as a GitHub Discussion, the shortest accurate version is:

> We confirmed that center-line parking/service access issues are not mainly a detection failure. The mod already detects and logs those transitions correctly. The real gap is that current pathfinding deterrence is generic and does not model parking-access / garage-access / service-access maneuvers directly at route-selection time.
>
> We also confirmed that vanilla `VehicleUtils.ValidateParkingSpace` can invalidate planned routes by setting `PathFlags.Obsolete`, and the mod now uses that for illegal planned ingress into parking lanes, garage lanes, and parking connections. However, generic building/service access connections appear in navigation without being tagged as `ParkingSpace`, so they are not covered by the current patch.
>
> The next likely direction is to find a car-navigation hook that sees generic service access connections in planned navigation before they are executed, so the route can be invalidated without layering a second broad avoidance system on top of the existing policy.