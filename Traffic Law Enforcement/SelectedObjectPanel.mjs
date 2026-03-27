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
const publicTransportLaneAllowanceBinding = api.bindValue(group, "publicTransportLaneAllowance", "");
const vehicleIndexBinding = api.bindValue(group, "vehicleIndex", "");
const violationPendingBinding = api.bindValue(group, "violationPending", "");
const totalsBinding = api.bindValue(group, "totals", "");
const lastReasonBinding = api.bindValue(group, "lastReason", "");

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
    compactClassificationRow: {
        display: "flex",
        alignItems: "baseline",
        gap: "8px",
        marginBottom: "8px",
        flexWrap: "wrap",
    },
    classificationRow: {
        display: "flex",
        alignItems: "baseline",
        gap: "8px",
        marginBottom: "10px",
        flexWrap: "wrap",
    },
    classificationText: {
        color: "#b0defc",
        fontWeight: 700,
        fontSize: "18px",
    },
    classificationIndex: {
        color: "#d6e1f2",
        fontWeight: 600,
        fontSize: "14px",
    },
    compactMessage: {
        fontSize: "14px",
        lineHeight: 1.35,
        fontWeight: 600,
        color: "#f7f9ff",
    },
    statusLabel: {
        color: "#c2cfdf",
        fontWeight: 700,
        fontSize: "13px",
        marginBottom: "8px",
    },
    statusBlock: {
        background: "rgba(45, 56, 75, 0.95)",
        border: "1px solid rgba(255, 255, 255, 0.06)",
        padding: "12px",
        minHeight: "48px",
        display: "flex",
        alignItems: "center",
        fontWeight: 700,
        fontSize: "16px",
        color: "#ffffff",
        marginBottom: "12px",
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
            title: props.collapsed ? "Expand section" : "Collapse section",
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
    const publicTransportLaneAllowance = api.useValue(publicTransportLaneAllowanceBinding);
    const vehicleIndex = api.useValue(vehicleIndexBinding);
    const violationPending = api.useValue(violationPendingBinding);
    const totals = api.useValue(totalsBinding);
    const lastReason = api.useValue(lastReasonBinding);

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
        : compact
            ? compactPanelWidth
            : fullPanelWidth;

    const foldout = compact
        ? null
        : h(FoldoutRow, {
              title: "Summary",
              collapsed,
              onToggleCollapsed,
          });

    const body = compact
        ? h(
              "div",
              { style: Object.assign({}, styles.compactBody, { width: panelWidth }) },
              isUltraCompact
                  ? h("div", { style: styles.compactMessage }, message)
                  : [
                        h(
                            "div",
                            { style: styles.compactClassificationRow, key: "classification" },
                            h("div", { style: styles.classificationText }, classification),
                            vehicleIndex
                                ? h("div", { style: styles.classificationIndex }, "#" + vehicleIndex)
                                : null
                        ),
                        h("div", { style: styles.statusLabel, key: "label" }, "TLE status"),
                        h("div", { style: styles.statusBlock, key: "status" }, tleStatus),
                    ]
          )
        : collapsed
            ? null
            : h(
                  "div",
                  { style: styles.body },
                  h(
                      "div",
                      { style: styles.classificationRow },
                      h("div", { style: styles.classificationText }, classification),
                      vehicleIndex
                          ? h("div", { style: styles.classificationIndex }, "#" + vehicleIndex)
                          : null
                  ),
                  h("div", { style: styles.statusLabel }, "TLE status"),
                  h("div", { style: styles.statusBlock }, tleStatus),
                  h(
                      "div",
                      { style: styles.rows },
                      h(Row, { label: "Role / type", value: role }),
                      h(Row, { label: "Active flags", value: violationPending }),
                      h(Row, { label: "Violations / fines", value: totals }),
                      h(Row, { label: "Last reason", value: lastReason }),
                      h(Row, { label: "PT lane policy", value: publicTransportLaneAllowance })
                  ),
                  h(
                      "div",
                      { style: styles.footer },
                      "If Developer Mode is enabled, press Tab for more details."
                  )
              );

    return h(
        ui.Portal,
        null,
        h(
            ui.Panel,
            {
                draggable: true,
                header: "Selected Object",
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
