using UnityEngine;

namespace ARMuseum.UI
{
    /// <summary>Stretches a RectTransform to the device safe area (notches, rounded corners, gesture bars).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform m_Rect;
        private Rect m_LastSafeArea;
        private int m_LastWidth;
        private int m_LastHeight;

        private void Awake()
        {
            m_Rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != m_LastSafeArea || Screen.width != m_LastWidth || Screen.height != m_LastHeight)
                Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            m_LastSafeArea = safe;
            m_LastWidth = Screen.width;
            m_LastHeight = Screen.height;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            m_Rect.anchorMin = min;
            m_Rect.anchorMax = max;
            m_Rect.offsetMin = Vector2.zero;
            m_Rect.offsetMax = Vector2.zero;
        }
    }
}
