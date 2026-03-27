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
const messageBinding = api.bindValue(group, "message", "");
const tleStatusBinding = api.bindValue(group, "tleStatus", "");
const roleBinding = api.bindValue(group, "role", "");
const publicTransportLanePolicyBinding = api.bindValue(group, "publicTransportLanePolicy", "");
const vehicleIndexBinding = api.bindValue(group, "vehicleIndex", "");
const violationPendingBinding = api.bindValue(group, "violationPending", "");
const totalsBinding = api.bindValue(group, "totals", "");
const lastReasonBinding = api.bindValue(group, "lastReason", "");
const headerTextBinding = api.bindValue(group, "headerText", "");
const summaryTitleBinding = api.bindValue(group, "summaryTitle", "");
const tleStatusLabelTextBinding = api.bindValue(group, "tleStatusLabelText", "");
const roleLabelTextBinding = api.bindValue(group, "roleLabelText", "");
const activeFlagsLabelTextBinding = api.bindValue(group, "activeFlagsLabelText", "");
const violationsFinesLabelTextBinding = api.bindValue(group, "violationsFinesLabelText", "");
const lastReasonLabelTextBinding = api.bindValue(group, "lastReasonLabelText", "");
const publicTransportLanePolicyLabelTextBinding = api.bindValue(group, "publicTransportLanePolicyLabelText", "");
const footerTextBinding = api.bindValue(group, "footerText", "");
const expandSectionTooltipTextBinding = api.bindValue(group, "expandSectionTooltipText", "");
const collapseSectionTooltipTextBinding = api.bindValue(group, "collapseSectionTooltipText", "");

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
        wordBreak: "break-word",
    },
    classificationLabel: {
        width: "138px",
        color: "#b0defc",
        fontWeight: 700,
        fontSize: "18px",
        lineHeight: 1.35,
        flexShrink: 0,
    },
    classificationValue: {
        flex: 1,
        color: "#b0defc",
        fontSize: "18px",
        fontWeight: 700,
        lineHeight: 1.35,
        wordBreak: "break-word",
    },
    footer: {
        marginTop: "10px",
        color: "rgba(220, 229, 238, 0.95)",
        fontSize: "12px",
        fontStyle: "italic",
        lineHeight: 1.35,
    },
};

function stopEvent(event) {
    if (!event) {
        return;
    }

    event.preventDefault();
    event.stopPropagation();
}

function Row(props) {
    if (!props.value) {
        return null;
    }

    return h(
        "div",
        { style: styles.row },
        h("div", { style: styles.label }, props.label),
        h("div", { style: styles.value }, props.value)
    );
}

function ClassificationRow(props) {
    return h(
        "div",
        { style: styles.classificationRow },
        h("div", { style: styles.classificationLabel }, props.label),
        props.value
            ? h("div", { style: styles.classificationValue }, props.value)
            : null
    );
}

function FoldoutRow(props) {
    return h(
        "div",
        {
            style: styles.foldout,
            onMouseDown: stopEvent,
            onClick: function (event) {
                stopEvent(event);
                props.onToggleCollapsed();
            },
            title: props.collapsed ? props.expandTooltip : props.collapseTooltip,
        },
        h("span", null, props.title),
        h("span", { style: styles.foldoutIcon }, props.collapsed ? "▶" : "▼")
    );
}

function SelectedObjectPanel() {
    const visible = api.useValue(visibleBinding);
    const compact = api.useValue(compactBinding);
    const collapsed = api.useValue(collapsedBinding);
    const classification = api.useValue(classificationBinding);
    const message = api.useValue(messageBinding);
    const tleStatus = api.useValue(tleStatusBinding);
    const role = api.useValue(roleBinding);
    const publicTransportLanePolicy = api.useValue(publicTransportLanePolicyBinding);
    const vehicleIndex = api.useValue(vehicleIndexBinding);
    const violationPending = api.useValue(violationPendingBinding);
    const totals = api.useValue(totalsBinding);
    const lastReason = api.useValue(lastReasonBinding);
    const headerText = api.useValue(headerTextBinding);
    const summaryTitle = api.useValue(summaryTitleBinding);
    const tleStatusLabelText = api.useValue(tleStatusLabelTextBinding);
    const roleLabelText = api.useValue(roleLabelTextBinding);
    const activeFlagsLabelText = api.useValue(activeFlagsLabelTextBinding);
    const violationsFinesLabelText = api.useValue(violationsFinesLabelTextBinding);
    const lastReasonLabelText = api.useValue(lastReasonLabelTextBinding);
    const publicTransportLanePolicyLabelText = api.useValue(publicTransportLanePolicyLabelTextBinding);
    const footerText = api.useValue(footerTextBinding);
    const expandSectionTooltipText = api.useValue(expandSectionTooltipTextBinding);
    const collapseSectionTooltipText = api.useValue(collapseSectionTooltipTextBinding);

    const onClose = React.useCallback(function () {
        api.trigger(group, "close");
    }, []);

    const onToggleCollapsed = React.useCallback(function () {
        api.trigger(group, "toggleCollapsed");
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

    const body = compact
        ? h(
              "div",
              { style: Object.assign({}, styles.compactBody, { width: panelWidth }) },
              isUltraCompact
                  ? h("div", { style: styles.compactMessage }, message)
                  : [
                        h(ClassificationRow, {
                            label: classification,
                            value: vehicleIndex ? "#" + vehicleIndex : "",
                            key: "classification",
                        }),
                        h(
                            "div",
                            { style: styles.rows, key: "rows" },
                            h(Row, { label: tleStatusLabelText, value: tleStatus })
                        ),
                    ]
          )
        : collapsed
            ? null
            : h(
                  "div",
                  { style: styles.body },
                  h(ClassificationRow, {
                      label: classification,
                      value: vehicleIndex ? "#" + vehicleIndex : "",
                  }),
                  h(
                      "div",
                      { style: styles.rows },
                      h(Row, { label: tleStatusLabelText, value: tleStatus }),
                      h(Row, { label: roleLabelText, value: role }),
                      h(Row, { label: activeFlagsLabelText, value: violationPending }),
                      h(Row, { label: violationsFinesLabelText, value: totals }),
                      h(Row, { label: lastReasonLabelText, value: lastReason }),
                      h(Row, { label: publicTransportLanePolicyLabelText, value: publicTransportLanePolicy })
                  ),
                  h(
                      "div",
                      { style: styles.footer },
                      footerText
                  )
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
