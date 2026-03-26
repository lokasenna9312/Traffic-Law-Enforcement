using UnityEngine;

namespace Traffic_Law_Enforcement
{
    internal sealed class SelectedVehiclePanelView : MonoBehaviour
    {
        internal struct State
        {
            public bool Visible;
            public bool Compact;
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

        private const float kWidth = 320f;
        private const float kTopInset = 88f;
        private const float kRightInset = 104f;
        private const float kPadding = 12f;
        private const float kHeaderLineHeight = 18f;
        private const float kBodyLineHeight = 18f;
        private const float kCompactHeight = 70f;

        private State m_State;

        private Texture2D m_BackgroundTexture;
        private GUIStyle m_CardStyle;
        private GUIStyle m_TitleStyle;
        private GUIStyle m_ClassificationStyle;
        private GUIStyle m_LabelStyle;
        private GUIStyle m_ValueStyle;
        private GUIStyle m_MessageStyle;
        private GUIStyle m_FooterStyle;

        internal void UpdateState(State state)
        {
            m_State = state;
            enabled = state.Visible;
        }

        private void Awake()
        {
            hideFlags = HideFlags.HideAndDontSave;
            CreateStyles();
            enabled = false;
        }

        private void OnDestroy()
        {
            if (m_BackgroundTexture != null)
            {
                Destroy(m_BackgroundTexture);
                m_BackgroundTexture = null;
            }
        }

        private void OnGUI()
        {
            if (!m_State.Visible)
            {
                return;
            }

            EnsureStyles();

            float height = CalculateHeight();
            Rect rect = new Rect(
                Screen.width - kWidth - kRightInset,
                kTopInset,
                kWidth,
                height);

            GUI.Box(rect, GUIContent.none, m_CardStyle);

            float x = rect.x + kPadding;
            float y = rect.y + kPadding;
            float contentWidth = rect.width - (kPadding * 2f);

            GUI.Label(
                new Rect(x, y, contentWidth, kHeaderLineHeight),
                "Selected Vehicle",
                m_TitleStyle);
            y += kHeaderLineHeight;

            if (!string.IsNullOrWhiteSpace(m_State.Classification))
            {
                GUI.Label(
                    new Rect(x, y, contentWidth, kHeaderLineHeight),
                    m_State.Classification,
                    m_ClassificationStyle);
                y += kHeaderLineHeight;
            }

            if (m_State.Compact)
            {
                if (!string.IsNullOrWhiteSpace(m_State.Message))
                {
                    GUI.Label(
                        new Rect(x, y + 4f, contentWidth, kBodyLineHeight),
                        m_State.Message,
                        m_MessageStyle);
                }

                return;
            }

            DrawRow(ref y, x, contentWidth, "TLE status", m_State.TleStatus);
            DrawRow(ref y, x, contentWidth, "Role / type", m_State.RoleOrType);
            DrawRow(ref y, x, contentWidth, "Vehicle index", m_State.VehicleIndex);
            DrawRow(ref y, x, contentWidth, "Violation / pending", m_State.ViolationPending);
            DrawRow(ref y, x, contentWidth, "Violations / fines", m_State.Totals);
            DrawRow(ref y, x, contentWidth, "Last reason", m_State.LastReason);
            DrawRow(ref y, x, contentWidth, "Resolved entity", m_State.ResolvedEntity, m_FooterStyle);
        }

        private float CalculateHeight()
        {
            if (m_State.Compact)
            {
                return kCompactHeight;
            }

            int bodyRowCount = CountNonEmptyRows(
                m_State.TleStatus,
                m_State.RoleOrType,
                m_State.VehicleIndex,
                m_State.ViolationPending,
                m_State.Totals,
                m_State.LastReason,
                m_State.ResolvedEntity);

            float headerHeight = kHeaderLineHeight;
            if (!string.IsNullOrWhiteSpace(m_State.Classification))
            {
                headerHeight += kHeaderLineHeight;
            }

            return
                (kPadding * 2f) +
                headerHeight +
                (bodyRowCount * kBodyLineHeight);
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

            float labelWidth = 112f;
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

        private void EnsureStyles()
        {
            if (m_CardStyle == null)
            {
                CreateStyles();
            }
        }

        private void CreateStyles()
        {
            if (m_BackgroundTexture == null)
            {
                m_BackgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
                m_BackgroundTexture.SetPixel(0, 0, new Color(0.07f, 0.1f, 0.14f, 0.82f));
                m_BackgroundTexture.Apply();
            }

            m_CardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(1, 1, 1, 1),
                normal =
                {
                    background = m_BackgroundTexture,
                    textColor = Color.white
                }
            };

            m_TitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = Color.white
                }
            };

            m_ClassificationStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = new Color(0.67f, 0.84f, 1f, 1f)
                }
            };

            m_LabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = new Color(0.74f, 0.8f, 0.86f, 1f)
                }
            };

            m_ValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = Color.white
                }
            };

            m_MessageStyle = new GUIStyle(m_ValueStyle)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            m_FooterStyle = new GUIStyle(m_ValueStyle)
            {
                fontSize = 10,
                normal =
                {
                    textColor = new Color(0.82f, 0.86f, 0.9f, 0.95f)
                }
            };
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
