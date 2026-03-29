# Selected Object Route Diagnostics Consultation Brief

This note is not a direct implementation handoff.

It is a consultation brief meant to be given to ChatGPT 5.4 so it can review the baseline idea, challenge weak assumptions, and recommend a final design.

After reading ChatGPT 5.4's response, Codex will do the actual design and implementation work.

## Goal

Add a new foldout section to the existing Selected Object panel that explains why a selected live road vehicle is currently using, leaving, or avoiding a lane group that looks surprising to the player.

The motivating case is a bus leaving a center PT lane for an apparently unnecessary turn.

However, the interface itself is not bus-only.

The v1 surface is intended for every live road-driving vehicle that the current Selected Object panel already treats as a valid live traffic target.

The section must help the user distinguish between these causes:

- vanilla route target / stop alignment,
- vanilla lane-group alignment for the next route lane,
- mod-side `mid-block` deterrence,
- mod-side `intersection movement` deterrence,
- PT-lane policy effects when relevant,
- insufficient evidence.

## Locked Scope Decisions

These decisions are already made for the baseline proposal.

Do not reopen them unless there is a strong technical reason.

1. The feature should extend the existing Selected Object panel, not create a separate debug window.
2. The foldout should apply to all eligible live road vehicles, not only buses.
3. PT-specific information should appear only when it is actually relevant to the selected vehicle.
4. Parked vehicles, non-road vehicles, and selections without live lane data should not get a full route-diagnostics dump.
5. The new foldout should not add a separate `Path state` row if that state can be absorbed into the existing panel's current `Live lane state` row.
6. PT-specific explanatory text should be folded into the main explanation line instead of becoming a redundant standalone row.
7. Route inspection logic should be extracted into a shared current-route helper, but `RoutePenaltyRerouteLoggingSystem`'s cache should not be reused directly by the panel.
8. The helper and the logger should share classification rules, while keeping separate responsibilities:
   - panel = current-state explanation,
   - logger = delta / historical reroute logging.

Relevant implementation files:

- [Traffic Law Enforcement/SelectedObjectBridgeSystem.cs](../Traffic%20Law%20Enforcement/SelectedObjectBridgeSystem.cs)
- [Traffic Law Enforcement/SelectedObjectPanelUISystem.cs](../Traffic%20Law%20Enforcement/SelectedObjectPanelUISystem.cs)
- [Traffic Law Enforcement/SelectedObjectPanel.mjs](../Traffic%20Law%20Enforcement/SelectedObjectPanel.mjs)

## What I Want From ChatGPT 5.4

Please review the baseline proposal in this note and answer these questions.

1. Is extending the existing Selected Object panel with a new foldout the right v1 design, or is there a cleaner diagnostic surface inside the same panel model?
2. For a common `all live road vehicles` foldout, what is the minimum high-signal information that every vehicle should show?
3. What PT-specific extra rows are justified for road public transport vehicles, and which PT-only details should be deferred from v1?
4. Given that route-inspection logic should live in a shared helper and the logger cache should not be reused directly, what is the cleanest helper boundary and API shape?
5. Is the proposed snapshot-and-binding shape appropriate for this codebase, or should the data contract be simpler?
6. Is the proposed explanation heuristic sound, especially the priority order between:
   - waypoint / stop alignment,
   - vanilla lane-group alignment,
   - mod-side `mid-block`,
   - mod-side `intersection`,
   - PT-lane policy?
7. Should the existing `Live lane state` row simply be strengthened, or should it be renamed to something like `Live routing state` once it absorbs route-readiness details?
8. What should be included in v1, and what should be explicitly deferred?

Please do not write code.

Please respond with design guidance, tradeoffs, and a recommended final shape for the feature.

## Why The Current Panel Is Not Enough

The current panel already shows:

- selected vehicle classification,
- TLE status,
- role / type,
- PT-lane policy,
- current lane / previous lane / lane changes,
- live-lane readiness.

But it does not show the route-selection data needed to answer the actual question:

> Is this vehicle moving the way it does because of current vanilla target alignment, ordinary lane-group alignment, or a future TLE penalty on the planned route?

For buses, the missing question becomes more specific:

> Is the bus leaving the center PT lane because the next waypoint / stop resolves to a curb-side route lane, or because TLE path penalties made a future maneuver more expensive?

It also means the design should avoid duplicating information that already exists in the panel.

At this point, the baseline proposal is:

- do not add a new `Path state` row inside the route-diagnostics foldout,
- strengthen the existing `Live lane state` row so it can carry route-readiness meaning too,
- do not add a separate PT-only explanatory row if the same idea can live inside `Best current explanation`.

## Confirmed Technical Background

### 1. PT-lane policy is not the primary suspect for Type 1 road PT vehicles

For live road PT vehicles that are still allowed on PT lanes, the mod synchronizes `PathfindParameters.m_IgnoredRules` so `RuleFlags.ForbidPrivateTraffic` is ignored for them during path setup.

Relevant code:

- [Traffic Law Enforcement/VehicleUtilsPatches.cs](../Traffic%20Law%20Enforcement/VehicleUtilsPatches.cs)

Important lines:

- `SetupPathfindPrefix(...)` adjusts ignored rules.
- `CalculateCostPostfix(...)` only adds PT-lane money cost when `RuleFlags.ForbidPrivateTraffic` is still present.

So for a normal allowed bus, PT-lane policy is usually not the direct reason it leaves the lane.

### 2. Vanilla buses pathfind toward waypoint route lanes, not “straight ahead”

Vanilla transport path setup pushes `RouteLane.m_StartLane` into pathfinding targets for route waypoints.

Relevant vanilla files:

- [.tmp/GameDecompiled/Game/Simulation/TransportPathfindSetup.cs](../.tmp/GameDecompiled/Game/Simulation/TransportPathfindSetup.cs)
- [.tmp/GameDecompiled/Game/Routes/WaypointConnectionSystem.cs](../.tmp/GameDecompiled/Game/Routes/WaypointConnectionSystem.cs)
- [.tmp/GameDecompiled/Game/Simulation/TransportCarAISystem.cs](../.tmp/GameDecompiled/Game/Simulation/TransportCarAISystem.cs)

This means a bus can appear to have a perfectly reasonable straight PT-lane continuation, yet still choose a different lane group because the next waypoint / stop is bound to a different approach lane.

### 3. Actual lane choice is a second-stage vanilla decision

Vanilla lane selection is not only “which path was found”.

`CarLaneSelectIterator` still applies:

- lane-switch cost,
- lane reservation / blocker cost,
- forbidden-lane cost,
- preferred-lane cost.

Relevant vanilla files:

- [.tmp/GameDecompiled/Game/Vehicles/VehicleUtils.cs](../.tmp/GameDecompiled/Game/Vehicles/VehicleUtils.cs)
- [.tmp/GameDecompiled/Game/Simulation/CarLaneSelectIterator.cs](../.tmp/GameDecompiled/Game/Simulation/CarLaneSelectIterator.cs)

`PreferPublicTransportLanes` is only a weak preference, not a hard lock.

This matters for all road vehicles, not only buses.

### 4. TLE can still influence road vehicles through other penalty axes

The mod affects route choice through:

- PT-lane policy when applicable,
- mid-block penalty bias,
- intersection movement penalty,
- any future route invalidation caused by legality systems.

Relevant mod files:

- [Traffic Law Enforcement/MidBlockPathfindingBiasSystem.cs](../Traffic%20Law%20Enforcement/MidBlockPathfindingBiasSystem.cs)
- [Traffic Law Enforcement/IntersectionMovementPathfindingPenaltyPatches.cs](../Traffic%20Law%20Enforcement/IntersectionMovementPathfindingPenaltyPatches.cs)
- [Traffic Law Enforcement/RoutePenaltyRerouteLoggingSystem.cs](../Traffic%20Law%20Enforcement/RoutePenaltyRerouteLoggingSystem.cs)

Observed logs already show `Road public transport vehicles` rerouting to avoid `mid-block` penalties.

### 5. The current Selected Object panel already has an applicability gate

The panel already distinguishes between:

- no selection,
- not a vehicle,
- compact non-ready display,
- full ready display.

So the new route-diagnostics foldout should probably piggyback on the existing `ApplicableReady` path instead of inventing a completely separate eligibility model.

Relevant file:

- [Traffic Law Enforcement/SelectedObjectPanelUISystem.cs](../Traffic%20Law%20Enforcement/SelectedObjectPanelUISystem.cs)

## Current Baseline UX Proposal

Add a new foldout section below the existing `Lane details` section.

Suggested title:

- `Route diagnostics`

The foldout is common to all eligible live road vehicles.

The contents use progressive disclosure.

### Common Rows For Every Eligible Live Road Vehicle

- `Current target`
- `Current route`
- `Navigation lanes`
- `Planned penalties`
- `Penalty tags`
- `Best current explanation`

These rows should appear whenever the bridge can resolve meaningful route data for the selected live road vehicle.

### PT-Specific Supplemental Rows

Show these only when the selected vehicle is a road public transport vehicle or when the current target can resolve PT-relevant route information:

- `Waypoint route lane`
- `Connected stop`

Important nuance:

- the main summary area can keep the existing `PT lane policy` row,
- the new foldout should avoid duplicating the same PT policy text unless it is needed to explain the decision,
- PT-specific explanatory wording should normally be appended to `Best current explanation`, not shown as a standalone extra row.

### State Row Consolidation

The baseline proposal is to avoid creating a second state row inside the new foldout.

Instead:

- keep the existing panel-level state row,
- strengthen it so it can represent route-readiness as well as live-lane readiness,
- optionally rename it if ChatGPT 5.4 thinks the current label becomes misleading.

Examples of the intended richer state family:

- `Ready`
- `Ready, no current route`
- `Ready, target unresolved`
- `Ready, path obsolete`
- `No live lane`
- `Parked road car`
- `Not applicable`

### Display Rules

Show the section only when all of the following are true:

- the selection resolved to an eligible live road vehicle,
- the selected entity still exists,
- the selection is on the current full panel path,
- there is enough live lane / path context to produce useful output.

For parked vehicles or non-road vehicles:

- either hide the section entirely,
- or show a single compact “not available for this selection” row.

Within the foldout:

- prefer hiding irrelevant rows over rendering many `N/A` strings,
- default the foldout to collapsed.

## Current Baseline Data Contract

Keep the UI dumb.

Follow the current pattern of preformatted strings in the bridge layer instead of pushing raw ECS interpretation into `SelectedObjectPanel.mjs`.

Baseline shape:

- one top-level `HasRouteDiagnostics` or `ShowRouteDiagnostics` gate,
- one common set of preformatted route-diagnostics strings,
- one PT-specific availability flag,
- PT-only strings only when relevant.

Possible snapshot additions:

- `bool HasRouteDiagnostics`
- `bool HasPtRouteDiagnostics`
- `string CurrentTargetText`
- `string CurrentRouteText`
- `string NavigationLanePreviewText`
- `string PlannedPenaltyBreakdownText`
- `string PlannedPenaltyTagsText`
- `string RouteDecisionExplanationText`
- `string WaypointRouteLaneText`
- `string ConnectedStopText`

The baseline assumption is that route-readiness state should reuse or extend the current panel's existing state field rather than introducing a second `PathStateText` field.

I am intentionally not locking the exact field list yet.

One of the questions for ChatGPT 5.4 is whether this should be even simpler, for example:

- only preformatted visible rows,
- or a very small typed snapshot plus formatted strings.

## Current Baseline Implementation Architecture

### Preferred approach: compute current route diagnostics on demand

This is now a fixed baseline decision.

Do not reuse `RoutePenaltyRerouteLoggingSystem` snapshot cache directly.

Reasons:

- that system is delta-based and optimized for reroute logging,
- it is not a stable single-source-of-truth for arbitrary selected vehicles,
- the selected vehicle may not have a meaningful previous snapshot,
- the panel needs current explanatory state, not historical change detection.

### Preferred shared helper extraction

Create a new helper focused on current-route inspection, for example:

- `Traffic Law Enforcement/RoutePenaltyInspection.cs`

Move or mirror the route-penalty classification logic so it can be used both by:

- `RoutePenaltyRerouteLoggingSystem`,
- `SelectedObjectBridgeSystem`.

The intended separation of responsibility is:

- the helper owns current-route classification rules,
- the panel consumes the helper to explain the selected vehicle's current route,
- the logger consumes the helper to compare previous vs current snapshots and emit reroute logs.

At minimum, the helper should be able to:

- inspect `current lane + CarNavigationLane buffer`,
- resolve whether PT-lane segments are unauthorized,
- classify `mid-block` transitions using `MidBlockCrossingPolicy`,
- classify illegal intersection transitions using `IntersectionMovementPolicy`,
- describe lane kinds in the same category family used by the reroute logger,
- return:
  - comparable penalty profile,
  - human-readable breakdown,
  - human-readable tag summary.

This avoids future drift where the logger and the panel explain the same route differently.

## Current Baseline File-Level Plan

### 1. Extend SelectedObjectBridgeSystem

Update:

- [Traffic Law Enforcement/SelectedObjectBridgeSystem.cs](../Traffic%20Law%20Enforcement/SelectedObjectBridgeSystem.cs)

Add lookups for:

- `ComponentLookup<Target>`
- `ComponentLookup<CurrentRoute>`
- `ComponentLookup<PathOwner>`
- `ComponentLookup<RouteLane>`
- `ComponentLookup<Waypoint>`
- `ComponentLookup<Connected>`
- `BufferLookup<CarNavigationLane>`

Add route-diagnostics builders that produce:

- common route summary strings for any eligible live road vehicle,
- PT-specific target / route-lane / stop summary strings when relevant,
- planned penalty breakdown and tags,
- one-line explanation.

### 2. Add a shared route inspection helper

New file:

- `Traffic Law Enforcement/RoutePenaltyInspection.cs`

Responsibilities:

- inspect one vehicle’s current planned route,
- return a stable `RoutePenaltyProfile`-like summary,
- expose reusable formatting helpers.

Recommended extracted logic:

- unauthorized PT-lane segment detection,
- mid-block tag generation,
- intersection tag generation,
- lane-kind description.

### 3. Extend SelectedObjectPanelUISystem bindings

Update:

- [Traffic Law Enforcement/SelectedObjectPanelUISystem.cs](../Traffic%20Law%20Enforcement/SelectedObjectPanelUISystem.cs)

Add:

- new bindings for the route-diagnostics section,
- a new collapsed-state binding,
- localized labels and title strings,
- panel state fields that mirror the new snapshot data,
- optional row-visibility bindings if empty-string suppression becomes awkward.

Suggested binding names:

- `routeDiagnosticsCollapsed`
- `routeDiagnosticsTitleText`
- `currentTargetLabelText`
- `currentRouteLabelText`
- `navigationLanesLabelText`
- `plannedPenaltiesLabelText`
- `penaltyTagsLabelText`
- `routeDecisionExplanationLabelText`
- `waypointRouteLaneLabelText`
- `connectedStopLabelText`
- `currentTarget`
- `currentRoute`
- `navigationLanes`
- `plannedPenalties`
- `penaltyTags`
- `routeDecisionExplanation`
- `waypointRouteLane`
- `connectedStop`

If the existing state row is renamed, the expectation is to evolve the already-existing state binding rather than introduce a second parallel state field just for the foldout.

### 4. Extend the frontend panel

Update:

- [Traffic Law Enforcement/SelectedObjectPanel.mjs](../Traffic%20Law%20Enforcement/SelectedObjectPanel.mjs)

Add a second foldout section under the existing body.

Requirements:

- match the current panel styling,
- do not add a separate floating tool,
- keep rendering logic simple,
- reuse the existing `Row` and `FoldoutRow` pattern,
- support conditional PT-only rows without turning the frontend into a rules engine.

### 5. Add localization

Update:

- [Traffic Law Enforcement/Localization/en-US.properties](../Traffic%20Law%20Enforcement/Localization/en-US.properties)
- [Traffic Law Enforcement/Localization/ko-KR.properties](../Traffic%20Law%20Enforcement/Localization/ko-KR.properties)

Add labels and fallbacks for the new section only.

No large localization refactor is needed.

## Explanation Heuristic

The new `Best current explanation` line should be derived in this order.

### Rule 1. If a PT vehicle has a clear target-lane mismatch, say that first

If the selected vehicle has:

- a live `Target`,
- PT-relevant route-lane information,
- and the target route lane clearly points to a different approach lane or lane group,

then the explanation should start with something like:

- `Vehicle is aligning for the next waypoint / stop approach lane.`

This is the strongest PT-specific vanilla explanation.

### Rule 2. If planned penalties exist, mention them as a current modifier or primary suspect

If the route inspection finds current planned penalties:

- `mid-block(...)`
- `intersection(...)`
- `PT-lane(...)`

then say so clearly.

Examples:

- `Current planned route contains deterrence tags: mid-block(parking-access-ingress).`
- `Current planned route contains deterrence tags: intersection(illegal right; allowed forward).`

For non-PT vehicles, this may be the primary explanation branch.

For PT vehicles, this may be an additional modifier after the target-lane explanation.

### Rule 3. If the selected PT vehicle is currently allowed on PT lanes, say PT policy is not the direct blocker

If the selected vehicle is currently allowed on PT lanes, say so explicitly:

- `PT-lane policy is currently permissive for this vehicle.`

This prevents the user from blaming the wrong subsystem.

Skip this rule for vehicles where PT policy is irrelevant.

### Rule 4. If nothing stronger is found, fall back to vanilla lane-group alignment

Fallback text:

- `No route-target mismatch or current TLE penalty tag was identified; current behavior is most likely vanilla lane-group alignment.`

This should be presented as inference, not certainty.

## Minimal Navigation Preview Requirement

The panel does not need a full path dump.

A short preview is enough, for example:

- first 3 to 5 upcoming lane entities,
- optionally each lane kind:
  - `road`
  - `intersection-right`
  - `parking-connection`
  - `building-service-access-connection`

The goal is not raw ECS exhaustiveness.

The goal is to let a human confirm whether the vehicle is:

- still lined up for apparent straight-through travel,
- already committed to a different lane group,
- or carrying a future illegal maneuver tag.

## Desired Outcome For The Final Design

The implementation is successful if all of the following are true.

1. Selecting a live road taxi, service vehicle, or other non-PT road vehicle shows a useful common route-diagnostics foldout.
2. Selecting a live road bus shows the same common foldout plus PT-specific route-lane / stop information when available.
3. The foldout shows the current target and route entities without dumping raw ECS noise.
4. If the planned route contains mod-detectable penalties, the panel shows the same category family that the reroute logger would classify.
5. In a screenshot-like case where a bus leaves a center PT lane, the panel lets the user tell whether the likely cause is:
   - next stop / waypoint alignment,
   - mid-block penalty,
   - intersection penalty,
   - PT-lane policy,
   - or unresolved / best-effort vanilla lane alignment.
6. In a non-bus case, the same foldout still helps explain surprising route behavior without PT-only clutter.

## Manual Validation Scenarios

Use at least these two scene types.

### Scenario A. The original motivating PT case

- a road public transport vehicle,
- center-running PT lanes,
- a large intersection,
- a visible case where straight-through travel looks better to a human than the observed bus maneuver.

Then confirm:

- the selected object panel says the bus is PT-allowed when appropriate,
- the route-diagnostics section shows the next target,
- the waypoint route lane either points to a curb-side approach or does not,
- planned penalty tags either exist or do not,
- the final explanation matches the visible behavior closely enough to debug the case without external log parsing.

### Scenario B. A non-PT road vehicle

- a taxi, service vehicle, or freight-like road vehicle,
- a case where the route seems to avoid or enter a suspicious turn or access movement.

Then confirm:

- the same foldout appears,
- PT-only rows are absent unless actually relevant,
- planned penalty tags still explain `mid-block` or `intersection` pressure when present,
- the explanation still reads naturally for a non-PT vehicle.

## Out of Scope

Do not include these in the first implementation:

- a separate debug window,
- persistent per-vehicle route history,
- a full ECS inspector dump,
- route editing tools,
- automatic screenshots,
- performance-heavy whole-route visual overlays.

## Current Baseline Implementation Order

1. Add the shared route-inspection helper.
2. Extend `SelectedObjectBridgeSystem` snapshot production.
3. Add new UI bindings in `SelectedObjectPanelUISystem`.
4. Render the new foldout in `SelectedObjectPanel.mjs`.
5. Add `en-US` and `ko-KR` strings.
6. Validate first with the bus case, then with one non-PT road vehicle case.

## Prompt For ChatGPT 5.4

If this note is handed to ChatGPT 5.4, use a prompt like this:

> Read this consultation brief and critique the proposed design for adding `Route diagnostics` to the existing Selected Object panel. Do not write code. I want design guidance only. Scope is already fixed to `all eligible live road vehicles`, with PT-specific extra rows only when relevant. Also treat the following as already decided: route classification should move into a shared current-route helper, and `RoutePenaltyRerouteLoggingSystem`'s cache should not be reused directly by the panel. Please:
> 1. say whether the existing-panel foldout approach is the right v1,
> 2. recommend the minimum common diagnostics that every live road vehicle should show,
> 3. recommend which PT-specific extras belong in v1,
> 4. recommend the cleanest helper API and ownership boundary between helper, panel bridge, and reroute logger,
> 5. refine the snapshot / binding contract,
> 6. say whether the existing `Live lane state` row should absorb route-readiness state and whether it should be renamed,
> 7. refine the explanation heuristic for both generic road vehicles and PT vehicles,
> 8. identify risks, weak assumptions, and what should be deferred from v1.
>
> Assume Codex will read your answer and then do the actual design and implementation. Optimize your response for that handoff.
