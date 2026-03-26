using UnityEngine;

namespace Traffic_Law_Enforcement
{
    internal sealed class SelectedVehiclePanelView : MonoBehaviour
    {
        private const float kWidth = 456f;
        private const float kTopInset = 78f;
        private const float kRightInset = 88f;
        private const float kPadding = 16f;
        private const float kTitleBarHeight = 36f;
        private const float kTitleLineHeight = 22f;
        private const float kClassificationLineHeight = 24f;
        private const float kBodyLineHeight = 23f;
        private const float kCompactMinHeight = 118f;
        private const float kButtonSize = 20f;
        private const float kRowLabelWidth = 144f;
        private const float kSectionGap = 10f;
        private const float kStatusLabelHeight = 17f;
        private const float kStatusBlockPadding = 10f;

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

        private Texture2D m_BodyTexture;
        private Texture2D m_TitleBarTexture;
        private Texture2D m_ButtonTexture;
        private Texture2D m_StatusTexture;
        private GUIStyle m_WindowStyle;
        private GUIStyle m_TitleBarStyle;
        private GUIStyle m_TitleStyle;
        private GUIStyle m_ButtonStyle;
        private GUIStyle m_ClassificationStyle;
        private GUIStyle m_StatusBlockStyle;
        private GUIStyle m_StatusLabelStyle;
        private GUIStyle m_StatusValueStyle;
        private GUIStyle m_LabelStyle;
        private GUIStyle m_ValueStyle;
        private GUIStyle m_MessageStyle;
        private GUIStyle m_FooterStyle;

        internal void UpdateState(State state)
        {
            if (!string.Equals(m_LastSelectionToken, state.SelectionToken))
            {
                m_LastSelectionToken = state.SelectionToken;
                m_DismissedSelectionToken = string.Empty;
                m_Collapsed = false;
            }

            m_State = state;
            enabled = state.Visible && !IsDismissed();
        }

        private void Awake()
        {
            hideFlags = HideFlags.HideAndDontSave;
            ResetWindowPosition();
            enabled = false;
        }

        private void OnDestroy()
        {
            if (m_BodyTexture != null)
            {
                Destroy(m_BodyTexture);
                m_BodyTexture = null;
            }

            if (m_TitleBarTexture != null)
            {
                Destroy(m_TitleBarTexture);
                m_TitleBarTexture = null;
            }

            if (m_ButtonTexture != null)
            {
                Destroy(m_ButtonTexture);
                m_ButtonTexture = null;
            }

            if (m_StatusTexture != null)
            {
                Destroy(m_StatusTexture);
                m_StatusTexture = null;
            }
        }

        private void OnGUI()
        {
            if (!m_State.Visible || IsDismissed())
            {
                return;
            }

            EnsureStyles();

            float height = CalculateHeight();
            Rect rect = GetWindowRect(height);

            GUI.Box(rect, GUIContent.none, m_WindowStyle);

            Rect titleBarRect = new Rect(rect.x, rect.y, rect.width, kTitleBarHeight);
            GUI.Box(titleBarRect, GUIContent.none, m_TitleBarStyle);

            Rect closeButtonRect = new Rect(
                titleBarRect.xMax - kPadding - kButtonSize,
                titleBarRect.y + 8f,
                kButtonSize,
                kButtonSize);
            Rect collapseButtonRect = new Rect(
                closeButtonRect.x - 6f - kButtonSize,
                closeButtonRect.y,
                kButtonSize,
                kButtonSize);
            Rect dragHandleRect = new Rect(
                titleBarRect.x + 4f,
                titleBarRect.y + 4f,
                Mathf.Max(0f, collapseButtonRect.x - titleBarRect.x - 12f),
                titleBarRect.height - 8f);

            HandleDrag(dragHandleRect, height);

            if (GUI.Button(closeButtonRect, "X", m_ButtonStyle))
            {
                m_DismissedSelectionToken = m_State.SelectionToken;
                enabled = false;
                return;
            }

            if (GUI.Button(collapseButtonRect, m_Collapsed ? "+" : "-", m_ButtonStyle))
            {
                m_Collapsed = !m_Collapsed;
                return;
            }

            float x = rect.x + kPadding;
            float y = titleBarRect.y + 7f;
            float contentWidth = rect.width - (kPadding * 2f);
            float titleWidth = contentWidth - ((kButtonSize * 2f) + 14f);

            GUI.Label(
                new Rect(x, y, titleWidth, kTitleLineHeight),
                "Selected Vehicle",
                m_TitleStyle);
            y = titleBarRect.yMax + kPadding;

            if (m_Collapsed)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(m_State.Classification))
            {
                GUI.Label(
                    new Rect(x, y, contentWidth, kClassificationLineHeight),
                    m_State.Classification,
                    m_ClassificationStyle);
                y += kClassificationLineHeight;
            }

            if (m_State.Compact)
            {
                if (!string.IsNullOrWhiteSpace(m_State.Message))
                {
                    float messageHeight = m_MessageStyle.CalcHeight(
                        new GUIContent(m_State.Message),
                        contentWidth);
                    GUI.Label(
                        new Rect(x, y + 2f, contentWidth, messageHeight),
                        m_State.Message,
                        m_MessageStyle);
                }

                return;
            }

            DrawStatusBlock(ref y, x, contentWidth, m_State.TleStatus);
            y += kSectionGap;

            DrawRow(ref y, x, contentWidth, "Role / type", m_State.RoleOrType);
            DrawRow(ref y, x, contentWidth, "Vehicle index", m_State.VehicleIndex);
            DrawRow(ref y, x, contentWidth, "Violation / pending", m_State.ViolationPending);
            DrawRow(ref y, x, contentWidth, "Violations / fines", m_State.Totals);
            DrawRow(ref y, x, contentWidth, "Last reason", m_State.LastReason);
            DrawRow(ref y, x, contentWidth, "Resolved entity", m_State.ResolvedEntity, m_FooterStyle);
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
                float messageHeight = m_MessageStyle.CalcHeight(
                    new GUIContent(m_State.Message ?? string.Empty),
                    kWidth - (kPadding * 2f));

                return Mathf.Max(
                    kCompactMinHeight,
                    height + messageHeight + kPadding);
            }

            height += GetStatusBlockHeight(kWidth - (kPadding * 2f), m_State.TleStatus);
            height += kSectionGap;
            height += CountNonEmptyRows(
                m_State.RoleOrType,
                m_State.VehicleIndex,
                m_State.ViolationPending,
                m_State.Totals,
                m_State.LastReason,
                m_State.ResolvedEntity) * kBodyLineHeight;

            return height + kPadding;
        }

        private void DrawRow(
            ref float y,
            float x,
            float width,
            string label,
            string value,
            GUIStyle valueStyle = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            float labelWidth = kRowLabelWidth;
            GUI.Label(
                new Rect(x, y, labelWidth, kBodyLineHeight),
                label,
                m_LabelStyle);
            GUI.Label(
                new Rect(x + labelWidth, y, width - labelWidth, kBodyLineHeight),
                value,
                valueStyle ?? m_ValueStyle);
            y += kBodyLineHeight;
        }

        private void DrawStatusBlock(
            ref float y,
            float x,
            float width,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            GUI.Label(
                new Rect(x, y, width, kStatusLabelHeight),
                "TLE status",
                m_StatusLabelStyle);
            y += kStatusLabelHeight;

            float blockY = y + 2f;
            float innerWidth = width - (kStatusBlockPadding * 2f);
            float valueHeight = m_StatusValueStyle.CalcHeight(
                new GUIContent(value),
                innerWidth);
            float blockHeight = valueHeight + (kStatusBlockPadding * 2f);

            GUI.Box(
                new Rect(x, blockY, width, blockHeight),
                GUIContent.none,
                m_StatusBlockStyle);
            GUI.Label(
                new Rect(
                    x + kStatusBlockPadding,
                    blockY + kStatusBlockPadding,
                    innerWidth,
                    valueHeight),
                value,
                m_StatusValueStyle);
            y = blockY + blockHeight;
        }

        private float GetStatusBlockHeight(float width, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0f;
            }

            float innerWidth = width - (kStatusBlockPadding * 2f);
            float valueHeight = m_StatusValueStyle.CalcHeight(
                new GUIContent(value),
                innerWidth);
            return kStatusLabelHeight + 2f + valueHeight + (kStatusBlockPadding * 2f);
        }

        private Rect GetWindowRect(float height)
        {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (m_LastScreenSize != screenSize)
            {
                if (!m_HasCustomPosition)
                {
                    ResetWindowPosition();
                }

                m_LastScreenSize = screenSize;
            }

            ClampWindowPosition(height);
            return new Rect(m_WindowPosition.x, m_WindowPosition.y, kWidth, height);
        }

        private void HandleDrag(Rect dragHandleRect, float height)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            Vector2 mousePosition = currentEvent.mousePosition;
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0 && dragHandleRect.Contains(mousePosition))
                    {
                        m_IsDragging = true;
                        m_HasCustomPosition = true;
                        m_DragOffset = mousePosition - m_WindowPosition;
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (m_IsDragging)
                    {
                        m_WindowPosition = mousePosition - m_DragOffset;
                        ClampWindowPosition(height);
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (currentEvent.button == 0 && m_IsDragging)
                    {
                        m_IsDragging = false;
                        currentEvent.Use();
                    }
                    break;
            }
        }

        private void EnsureStyles()
        {
            if (m_WindowStyle == null)
            {
                CreateStyles();
            }
        }

        private void CreateStyles()
        {
            if (m_BodyTexture == null)
            {
                m_BodyTexture = CreateSolidTexture(new Color(0.09f, 0.13f, 0.18f, 0.9f));
            }

            if (m_TitleBarTexture == null)
            {
                m_TitleBarTexture = CreateSolidTexture(new Color(0.15f, 0.19f, 0.25f, 0.98f));
            }

            if (m_ButtonTexture == null)
            {
                m_ButtonTexture = CreateSolidTexture(new Color(0.23f, 0.28f, 0.36f, 1f));
            }

            if (m_StatusTexture == null)
            {
                m_StatusTexture = CreateSolidTexture(new Color(0.14f, 0.18f, 0.24f, 0.96f));
            }

            m_WindowStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(1, 1, 1, 1),
                normal =
                {
                    background = m_BodyTexture,
                    textColor = Color.white
                }
            };

            m_TitleBarStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                normal =
                {
                    background = m_TitleBarTexture,
                    textColor = Color.white
                }
            };

            m_TitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = new Color(0.97f, 0.98f, 1f, 1f)
                }
            };

            m_ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(1, 1, 1, 1),
                normal =
                {
                    background = m_ButtonTexture,
                    textColor = new Color(0.95f, 0.97f, 1f, 1f)
                }
            };

            m_ClassificationStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = new Color(0.69f, 0.87f, 1f, 1f)
                }
            };

            m_StatusBlockStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(1, 1, 1, 1),
                normal =
                {
                    background = m_StatusTexture,
                    textColor = Color.white
                }
            };

            m_StatusLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = new Color(0.75f, 0.81f, 0.89f, 1f)
                }
            };

            m_StatusValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = Color.white
                }
            };

            m_LabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = new Color(0.74f, 0.8f, 0.86f, 1f)
                }
            };

            m_ValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = Color.white
                }
            };

            m_MessageStyle = new GUIStyle(m_ValueStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            m_FooterStyle = new GUIStyle(m_ValueStyle)
            {
                fontSize = 12,
                normal =
                {
                    textColor = new Color(0.82f, 0.86f, 0.9f, 0.95f)
                }
            };
        }

        private bool IsDismissed()
        {
            return !string.IsNullOrWhiteSpace(m_State.SelectionToken) &&
                string.Equals(m_DismissedSelectionToken, m_State.SelectionToken);
        }

        private void ResetWindowPosition()
        {
            m_WindowPosition = new Vector2(
                Screen.width - kWidth - kRightInset,
                kTopInset);
            m_LastScreenSize = new Vector2(Screen.width, Screen.height);
        }

        private void ClampWindowPosition(float height)
        {
            float maxX = Mathf.Max(0f, Screen.width - kWidth);
            float maxY = Mathf.Max(0f, Screen.height - height);
            m_WindowPosition = new Vector2(
                Mathf.Clamp(m_WindowPosition.x, 0f, maxX),
                Mathf.Clamp(m_WindowPosition.y, 0f, maxY));
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
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
}
