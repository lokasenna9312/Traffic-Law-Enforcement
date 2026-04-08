# Ordinary-Car Semantic Probe Implementation

Repo: `Traffic-Law-Enforcement`  
Branch: `develop`  
Target game build: `Cities: Skylines II 1.5.6f1`  
Timestamp: `2026-04-06 09:59` (Asia/Seoul)

## A. Exact Files And Methods Changed

Modified files:

- `Traffic Law Enforcement/LaneTransitionViolationSystem.cs`
- `docs/2026-04-06-09-59-ordinary-car-semantic-probe-implementation.md`

Changed methods / sections in `LaneTransitionViolationSystem.cs`:

- using directives
- component lookup field declarations
- `OnCreate()`
- enforcement-active lookup updates in `OnUpdate()`
- `ProcessTransition(...)`
- `MaybeLogRealizedIngressTrace(...)`
- `MaybeLogRealizedEgressTrace(...)`
- added:
  - `MaybeLogOrdinaryCarSemanticLateSeam(...)`

## B. Copy-Paste-Ready Code Blocks

### 1. Added using

```csharp
using Game.Pathfind;
```

### 2. Added minimal lookup fields

```csharp
private ComponentLookup<PersonalCar> m_PersonalCarData;
private ComponentLookup<Target> m_TargetData;
private ComponentLookup<PathOwner> m_PathOwnerData;
private ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;
```

### 3. Added lookup initialization in `OnCreate()`

```csharp
m_PersonalCarData = GetComponentLookup<PersonalCar>(true);
m_TargetData = GetComponentLookup<Target>(true);
m_PathOwnerData = GetComponentLookup<PathOwner>(true);
m_SpawnLocationData = GetComponentLookup<Game.Objects.SpawnLocation>(true);
```

### 4. Added lookup updates in the enforcement-active branch of `OnUpdate()`

```csharp
m_PersonalCarData.Update(this);
m_TargetData.Update(this);
m_PathOwnerData.Update(this);
m_SpawnLocationData.Update(this);
```

### 5. Updated ingress helper signature

```csharp
private void MaybeLogRealizedIngressTrace(
    Entity vehicle,
    CarCurrentLane currentLane,
    VehicleLaneHistory history)
```

### 6. Updated `ProcessTransition(...)` call site

```csharp
MaybeLogRealizedIngressTrace(vehicle, currentLane, history);
```

### 7. Added ordinary-car semantic probe call in ingress late seam

```csharp
MaybeLogOrdinaryCarSemanticLateSeam(vehicle, currentLane, "IngressLate");
```

### 8. Added ordinary-car semantic probe call in egress late seam

```csharp
MaybeLogOrdinaryCarSemanticLateSeam(vehicle, currentLane, "EgressLate");
```

### 9. Added the probe helper

```csharp
private void MaybeLogOrdinaryCarSemanticLateSeam(
    Entity vehicle,
    CarCurrentLane currentLane,
    string seamKind)
{
    if (!m_PersonalCarData.TryGetComponent(vehicle, out PersonalCar personalCar))
    {
        return;
    }

    bool transporting =
        (personalCar.m_State & PersonalCarFlags.Transporting) != 0;
    bool boarding =
        (personalCar.m_State & PersonalCarFlags.Boarding) != 0;
    bool disembarking =
        (personalCar.m_State & PersonalCarFlags.Disembarking) != 0;
    bool homeTarget =
        (personalCar.m_State & PersonalCarFlags.HomeTarget) != 0;

    Entity targetEntity = Entity.Null;
    if (m_TargetData.TryGetComponent(vehicle, out Target target))
    {
        targetEntity = target.m_Target;
    }

    bool hasPathOwner =
        m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner);
    bool parkingSpaceReached =
        hasPathOwner && VehicleUtils.ParkingSpaceReached(currentLane, pathOwner);
    bool pathEndReached =
        VehicleUtils.PathEndReached(currentLane);
    bool hasSpawnLocation =
        currentLane.m_Lane != Entity.Null &&
        m_SpawnLocationData.HasComponent(currentLane.m_Lane);

    string hypothesis =
        pathEndReached && hasSpawnLocation && (transporting || disembarking) && !parkingSpaceReached
            ? "TransportDropoffLike"
            : parkingSpaceReached || boarding
                ? "ParkingAdjacent"
                : "Unresolved";

    string message =
        "[ORDINARY_CAR_ACCESS_LATE_SEMANTIC_PROBE] " +
        $"vehicle={FocusedLoggingService.FormatEntity(vehicle)} " +
        $"seamKind={seamKind} " +
        $"target={FocusedLoggingService.FormatEntity(targetEntity)} " +
        $"transporting={transporting} " +
        $"boarding={boarding} " +
        $"disembarking={disembarking} " +
        $"homeTarget={homeTarget} " +
        $"hasPathOwner={hasPathOwner} " +
        $"parkingSpaceReached={parkingSpaceReached} " +
        $"pathEndReached={pathEndReached} " +
        $"hasSpawnLocation={hasSpawnLocation} " +
        $"hypothesis={hypothesis}";

    EnforcementLoggingPolicy.RecordEnforcementEvent(message, vehicle);
}
```

## C. Fields Intentionally Omitted For Compile-Safety

I intentionally omitted the Priority 2 fields in this patch:

- `TravelPurpose`
- `HouseholdMember`
- `Household`
- `Worker`
- `PropertyRenter`
- home/work/target property comparisons

Reason:

- they were not required for the first-pass probe
- the patch goal was a minimal compile-safe implementation
- Priority 1 fields are already enough to separate:
  - `TransportDropoffLike`
  - `ParkingAdjacent`
  - `Unresolved`
- leaving the resident/home/work layer out avoids growing this patch into a larger speculative semantic probe

## D. Short Self-Review

### Why the patch is probe-only

- it only adds log lookups and one helper
- it does not touch:
  - `MidBlockCrossingPolicy`
  - violation detection results
  - carry/seed logic
  - buffering
  - apply logic
- it only emits:
  - `[ORDINARY_CAR_ACCESS_LATE_SEMANTIC_PROBE]`
  when the existing watched late ordinary-access seams already fire

### Why the patch is compile-safe

- only Priority 1 fields were added
- all added component types are already confirmed in repo or vanilla decompilation:
  - `PersonalCar`
  - `Target`
  - `PathOwner`
  - `Game.Objects.SpawnLocation`
- build result:
  - `dotnet build "Traffic Law Enforcement/Traffic Law Enforcement.csproj" -c Debug`
  - warnings: `0`
  - errors: `0`

### What the resulting logs mean

- `TransportDropoffLike`
  - `pathEndReached=true`
  - `hasSpawnLocation=true`
  - and `transporting=true` or `disembarking=true`
  - and `parkingSpaceReached=false`

- `ParkingAdjacent`
  - `parkingSpaceReached=true`
  - or `boarding=true`

- `Unresolved`
  - neither of the above patterns matched
  - the late seam still lacks enough semantic context for a narrow subtype call

## Commit

If the worktree remains clean enough, this patch should be committed after this report is added.
