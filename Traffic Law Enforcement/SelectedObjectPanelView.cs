using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Traffic_Law_Enforcement
{
    internal sealed class SelectedObjectPanelView : MonoBehaviour
    {
        private const float kWidth = 516f;
        private const float kTopInset = 78f;
        private const float kRightInset = 88f;
        private const float kPadding = 18f;
        private const float kTitleBarHeight = 44f;
        private const float kTitleLineHeight = 24f;
        private const float kClassificationLineHeight = 30f;
        private const float kBodyLineHeight = 30f;
        private const float kCompactMessageHeight = 40f;
        private const float kButtonSize = 20f;
        private const float kButtonGap = 8f;
        private const float kRowLabelWidth = 162f;
        private const float kSectionGap = 12f;
        private const float kStatusLabelHeight = 18f;
        private const float kStatusBlockHeight = 46f;
        private const int kSortingOrder = 240;

        private static readonly Color kPanelColor = new Color(0.08f, 0.12f, 0.17f, 0.92f);
        private static readonly Color kTitleBarColor = new Color(0.16f, 0.20f, 0.27f, 0.98f);
        private static readonly Color kStatusBlockColor = new Color(0.15f, 0.19f, 0.25f, 1f);
        private static readonly Color kButtonColor = new Color(0.27f, 0.32f, 0.40f, 1f);
        private static readonly Color kTitleColor = new Color(0.97f, 0.98f, 1f, 1f);
        private static readonly Color kClassificationColor = new Color(0.69f, 0.87f, 1f, 1f);
        private static readonly Color kLabelColor = new Color(0.76f, 0.81f, 0.88f, 1f);
        private static readonly Color kFooterColor = new Color(0.82f, 0.86f, 0.90f, 0.95f);

        private sealed class RowView
        {
            public RectTransform Root;
            public Text Label;
            public Text Value;
        }

        internal struct State
        {
            public bool Visible;
            public bool Compact;
            public string SelectionToken;
            public string Classification;
            public string Message;
            public string TleStatus;
            public string RoleOrType;
            public string VehicleIndex;
            public string ViolationPending;
            public string Totals;
            public string LastReason;
            public string ResolvedEntity;
        }

        private State m_State;
        private string m_LastSelectionToken;
        private string m_DismissedSelectionToken;
        private bool m_Collapsed;
        private bool m_HasCustomPosition;
        private bool m_IsDragging;
        private Vector2 m_WindowPosition;
        private Vector2 m_DragOffset;
        private Vector2 m_LastScreenSize;

        private Font m_Font;
        private Canvas m_Canvas;
        private RectTransform m_UiRootRect;
        private RectTransform m_PanelRect;
        private Image m_PanelBackground;
        private RectTransform m_TitleBarRect;
        private Image m_TitleBarBackground;
        private Text m_TitleText;
        private Button m_CollapseButton;
        private Text m_CollapseButtonText;
        private Button m_CloseButton;
        private Text m_CloseButtonText;
        private Text m_ClassificationText;
        private Text m_MessageText;
        private Text m_StatusLabelText;
        private RectTransform m_StatusBlockRect;
        private Image m_StatusBlockBackground;
        private Text m_StatusValueText;
        private readonly RowView[] m_Rows = new RowView[6];

        internal void UpdateState(State state)
        {
            if (!string.Equals(m_LastSelectionToken, state.SelectionToken))
            {
                m_LastSelectionToken = state.SelectionToken;
                m_DismissedSelectionToken = string.Empty;
                m_Collapsed = false;
            }

            m_State = state;
            ApplyState();
        }

        internal void BeginDrag(Vector2 pointerTopLeft)
        {
            m_IsDragging = true;
            m_HasCustomPosition = true;
            m_DragOffset = pointerTopLeft - m_WindowPosition;
        }

        internal void DragTo(Vector2 pointerTopLeft)
        {
            if (!m_IsDragging)
            {
                return;
            }

            m_WindowPosition = pointerTopLeft - m_DragOffset;
            ClampWindowPosition(CalculateHeight());
            ApplyWindowPosition();
        }

        internal void EndDrag()
        {
            m_IsDragging = false;
        }

        private void Awake()
        {
            hideFlags = HideFlags.HideAndDontSave;
            ResetWindowPosition();
            EnsureUi();
            ApplyState();
        }

        private void LateUpdate()
        {
            if (m_PanelRect == null || !m_PanelRect.gameObject.activeSelf)
            {
                return;
            }

            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (m_LastScreenSize == screenSize)
            {
                return;
            }

            if (!m_HasCustomPosition)
            {
                ResetWindowPosition();
            }

            ClampWindowPosition(CalculateHeight());
            ApplyWindowPosition();
            m_LastScreenSize = screenSize;
        }

        private void EnsureUi()
        {
            if (m_Canvas != null)
            {
                return;
            }

            m_Font = ResolveFont();

            m_Canvas = gameObject.GetComponent<Canvas>();
            if (m_Canvas == null)
            {
                m_Canvas = gameObject.AddComponent<Canvas>();
            }

            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = kSortingOrder;
            m_Canvas.pixelPerfect = false;

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            GameObject uiRootObject = new GameObject("UiRoot", typeof(RectTransform));
            uiRootObject.transform.SetParent(gameObject.transform, false);
            m_UiRootRect = (RectTransform)uiRootObject.transform;
            m_UiRootRect.anchorMin = Vector2.zero;
            m_UiRootRect.anchorMax = Vector2.one;
            m_UiRootRect.offsetMin = Vector2.zero;
            m_UiRootRect.offsetMax = Vector2.zero;

            m_PanelRect = CreateRect("Panel", m_UiRootRect);
            m_PanelBackground = m_PanelRect.gameObject.AddComponent<Image>();
            m_PanelBackground.color = kPanelColor;
            m_PanelBackground.raycastTarget = true;

            m_TitleBarRect = CreateRect("TitleBar", m_PanelRect);
            m_TitleBarBackground = m_TitleBarRect.gameObject.AddComponent<Image>();
            m_TitleBarBackground.color = kTitleBarColor;
            m_TitleBarBackground.raycastTarget = true;

            RectTransform dragHandleRect = CreateRect("DragHandle", m_TitleBarRect);
            Image dragHandleImage = dragHandleRect.gameObject.AddComponent<Image>();
            dragHandleImage.color = new Color(0f, 0f, 0f, 0.001f);
            dragHandleImage.raycastTarget = true;
            SelectedObjectPanelDragHandle dragHandle =
                dragHandleRect.gameObject.AddComponent<SelectedObjectPanelDragHandle>();
            dragHandle.Initialize(this);

            m_TitleText = CreateText(
                "TitleText",
                m_TitleBarRect,
                20,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                kTitleColor);

            m_CollapseButton = CreateButton(m_TitleBarRect, "-", out m_CollapseButtonText);
            m_CollapseButton.onClick.AddListener(ToggleCollapsed);

            m_CloseButton = CreateButton(m_TitleBarRect, "X", out m_CloseButtonText);
            m_CloseButton.onClick.AddListener(CloseCurrentSelection);

            m_ClassificationText = CreateText(
                "ClassificationText",
                m_PanelRect,
                18,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                kClassificationColor);

            m_MessageText = CreateText(
                "MessageText",
                m_PanelRect,
                16,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                Color.white);
            m_MessageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            m_MessageText.verticalOverflow = VerticalWrapMode.Truncate;

            m_StatusLabelText = CreateText(
                "StatusLabelText",
                m_PanelRect,
                13,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                kLabelColor);

            m_StatusBlockRect = CreateRect("StatusBlock", m_PanelRect);
            m_StatusBlockBackground = m_StatusBlockRect.gameObject.AddComponent<Image>();
            m_StatusBlockBackground.color = kStatusBlockColor;
            m_StatusBlockBackground.raycastTarget = true;

            m_StatusValueText = CreateText(
                "StatusValueText",
                m_StatusBlockRect,
                16,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white);

            CreateRows();
        }

        private void CreateRows()
        {
            string[] labels =
            {
                "Role / type",
                "Vehicle index",
                "Violation / pending",
                "Violations / fines",
                "Last reason",
                "Resolved entity",
            };

            for (int index = 0; index < m_Rows.Length; index += 1)
            {
                RowView row = new RowView();
                row.Root = CreateRect($"Row{index}", m_PanelRect);

                Color valueColor = index == m_Rows.Length - 1
                    ? kFooterColor
                    : Color.white;

                row.Label = CreateText(
                    $"Row{index}Label",
                    row.Root,
                    14,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft,
                    kLabelColor);
                row.Label.text = labels[index];

                row.Value = CreateText(
                    $"Row{index}Value",
                    row.Root,
                    14,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft,
                    valueColor);
                row.Value.horizontalOverflow = HorizontalWrapMode.Wrap;
                row.Value.verticalOverflow = VerticalWrapMode.Truncate;

                m_Rows[index] = row;
            }
        }

        private void ApplyState()
        {
            EnsureUi();

            bool visible = m_State.Visible && !IsDismissed();
            m_PanelRect.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            m_TitleText.text = "Selected Object";
            m_CollapseButtonText.text = m_Collapsed ? "+" : "-";
            m_CloseButtonText.text = "X";
            m_ClassificationText.text = m_State.Classification ?? string.Empty;
            m_MessageText.text = m_State.Message ?? string.Empty;
            m_StatusLabelText.text = "TLE status";
            m_StatusValueText.text = m_State.TleStatus ?? string.Empty;

            string[] rowValues =
            {
                m_State.RoleOrType,
                m_State.VehicleIndex,
                m_State.ViolationPending,
                m_State.Totals,
                m_State.LastReason,
                m_State.ResolvedEntity,
            };

            for (int index = 0; index < m_Rows.Length; index += 1)
            {
                bool rowVisible = !string.IsNullOrWhiteSpace(rowValues[index]);
                m_Rows[index].Root.gameObject.SetActive(rowVisible);
                m_Rows[index].Value.text = rowValues[index] ?? string.Empty;
            }

            RefreshLayout();
        }

        private void RefreshLayout()
        {
            float height = CalculateHeight();
            m_PanelRect.sizeDelta = new Vector2(kWidth, height);
            ApplyWindowPosition();

            float contentWidth = kWidth - (kPadding * 2f);
            float y = 0f;

            SetRect(m_TitleBarRect, 0f, y, kWidth, kTitleBarHeight);
            SetRect(
                (RectTransform)m_CloseButton.transform,
                kWidth - kPadding - kButtonSize,
                12f,
                kButtonSize,
                kButtonSize);
            SetRect(
                (RectTransform)m_CollapseButton.transform,
                kWidth - kPadding - (kButtonSize * 2f) - kButtonGap,
                12f,
                kButtonSize,
                kButtonSize);
            SetRect(
                m_TitleText.rectTransform,
                kPadding,
                9f,
                contentWidth - (kButtonSize * 2f) - kButtonGap - 12f,
                kTitleLineHeight);
            SetRect(
                m_TitleBarRect.Find("DragHandle") as RectTransform,
                6f,
                5f,
                Mathf.Max(0f, kWidth - (kPadding * 2f) - (kButtonSize * 2f) - kButtonGap - 14f),
                kTitleBarHeight - 10f);

            y += kTitleBarHeight;
            if (m_Collapsed)
            {
                return;
            }

            y += kPadding;

            bool hasClassification = !string.IsNullOrWhiteSpace(m_State.Classification);
            m_ClassificationText.gameObject.SetActive(hasClassification);
            if (hasClassification)
            {
                SetRect(
                    m_ClassificationText.rectTransform,
                    kPadding,
                    y,
                    contentWidth,
                    kClassificationLineHeight);
                y += kClassificationLineHeight;
            }

            if (m_State.Compact)
            {
                SetRect(
                    m_MessageText.rectTransform,
                    kPadding,
                    y + 2f,
                    contentWidth,
                    kCompactMessageHeight);
                m_MessageText.gameObject.SetActive(true);
                m_StatusLabelText.gameObject.SetActive(false);
                m_StatusBlockRect.gameObject.SetActive(false);
                return;
            }

            m_MessageText.gameObject.SetActive(false);

            bool hasStatus = !string.IsNullOrWhiteSpace(m_State.TleStatus);
            m_StatusLabelText.gameObject.SetActive(hasStatus);
            m_StatusBlockRect.gameObject.SetActive(hasStatus);
            if (hasStatus)
            {
                SetRect(
                    m_StatusLabelText.rectTransform,
                    kPadding,
                    y,
                    contentWidth,
                    kStatusLabelHeight);
                y += kStatusLabelHeight + 3f;

                SetRect(
                    m_StatusBlockRect,
                    kPadding,
                    y,
                    contentWidth,
                    kStatusBlockHeight);
                SetRect(
                    m_StatusValueText.rectTransform,
                    12f,
                    11f,
                    contentWidth - 24f,
                    kStatusBlockHeight - 22f);
                y += kStatusBlockHeight + kSectionGap;
            }

            for (int index = 0; index < m_Rows.Length; index += 1)
            {
                if (!m_Rows[index].Root.gameObject.activeSelf)
                {
                    continue;
                }

                SetRect(m_Rows[index].Root, kPadding, y, contentWidth, kBodyLineHeight);
                SetRect(m_Rows[index].Label.rectTransform, 0f, 0f, kRowLabelWidth, kBodyLineHeight);
                SetRect(
                    m_Rows[index].Value.rectTransform,
                    kRowLabelWidth,
                    0f,
                    contentWidth - kRowLabelWidth,
                    kBodyLineHeight);
                y += kBodyLineHeight;
            }
        }

        private float CalculateHeight()
        {
            float height = kTitleBarHeight;
            if (m_Collapsed)
            {
                return height;
            }

            height += kPadding;
            if (!string.IsNullOrWhiteSpace(m_State.Classification))
            {
                height += kClassificationLineHeight;
            }

            if (m_State.Compact)
            {
                return height + kCompactMessageHeight + kPadding;
            }

            if (!string.IsNullOrWhiteSpace(m_State.TleStatus))
            {
                height += kStatusLabelHeight + 3f + kStatusBlockHeight + kSectionGap;
            }

            height += CountNonEmptyRows(
                m_State.RoleOrType,
                m_State.VehicleIndex,
                m_State.ViolationPending,
                m_State.Totals,
                m_State.LastReason,
                m_State.ResolvedEntity) * kBodyLineHeight;

            return height + kPadding;
        }

        private void CloseCurrentSelection()
        {
            m_DismissedSelectionToken = m_State.SelectionToken;
            ApplyState();
        }

        private void ToggleCollapsed()
        {
            m_Collapsed = !m_Collapsed;
            ApplyState();
        }

        private void ResetWindowPosition()
        {
            m_WindowPosition = new Vector2(
                Screen.width - kWidth - kRightInset,
                kTopInset);
            m_LastScreenSize = new Vector2(Screen.width, Screen.height);
        }

        private void ApplyWindowPosition()
        {
            m_PanelRect.anchoredPosition = new Vector2(m_WindowPosition.x, -m_WindowPosition.y);
        }

        private void ClampWindowPosition(float height)
        {
            float maxX = Mathf.Max(0f, Screen.width - kWidth);
            float maxY = Mathf.Max(0f, Screen.height - height);
            m_WindowPosition = new Vector2(
                Mathf.Clamp(m_WindowPosition.x, 0f, maxX),
                Mathf.Clamp(m_WindowPosition.y, 0f, maxY));
        }

        private bool IsDismissed()
        {
            return !string.IsNullOrWhiteSpace(m_State.SelectionToken) &&
                string.Equals(m_DismissedSelectionToken, m_State.SelectionToken);
        }

        private Font ResolveFont()
        {
            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtinFont != null)
            {
                return builtinFont;
            }

            return Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Malgun Gothic", "Arial" },
                16);
        }

        private Button CreateButton(
            RectTransform parent,
            string text,
            out Text buttonLabel)
        {
            RectTransform rect = CreateRect(text + "Button", parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = kButtonColor;
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = kButtonColor,
                highlightedColor = new Color(0.34f, 0.40f, 0.48f, 1f),
                pressedColor = new Color(0.20f, 0.24f, 0.31f, 1f),
                selectedColor = kButtonColor,
                disabledColor = new Color(0.20f, 0.24f, 0.31f, 0.65f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            buttonLabel = CreateText(
                text + "ButtonLabel",
                rect,
                11,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                kTitleColor);
            buttonLabel.raycastTarget = false;

            SetRect(buttonLabel.rectTransform, 0f, 0f, 0f, 0f, stretch: true);
            return button;
        }

        private Text CreateText(
            string name,
            RectTransform parent,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = m_Font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static void SetRect(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height,
            bool stretch = false)
        {
            if (stretch)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static int CountNonEmptyRows(params string[] values)
        {
            int count = 0;
            for (int index = 0; index < values.Length; index += 1)
            {
                if (!string.IsNullOrWhiteSpace(values[index]))
                {
                    count += 1;
                }
            }

            return count;
        }
    }

    internal sealed class SelectedObjectPanelDragHandle :
        MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IEndDragHandler
    {
        private SelectedObjectPanelView m_Owner;

        internal void Initialize(SelectedObjectPanelView owner)
        {
            m_Owner = owner;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_Owner?.BeginDrag(ToTopLeft(eventData.position));
        }

        public void OnDrag(PointerEventData eventData)
        {
            m_Owner?.DragTo(ToTopLeft(eventData.position));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_Owner?.EndDrag();
        }

        private static Vector2 ToTopLeft(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }
    }
}

