using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ARMuseum.Audio;
using ARMuseum.Exhibits;

namespace ARMuseum.UI
{
    /// <summary>
    /// World-space museum placard for an exhibit. The card lives on a World Space canvas anchored to the
    /// artifact: it floats beside the statue on the viewer's right, billboards toward the AR camera so the
    /// text stays readable, and keeps a clear gap from the model. The side direction is derived from the
    /// camera's position relative to the artifact (not from where the phone is pointing), so turning the
    /// phone in place does not move the card, while walking around the statue swings it to stay on the
    /// viewer's right. Starts hidden; fades and pops in/out.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExhibitInfoPanel : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform sheet;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text categoryText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text symbolismText;
        [SerializeField] private Text historyText;
        [SerializeField] private Text provenanceText;

        [Header("Expandable details")]
        [SerializeField] private Button detailsToggle;
        [SerializeField] private Text detailsToggleLabel;
        [SerializeField] private GameObject[] detailObjects;

        [Header("Artifact rotation")]
        [SerializeField] private Button rotateLeftButton;
        [SerializeField] private Button rotateRightButton;
        [SerializeField] private Button resetRotationButton;

        [Header("Narration")]
        [SerializeField] private NarrationService narrator;
        [SerializeField] private Button listenButton;
        [SerializeField] private Button pauseNarrationButton;
        [SerializeField] private Button resumeNarrationButton;
        [SerializeField] private Button stopNarrationButton;
        [SerializeField] private Text narrationStatusText;
        private bool m_HasNarrationStatus;
        private NarrationState m_LastNarrationState;
        private bool m_LastHasClip;
        private bool m_LastHasText;

        [Header("World anchoring")]
        [SerializeField] private Camera viewCamera;
        [Tooltip("Gap (metres) between the statue's silhouette and the card's left edge.")]
        [SerializeField] private float sideGap = 0.04f;
        [Tooltip("Raise the card's centre above the statue's centre (metres).")]
        [SerializeField] private float verticalOffset = 0.01f;
        [Tooltip("Minimum clearance (metres) between the card's bottom edge and the floor.")]
        [SerializeField] private float floorClearance = 0.02f;
        [Tooltip("Camera distance (metres) at which the card is shown at its authored size.")]
        [SerializeField] private float referenceDistance = 0.9f;
        [SerializeField] private float minDistanceScale = 0.8f;
        [SerializeField] private float maxDistanceScale = 1.3f;
        [Tooltip("How quickly the card follows when the viewer walks around the statue.")]
        [SerializeField] private float followSmoothing = 8f;

        [Header("Animation")]
        [SerializeField] private float animationDuration = 0.25f;
        [SerializeField] private float popScale = 0.88f;

        private Canvas m_Canvas;
        private RectTransform m_CanvasRect;
        private ExhibitArtifact m_Anchor;
        private ArtifactTurntable m_Turntable;
        private float m_BaseScale = 0.0005f;
        private float m_Progress;   // 0 = hidden, 1 = fully shown
        private bool m_Target;      // desired state
        private bool m_DetailsExpanded;
        private bool m_Snap;
        private bool m_HasRight;
        private Vector3 m_Right = Vector3.right;
        private float m_DistanceScale = 1f;
        private readonly StringBuilder m_Builder = new StringBuilder();

        public bool IsOpen => m_Target;

        private void Awake()
        {
            Canvas own = GetComponentInParent<Canvas>();
            m_Canvas = own != null ? own.rootCanvas : null;
            if (m_Canvas == null)
            {
                Debug.LogError("ExhibitInfoPanel needs to live under a Canvas.", this);
                enabled = false;
                return;
            }

            if (m_Canvas.renderMode != RenderMode.WorldSpace)
                Debug.LogWarning("ExhibitInfoPanel expects a World Space canvas.", m_Canvas);

            m_CanvasRect = (RectTransform)m_Canvas.transform;
            if (m_CanvasRect.localScale.x > 0f)
                m_BaseScale = m_CanvasRect.localScale.x;

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
            if (detailsToggle != null)
                detailsToggle.onClick.AddListener(ToggleDetails);
            if (rotateLeftButton != null)
                rotateLeftButton.onClick.AddListener(RotateLeft);
            if (rotateRightButton != null)
                rotateRightButton.onClick.AddListener(RotateRight);
            if (resetRotationButton != null)
                resetRotationButton.onClick.AddListener(ResetRotation);
            if (listenButton != null)
                listenButton.onClick.AddListener(PlayNarration);
            if (pauseNarrationButton != null)
                pauseNarrationButton.onClick.AddListener(PauseNarration);
            if (resumeNarrationButton != null)
                resumeNarrationButton.onClick.AddListener(ResumeNarration);
            if (stopNarrationButton != null)
                stopNarrationButton.onClick.AddListener(StopNarration);

            SetDetailsExpanded(false);
            m_Target = false;
            m_Progress = 0f;
            ApplyProgress();
        }

        private void OnEnable()
        {
            // AR camera poses are often applied just before rendering; re-aim the card then so it never lags a frame.
            Application.onBeforeRender += HandleBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= HandleBeforeRender;
        }

        private void Update()
        {
            RefreshNarrationUI();
            if (m_Target && m_Anchor == null)
                m_Target = false;   // artifact was destroyed while the card was open

            float target = m_Target ? 1f : 0f;
            if (Mathf.Approximately(m_Progress, target))
                return;

            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, animationDuration);
            m_Progress = Mathf.MoveTowards(m_Progress, target, step);
            ApplyProgress();
        }

        private void LateUpdate()
        {
            UpdatePlacement(Time.unscaledDeltaTime);
        }

        private void HandleBeforeRender()
        {
            UpdatePlacement(0f);
        }

        public void Show(ExhibitData data, ExhibitArtifact artifact)
        {
            if (data == null || artifact == null || m_Canvas == null)
                return;

            m_Anchor = artifact;
            m_Turntable = artifact.GetComponent<ArtifactTurntable>();
            Populate(data);
            SetDetailsExpanded(false);

            m_Canvas.enabled = true;
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;

            m_Snap = true;
            m_Target = true;
            UpdatePlacement(0f);
            ApplyProgress();
        }

        public void Hide()
        {
            // Closing the card ends narration so it never plays unseen or overlaps a later session.
            if (narrator != null)
                narrator.Stop();
            m_Target = false;
        }

        /// <summary>Turns the open artifact one step to the left (no effect if the artifact has no turntable).</summary>
        public void RotateLeft()
        {
            if (m_Turntable != null)
                m_Turntable.RotateStep(-1);
        }

        /// <summary>Turns the open artifact one step to the right (no effect if the artifact has no turntable).</summary>
        public void RotateRight()
        {
            if (m_Turntable != null)
                m_Turntable.RotateStep(1);
        }

        /// <summary>Animates the open artifact back to its initial orientation.</summary>
        public void ResetRotation()
        {
            if (m_Turntable != null)
                m_Turntable.ResetRotation();
        }

        /// <summary>Starts narration of the open exhibit from the beginning.</summary>
/// <summary>Starts narration of the open exhibit from the beginning (recorded clip if assigned, else text-to-speech).</summary>
        public void PlayNarration()
        {
            if (narrator == null || m_Anchor == null || m_Anchor.Data == null)
                return;
            narrator.Play(m_Anchor.Data.narration, m_Anchor.Data.narrationClip, m_Anchor.Data);
        }

        public void PauseNarration()
        {
            if (narrator != null)
                narrator.Pause();
        }

        public void ResumeNarration()
        {
            if (narrator != null)
                narrator.Resume();
        }

        public void StopNarration()
        {
            if (narrator != null)
                narrator.Stop();
        }

        /// <summary>Enables only the narration buttons that make sense for the current state, and shows its status.</summary>
/// <summary>Enables only the narration buttons that make sense for the current state, and shows its status.</summary>
        private void RefreshNarrationUI()
        {
            if (narrator == null)
                return;

            ExhibitData data = m_Anchor != null ? m_Anchor.Data : null;
            bool hasClip = data != null && data.narrationClip != null;
            bool hasText = data != null && !string.IsNullOrWhiteSpace(data.narration);
            NarrationState s = narrator.State;

            SetInteractable(listenButton, narrator.CanPlay(hasClip, hasText));
            SetInteractable(pauseNarrationButton, s == NarrationState.Playing);
            SetInteractable(resumeNarrationButton, s == NarrationState.Paused);
            SetInteractable(stopNarrationButton, s == NarrationState.Playing || s == NarrationState.Paused);

            bool changed = !m_HasNarrationStatus || m_LastNarrationState != s
                           || m_LastHasClip != hasClip || m_LastHasText != hasText;
            if (narrationStatusText != null && changed)
            {
                // Only rebuild the status string when something it depends on changes, not every frame.
                m_HasNarrationStatus = true;
                m_LastNarrationState = s;
                m_LastHasClip = hasClip;
                m_LastHasText = hasText;
                narrationStatusText.text = "Narration: " + narrator.GetStatus(hasClip, hasText);
            }
        }

        private static void SetInteractable(Button button, bool value)
        {
            if (button != null && button.interactable != value)
                button.interactable = value;
        }

        public void ToggleDetails()
        {
            SetDetailsExpanded(!m_DetailsExpanded);
        }

        private void SetDetailsExpanded(bool expanded)
        {
            m_DetailsExpanded = expanded;

            if (detailObjects != null)
            {
                for (int i = 0; i < detailObjects.Length; i++)
                {
                    if (detailObjects[i] != null)
                        detailObjects[i].SetActive(expanded);
                }
            }

            if (detailsToggleLabel != null)
                detailsToggleLabel.text = expanded ? "HIDE DETAILS  \u2013" : "SYMBOLISM \u00B7 HISTORY \u00B7 PROVENANCE  +";

            if (scrollRect != null && scrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                if (!expanded)
                    scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private Camera ResolveCamera()
        {
            if (viewCamera == null || !viewCamera.isActiveAndEnabled)
                viewCamera = Camera.main;

            if (viewCamera != null && m_Canvas.worldCamera != viewCamera)
                m_Canvas.worldCamera = viewCamera;

            return viewCamera;
        }

        /// <summary>
        /// Positions the world-space canvas relative to the artifact and camera. dt = 0 re-aims using the
        /// latest camera pose without advancing the smoothing state.
        /// </summary>
        private void UpdatePlacement(float dt)
        {
            if (m_Anchor == null || m_Canvas == null || !m_Canvas.enabled)
                return;

            Camera cam = ResolveCamera();
            if (cam == null)
                return;

            Vector3 camPos = cam.transform.position;
            Vector3 center = m_Anchor.AnchorPoint;
            float t = m_Snap ? 1f : 1f - Mathf.Exp(-followSmoothing * dt);

            // Viewer's right, perpendicular to the camera -> statue line on the horizontal plane.
            Vector3 flat = center - camPos;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0004f)
            {
                Vector3 targetRight = Vector3.Cross(Vector3.up, flat.normalized);
                Vector3 blended = m_HasRight ? Vector3.Slerp(m_Right, targetRight, t) : targetRight;
                if (blended.sqrMagnitude > 0.0001f)
                    m_Right = blended.normalized;
                m_HasRight = true;
            }
            else if (!m_HasRight)
            {
                Vector3 fallback = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up);
                m_Right = fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.right;
                m_HasRight = true;
            }

            // Mild distance scaling so the card stays legible when the viewer steps back.
            float distance = Vector3.Distance(camPos, center);
            float targetScale = Mathf.Clamp(distance / Mathf.Max(0.05f, referenceDistance), minDistanceScale, maxDistanceScale);
            m_DistanceScale = m_Snap ? targetScale : Mathf.Lerp(m_DistanceScale, targetScale, t);

            Vector2 size = m_CanvasRect.rect.size;
            float worldWidth = size.x * m_BaseScale * m_DistanceScale;
            float worldHeight = size.y * m_BaseScale * m_DistanceScale;

            // The canvas pivot is its left-centre edge, so the gap from the silhouette is exact at any card size.
            float half = m_Anchor.ProjectedHalfExtent(m_Right);
            Vector3 position = center + m_Right * (half + sideGap * m_DistanceScale);
            float minY = m_Anchor.transform.position.y + worldHeight * 0.5f + floorClearance;
            position.y = Mathf.Max(center.y + verticalOffset, minY);

            // Billboard: the canvas's +Z points away from the viewer; world-up is kept so the text stays upright.
            Vector3 cardCenter = position + m_Right * (worldWidth * 0.5f);
            Vector3 toCard = cardCenter - camPos;
            if (toCard.sqrMagnitude > 0.000001f)
                m_CanvasRect.rotation = Quaternion.LookRotation(toCard.normalized, Vector3.up);

            m_CanvasRect.position = position;

            float eased = 1f - Mathf.Pow(1f - m_Progress, 3f);
            m_CanvasRect.localScale = Vector3.one * (m_BaseScale * m_DistanceScale * Mathf.Lerp(popScale, 1f, eased));

            m_Snap = false;
        }

        private void ApplyProgress()
        {
            if (m_Canvas == null)
                return;

            float eased = 1f - Mathf.Pow(1f - m_Progress, 3f);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = eased;
                canvasGroup.interactable = m_Target;
                canvasGroup.blocksRaycasts = m_Target;
            }

            m_Canvas.enabled = m_Target || m_Progress > 0f;
        }

        private void Populate(ExhibitData data)
        {
            SetText(titleText, data.title);
            SetText(subtitleText, data.subtitle);
            SetText(categoryText, data.category);
            SetText(descriptionText, data.description);
            SetText(historyText, data.historicalContext);
            SetText(provenanceText, data.provenanceNote);

            m_Builder.Length = 0;
            if (data.symbolism != null)
            {
                for (int i = 0; i < data.symbolism.Length; i++)
                {
                    var entry = data.symbolism[i];
                    if (entry == null)
                        continue;
                    if (m_Builder.Length > 0)
                        m_Builder.Append("\n\n");
                    m_Builder.Append("<b>").Append(entry.label).Append("</b>\n").Append(entry.meaning);
                }
            }
            SetText(symbolismText, m_Builder.ToString());
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }
    }
}
