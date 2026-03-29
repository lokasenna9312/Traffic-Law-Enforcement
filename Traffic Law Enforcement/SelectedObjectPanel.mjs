/*!
 * Cities: Skylines II UI Module
 *
 * Id: TrafficLawEnforcement.SelectedObjectPanel
 * Author: Pectus Solentis
 * Version: 0.0.0
 * Dependencies:
 */

const React = window.React;
const api = window["cs2/api"];
const ui = window["cs2/ui"];

const group = "selectedObjectPanel";
const h = React.createElement;

const visibleBinding = api.bindValue(group, "visible", false);
const compactBinding = api.bindValue(group, "compact", false);
const collapsedBinding = api.bindValue(group, "collapsed", false);
const classificationBinding = api.bindValue(group, "classification", "");
const classificationLabelTextBinding = api.bindValue(group, "classificationLabelText", "");
const messageBinding = api.bindValue(group, "message", "");
const tleStatusBinding = api.bindValue(group, "tleStatus", "");
const roleBinding = api.bindValue(group, "role", "");
const publicTransportLanePolicyBinding = api.bindValue(group, "publicTransportLanePolicy", "");
const vehicleIndexBinding = api.bindValue(group, "vehicleIndex", "");
const violationPendingBinding = api.bindValue(group, "violationPending", "");
const totalsBinding = api.bindValue(group, "totals", "");
const lastReasonBinding = api.bindValue(group, "lastReason", "");
const repeatPenaltyBinding = api.bindValue(group, "repeatPenalty", "");
const entitySelectionLabelTextBinding = api.bindValue(group, "entitySelectionLabelText", "");
const entitySelectionPlaceholderTextBinding = api.bindValue(group, "entitySelectionPlaceholderText", "");
const entitySelectionSubmitTextBinding = api.bindValue(group, "entitySelectionSubmitText", "");
const entitySelectionSuggestedValueBinding = api.bindValue(group, "entitySelectionSuggestedValue", "");
const entitySelectionStatusBinding = api.bindValue(group, "entitySelectionStatus", "");
const entitySelectionStatusIsErrorBinding = api.bindValue(group, "entitySelectionStatusIsError", false);
const pathActionLabelTextBinding = api.bindValue(group, "pathActionLabelText", "");
const pathObsoleteButtonTextBinding = api.bindValue(group, "pathObsoleteButtonText", "");
const pathObsoleteButtonVisibleBinding = api.bindValue(group, "pathObsoleteButtonVisible", false);
const pathObsoleteButtonEnabledBinding = api.bindValue(group, "pathObsoleteButtonEnabled", false);
const pathObsoleteStatusBinding = api.bindValue(group, "pathObsoleteStatus", "");
const pathObsoleteStatusIsErrorBinding = api.bindValue(group, "pathObsoleteStatusIsError", false);
const headerTextBinding = api.bindValue(group, "headerText", "");
const summaryTitleBinding = api.bindValue(group, "summaryTitle", "");
const tleStatusLabelTextBinding = api.bindValue(group, "tleStatusLabelText", "");
const roleLabelTextBinding = api.bindValue(group, "roleLabelText", "");
const activeFlagsLabelTextBinding = api.bindValue(group, "activeFlagsLabelText", "");
const violationsFinesLabelTextBinding = api.bindValue(group, "violationsFinesLabelText", "");
const lastReasonLabelTextBinding = api.bindValue(group, "lastReasonLabelText", "");
const repeatPenaltyLabelTextBinding = api.bindValue(group, "repeatPenaltyLabelText", "");
const publicTransportLanePolicyLabelTextBinding = api.bindValue(group, "publicTransportLanePolicyLabelText", "");
const footerTextBinding = api.bindValue(group, "footerText", "");
const expandSectionTooltipTextBinding = api.bindValue(group, "expandSectionTooltipText", "");
const collapseSectionTooltipTextBinding = api.bindValue(group, "collapseSectionTooltipText", "");
const laneDetailsTitleTextBinding = api.bindValue(group, "laneDetailsTitleText", "");
const currentLaneLabelTextBinding = api.bindValue(group, "currentLaneLabelText", "");
const previousLaneLabelTextBinding = api.bindValue(group, "previousLaneLabelText", "");
const laneChangesLabelTextBinding = api.bindValue(group, "laneChangesLabelText", "");
const liveLaneStateLabelTextBinding = api.bindValue(group, "liveLaneStateLabelText", "");
const routeDiagnosticsTitleTextBinding = api.bindValue(group, "routeDiagnosticsTitleText", "");
const currentTargetLabelTextBinding = api.bindValue(group, "currentTargetLabelText", "");
const currentRouteLabelTextBinding = api.bindValue(group, "currentRouteLabelText", "");
const targetRoadLabelTextBinding = api.bindValue(group, "targetRoadLabelText", "");
const routeExplanationLabelTextBinding = api.bindValue(group, "routeExplanationLabelText", "");
const connectedStopLabelTextBinding = api.bindValue(group, "connectedStopLabelText", "");
const currentLaneBinding = api.bindValue(group, "currentLane", "");
const previousLaneBinding = api.bindValue(group, "previousLane", "");
const laneChangesBinding = api.bindValue(group, "laneChanges", "");
const liveLaneStateBinding = api.bindValue(group, "liveLaneState", "");
const laneDetailsVisibleBinding = api.bindValue(group, "laneDetailsVisible", false);
const laneDetailsCollapsedBinding = api.bindValue(group, "laneDetailsCollapsed", true);
const routeDiagnosticsVisibleBinding = api.bindValue(group, "routeDiagnosticsVisible", false);
const routeDiagnosticsCollapsedBinding = api.bindValue(group, "routeDiagnosticsCollapsed", true);
const currentTargetBinding = api.bindValue(group, "currentTarget", "");
const currentTargetEntityBinding = api.bindValue(group, "currentTargetEntity", "");
const currentRouteBinding = api.bindValue(group, "currentRoute", "");
const currentRouteEntityTextBinding = api.bindValue(group, "currentRouteEntityText", "");
const currentRouteColorBinding = api.bindValue(group, "currentRouteColor", "");
const targetRoadBinding = api.bindValue(group, "targetRoad", "");
const routeExplanationBinding = api.bindValue(group, "routeExplanation", "");
const connectedStopBinding = api.bindValue(group, "connectedStop", "");

const initialPosition = { x: 0.76, y: 0.1 };
const ultraCompactPanelWidth = "240px";
const compactPanelWidth = "340px";
const fullPanelWidth = "560px";

const styles = {
    foldout: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        width: "100%",
        padding: "10px 18px",
        boxSizing: "border-box",
        background: "rgba(28, 40, 58, 0.94)",
        borderTop: "1px solid rgba(255, 255, 255, 0.06)",
        borderBottom: "1px solid rgba(255, 255, 255, 0.06)",
        cursor: "pointer",
        color: "#e6eefc",
        fontWeight: 700,
        fontSize: "14px",
    },
    foldoutIcon: {
        fontSize: "12px",
        color: "#b8c6da",
    },
    body: {
        padding: "18px",
        width: "100%",
        boxSizing: "border-box",
    },
    compactBody: {
        padding: "14px 16px",
        width: "100%",
        boxSizing: "border-box",
    },
    compactMessage: {
        fontSize: "14px",
        lineHeight: 1.35,
        fontWeight: 600,
        color: "#f7f9ff",
        marginBottom: "12px",
    },
    selectionBlock: {
        display: "flex",
        flexDirection: "column",
        gap: "8px",
        marginBottom: "2px",
    },
    selectionRow: {
        display: "flex",
        gap: "8px",
        alignItems: "center",
        minHeight: "30px",
    },
    selectionRowInputCell: {
        flex: 1,
        minWidth: 0,
    },
    selectionInput: {
        width: "100%",
        height: "34px",
        padding: "6px 10px",
        boxSizing: "border-box",
        borderRadius: "6px",
        border: "1px solid rgba(163, 187, 214, 0.32)",
        background: "rgba(14, 22, 34, 0.9)",
        color: "#ffffff",
        fontSize: "14px",
        outline: "none",
    },
    selectionButton: {
        height: "34px",
        padding: "0 12px",
        borderRadius: "6px",
        border: "1px solid rgba(89, 168, 255, 0.42)",
        background: "rgba(41, 103, 168, 0.9)",
        color: "#ffffff",
        fontSize: "13px",
        fontWeight: 700,
        cursor: "pointer",
        flexShrink: 0,
    },
    selectionButtonDisabled: {
        opacity: 0.45,
        cursor: "default",
    },
    selectionStatus: {
        minHeight: "16px",
        color: "#b8c6da",
        fontSize: "12px",
        lineHeight: 1.3,
        paddingLeft: "146px",
    },
    actionStatus: {
        minHeight: "16px",
        color: "#b8c6da",
        fontSize: "12px",
        lineHeight: 1.3,
        paddingLeft: "146px",
    },
    selectionStatusError: {
        color: "#ffb39f",
    },
    selectionStatusSuccess: {
        color: "#b8ebc0",
    },
    rows: {
        display: "flex",
        flexDirection: "column",
        gap: "0px",
    },
    row: {
        display: "flex",
        minHeight: "30px",
        alignItems: "center",
    },
    rowMultiline: {
        alignItems: "flex-start",
        paddingTop: "2px",
        paddingBottom: "2px",
    },
    classificationRow: {
        display: "flex",
        minHeight: "34px",
        alignItems: "center",
        marginBottom: "2px",
    },
    label: {
        width: "138px",
        color: "#c2cfdf",
        fontSize: "14px",
        lineHeight: 1.35,
        flexShrink: 0,
    },
    value: {
        flex: 1,
        color: "#ffffff",
        fontSize: "14px",
        lineHeight: 1.35,
        whiteSpace: "pre-line",
        wordBreak: "break-word",
    },
    valueMultiline: {
        display: "flex",
        flexDirection: "column",
        gap: "2px",
    },
    classificationLabel: {
        width: "138px",
        display: "flex",
        alignItems: "center",
        color: "#b0defc",
        fontWeight: 700,
        fontSize: "14px",
        lineHeight: 1.35,
        flexShrink: 0,
    },
    classificationValue: {
        flex: 1,
        display: "flex",
        alignItems: "center",
        gap: "8px",
        minHeight: "30px",
        minWidth: 0,
        color: "#ffffff",
        fontSize: "14px",
        lineHeight: 1.35,
        wordBreak: "break-word",
    },
    classificationPrimary: {
        color: "#d7ebfd",
        fontWeight: 700,
    },
    classificationSecondary: {
        color: "#c2cfdf",
        fontWeight: 600,
    },
    valueWithTail: {
        flex: 1,
        display: "flex",
        alignItems: "center",
        gap: "8px",
        minWidth: 0,
    },
    valueMain: {
        flex: 1,
        minWidth: 0,
        color: "#ffffff",
        fontSize: "14px",
        lineHeight: 1.35,
        wordBreak: "break-word",
    },
    valueTail: {
        flexShrink: 0,
        color: "#c2cfdf",
        fontSize: "14px",
        lineHeight: 1.35,
        fontWeight: 600,
    },
    routeValueMain: {
        flex: 1,
        minWidth: 0,
        display: "flex",
        alignItems: "center",
    },
    routeColorSwatch: {
        width: "12px",
        height: "12px",
        borderRadius: "2px",
        border: "1px solid rgba(255, 255, 255, 0.24)",
        flexShrink: 0,
        marginRight: "6px",
    },
    footer: {
        marginTop: "10px",
        color: "rgba(220, 229, 238, 0.95)",
        fontSize: "12px",
        lineHeight: 1.35,
    },
    subsectionFoldout: {
        marginTop: "14px",
        marginBottom: "8px",
        marginLeft: "-18px",
        marginRight: "-18px",
        width: "calc(100% + 36px)",
        borderRadius: "0",
        background: "rgba(28, 40, 58, 0.94)",
    },
    subsectionBody: {
        display: "flex",
        flexDirection: "column",
        gap: "0px",
        paddingLeft: "6px",
    },
};

function stopEvent(event) {
    if (!event) {
        return;
    }

    event.preventDefault();
    event.stopPropagation();
}

function stopPropagationOnly(event) {
    if (!event) {
        return;
    }

    event.stopPropagation();
}

function Row(props) {
    if (!props.value) {
        return null;
    }

    const isMultiline =
        typeof props.value === "string" && props.value.indexOf("\n") >= 0;
    const valueLines = isMultiline
        ? props.value.split(/\r?\n/).filter(function (line) { return line.length > 0; })
        : null;

    return h(
        "div",
        {
            style: isMultiline
                ? Object.assign({}, styles.row, styles.rowMultiline)
                : styles.row,
        },
        h("div", { style: styles.label }, props.label),
        h(
            "div",
            {
                style: isMultiline
                    ? Object.assign({}, styles.value, styles.valueMultiline)
                    : styles.value,
            },
            isMultiline
                ? valueLines.map(function (line, index) {
                      return h("div", { key: index }, line);
                  })
                : props.value
        )
    );
}

function ClassificationRow(props) {
    if (!props.primaryValue && !props.secondaryValue) {
        return null;
    }

    return h(
        "div",
        { style: styles.classificationRow },
        h("div", { style: styles.label }, props.label),
        h(
            "div",
            { style: styles.classificationValue },
            h(
                "span",
                { style: Object.assign({}, styles.classificationPrimary, styles.valueMain) },
                props.primaryValue || props.secondaryValue
            ),
            props.primaryValue && props.secondaryValue
                ? h("span", { style: styles.classificationSecondary }, props.secondaryValue)
                : null
        )
    );
}

function TailValueRow(props) {
    if (!props.mainValue && !props.tailValue) {
        return null;
    }

    return h(
        "div",
        { style: styles.row },
        h("div", { style: styles.label }, props.label),
        h(
            "div",
            { style: styles.valueWithTail },
            h(
                "div",
                { style: styles.valueMain },
                props.mainValue || props.tailValue
            ),
            props.mainValue && props.tailValue
                ? h("span", { style: styles.valueTail }, props.tailValue)
                : null
        )
    );
}

function CurrentRouteRow(props) {
    if (!props.routeName && !props.routeEntityText) {
        return null;
    }

    return h(
        "div",
        { style: styles.row },
        h("div", { style: styles.label }, props.label),
        h(
            "div",
            { style: styles.valueWithTail },
            h(
                "div",
                { style: styles.routeValueMain },
                props.routeColor
                    ? h("span", {
                          style: Object.assign(
                              {},
                              styles.routeColorSwatch,
                              { background: props.routeColor }
                          ),
                      })
                    : null,
                h(
                    "div",
                    { style: styles.valueMain },
                    props.routeName || props.routeEntityText
                )
            ),
            props.routeName && props.routeEntityText
                ? h("span", { style: styles.valueTail }, props.routeEntityText)
                : null
        )
    );
}

function FoldoutRow(props) {
    return h(
        "div",
        {
            style: Object.assign({}, styles.foldout, props.style || null),
            onMouseDown: stopEvent,
            onClick: function (event) {
                stopEvent(event);
                props.onToggleCollapsed();
            },
            title: props.collapsed ? props.expandTooltip : props.collapseTooltip,
        },
        h("span", null, props.title),
        h("span", { style: styles.foldoutIcon }, props.collapsed ? "▷" : "▽")
    );
}

function SelectedObjectPanel() {
    const visible = api.useValue(visibleBinding);
    const compact = api.useValue(compactBinding);
    const collapsed = api.useValue(collapsedBinding);
    const classification = api.useValue(classificationBinding);
    const classificationLabelText = api.useValue(classificationLabelTextBinding);
    const message = api.useValue(messageBinding);
    const tleStatus = api.useValue(tleStatusBinding);
    const role = api.useValue(roleBinding);
    const publicTransportLanePolicy = api.useValue(publicTransportLanePolicyBinding);
    const vehicleIndex = api.useValue(vehicleIndexBinding);
    const violationPending = api.useValue(violationPendingBinding);
    const totals = api.useValue(totalsBinding);
    const lastReason = api.useValue(lastReasonBinding);
    const repeatPenalty = api.useValue(repeatPenaltyBinding);
    const entitySelectionLabelText = api.useValue(entitySelectionLabelTextBinding);
    const entitySelectionPlaceholderText = api.useValue(entitySelectionPlaceholderTextBinding);
    const entitySelectionSubmitText = api.useValue(entitySelectionSubmitTextBinding);
    const entitySelectionSuggestedValue = api.useValue(entitySelectionSuggestedValueBinding);
    const entitySelectionStatus = api.useValue(entitySelectionStatusBinding);
    const entitySelectionStatusIsError = api.useValue(entitySelectionStatusIsErrorBinding);
    const pathActionLabelText = api.useValue(pathActionLabelTextBinding);
    const pathObsoleteButtonText = api.useValue(pathObsoleteButtonTextBinding);
    const pathObsoleteButtonVisible = api.useValue(pathObsoleteButtonVisibleBinding);
    const pathObsoleteButtonEnabled = api.useValue(pathObsoleteButtonEnabledBinding);
    const pathObsoleteStatus = api.useValue(pathObsoleteStatusBinding);
    const pathObsoleteStatusIsError = api.useValue(pathObsoleteStatusIsErrorBinding);
    const headerText = api.useValue(headerTextBinding);
    const summaryTitle = api.useValue(summaryTitleBinding);
    const tleStatusLabelText = api.useValue(tleStatusLabelTextBinding);
    const roleLabelText = api.useValue(roleLabelTextBinding);
    const activeFlagsLabelText = api.useValue(activeFlagsLabelTextBinding);
    const violationsFinesLabelText = api.useValue(violationsFinesLabelTextBinding);
    const lastReasonLabelText = api.useValue(lastReasonLabelTextBinding);
    const repeatPenaltyLabelText = api.useValue(repeatPenaltyLabelTextBinding);
    const publicTransportLanePolicyLabelText = api.useValue(publicTransportLanePolicyLabelTextBinding);
    const footerText = api.useValue(footerTextBinding);
    const expandSectionTooltipText = api.useValue(expandSectionTooltipTextBinding);
    const collapseSectionTooltipText = api.useValue(collapseSectionTooltipTextBinding);
    const laneDetailsTitleText = api.useValue(laneDetailsTitleTextBinding);
    const currentLaneLabelText = api.useValue(currentLaneLabelTextBinding);
    const previousLaneLabelText = api.useValue(previousLaneLabelTextBinding);
    const laneChangesLabelText = api.useValue(laneChangesLabelTextBinding);
    const liveLaneStateLabelText = api.useValue(liveLaneStateLabelTextBinding);
    const routeDiagnosticsTitleText = api.useValue(routeDiagnosticsTitleTextBinding);
    const currentTargetLabelText = api.useValue(currentTargetLabelTextBinding);
    const currentRouteLabelText = api.useValue(currentRouteLabelTextBinding);
    const targetRoadLabelText = api.useValue(targetRoadLabelTextBinding);
    const routeExplanationLabelText = api.useValue(routeExplanationLabelTextBinding);
    const connectedStopLabelText = api.useValue(connectedStopLabelTextBinding);
    const currentLane = api.useValue(currentLaneBinding);
    const previousLane = api.useValue(previousLaneBinding);
    const laneChanges = api.useValue(laneChangesBinding);
    const liveLaneState = api.useValue(liveLaneStateBinding);
    const laneDetailsVisible = api.useValue(laneDetailsVisibleBinding);
    const laneDetailsCollapsed = api.useValue(laneDetailsCollapsedBinding);
    const routeDiagnosticsVisible = api.useValue(routeDiagnosticsVisibleBinding);
    const routeDiagnosticsCollapsed = api.useValue(routeDiagnosticsCollapsedBinding);
    const currentTarget = api.useValue(currentTargetBinding);
    const currentTargetEntity = api.useValue(currentTargetEntityBinding);
    const currentRoute = api.useValue(currentRouteBinding);
    const currentRouteEntityText = api.useValue(currentRouteEntityTextBinding);
    const currentRouteColor = api.useValue(currentRouteColorBinding);
    const targetRoad = api.useValue(targetRoadBinding);
    const routeExplanation = api.useValue(routeExplanationBinding);
    const connectedStop = api.useValue(connectedStopBinding);
    const [entitySelectionInput, setEntitySelectionInput] = React.useState(entitySelectionSuggestedValue);

    React.useEffect(
        function () {
            setEntitySelectionInput(entitySelectionSuggestedValue || "");
        },
        [entitySelectionSuggestedValue]
    );

    const onClose = React.useCallback(function () {
        api.trigger(group, "close");
    }, []);

    const onToggleCollapsed = React.useCallback(function () {
        api.trigger(group, "toggleCollapsed");
    }, []);

    const onToggleLaneDetails = React.useCallback(function () {
        api.trigger(group, "toggleLaneDetailsCollapsed");
    }, []);

    const onToggleRouteDiagnostics = React.useCallback(function () {
        api.trigger(group, "toggleRouteDiagnosticsCollapsed");
    }, []);

    const onSubmitEntitySelection = React.useCallback(
        function (event) {
            stopEvent(event);
            api.trigger(group, "submitEntitySelection", entitySelectionInput || "");
        },
        [entitySelectionInput]
    );

    const onMarkSelectedVehiclePathObsolete = React.useCallback(function (event) {
        stopEvent(event);
        api.trigger(group, "markSelectedVehiclePathObsolete");
    }, []);

    if (!visible) {
        return null;
    }

    const isUltraCompact = compact && !classification;
    const panelWidth = isUltraCompact
        ? ultraCompactPanelWidth
        : compact || collapsed
            ? compactPanelWidth
            : fullPanelWidth;

    const foldout = compact
        ? null
        : h(FoldoutRow, {
              title: summaryTitle,
              collapsed,
              onToggleCollapsed,
              expandTooltip: expandSectionTooltipText,
              collapseTooltip: collapseSectionTooltipText,
          });

    const selectionBlock = h(
        "div",
        {
            style: styles.selectionBlock,
            onMouseDown: stopPropagationOnly,
            onClick: stopPropagationOnly,
        },
        h(
            "div",
            { style: styles.selectionRow },
            h("div", { style: styles.label }, entitySelectionLabelText),
            h(
                "div",
                { style: styles.selectionRowInputCell },
                h("input", {
                    type: "text",
                    value: entitySelectionInput || "",
                    placeholder: entitySelectionPlaceholderText,
                    style: styles.selectionInput,
                    onChange: function (event) {
                        setEntitySelectionInput(event.target.value);
                    },
                    onKeyDown: function (event) {
                        stopPropagationOnly(event);
                        if (event.key === "Enter") {
                            onSubmitEntitySelection(event);
                        }
                    },
                    onMouseDown: stopPropagationOnly,
                    onClick: stopPropagationOnly,
                })
            ),
            h(
                "button",
                {
                    type: "button",
                    style: styles.selectionButton,
                    onMouseDown: stopEvent,
                    onClick: onSubmitEntitySelection,
                },
                entitySelectionSubmitText
            )
        ),
        entitySelectionStatus
            ? h(
                  "div",
                  {
                      style: Object.assign(
                          {},
                          styles.selectionStatus,
                          entitySelectionStatusIsError
                              ? styles.selectionStatusError
                              : styles.selectionStatusSuccess
                      ),
                  },
                  entitySelectionStatus
              )
            : null,
        pathObsoleteButtonVisible
            ? h(
                  "div",
                  { style: styles.selectionRow },
                  h("div", { style: styles.label }, pathActionLabelText),
                  h(
                      "div",
                      { style: styles.selectionRowInputCell },
                      h(
                          "button",
                          {
                              type: "button",
                              style: Object.assign(
                                  {},
                                  styles.selectionButton,
                                  pathObsoleteButtonEnabled ? null : styles.selectionButtonDisabled
                              ),
                              disabled: !pathObsoleteButtonEnabled,
                              onMouseDown: stopEvent,
                              onClick: onMarkSelectedVehiclePathObsolete,
                          },
                          pathObsoleteButtonText
                      )
                  )
              )
            : null,
        pathObsoleteButtonVisible && pathObsoleteStatus
            ? h(
                  "div",
                  {
                      style: Object.assign(
                          {},
                          styles.actionStatus,
                          pathObsoleteStatusIsError ? styles.selectionStatusError : null
                      ),
                  },
                  pathObsoleteStatus
              )
            : null
    );

    const body = compact
        ? h(
              "div",
              { style: Object.assign({}, styles.compactBody, { width: panelWidth }) },
              [
                  selectionBlock,
                  isUltraCompact
                      ? h("div", { style: styles.compactMessage, key: "message" }, message)
                      : [
                        h(ClassificationRow, {
                            label: classificationLabelText,
                            primaryValue: classification,
                            secondaryValue: vehicleIndex,
                            key: "classification",
                        }),
                        h(
                            "div",
                            { style: styles.rows, key: "rows" },
                            h(Row, { label: tleStatusLabelText, value: tleStatus })
                        ),
                    ],
              ]
          )
        : collapsed
            ? null
            : h(
                  "div",
                  { style: styles.body },
                  selectionBlock,
                  message
                      ? h("div", { style: styles.compactMessage }, message)
                      : null,
                  h(ClassificationRow, {
                      label: classificationLabelText,
                      primaryValue: classification,
                      secondaryValue: vehicleIndex,
                  }),
                  h(
                      "div",
                      { style: styles.rows },
                      h(Row, { label: tleStatusLabelText, value: tleStatus }),
                      h(Row, { label: roleLabelText, value: role }),
                      h(Row, { label: activeFlagsLabelText, value: violationPending }),
                      h(Row, { label: violationsFinesLabelText, value: totals }),
                      h(Row, { label: lastReasonLabelText, value: lastReason }),
                      h(Row, { label: repeatPenaltyLabelText, value: repeatPenalty }),
                      h(Row, { label: publicTransportLanePolicyLabelText, value: publicTransportLanePolicy })
                  ),
                  h(
                      "div",
                      { style: styles.footer },
                      footerText
                  ),
                  laneDetailsVisible
                      ? h(FoldoutRow, {
                            title: laneDetailsTitleText,
                            collapsed: laneDetailsCollapsed,
                            onToggleCollapsed: onToggleLaneDetails,
                            expandTooltip: expandSectionTooltipText,
                            collapseTooltip: collapseSectionTooltipText,
                            style: styles.subsectionFoldout,
                        })
                      : null,
                   laneDetailsVisible && laneDetailsCollapsed
                       ? null
                       : !laneDetailsVisible
                           ? null
                           : h(
                                 "div",
                                 { style: styles.subsectionBody },
                                h(Row, { label: currentLaneLabelText, value: currentLane }),
                                h(Row, { label: previousLaneLabelText, value: previousLane }),
                                h(Row, { label: laneChangesLabelText, value: laneChanges }),
                                h(Row, { label: liveLaneStateLabelText, value: liveLaneState })
                            ),
                   routeDiagnosticsVisible
                       ? h(FoldoutRow, {
                             title: routeDiagnosticsTitleText,
                             collapsed: routeDiagnosticsCollapsed,
                             onToggleCollapsed: onToggleRouteDiagnostics,
                             expandTooltip: expandSectionTooltipText,
                             collapseTooltip: collapseSectionTooltipText,
                             style: styles.subsectionFoldout,
                         })
                       : null,
                   routeDiagnosticsVisible && !routeDiagnosticsCollapsed
                       ? h(
                              "div",
                              { style: styles.subsectionBody },
                              h(TailValueRow, {
                                  label: currentTargetLabelText,
                                  mainValue: currentTarget,
                                  tailValue: currentTargetEntity,
                              }),
                              h(CurrentRouteRow, {
                                  label: currentRouteLabelText,
                                  routeName: currentRoute,
                                  routeColor: currentRouteColor,
                                  routeEntityText: currentRouteEntityText,
                              }),
                              h(Row, { label: targetRoadLabelText, value: targetRoad }),
                              h(Row, { label: connectedStopLabelText, value: connectedStop }),
                              h(Row, { label: routeExplanationLabelText, value: routeExplanation })
                         )
                       : null,
                   null
               );

    return h(
        ui.Portal,
        null,
        h(
            ui.Panel,
            {
                draggable: true,
                header: headerText,
                onClose,
                initialPosition,
                style: {
                    width: panelWidth,
                    maxWidth: panelWidth,
                    overflow: "hidden",
                },
            },
            foldout,
            body
        )
    );
}

export default function registerSelectedObjectPanel(registry) {
    registry.append("GameTopRight", SelectedObjectPanel);
}

export const hasCSS = false;
