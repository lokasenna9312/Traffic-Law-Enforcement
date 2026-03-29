using System.Collections.Generic;

namespace Traffic_Law_Enforcement
{
    internal static class LocalizationKeys
    {
        public static Dictionary<string, string> Build(Setting setting)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();

            map["Settings.Title"] = setting.GetSettingsLocaleID();

            map["Tab.CurrentSave"] = setting.GetOptionTabLocaleID(Setting.kCurrentSaveTab);
            map["Tab.NewSaveDefaults"] = setting.GetOptionTabLocaleID(Setting.kNewSaveDefaultsTab);
            map["Tab.PolicyImpact"] = setting.GetOptionTabLocaleID(Setting.kPolicyImpactTab);
            map["Tab.Debug"] = setting.GetOptionTabLocaleID(Setting.kDebugTab);

            map["Group.General"] = setting.GetOptionGroupLocaleID(Setting.kGeneralGroup);
            map["Group.PublicTransportLaneAuthorized"] = setting.GetOptionGroupLocaleID(Setting.kPublicTransportLaneAuthorizedGroup);
            map["Group.PublicTransportLaneAdditional"] = setting.GetOptionGroupLocaleID(Setting.kPublicTransportLaneAdditionalGroup);
            map["Group.PublicTransportLanePressure"] = setting.GetOptionGroupLocaleID(Setting.kPublicTransportLanePressureGroup);
            map["Group.FineAmounts"] = setting.GetOptionGroupLocaleID(Setting.kFineGroup);
            map["Group.RepeatOffenders"] = setting.GetOptionGroupLocaleID(Setting.kRepeatOffenderGroup);
            map["Group.TemplateActions"] = setting.GetOptionGroupLocaleID(Setting.kTemplateActionsGroup);
            map["Group.PolicyImpact"] = setting.GetOptionGroupLocaleID(Setting.kPolicyImpactGroup);
            map["Group.DebugLogging"] = setting.GetOptionGroupLocaleID(Setting.kDebugLoggingGroup);
            map["Group.FocusedLogging"] = setting.GetOptionGroupLocaleID(Setting.kFocusedLoggingGroup);
            map["Group.DebugBindings"] = setting.GetOptionGroupLocaleID(Setting.kDebugBindingsGroup);
            map["Group.Chirper"] = setting.GetOptionGroupLocaleID(Setting.kChirperGroup);
            map["Group.LogPath"] = setting.GetOptionGroupLocaleID(Setting.kLogPathGroup);

            AddOption(map, setting, nameof(Setting.EnablePublicTransportLaneEnforcement));
            AddOption(map, setting, nameof(Setting.EnableMidBlockCrossingEnforcement));
            AddOption(map, setting, nameof(Setting.EnableIntersectionMovementEnforcement));

            AddOption(map, setting, nameof(Setting.AllowRoadPublicTransportVehicles));
            AddOption(map, setting, nameof(Setting.AllowTaxis));
            AddOption(map, setting, nameof(Setting.AllowPoliceCars));
            AddOption(map, setting, nameof(Setting.AllowFireEngines));
            AddOption(map, setting, nameof(Setting.AllowAmbulances));
            AddOption(map, setting, nameof(Setting.AllowGarbageTrucks));
            AddOption(map, setting, nameof(Setting.AllowPostVans));
            AddOption(map, setting, nameof(Setting.AllowRoadMaintenanceVehicles));
            AddOption(map, setting, nameof(Setting.AllowSnowplows));
            AddOption(map, setting, nameof(Setting.AllowVehicleMaintenanceVehicles));

            AddOption(map, setting, nameof(Setting.AllowPersonalCars));
            AddOption(map, setting, nameof(Setting.AllowDeliveryTrucks));
            AddOption(map, setting, nameof(Setting.AllowCargoTransportVehicles));
            AddOption(map, setting, nameof(Setting.AllowHearses));
            AddOption(map, setting, nameof(Setting.AllowPrisonerTransports));
            AddOption(map, setting, nameof(Setting.AllowParkMaintenanceVehicles));

            AddOption(map, setting, nameof(Setting.PublicTransportLaneExitPressureThresholdDays));
            AddOption(map, setting, nameof(Setting.PublicTransportLaneFineAmount));
            AddOption(map, setting, nameof(Setting.MidBlockCrossingFineAmount));
            AddOption(map, setting, nameof(Setting.IntersectionMovementFineAmount));

            AddOption(map, setting, nameof(Setting.DefaultEnablePublicTransportLaneEnforcement));
            AddOption(map, setting, nameof(Setting.DefaultEnableMidBlockCrossingEnforcement));
            AddOption(map, setting, nameof(Setting.DefaultEnableIntersectionMovementEnforcement));

            AddOption(map, setting, nameof(Setting.DefaultAllowRoadPublicTransportVehicles));
            AddOption(map, setting, nameof(Setting.DefaultAllowTaxis));
            AddOption(map, setting, nameof(Setting.DefaultAllowPoliceCars));
            AddOption(map, setting, nameof(Setting.DefaultAllowFireEngines));
            AddOption(map, setting, nameof(Setting.DefaultAllowAmbulances));
            AddOption(map, setting, nameof(Setting.DefaultAllowGarbageTrucks));
            AddOption(map, setting, nameof(Setting.DefaultAllowPostVans));
            AddOption(map, setting, nameof(Setting.DefaultAllowRoadMaintenanceVehicles));
            AddOption(map, setting, nameof(Setting.DefaultAllowSnowplows));
            AddOption(map, setting, nameof(Setting.DefaultAllowVehicleMaintenanceVehicles));

            AddOption(map, setting, nameof(Setting.DefaultAllowPersonalCars));
            AddOption(map, setting, nameof(Setting.DefaultAllowDeliveryTrucks));
            AddOption(map, setting, nameof(Setting.DefaultAllowCargoTransportVehicles));
            AddOption(map, setting, nameof(Setting.DefaultAllowHearses));
            AddOption(map, setting, nameof(Setting.DefaultAllowPrisonerTransports));
            AddOption(map, setting, nameof(Setting.DefaultAllowParkMaintenanceVehicles));

            AddOption(map, setting, nameof(Setting.DefaultPublicTransportLaneExitPressureThresholdDays));
            AddOption(map, setting, nameof(Setting.DefaultPublicTransportLaneFineAmount));
            AddOption(map, setting, nameof(Setting.DefaultMidBlockCrossingFineAmount));
            AddOption(map, setting, nameof(Setting.DefaultIntersectionMovementFineAmount));

            AddOption(map, setting, nameof(Setting.EnablePublicTransportLaneRepeatPenalty));
            AddOption(map, setting, nameof(Setting.PublicTransportLaneRepeatWindowMonths));
            AddOption(map, setting, nameof(Setting.PublicTransportLaneRepeatThreshold));
            AddOption(map, setting, nameof(Setting.PublicTransportLaneRepeatMultiplierPercent));

            AddOption(map, setting, nameof(Setting.EnableMidBlockCrossingRepeatPenalty));
            AddOption(map, setting, nameof(Setting.MidBlockCrossingRepeatWindowMonths));
            AddOption(map, setting, nameof(Setting.MidBlockCrossingRepeatThreshold));
            AddOption(map, setting, nameof(Setting.MidBlockCrossingRepeatMultiplierPercent));

            AddOption(map, setting, nameof(Setting.EnableIntersectionMovementRepeatPenalty));
            AddOption(map, setting, nameof(Setting.IntersectionMovementRepeatWindowMonths));
            AddOption(map, setting, nameof(Setting.IntersectionMovementRepeatThreshold));
            AddOption(map, setting, nameof(Setting.IntersectionMovementRepeatMultiplierPercent));

            AddOption(map, setting, nameof(Setting.DefaultEnablePublicTransportLaneRepeatPenalty));
            AddOption(map, setting, nameof(Setting.DefaultPublicTransportLaneRepeatWindowMonths));
            AddOption(map, setting, nameof(Setting.DefaultPublicTransportLaneRepeatThreshold));
            AddOption(map, setting, nameof(Setting.DefaultPublicTransportLaneRepeatMultiplierPercent));

            AddOption(map, setting, nameof(Setting.DefaultEnableMidBlockCrossingRepeatPenalty));
            AddOption(map, setting, nameof(Setting.DefaultMidBlockCrossingRepeatWindowMonths));
            AddOption(map, setting, nameof(Setting.DefaultMidBlockCrossingRepeatThreshold));
            AddOption(map, setting, nameof(Setting.DefaultMidBlockCrossingRepeatMultiplierPercent));

            AddOption(map, setting, nameof(Setting.DefaultEnableIntersectionMovementRepeatPenalty));
            AddOption(map, setting, nameof(Setting.DefaultIntersectionMovementRepeatWindowMonths));
            AddOption(map, setting, nameof(Setting.DefaultIntersectionMovementRepeatThreshold));
            AddOption(map, setting, nameof(Setting.DefaultIntersectionMovementRepeatMultiplierPercent));

            AddOption(map, setting, nameof(Setting.ResetCurrentSaveSettingsToCodeDefaults));
            AddOption(map, setting, nameof(Setting.CopyCurrentSaveSettingsToDefaults));
            AddOption(map, setting, nameof(Setting.ResetDefaultsToCodeDefaults));

            AddOption(map, setting, nameof(Setting.EnableEstimatedRerouteLogging));
            AddOption(map, setting, nameof(Setting.EnableEnforcementEventLogging));
            AddOption(map, setting, nameof(Setting.EnablePathfindingPenaltyDiagnosticLogging));
            AddOption(map, setting, nameof(Setting.EnableType2PublicTransportLaneUsageLogging));
            AddOption(map, setting, nameof(Setting.EnableType3PublicTransportLaneUsageLogging));
            AddOption(map, setting, nameof(Setting.EnableType4PublicTransportLaneUsageLogging));
            AddOption(map, setting, nameof(Setting.EnablePathObsoleteSourceLogging));
            AddOption(map, setting, nameof(Setting.EnableAllVehicleRouteSelectionChangeLogging));
            AddOption(map, setting, nameof(Setting.EnableFocusedRouteRebuildDiagnosticsLogging));
            AddOption(map, setting, nameof(Setting.EnableFocusedVehicleOnlyRouteLogging));
            AddOption(map, setting, nameof(Setting.EnablePolicyTrackedVehicleVanillaPathfindRulesExperiment));
            AddOption(map, setting, nameof(Setting.SelectedObjectPanelToggleBinding));
            AddOption(map, setting, nameof(Setting.FocusedLoggingPanelToggleBinding));
            AddOption(map, setting, nameof(Setting.ModLogPath));

            AddOption(map, setting, nameof(Setting.PolicyImpactTotalStatistics));
            AddOption(map, setting, nameof(Setting.PolicyImpactPublicTransportLaneStatistics));
            AddOption(map, setting, nameof(Setting.PolicyImpactMidBlockStatistics));
            AddOption(map, setting, nameof(Setting.PolicyImpactIntersectionStatistics));

            AddOption(map, setting, nameof(Setting.SendMonthlyChirperPreviewNow));

            map["BindingLabel.SelectedObjectPanelToggle"] =
                setting.GetBindingKeyLocaleID(KeybindingIds.SelectedObjectPanelToggleActionName);
            map["BindingHint.SelectedObjectPanelToggle"] =
                setting.GetBindingKeyHintLocaleID(KeybindingIds.SelectedObjectPanelToggleActionName);
            map["BindingLabel.FocusedLoggingPanelToggle"] =
                setting.GetBindingKeyLocaleID(KeybindingIds.FocusedLoggingPanelToggleActionName);
            map["BindingHint.FocusedLoggingPanelToggle"] =
                setting.GetBindingKeyHintLocaleID(KeybindingIds.FocusedLoggingPanelToggleActionName);
            map["SelectedObjectPanel.HeaderText"] = SelectedObjectPanelUISystem.kHeaderTextLocaleId;
            map["SelectedObjectPanel.SummaryTitle"] = SelectedObjectPanelUISystem.kSummaryTitleLocaleId;
            map["SelectedObjectPanel.ClassificationLabel"] = SelectedObjectPanelUISystem.kClassificationLabelLocaleId;
            map["SelectedObjectPanel.TleStatusLabel"] = SelectedObjectPanelUISystem.kTleStatusLabelLocaleId;
            map["SelectedObjectPanel.RoleLabel"] = SelectedObjectPanelUISystem.kRoleLabelLocaleId;
            map["SelectedObjectPanel.ActiveFlagsLabel"] = SelectedObjectPanelUISystem.kActiveFlagsLabelLocaleId;
            map["SelectedObjectPanel.ViolationsFinesLabel"] = SelectedObjectPanelUISystem.kViolationsFinesLabelLocaleId;
            map["SelectedObjectPanel.LastReasonLabel"] = SelectedObjectPanelUISystem.kLastReasonLabelLocaleId;
            map["SelectedObjectPanel.RepeatPenaltyLabel"] = SelectedObjectPanelUISystem.kRepeatPenaltyLabelLocaleId;
            map["SelectedObjectPanel.PublicTransportLanePolicyLabel"] = SelectedObjectPanelUISystem.kPublicTransportLanePolicyLabelLocaleId;
            map["SelectedObjectPanel.EntitySelectionLabel"] = SelectedObjectPanelUISystem.kEntitySelectionLabelLocaleId;
            map["SelectedObjectPanel.EntitySelectionPlaceholder"] = SelectedObjectPanelUISystem.kEntitySelectionPlaceholderLocaleId;
            map["SelectedObjectPanel.EntitySelectionSubmit"] = SelectedObjectPanelUISystem.kEntitySelectionSubmitLocaleId;
            map["SelectedObjectPanel.EntitySelectionStatusInvalidFormat"] = SelectedObjectPanelUISystem.kEntitySelectionStatusInvalidFormatLocaleId;
            map["SelectedObjectPanel.EntitySelectionStatusEntityNotFoundFormat"] = SelectedObjectPanelUISystem.kEntitySelectionStatusEntityNotFoundFormatLocaleId;
            map["SelectedObjectPanel.EntitySelectionStatusSelectedFormat"] = SelectedObjectPanelUISystem.kEntitySelectionStatusSelectedFormatLocaleId;
            map["SelectedObjectPanel.EntitySelectionStatusUnavailable"] = SelectedObjectPanelUISystem.kEntitySelectionStatusUnavailableLocaleId;
            map["SelectedObjectPanel.PathActionLabel"] = SelectedObjectPanelUISystem.kPathActionLabelLocaleId;
            map["SelectedObjectPanel.PathObsoleteButton"] = SelectedObjectPanelUISystem.kPathObsoleteButtonLocaleId;
            map["SelectedObjectPanel.PathObsoleteStatusUnavailable"] = SelectedObjectPanelUISystem.kPathObsoleteStatusUnavailableLocaleId;
            map["SelectedObjectPanel.PathObsoleteStatusNoPathOwner"] = SelectedObjectPanelUISystem.kPathObsoleteStatusNoPathOwnerLocaleId;
            map["SelectedObjectPanel.PathObsoleteStatusPending"] = SelectedObjectPanelUISystem.kPathObsoleteStatusPendingLocaleId;
            map["SelectedObjectPanel.PathObsoleteStatusAlreadyObsolete"] = SelectedObjectPanelUISystem.kPathObsoleteStatusAlreadyObsoleteLocaleId;
            map["SelectedObjectPanel.PathObsoleteStatusMarkedFormat"] = SelectedObjectPanelUISystem.kPathObsoleteStatusMarkedFormatLocaleId;
            map["SelectedObjectPanel.ActiveFlagsFormat"] = SelectedObjectPanelUISystem.kActiveFlagsValueFormatLocaleId;
            map["SelectedObjectPanel.ActiveFlagsViolationName"] = SelectedObjectPanelUISystem.kActiveFlagsViolationNameLocaleId;
            map["SelectedObjectPanel.ActiveFlagsPendingName"] = SelectedObjectPanelUISystem.kActiveFlagsPendingNameLocaleId;
            map["SelectedObjectPanel.FlagOn"] = SelectedObjectPanelUISystem.kFlagOnLocaleId;
            map["SelectedObjectPanel.FlagOff"] = SelectedObjectPanelUISystem.kFlagOffLocaleId;
            map["SelectedObjectPanel.TotalsFormat"] = SelectedObjectPanelUISystem.kTotalsValueFormatLocaleId;
            map["SelectedObjectPanel.NoSelection"] = SelectedObjectPanelUISystem.kNoSelectionLocaleId;
            map["SelectedObjectPanel.NotVehicle"] = SelectedObjectPanelUISystem.kNotVehicleLocaleId;
            map["SelectedObjectPanel.NotApplicable"] = SelectedObjectPanelUISystem.kNotApplicableLocaleId;
            map["SelectedObjectPanel.NoLiveLane"] = SelectedObjectPanelUISystem.kNoLiveLaneLocaleId;
            map["SelectedObjectPanel.Tracking"] = SelectedObjectPanelUISystem.kTrackingLocaleId;
            map["SelectedObjectPanel.FooterHint"] = SelectedObjectPanelUISystem.kFooterHintLocaleId;
            map["SelectedObjectPanel.ExpandSection"] = SelectedObjectPanelUISystem.kExpandSectionLocaleId;
            map["SelectedObjectPanel.CollapseSection"] = SelectedObjectPanelUISystem.kCollapseSectionLocaleId;
            map["SelectedObjectPanel.LaneDetailsTitle"] = SelectedObjectPanelUISystem.kLaneDetailsTitleLocaleId;
            map["SelectedObjectPanel.CurrentLaneLabel"] = SelectedObjectPanelUISystem.kCurrentLaneLabelLocaleId;
            map["SelectedObjectPanel.PreviousLaneLabel"] = SelectedObjectPanelUISystem.kPreviousLaneLabelLocaleId;
            map["SelectedObjectPanel.LaneChangesLabel"] = SelectedObjectPanelUISystem.kLaneChangesLabelLocaleId;
            map["SelectedObjectPanel.LiveLaneStateLabel"] = SelectedObjectPanelUISystem.kLiveLaneStateLabelLocaleId;
            map["SelectedObjectPanel.RouteDiagnosticsTitle"] = SelectedObjectPanelUISystem.kRouteDiagnosticsTitleLocaleId;
            map["SelectedObjectPanel.CurrentTargetLabel"] = SelectedObjectPanelUISystem.kCurrentTargetLabelLocaleId;
            map["SelectedObjectPanel.CurrentRouteLabel"] = SelectedObjectPanelUISystem.kCurrentRouteLabelLocaleId;
            map["SelectedObjectPanel.TargetRoadLabel"] = SelectedObjectPanelUISystem.kTargetRoadLabelLocaleId;
            map["SelectedObjectPanel.RouteExplanationLabel"] = SelectedObjectPanelUISystem.kRouteExplanationLabelLocaleId;
            map["SelectedObjectPanel.ConnectedStopLabel"] = SelectedObjectPanelUISystem.kConnectedStopLabelLocaleId;
            map["SelectedObjectPanel.LiveLaneStateReady"] = SelectedObjectPanelUISystem.kLiveLaneStateReadyLocaleId;
            map["SelectedObjectPanel.LiveLaneStateNoLiveLane"] = SelectedObjectPanelUISystem.kLiveLaneStateNoLiveLaneLocaleId;
            map["SelectedObjectPanel.LiveLaneStateNotApplicable"] = SelectedObjectPanelUISystem.kLiveLaneStateNotApplicableLocaleId;
            map["SelectedObjectPanel.LiveLaneStateParkedRoadCar"] = SelectedObjectPanelUISystem.kLiveLaneStateParkedRoadCarLocaleId;
            map["SelectedObjectPanel.LiveLaneStateNoPathOwner"] = SelectedObjectPanelUISystem.kLiveLaneStateNoPathOwnerLocaleId;
            map["SelectedObjectPanel.LiveLaneStateNoCurrentRoute"] = SelectedObjectPanelUISystem.kLiveLaneStateNoCurrentRouteLocaleId;
            map["SelectedObjectPanel.LiveLaneStateNoCurrentTarget"] = SelectedObjectPanelUISystem.kLiveLaneStateNoCurrentTargetLocaleId;
            map["SelectedObjectPanel.LiveLaneStatePathPending"] = SelectedObjectPanelUISystem.kLiveLaneStatePathPendingLocaleId;
            map["SelectedObjectPanel.LiveLaneStatePathScheduled"] = SelectedObjectPanelUISystem.kLiveLaneStatePathScheduledLocaleId;
            map["SelectedObjectPanel.LiveLaneStatePathObsolete"] = SelectedObjectPanelUISystem.kLiveLaneStatePathObsoleteLocaleId;
            map["SelectedObjectPanel.LiveLaneStatePathFailed"] = SelectedObjectPanelUISystem.kLiveLaneStatePathFailedLocaleId;
            map["SelectedObjectPanel.LiveLaneStatePathStuck"] = SelectedObjectPanelUISystem.kLiveLaneStatePathStuckLocaleId;
            map["SelectedObjectPanel.LiveLaneStatePathUpdated"] = SelectedObjectPanelUISystem.kLiveLaneStatePathUpdatedLocaleId;
            map["SelectedObjectPanel.None"] = SelectedObjectPanelUISystem.kNoneLocaleId;
            map["SelectedObjectPanel.Classification.RoadCar"] = SelectedObjectBridgeSystem.kClassificationRoadCarLocaleId;
            map["SelectedObjectPanel.Classification.ParkedRoadCar"] = SelectedObjectBridgeSystem.kClassificationParkedRoadCarLocaleId;
            map["SelectedObjectPanel.Classification.RailVehicle"] = SelectedObjectBridgeSystem.kClassificationRailVehicleLocaleId;
            map["SelectedObjectPanel.Classification.ParkedRailVehicle"] = SelectedObjectBridgeSystem.kClassificationParkedRailVehicleLocaleId;
            map["SelectedObjectPanel.Classification.Tram"] = SelectedObjectBridgeSystem.kClassificationTramLocaleId;
            map["SelectedObjectPanel.Classification.ParkedTram"] = SelectedObjectBridgeSystem.kClassificationParkedTramLocaleId;
            map["SelectedObjectPanel.Classification.Train"] = SelectedObjectBridgeSystem.kClassificationTrainLocaleId;
            map["SelectedObjectPanel.Classification.ParkedTrain"] = SelectedObjectBridgeSystem.kClassificationParkedTrainLocaleId;
            map["SelectedObjectPanel.Classification.Subway"] = SelectedObjectBridgeSystem.kClassificationSubwayLocaleId;
            map["SelectedObjectPanel.Classification.ParkedSubway"] = SelectedObjectBridgeSystem.kClassificationParkedSubwayLocaleId;
            map["SelectedObjectPanel.Classification.OtherVehicle"] = SelectedObjectBridgeSystem.kClassificationOtherVehicleLocaleId;
            map["SelectedObjectPanel.Role.RoadPublicTransportVehicle"] = SelectedObjectBridgeSystem.kRoleRoadPublicTransportLocaleId;
            map["SelectedObjectPanel.Role.Taxi"] = SelectedObjectBridgeSystem.kRoleTaxiLocaleId;
            map["SelectedObjectPanel.Role.PoliceCar"] = SelectedObjectBridgeSystem.kRolePoliceCarLocaleId;
            map["SelectedObjectPanel.Role.FireEngine"] = SelectedObjectBridgeSystem.kRoleFireEngineLocaleId;
            map["SelectedObjectPanel.Role.Ambulance"] = SelectedObjectBridgeSystem.kRoleAmbulanceLocaleId;
            map["SelectedObjectPanel.Role.GarbageTruck"] = SelectedObjectBridgeSystem.kRoleGarbageTruckLocaleId;
            map["SelectedObjectPanel.Role.PostVan"] = SelectedObjectBridgeSystem.kRolePostVanLocaleId;
            map["SelectedObjectPanel.Role.RoadMaintenanceVehicle"] = SelectedObjectBridgeSystem.kRoleRoadMaintenanceVehicleLocaleId;
            map["SelectedObjectPanel.Role.Snowplow"] = SelectedObjectBridgeSystem.kRoleSnowplowLocaleId;
            map["SelectedObjectPanel.Role.VehicleMaintenanceVehicle"] = SelectedObjectBridgeSystem.kRoleVehicleMaintenanceVehicleLocaleId;
            map["SelectedObjectPanel.Role.PersonalCar"] = SelectedObjectBridgeSystem.kRolePersonalCarLocaleId;
            map["SelectedObjectPanel.Role.DeliveryTruck"] = SelectedObjectBridgeSystem.kRoleDeliveryTruckLocaleId;
            map["SelectedObjectPanel.Role.CargoTransport"] = SelectedObjectBridgeSystem.kRoleCargoTransportLocaleId;
            map["SelectedObjectPanel.Role.Hearse"] = SelectedObjectBridgeSystem.kRoleHearseLocaleId;
            map["SelectedObjectPanel.Role.PrisonerTransport"] = SelectedObjectBridgeSystem.kRolePrisonerTransportLocaleId;
            map["SelectedObjectPanel.Role.ParkMaintenanceVehicle"] = SelectedObjectBridgeSystem.kRoleParkMaintenanceVehicleLocaleId;
            map["SelectedObjectPanel.Role.UnclassifiedRoadVehicle"] = SelectedObjectBridgeSystem.kRoleUnclassifiedRoadVehicleLocaleId;
            map["SelectedObjectPanel.Role.EmergencyQualifier"] = SelectedObjectBridgeSystem.kRoleEmergencyQualifierLocaleId;
            map["SelectedObjectPanel.Reason.NoneRecorded"] = SelectedObjectBridgeSystem.kReasonNoneRecordedLocaleId;
            map["SelectedObjectPanel.Reason.PublicTransportLaneRevokedByModFormat"] = SelectedObjectBridgeSystem.kReasonPublicTransportLaneRevokedByModFormatLocaleId;
            map["SelectedObjectPanel.Reason.PublicTransportLaneMissingVanillaCategoriesFormat"] = SelectedObjectBridgeSystem.kReasonPublicTransportLaneMissingVanillaCategoriesFormatLocaleId;
            map["SelectedObjectPanel.Reason.PublicTransportLaneMissingGrantedRoleFormat"] = SelectedObjectBridgeSystem.kReasonPublicTransportLaneMissingGrantedRoleFormatLocaleId;
            map["SelectedObjectPanel.Reason.PublicTransportLaneNotGrantedRoleFormat"] = SelectedObjectBridgeSystem.kReasonPublicTransportLaneNotGrantedRoleFormatLocaleId;
            map["SelectedObjectPanel.Reason.NoPublicTransportLanePermissionFlags"] = SelectedObjectBridgeSystem.kReasonNoPublicTransportLanePermissionFlagsLocaleId;
            map["SelectedObjectPanel.Reason.OppositeFlowSameSegment"] = SelectedObjectBridgeSystem.kReasonOppositeFlowSameSegmentLocaleId;
            map["SelectedObjectPanel.Reason.EnteredGarageAccessNoSideAccess"] = SelectedObjectBridgeSystem.kReasonEnteredGarageAccessNoSideAccessLocaleId;
            map["SelectedObjectPanel.Reason.EnteredParkingAccessNoSideAccess"] = SelectedObjectBridgeSystem.kReasonEnteredParkingAccessNoSideAccessLocaleId;
            map["SelectedObjectPanel.Reason.EnteredParkingConnectionNoSideAccess"] = SelectedObjectBridgeSystem.kReasonEnteredParkingConnectionNoSideAccessLocaleId;
            map["SelectedObjectPanel.Reason.EnteredBuildingAccessNoSideAccess"] = SelectedObjectBridgeSystem.kReasonEnteredBuildingAccessNoSideAccessLocaleId;
            map["SelectedObjectPanel.Reason.ExitedParkingAccessNoSideAccess"] = SelectedObjectBridgeSystem.kReasonExitedParkingAccessNoSideAccessLocaleId;
            map["SelectedObjectPanel.Reason.ExitedGarageAccessNoSideAccess"] = SelectedObjectBridgeSystem.kReasonExitedGarageAccessNoSideAccessLocaleId;
            map["SelectedObjectPanel.Reason.ExitedParkingConnectionNoSideAccess"] = SelectedObjectBridgeSystem.kReasonExitedParkingConnectionNoSideAccessLocaleId;
            map["SelectedObjectPanel.Reason.ExitedBuildingAccessNoSideAccess"] = SelectedObjectBridgeSystem.kReasonExitedBuildingAccessNoSideAccessLocaleId;
            map["SelectedObjectPanel.Reason.IntersectionMovementFormat"] = SelectedObjectBridgeSystem.kReasonIntersectionMovementFormatLocaleId;
            map["SelectedObjectPanel.Reason.RepeatPenaltyAppliedFormat"] = SelectedObjectBridgeSystem.kReasonRepeatPenaltyAppliedFormatLocaleId;
            map["SelectedObjectPanel.Reason.RepeatPenaltyApplied"] = SelectedObjectBridgeSystem.kReasonRepeatPenaltyAppliedLocaleId;
            map["SelectedObjectPanel.Reason.RepeatPenaltyNotApplied"] = SelectedObjectBridgeSystem.kReasonRepeatPenaltyNotAppliedLocaleId;
            map["SelectedObjectPanel.PublicTransportLanePolicyMeaningFormat"] =
                SelectedObjectBridgeSystem.kPublicTransportLanePolicyMeaningFormatLocaleId;
            map["SelectedObjectPanel.PublicTransportLanePolicyQualifierPublicTransport"] =
                SelectedObjectBridgeSystem.kPublicTransportLanePolicyQualifierPublicTransportLocaleId;
            map["SelectedObjectPanel.PublicTransportLanePolicyQualifierEmergency"] =
                SelectedObjectBridgeSystem.kPublicTransportLanePolicyQualifierEmergencyLocaleId;
            map["SelectedObjectPanel.PublicTransportLanePolicyVanillaAllow"] =
                SelectedObjectBridgeSystem.kPublicTransportLanePolicyVanillaAllowLocaleId;
            map["SelectedObjectPanel.PublicTransportLanePolicyVanillaDeny"] =
                SelectedObjectBridgeSystem.kPublicTransportLanePolicyVanillaDenyLocaleId;
            map["SelectedObjectPanel.PublicTransportLanePolicyTleAllow"] =
                SelectedObjectBridgeSystem.kPublicTransportLanePolicyTleAllowLocaleId;
            map["SelectedObjectPanel.PublicTransportLanePolicyTleDeny"] =
                SelectedObjectBridgeSystem.kPublicTransportLanePolicyTleDenyLocaleId;
            map["SelectedObjectPanel.RouteExplanation.NoCurrentRoute"] =
                SelectedObjectBridgeSystem.kRouteExplanationNoCurrentRouteLocaleId;
            map["SelectedObjectPanel.RouteExplanation.NoCurrentTarget"] =
                SelectedObjectBridgeSystem.kRouteExplanationNoCurrentTargetLocaleId;
            map["SelectedObjectPanel.RouteExplanation.WaypointAlignment"] =
                SelectedObjectBridgeSystem.kRouteExplanationWaypointAlignmentLocaleId;
            map["SelectedObjectPanel.RouteExplanation.PenaltyPrimaryFormat"] =
                SelectedObjectBridgeSystem.kRouteExplanationPenaltyPrimaryFormatLocaleId;
            map["SelectedObjectPanel.RouteExplanation.PenaltyModifierFormat"] =
                SelectedObjectBridgeSystem.kRouteExplanationPenaltyModifierFormatLocaleId;
            map["SelectedObjectPanel.RouteExplanation.PtPermissive"] =
                SelectedObjectBridgeSystem.kRouteExplanationPtPermissiveLocaleId;
            map["SelectedObjectPanel.RouteExplanation.GenericFallback"] =
                SelectedObjectBridgeSystem.kRouteExplanationGenericFallbackLocaleId;
            map["SelectedObjectPanel.RouteDirectConnect.AlreadyOnStart"] =
                SelectedObjectBridgeSystem.kRouteDirectConnectAlreadyOnStartLocaleId;
            map["SelectedObjectPanel.RouteDirectConnect.NextHop"] =
                SelectedObjectBridgeSystem.kRouteDirectConnectNextHopLocaleId;
            map["SelectedObjectPanel.RouteDirectConnect.ViaFormat"] =
                SelectedObjectBridgeSystem.kRouteDirectConnectViaFormatLocaleId;
            map["SelectedObjectPanel.RouteDirectConnect.NoPreview"] =
                SelectedObjectBridgeSystem.kRouteDirectConnectNoPreviewLocaleId;
            map["SelectedObjectPanel.RouteDirectConnect.MissingStart"] =
                SelectedObjectBridgeSystem.kRouteDirectConnectMissingStartLocaleId;
            map["SelectedObjectPanel.RouteFullPath.ContainsStart"] =
                SelectedObjectBridgeSystem.kRouteFullPathContainsStartLocaleId;
            map["SelectedObjectPanel.RouteFullPath.MissingStart"] =
                SelectedObjectBridgeSystem.kRouteFullPathMissingStartLocaleId;
            map["SelectedObjectPanel.RouteFullPath.Missing"] =
                SelectedObjectBridgeSystem.kRouteFullPathMissingLocaleId;
            map["FocusedLoggingPanel.HeaderText"] =
                FocusedLoggingPanelUISystem.kHeaderTextLocaleId;
            map["FocusedLoggingPanel.SelectedVehicleLabel"] =
                FocusedLoggingPanelUISystem.kSelectedVehicleLabelLocaleId;
            map["FocusedLoggingPanel.SelectedRoleLabel"] =
                FocusedLoggingPanelUISystem.kSelectedRoleLabelLocaleId;
            map["FocusedLoggingPanel.SelectedWatchStatusLabel"] =
                FocusedLoggingPanelUISystem.kSelectedWatchStatusLabelLocaleId;
            map["FocusedLoggingPanel.WatchedCountLabel"] =
                FocusedLoggingPanelUISystem.kWatchedCountLabelLocaleId;
            map["FocusedLoggingPanel.WatchedVehiclesLabel"] =
                FocusedLoggingPanelUISystem.kWatchedVehiclesLabelLocaleId;
            map["FocusedLoggingPanel.BurstLoggingLabel"] =
                FocusedLoggingPanelUISystem.kBurstLoggingLabelLocaleId;
            map["FocusedLoggingPanel.WatchSelected"] =
                FocusedLoggingPanelUISystem.kWatchSelectedButtonLocaleId;
            map["FocusedLoggingPanel.UnwatchSelected"] =
                FocusedLoggingPanelUISystem.kUnwatchSelectedButtonLocaleId;
            map["FocusedLoggingPanel.ClearWatched"] =
                FocusedLoggingPanelUISystem.kClearWatchedButtonLocaleId;
            map["FocusedLoggingPanel.ToggleBurstLogging"] =
                FocusedLoggingPanelUISystem.kToggleBurstLoggingButtonLocaleId;
            map["FocusedLoggingPanel.BurstLoggingActiveFormat"] =
                FocusedLoggingPanelUISystem.kBurstLoggingActiveFormatLocaleId;
            map["FocusedLoggingPanel.BurstLoggingInactive"] =
                FocusedLoggingPanelUISystem.kBurstLoggingInactiveLocaleId;
            map["FocusedLoggingPanel.WatchedCountFormat"] =
                FocusedLoggingPanelUISystem.kWatchedCountFormatLocaleId;
            map["FocusedLoggingPanel.WatchedStatus"] =
                FocusedLoggingPanelUISystem.kWatchedStatusLocaleId;
            map["FocusedLoggingPanel.NotWatchedStatus"] =
                FocusedLoggingPanelUISystem.kNotWatchedStatusLocaleId;
            map["FocusedLoggingPanel.NoEligibleSelection"] =
                FocusedLoggingPanelUISystem.kNoEligibleSelectionLocaleId;
            map["FocusedLoggingPanel.FooterHint"] =
                FocusedLoggingPanelUISystem.kFooterHintLocaleId;
            map["FocusedLoggingPanel.Warning"] =
                FocusedLoggingPanelUISystem.kWarningLocaleId;
            map["FocusedLoggingPanel.None"] =
                FocusedLoggingPanelUISystem.kNoneLocaleId;

            map["MonthlyChirper.SenderText"] = MonthlyEnforcementChirperSystem.kSenderTextLocaleId;
            map["MonthlyChirper.PeriodPointFormat"] = MonthlyEnforcementChirperSystem.kPeriodPointFormatLocaleId;
            map["MonthlyChirper.ReportHeaderFormat"] = MonthlyEnforcementChirperSystem.kReportHeaderFormatLocaleId;
            map["MonthlyChirper.TotalLineFormat"] = MonthlyEnforcementChirperSystem.kTotalLineFormatLocaleId;
            map["MonthlyChirper.PublicTransportLaneLineFormat"] = MonthlyEnforcementChirperSystem.kPublicTransportLaneLineFormatLocaleId;
            map["MonthlyChirper.MidBlockLineFormat"] = MonthlyEnforcementChirperSystem.kMidBlockLineFormatLocaleId;
            map["MonthlyChirper.IntersectionLineFormat"] = MonthlyEnforcementChirperSystem.kIntersectionLineFormatLocaleId;
            map["MonthlyChirper.NoRate"] = MonthlyEnforcementChirperSystem.kNoRateLocaleId;

            map["Budget.FineIncomeBudgetItem"] = BudgetUIPatches.FineIncomeBudgetItemLocaleId;
            map["Budget.FineIncomePublicTransportLane"] = BudgetUIPatches.FineIncomePublicTransportLaneLocaleId;
            map["Budget.FineIncomeMidBlockCrossing"] = BudgetUIPatches.FineIncomeMidBlockCrossingLocaleId;
            map["Budget.FineIncomeIntersectionMovement"] = BudgetUIPatches.FineIncomeIntersectionMovementLocaleId;
            map["Budget.FineIncomeBudgetDescription"] = BudgetUIPatches.FineIncomeBudgetDescriptionLocaleId;

            map["PolicyImpact.LoadedSaveOnly"] = EnforcementPolicyImpactService.kLoadedSaveOnlyLocaleId;
            map["PolicyImpact.WaitingForTime"] = EnforcementPolicyImpactService.kWaitingForTimeLocaleId;
            map["PolicyImpact.NoData"] = EnforcementPolicyImpactService.kNoDataLocaleId;
            map["PolicyImpact.Note"] = EnforcementPolicyImpactService.kNoteLocaleId;
            map["PolicyImpact.TotalLabel"] = EnforcementPolicyImpactService.kTotalLabelLocaleId;
            map["PolicyImpact.PublicTransportLaneLabel"] = EnforcementPolicyImpactService.kPublicTransportLaneLabelLocaleId;
            map["PolicyImpact.MidBlockLabel"] = EnforcementPolicyImpactService.kMidBlockLabelLocaleId;
            map["PolicyImpact.IntersectionLabel"] = EnforcementPolicyImpactService.kIntersectionLabelLocaleId;
            map["PolicyImpact.StatisticsLineFormat"] = EnforcementPolicyImpactService.kStatisticsLineFormat;

            return map;
        }

        private static void AddOption(Dictionary<string, string> map, Setting setting, string optionName)
        {
            map[$"OptionLabel.{optionName}"] = setting.GetOptionLabelLocaleID(optionName);
            map[$"OptionDesc.{optionName}"] = setting.GetOptionDescLocaleID(optionName);
        }
    }
}
