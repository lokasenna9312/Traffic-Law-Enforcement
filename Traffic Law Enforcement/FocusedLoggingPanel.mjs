/*!
 * Cities: Skylines II UI Module
 *
 * Id: TrafficLawEnforcement.FocusedLoggingPanel
 * Author: Pectus Solentis
 * Version: 0.0.0
 * Dependencies:
 */

const React = window.React;
const api = window["cs2/api"];
const ui = window["cs2/ui"];

const group = "focusedLoggingPanel";
const h = React.createElement;

const visibleBinding = api.bindValue(group, "visible", false);
const headerTextBinding = api.bindValue(group, "headerText", "");
const selectedVehicleLabelTextBinding = api.bindValue(group, "selectedVehicleLabelText", "");
const selectedRoleLabelTextBinding = api.bindValue(group, "selectedRoleLabelText", "");
const selectedWatchStatusLabelTextBinding = api.bindValue(group, "selectedWatchStatusLabelText", "");
const watchedCountLabelTextBinding = api.bindValue(group, "watchedCountLabelText", "");
const watchedVehiclesLabelTextBinding = api.bindValue(group, "watchedVehiclesLabelText", "");
const burstLoggingLabelTextBinding = api.bindValue(group, "burstLoggingLabelText", "");
const watchSelectedTextBinding = api.bindValue(group, "watchSelectedText", "");
const unwatchSelectedTextBinding = api.bindValue(group, "unwatchSelectedText", "");
const clearWatchedTextBinding = api.bindValue(group, "clearWatchedText", "");
const toggleBurstLoggingTextBinding = api.bindValue(group, "toggleBurstLoggingText", "");
const footerHintTextBinding = api.bindValue(group, "footerHintText", "");
const selectedVehicleBinding = api.bindValue(group, "selectedVehicle", "");
const selectedRoleBinding = api.bindValue(group, "selectedRole", "");
const selectedWatchStatusBinding = api.bindValue(group, "selectedWatchStatus", "");
const watchedCountBinding = api.bindValue(group, "watchedCount", "");
const watchedVehiclesBinding = api.bindValue(group, "watchedVehicles", "");
const burstLoggingBinding = api.bindValue(group, "burstLogging", "");
const burstLoggingActiveBinding = api.bindValue(group, "burstLoggingActive", false);
const messageBinding = api.bindValue(group, "message", "");
const watchSelectedEnabledBinding = api.bindValue(group, "watchSelectedEnabled", false);
const unwatchSelectedEnabledBinding = api.bindValue(group, "unwatchSelectedEnabled", false);
const clearWatchedEnabledBinding = api.bindValue(group, "clearWatchedEnabled", false);
const toggleBurstLoggingEnabledBinding = api.bindValue(group, "toggleBurstLoggingEnabled", false);

const initialPosition = { x: 0.6, y: 0.54 };
const panelWidth = "420px";

const styles = {
    body: {
        padding: "16px 18px 14px 18px",
        width: "100%",
        boxSizing: "border-box",
    },
    rows: {
        display: "flex",
        flexDirection: "column",
        gap: "0px",
    },
    row: {
        display: "flex",
        minHeight: "30px",
        alignItems: "flex-start",
    },
    label: {
        width: "126px",
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
    message: {
        marginTop: "6px",
        marginBottom: "10px",
        color: "#d7ebfd",
        fontSize: "13px",
        lineHeight: 1.35,
    },
    buttons: {
        display: "flex",
        flexWrap: "wrap",
        gap: "8px",
        marginTop: "14px",
    },
    button: {
        border: "none",
        borderRadius: "8px",
        padding: "8px 12px",
        fontSize: "13px",
        fontWeight: 700,
        cursor: "pointer",
        background: "#2f89b7",
        color: "#f6fbff",
    },
    disabledButton: {
        background: "rgba(133, 150, 169, 0.42)",
        color: "rgba(246, 251, 255, 0.75)",
        cursor: "default",
    },
    footer: {
        marginTop: "12px",
        color: "rgba(220, 229, 238, 0.95)",
        fontSize: "12px",
        lineHeight: 1.35,
    },
    switchRow: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        marginTop: "14px",
        padding: "10px 0 2px 0",
        gap: "12px",
    },
    switchLabelBlock: {
        display: "flex",
        flexDirection: "column",
        gap: "2px",
        minWidth: 0,
        flex: 1,
    },
    switchLabel: {
        color: "#c2cfdf",
        fontSize: "13px",
        lineHeight: 1.3,
        fontWeight: 700,
    },
    switchValue: {
        color: "#ffffff",
        fontSize: "13px",
        lineHeight: 1.3,
        wordBreak: "break-word",
    },
    switchButton: {
        border: "none",
        borderRadius: "999px",
        width: "58px",
        minWidth: "58px",
        height: "30px",
        padding: "3px",
        background: "rgba(105, 123, 144, 0.7)",
        cursor: "pointer",
        display: "flex",
        alignItems: "center",
        justifyContent: "flex-start",
        transition: "background 120ms ease",
    },
    switchButtonActive: {
        background: "#3fa36f",
        justifyContent: "flex-end",
    },
    switchKnob: {
        width: "24px",
        height: "24px",
        borderRadius: "50%",
        background: "#f7fbff",
        boxShadow: "0 1px 4px rgba(0, 0, 0, 0.35)",
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
    return h(
        "div",
        { style: styles.row },
        h("div", { style: styles.label }, props.label),
        h("div", { style: styles.value }, props.value || "")
    );
}

function ActionButton(props) {
    const style = props.disabled
        ? Object.assign({}, styles.button, styles.disabledButton)
        : styles.button;

    return h(
        "button",
        {
            type: "button",
            style,
            disabled: props.disabled,
            onMouseDown: stopEvent,
            onClick: function (event) {
                stopEvent(event);
                if (!props.disabled) {
                    props.onClick();
                }
            },
        },
        props.label
    );
}

function ToggleSwitch(props) {
    const style = props.active
        ? Object.assign({}, styles.switchButton, styles.switchButtonActive)
        : styles.switchButton;

    return h(
        "button",
        {
            type: "button",
            style: props.disabled
                ? Object.assign({}, style, styles.disabledButton)
                : style,
            disabled: props.disabled,
            onMouseDown: stopEvent,
            onClick: function (event) {
                stopEvent(event);
                if (!props.disabled) {
                    props.onClick();
                }
            },
            title: props.label,
        },
        h("span", { style: styles.switchKnob })
    );
}

function FocusedLoggingPanel() {
    const visible = api.useValue(visibleBinding);
    const headerText = api.useValue(headerTextBinding);
    const selectedVehicleLabelText = api.useValue(selectedVehicleLabelTextBinding);
    const selectedRoleLabelText = api.useValue(selectedRoleLabelTextBinding);
    const selectedWatchStatusLabelText = api.useValue(selectedWatchStatusLabelTextBinding);
    const watchedCountLabelText = api.useValue(watchedCountLabelTextBinding);
    const watchedVehiclesLabelText = api.useValue(watchedVehiclesLabelTextBinding);
    const burstLoggingLabelText = api.useValue(burstLoggingLabelTextBinding);
    const watchSelectedText = api.useValue(watchSelectedTextBinding);
    const unwatchSelectedText = api.useValue(unwatchSelectedTextBinding);
    const clearWatchedText = api.useValue(clearWatchedTextBinding);
    const toggleBurstLoggingText = api.useValue(toggleBurstLoggingTextBinding);
    const footerHintText = api.useValue(footerHintTextBinding);
    const selectedVehicle = api.useValue(selectedVehicleBinding);
    const selectedRole = api.useValue(selectedRoleBinding);
    const selectedWatchStatus = api.useValue(selectedWatchStatusBinding);
    const watchedCount = api.useValue(watchedCountBinding);
    const watchedVehicles = api.useValue(watchedVehiclesBinding);
    const burstLogging = api.useValue(burstLoggingBinding);
    const burstLoggingActive = api.useValue(burstLoggingActiveBinding);
    const message = api.useValue(messageBinding);
    const watchSelectedEnabled = api.useValue(watchSelectedEnabledBinding);
    const unwatchSelectedEnabled = api.useValue(unwatchSelectedEnabledBinding);
    const clearWatchedEnabled = api.useValue(clearWatchedEnabledBinding);
    const toggleBurstLoggingEnabled = api.useValue(toggleBurstLoggingEnabledBinding);

    const onClose = React.useCallback(function () {
        api.trigger(group, "close");
    }, []);

    const onWatchSelected = React.useCallback(function () {
        api.trigger(group, "watchSelected");
    }, []);

    const onUnwatchSelected = React.useCallback(function () {
        api.trigger(group, "unwatchSelected");
    }, []);

    const onClearWatched = React.useCallback(function () {
        api.trigger(group, "clearWatched");
    }, []);

    const onToggleBurstLogging = React.useCallback(function () {
        api.trigger(group, "toggleBurstLogging");
    }, []);

    if (!visible) {
        return null;
    }

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
            h(
                "div",
                { style: styles.body },
                h(
                    "div",
                    { style: styles.rows },
                    h(Row, { label: selectedVehicleLabelText, value: selectedVehicle }),
                    h(Row, { label: selectedRoleLabelText, value: selectedRole }),
                    h(Row, { label: selectedWatchStatusLabelText, value: selectedWatchStatus }),
                    h(Row, { label: watchedCountLabelText, value: watchedCount }),
                    h(Row, { label: watchedVehiclesLabelText, value: watchedVehicles })
                ),
                message ? h("div", { style: styles.message }, message) : null,
                h(
                    "div",
                    { style: styles.switchRow },
                    h(
                        "div",
                        { style: styles.switchLabelBlock },
                        h("div", { style: styles.switchLabel }, burstLoggingLabelText),
                        h("div", { style: styles.switchValue }, burstLogging)
                    ),
                    h(ToggleSwitch, {
                        label: toggleBurstLoggingText,
                        active: burstLoggingActive,
                        disabled: !toggleBurstLoggingEnabled,
                        onClick: onToggleBurstLogging,
                    })
                ),
                h(
                    "div",
                    { style: styles.buttons },
                    h(ActionButton, {
                        label: watchSelectedText,
                        disabled: !watchSelectedEnabled,
                        onClick: onWatchSelected,
                    }),
                    h(ActionButton, {
                        label: unwatchSelectedText,
                        disabled: !unwatchSelectedEnabled,
                        onClick: onUnwatchSelected,
                    }),
                    h(ActionButton, {
                        label: clearWatchedText,
                        disabled: !clearWatchedEnabled,
                        onClick: onClearWatched,
                    })
                ),
                footerHintText
                    ? h("div", { style: styles.footer }, footerHintText)
                    : null
            )
        )
    );
}

export default function registerFocusedLoggingPanel(registry) {
    registry.append("GameTopRight", FocusedLoggingPanel);
}

export const hasCSS = false;
