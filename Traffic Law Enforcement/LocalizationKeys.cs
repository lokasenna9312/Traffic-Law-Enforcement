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
            map["Group.Debug"] = setting.GetOptionGroupLocaleID(Setting.kDebugGroup);
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
            AddOption(map, setting, nameof(Setting.EnableSaveIdentificationLogging));
            AddOption(map, setting, nameof(Setting.ModLogPath));

            AddOption(map, setting, nameof(Setting.PolicyImpactTotalStatistics));
            AddOption(map, setting, nameof(Setting.PolicyImpactPublicTransportLaneStatistics));
            AddOption(map, setting, nameof(Setting.PolicyImpactMidBlockStatistics));
            AddOption(map, setting, nameof(Setting.PolicyImpactIntersectionStatistics));

            AddOption(map, setting, nameof(Setting.SendMonthlyChirperPreviewNow));

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