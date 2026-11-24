using UnityEngine;
using UnityEngine.UI;
using ARMuseum.Audio;
using ARMuseum.Exhibits;
using ARMuseum.Museum;
using ARMuseum.Welcome;

namespace ARMuseum.Details
{
    /// <summary>
    /// Exhibit details screen between the welcome screen and AR. Opened for the selected exhibit,
    /// shows its image, history, significance, symbolism and an audio guide. Back returns to the
    /// welcome screen with the selection unchanged. VIEW IN AR stops narration and enters AR through
    /// the welcome controller, which calls the existing ARExperienceGate.
    /// Uses NarrationService; does not touch ExhibitInfoPanel.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExhibitDetailsScreen : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MuseumExhibitManager manager;
        [SerializeField] private WelcomeScreenController welcome;
        [SerializeField] private NarrationService narrator;

        [Header("Root")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Header and footer")]
        [SerializeField] private Button backButton;
        [SerializeField] private Text categoryText;
        [SerializeField] private Button viewInArButton;

        [Header("Hero and content")]
        [SerializeField] private Image heroImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text periodText;
        [SerializeField] private Text introText;
        [SerializeField] private Text historyBody;
        [SerializeField] private Text significanceBody;
        [SerializeField] private RectTransform symbolismContainer;

        [Header("Audio guide")]
        [SerializeField] private Button listenButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Text narrationStatusText;

        [SerializeField] private Font labelFont;

        private static readonly Color s_Gold = new Color(0.788f, 0.663f, 0.431f, 1f);
        private static readonly Color s_Ivory = new Color(0.953f, 0.922f, 0.847f, 1f);

        private bool m_Open;
        private int m_Index = -1;

        public bool IsOpen => m_Open;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(Back);
            if (viewInArButton != null) viewInArButton.onClick.AddListener(ViewInAR);
            if (listenButton != null) listenButton.onClick.AddListener(Listen);
            if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (stopButton != null) stopButton.onClick.AddListener(Stop);

            SetVisible(false);
        }

        private void Update()
        {
            if (m_Open)
                RefreshAudioUI();
        }

        /// <summary>Opens the details screen for the exhibit at the given index.</summary>
        public void Show(int index)
        {
            ExhibitData exhibit = manager != null ? manager.GetExhibit(index) : null;
            if (exhibit == null)
                return;

            // A different exhibit's narration must not carry over into this one.
            if (narrator != null && narrator.CurrentKey != null && narrator.CurrentKey != exhibit)
                narrator.Stop();

            m_Index = index;
            Populate(exhibit, index);
            RefreshAudioUI();
            SetVisible(true);

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        /// <summary>Closes the details screen without changing the selection.</summary>
        public void Hide()
        {
            if (narrator != null)
                narrator.Stop();

            SetVisible(false);
        }

        private void Back()
        {
            Hide();
            if (welcome != null)
                welcome.ShowWelcome();
        }

        private void ViewInAR()
        {
            // Narration must not continue into AR. The details screen closes first, then the
            // welcome controller re-shows its canvas and runs the existing entry flow.
            Hide();
            if (welcome != null)
                welcome.EnterARFromDetails();
        }

        private void Listen()
        {
            ExhibitData exhibit = CurrentExhibit();
            if (narrator != null && exhibit != null)
                narrator.Play(exhibit.narration, exhibit.narrationClip, exhibit);
        }

        private void Pause()
        {
            if (narrator != null) narrator.Pause();
        }

        private void Resume()
        {
            if (narrator != null) narrator.Resume();
        }

        private void Stop()
        {
            if (narrator != null) narrator.Stop();
        }

        private ExhibitData CurrentExhibit()
        {
            return manager != null && m_Index >= 0 ? manager.GetExhibit(m_Index) : null;
        }

        private void RefreshAudioUI()
        {
            ExhibitData exhibit = CurrentExhibit();
            bool hasClip = exhibit != null && exhibit.narrationClip != null;
            bool hasText = exhibit != null && !string.IsNullOrWhiteSpace(exhibit.narration);
            NarrationState state = narrator != null ? narrator.State : NarrationState.Unavailable;

            SetInteractable(listenButton, narrator != null && narrator.CanPlay(hasClip, hasText));
            SetInteractable(pauseButton, state == NarrationState.Playing);
            SetInteractable(resumeButton, state == NarrationState.Paused);
            SetInteractable(stopButton, state == NarrationState.Playing || state == NarrationState.Paused);

            if (narrationStatusText != null && narrator != null)
                narrationStatusText.text = "Narration: " + narrator.GetStatus(hasClip, hasText);
        }

        private void Populate(ExhibitData exhibit, int index)
        {
            if (categoryText != null)
                categoryText.text = (index + 1).ToString("00") + "  \u00B7  " + (exhibit.category ?? string.Empty).ToUpperInvariant();

            if (heroImage != null)
            {
                heroImage.sprite = exhibit.detailImage;
                heroImage.color = exhibit.detailImage != null ? Color.white : new Color(0.11f, 0.105f, 0.1f, 1f);
            }

            SetText(titleText, exhibit.DisplayName);
            SetText(periodText, (exhibit.period ?? string.Empty).ToUpperInvariant());
            SetText(introText, exhibit.introduction);
            SetText(historyBody, exhibit.historicalContext);
            SetText(significanceBody, exhibit.culturalSignificance);
            BuildSymbolism(exhibit);
        }

        private void BuildSymbolism(ExhibitData exhibit)
        {
            if (symbolismContainer == null)
                return;

            for (int i = symbolismContainer.childCount - 1; i >= 0; i--)
                Destroy(symbolismContainer.GetChild(i).gameObject);

            if (exhibit.symbolism == null)
                return;

            foreach (SymbolEntry entry in exhibit.symbolism)
            {
                if (entry == null)
                    continue;

                var row = new GameObject("Symbol", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                row.transform.SetParent(symbolismContainer, false);
                var column = row.GetComponent<VerticalLayoutGroup>();
                column.spacing = 4f;
                column.childControlWidth = true;
                column.childControlHeight = true;
                column.childForceExpandWidth = true;
                column.childForceExpandHeight = false;
                row.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                CreateText(row.transform, entry.label, 21, s_Gold, true);
                CreateText(row.transform, entry.meaning, 20, s_Ivory, false);
            }
        }

        private void CreateText(Transform parent, string value, int size, Color color, bool bold)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = labelFont != null ? labelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value ?? string.Empty;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.12f;
            text.raycastTarget = false;
            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void SetVisible(bool visible)
        {
            m_Open = visible;
            if (canvas != null)
                canvas.enabled = visible;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }

        private static void SetInteractable(Button button, bool value)
        {
            if (button != null && button.interactable != value)
                button.interactable = value;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }
    }
}
