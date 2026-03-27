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
const roleOrTypeBinding = api.bindValue(group, "roleOrType", "");
const vehicleIndexBinding = api.bindValue(group, "vehicleIndex", "");
const violationPendingBinding = api.bindValue(group, "violationPending", "");
const totalsBinding = api.bindValue(group, "totals", "");
const lastReasonBinding = api.bindValue(group, "lastReason", "");
const resolvedEntityBinding = api.bindValue(group, "resolvedEntity", "");

const initialPosition = { x: 0.76, y: 0.1 };
const compactPanelWidth = "420px";
const fullPanelWidth = "560px";

const styles = {
    wrapper: {
        position: "fixed",
        inset: 0,
        pointerEvents: "none",
        zIndex: 999,
    },
    panel: {
        position: "fixed",
        width: fullPanelWidth,
        maxWidth: fullPanelWidth,
        background: "rgba(35, 49, 70, 0.96)",
        border: "1px solid rgba(255, 255, 255, 0.08)",
        boxShadow: "0 8px 32px rgba(0, 0, 0, 0.35)",
        pointerEvents: "auto",
        overflow: "hidden",
    },
    header: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        width: "100%",
        gap: "10px",
        color: "#f7f9ff",
        fontWeight: 700,
        fontSize: "20px",
        background: "rgba(18, 26, 38, 0.98)",
        padding: "14px 16px",
        boxSizing: "border-box",
        cursor: "move",
    },
    headerActions: {
        display: "flex",
        alignItems: "center",
        gap: "8px",
    },
    headerButton: {
        width: "22px",
        height: "22px",
        border: "1px solid rgba(255, 255, 255, 0.18)",
        background: "rgba(255, 255, 255, 0.10)",
        color: "#f7f9ff",
        borderRadius: "4px",
        cursor: "pointer",
        padding: 0,
        fontSize: "15px",
        lineHeight: "20px",
        fontWeight: 700,
    },
    content: {
        width: "100%",
        boxSizing: "border-box",
    },
    body: {
        padding: "18px",
        width: fullPanelWidth,
        boxSizing: "border-box",
    },
    classification: {
        color: "#b0defc",
        fontWeight: 700,
        fontSize: "18px",
        marginBottom: "10px",
    },
    compactMessage: {
        fontSize: "16px",
        lineHeight: 1.45,
        fontWeight: 700,
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
        width: "162px",
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

function Header(props) {
    const collapseText = props.collapsed ? "+" : "-";

    return h(
        "div",
        { style: styles.header, onMouseDown: props.onStartDrag },
        h("span", null, "Selected Object"),
        h(
            "div",
            { style: styles.headerActions },
            props.compact
                ? null
                : h(
                      "button",
                      {
                          type: "button",
                          style: styles.headerButton,
                          onMouseDown: stopEvent,
                          onClick: function (event) {
                              stopEvent(event);
                              props.onToggleCollapsed();
                          },
                          title: props.collapsed ? "Expand" : "Collapse",
                      },
                      collapseText
                  ),
            h(
                "button",
                {
                    type: "button",
                    style: styles.headerButton,
                    onMouseDown: stopEvent,
                    onClick: function (event) {
                        stopEvent(event);
                        props.onClose();
                    },
                    title: "Close",
                },
                "×"
            )
        )
    );
}

function SelectedObjectPanel() {
    const visible = api.useValue(visibleBinding);
    const compact = api.useValue(compactBinding);
    const collapsed = api.useValue(collapsedBinding);
    const classification = api.useValue(classificationBinding);
    const message = api.useValue(messageBinding);
    const tleStatus = api.useValue(tleStatusBinding);
    const roleOrType = api.useValue(roleOrTypeBinding);
    const vehicleIndex = api.useValue(vehicleIndexBinding);
    const violationPending = api.useValue(violationPendingBinding);
    const totals = api.useValue(totalsBinding);
    const lastReason = api.useValue(lastReasonBinding);
    const resolvedEntity = api.useValue(resolvedEntityBinding);
    const dragRef = React.useRef(null);
    const panelRef = React.useRef(null);
    const [position, setPosition] = React.useState(function () {
        return {
            left: Math.round(window.innerWidth * initialPosition.x),
            top: Math.round(window.innerHeight * initialPosition.y),
        };
    });

    const onClose = React.useCallback(function () {
        api.trigger(group, "close");
    }, []);

    const onToggleCollapsed = React.useCallback(function () {
        api.trigger(group, "toggleCollapsed");
    }, []);

    const onStartDrag = React.useCallback(
        function (event) {
            stopEvent(event);
            dragRef.current = {
                startX: event.clientX,
                startY: event.clientY,
                left: position.left,
                top: position.top,
            };
        },
        [position.left, position.top]
    );

    React.useEffect(function () {
        function onMouseMove(event) {
            if (!dragRef.current) {
                return;
            }

            event.preventDefault();

            const nextLeft = dragRef.current.left + (event.clientX - dragRef.current.startX);
            const nextTop = dragRef.current.top + (event.clientY - dragRef.current.startY);
            const panelRect = panelRef.current
                ? panelRef.current.getBoundingClientRect()
                : { width: 560, height: 240 };
            const maxLeft = Math.max(0, window.innerWidth - panelRect.width);
            const maxTop = Math.max(0, window.innerHeight - panelRect.height);

            setPosition({
                left: Math.min(Math.max(0, nextLeft), maxLeft),
                top: Math.min(Math.max(0, nextTop), maxTop),
            });
        }

        function onMouseUp() {
            dragRef.current = null;
        }

        window.addEventListener("mousemove", onMouseMove, true);
        window.addEventListener("mouseup", onMouseUp, true);

        return function () {
            window.removeEventListener("mousemove", onMouseMove, true);
            window.removeEventListener("mouseup", onMouseUp, true);
        };
    }, []);

    const header = h(Header, {
        compact,
        collapsed,
        onClose,
        onToggleCollapsed,
        onStartDrag,
    });

    const body = compact
        ? h(
              "div",
              { style: Object.assign({}, styles.body, { width: compactPanelWidth }) },
              h("div", { style: styles.compactMessage }, message)
          )
        : collapsed
            ? null
            : h(
                  "div",
                  { style: styles.body },
                  h("div", { style: styles.classification }, classification),
                  h("div", { style: styles.statusLabel }, "TLE status"),
                  h("div", { style: styles.statusBlock }, tleStatus),
                  h(
                      "div",
                      { style: styles.rows },
                      h(Row, { label: "Role / type", value: roleOrType }),
                      h(Row, { label: "Vehicle index", value: vehicleIndex }),
                      h(Row, { label: "Violation / pending", value: violationPending }),
                      h(Row, { label: "Violations / fines", value: totals }),
                      h(Row, { label: "Last reason", value: lastReason }),
                      h(Row, { label: "Resolved entity", value: resolvedEntity })
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
            "div",
            { style: Object.assign({}, styles.wrapper, { display: visible ? "block" : "none" }) },
            h(
                "div",
                {
                    ref: panelRef,
                    style: Object.assign({}, styles.panel, {
                        left: position.left + "px",
                        top: position.top + "px",
                        width: compact ? compactPanelWidth : fullPanelWidth,
                        maxWidth: compact ? compactPanelWidth : fullPanelWidth,
                    }),
                },
                header,
                collapsed
                    ? null
                    : h(
                          "div",
                          { style: styles.content },
                          body
                      )
            )
        )
    );
}

export default function registerSelectedObjectPanel(registry) {
    registry.append("GameTopRight", SelectedObjectPanel);
}

export const hasCSS = false;
