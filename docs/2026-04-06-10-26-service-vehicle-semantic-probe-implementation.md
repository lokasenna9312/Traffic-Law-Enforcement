# Service-Vehicle Semantic Probe Implementation

Repo: `Traffic-Law-Enforcement`  
Branch: `develop`  
Target game build: `Cities: Skylines II 1.5.6f1`  
Timestamp: `2026-04-06 10:26` (Asia/Seoul)

## A. Exact Files And Methods Changed

Modified files:

- `Traffic Law Enforcement/LaneTransitionViolationSystem.cs`
- `docs/2026-04-06-10-26-service-vehicle-semantic-probe-implementation.md`

Changed sections in `LaneTransitionViolationSystem.cs`:

- using directives
- component lookup field declarations
- `OnCreate()`
- enforcement-active lookup updates in `OnUpdate()`
- `MaybeLogRealizedIngressTrace(...)`
- `MaybeLogRealizedEgressTrace(...)`
- added:
  - `MaybeLogServiceVehicleSemanticLateSeam(...)`

Important separation note:

- the existing ordinary-car semantic helper was left intact
- the service probe is a separate helper
- only the existing late-seam branches were extended to call the new helper

## B. Copy-Paste-Ready Code Blocks

### 1. Added using

```csharp
using System.Text;
```

### 2. Added minimal common + subtype lookups

```csharp
private ComponentLookup<PathInformation> m_PathInformationData;
private BufferLookup<Game.Simulation.ServiceDispatch> m_ServiceDispatchData;
private ComponentLookup<GarbageTruck> m_GarbageTruckData;
private ComponentLookup<MaintenanceVehicle> m_MaintenanceVehicleData;
private ComponentLookup<PostVan> m_PostVanData;
```

### 3. Added lookup initialization in `OnCreate()`

```csharp
m_PathInformationData = GetComponentLookup<PathInformation>(true);
m_ServiceDispatchData = GetBufferLookup<Game.Simulation.ServiceDispatch>(true);
m_GarbageTruckData = GetComponentLookup<GarbageTruck>(true);
m_MaintenanceVehicleData = GetComponentLookup<MaintenanceVehicle>(true);
m_PostVanData = GetComponentLookup<PostVan>(true);
```

### 4. Added lookup updates in the enforcement-active branch of `OnUpdate()`

```csharp
m_PathInformationData.Update(this);
m_ServiceDispatchData.Update(this);
m_GarbageTruckData.Update(this);
m_MaintenanceVehicleData.Update(this);
m_PostVanData.Update(this);
```

### 5. Added service probe call in ingress late seam

```csharp
MaybeLogServiceVehicleSemanticLateSeam(vehicle, "IngressLate");
```

### 6. Added service probe call in egress late seam

```csharp
MaybeLogServiceVehicleSemanticLateSeam(vehicle, "EgressLate");
```

### 7. Added the service helper

```csharp
private void MaybeLogServiceVehicleSemanticLateSeam(
    Entity vehicle,
    string seamKind)
{
    bool isDeliveryTruck =
        m_DeliveryTruckData.TryGetComponent(vehicle, out DeliveryTruck deliveryTruck);
    bool isGarbageTruck =
        m_GarbageTruckData.TryGetComponent(vehicle, out GarbageTruck garbageTruck);
    bool isMaintenanceVehicle =
        m_MaintenanceVehicleData.TryGetComponent(vehicle, out MaintenanceVehicle maintenanceVehicle);
    bool isPostVan =
        m_PostVanData.TryGetComponent(vehicle, out PostVan postVan);

    if (!isDeliveryTruck &&
        !isGarbageTruck &&
        !isMaintenanceVehicle &&
        !isPostVan)
    {
        return;
    }

    Entity targetEntity = Entity.Null;
    if (m_TargetData.TryGetComponent(vehicle, out Target target))
    {
        targetEntity = target.m_Target;
    }

    Entity ownerEntity = Entity.Null;
    if (m_OwnerData.TryGetComponent(vehicle, out Owner owner))
    {
        ownerEntity = owner.m_Owner;
    }

    bool hasPathInformation =
        m_PathInformationData.TryGetComponent(vehicle, out PathInformation pathInformation);
    Entity pathOrigin =
        hasPathInformation ? pathInformation.m_Origin : Entity.Null;
    Entity pathDestination =
        hasPathInformation ? pathInformation.m_Destination : Entity.Null;

    bool hasServiceDispatch =
        m_ServiceDispatchData.HasBuffer(vehicle);
    int serviceDispatchCount =
        hasServiceDispatch ? m_ServiceDispatchData[vehicle].Length : 0;

    bool deliveryReturning =
        isDeliveryTruck &&
        (deliveryTruck.m_State & DeliveryTruckFlags.Returning) != 0;
    bool deliveryDelivering =
        isDeliveryTruck &&
        (deliveryTruck.m_State & DeliveryTruckFlags.Delivering) != 0;

    bool garbageReturning =
        isGarbageTruck &&
        (garbageTruck.m_State & GarbageTruckFlags.Returning) != 0;

    bool maintenanceReturning =
        isMaintenanceVehicle &&
        (maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) != 0;
    bool maintenanceTransformTarget =
        isMaintenanceVehicle &&
        (maintenanceVehicle.m_State & MaintenanceVehicleFlags.TransformTarget) != 0;
    bool maintenanceEdgeTarget =
        isMaintenanceVehicle &&
        (maintenanceVehicle.m_State & MaintenanceVehicleFlags.EdgeTarget) != 0;

    bool postReturning =
        isPostVan &&
        (postVan.m_State & PostVanFlags.Returning) != 0;
    bool postDelivering =
        isPostVan &&
        (postVan.m_State & PostVanFlags.Delivering) != 0;
    bool postCollecting =
        isPostVan &&
        (postVan.m_State & PostVanFlags.Collecting) != 0;

    bool returnLike =
        deliveryReturning ||
        garbageReturning ||
        maintenanceReturning ||
        postReturning;

    bool workLike =
        deliveryDelivering ||
        maintenanceTransformTarget ||
        maintenanceEdgeTarget ||
        postDelivering ||
        postCollecting;

    bool hasCommonServiceContext =
        targetEntity != Entity.Null ||
        ownerEntity != Entity.Null ||
        hasPathInformation ||
        hasServiceDispatch;

    string hypothesis =
        returnLike
            ? "ReturnLike"
            : workLike
                ? "WorkLike"
                : hasCommonServiceContext
                    ? "ServiceContextOnly"
                    : "Unresolved";

    StringBuilder message = new StringBuilder(512);
    message.Append("[NON_PARKING_SERVICE_ACCESS_LATE_SEMANTIC_PROBE] ");
    message.Append($"vehicle={FocusedLoggingService.FormatEntity(vehicle)} ");
    message.Append($"seamKind={seamKind} ");
    message.Append($"target={FocusedLoggingService.FormatEntity(targetEntity)} ");
    message.Append($"owner={FocusedLoggingService.FormatEntity(ownerEntity)} ");
    message.Append($"hasPathInformation={hasPathInformation} ");
    message.Append($"pathOrigin={FocusedLoggingService.FormatEntity(pathOrigin)} ");
    message.Append($"pathDestination={FocusedLoggingService.FormatEntity(pathDestination)} ");
    message.Append($"hasServiceDispatch={hasServiceDispatch} ");
    message.Append($"serviceDispatchCount={serviceDispatchCount}");

    if (isDeliveryTruck)
    {
        message.Append($" isDeliveryTruck=true");
        message.Append($" deliveryReturning={deliveryReturning}");
        message.Append($" deliveryDelivering={deliveryDelivering}");
    }

    if (isGarbageTruck)
    {
        message.Append($" isGarbageTruck=true");
        message.Append($" garbageReturning={garbageReturning}");
    }

    if (isMaintenanceVehicle)
    {
        message.Append($" isMaintenanceVehicle=true");
        message.Append($" maintenanceReturning={maintenanceReturning}");
        message.Append($" maintenanceTransformTarget={maintenanceTransformTarget}");
        message.Append($" maintenanceEdgeTarget={maintenanceEdgeTarget}");
    }

    if (isPostVan)
    {
        message.Append($" isPostVan=true");
        message.Append($" postReturning={postReturning}");
        message.Append($" postDelivering={postDelivering}");
        message.Append($" postCollecting={postCollecting}");
    }

    message.Append($" hypothesis={hypothesis}");

    EnforcementLoggingPolicy.RecordEnforcementEvent(message.ToString(), vehicle);
}
```

## C. Subtype Fields Intentionally Omitted For Compile-Safety

I intentionally omitted these optional or broader fields in this patch:

- `CargoTransport`
- `CargoTransportFlags`
- request-target graph traversal
  - for example request entity dereference beyond `ServiceDispatch` count
- subtype-specific request target fields such as:
  - goods-delivery request needer
  - garbage request target
  - maintenance request target
  - post request target
- delivery extra flags beyond:
  - `Returning`
  - `Delivering`
- garbage extra flags beyond:
  - `Returning`
- maintenance extra flags beyond:
  - `Returning`
  - `TransformTarget`
  - `EdgeTarget`
- post extra flags beyond:
  - `Returning`
  - `Delivering`
  - `Collecting`

Reason:

- the goal was a minimal compile-safe B1+B2 implementation
- common anchors plus the clearest subtype flags are already enough to answer:
  - does service context survive?
  - is there a subtype-local return hint?
  - is there a subtype-local work hint?
- omitting cargo/request traversal avoids turning this into a larger speculative patch

## D. Short Self-Review

### Why the patch is probe-only

- it only adds read-side lookups and one logging helper
- it does not change:
  - `MidBlockCrossingPolicy`
  - violation detection
  - buffering
  - carry/seed logic
  - apply logic
- it only runs when the existing watched late ingress/egress seam already fired

### Why the patch is compile-safe

- it uses only confirmed common anchors:
  - `Target`
  - `Owner`
  - `PathInformation`
  - `ServiceDispatch`
- it uses only clearly confirmed subtype components/flags:
  - `DeliveryTruckFlags.Returning`
  - `DeliveryTruckFlags.Delivering`
  - `GarbageTruckFlags.Returning`
  - `MaintenanceVehicleFlags.Returning`
  - `MaintenanceVehicleFlags.TransformTarget`
  - `MaintenanceVehicleFlags.EdgeTarget`
  - `PostVanFlags.Returning`
  - `PostVanFlags.Delivering`
  - `PostVanFlags.Collecting`
- build result:
  - `dotnet build "Traffic Law Enforcement/Traffic Law Enforcement.csproj" -c Debug`
  - warnings: `0`
  - errors: `0`

### How to read the resulting logs

- `ReturnLike`
  - subtype-specific `Returning` flag is active
  - late seam still preserved a clear return semantic

- `WorkLike`
  - no return flag is active
  - but subtype-local work-state hints are active, such as:
    - `deliveryDelivering`
    - `maintenanceTransformTarget`
    - `maintenanceEdgeTarget`
    - `postDelivering`
    - `postCollecting`

- `ServiceContextOnly`
  - family-wide anchors still exist:
    - target
    - owner
    - path info
    - dispatch buffer
  - but no strong subtype direction/work hint is active

- `Unresolved`
  - neither subtype hint nor meaningful common anchor is available at the late seam

## Commit

If the worktree remains clean enough, this patch should be committed after this report is added.
