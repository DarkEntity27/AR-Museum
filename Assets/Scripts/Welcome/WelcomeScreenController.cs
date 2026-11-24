using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ARMuseum.Exhibits;
using ARMuseum.Museum;

namespace ARMuseum.Welcome
{
    /// <summary>
    /// Pre-AR welcome screen: a featured exhibit followed by a collection of cards. Cards and the
    /// featured block both reflect MuseumExhibitManager's selection; tapping a card selects that exhibit.
    /// ENTER AR MUSEUM calls ARExperienceGate, then fades this screen out and hides it.
    /// Exhibit data and thumbnails are read only.
    /// </summary>
    [DisallowMultipleComponent]
    public class WelcomeScreenController : MonoBehaviour
    {
        [SerializeField] private MuseumExhibitManager manager;
        [SerializeField] private ARExperienceGate gate;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button enterButton;
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Font labelFont;
        [SerializeField] private float fadeDuration = 0.35f;

        [Tooltip("Thumbnail render for each exhibit, in MuseumExhibitManager order.")]
        [SerializeField] private Sprite[] thumbnails;

        [Tooltip("Style or tradition line for each exhibit, in MuseumExhibitManager order.")]
        [SerializeField] private string[] traditions;

        [SerializeField] private int featuredIntroLength = 150;
        [SerializeField] private int cardDescriptionLength = 110;

        // Render backdrop colour of the thumbnails, so the featured image blends into its frame.
        private static readonly Color s_RenderBackdrop = new Color(0.11f, 0.105f, 0.1f, 1f);
        private static readonly Color s_CardBackground = new Color(0.125f, 0.12f, 0.115f, 1f);
        private static readonly Color s_Gold = new Color(0.788f, 0.663f, 0.431f, 1f);
        private static readonly Color s_Bronze = new Color(0.69f, 0.553f, 0.341f, 1f);
        private static readonly Color s_Ivory = new Color(0.953f, 0.922f, 0.847f, 1f);
        private static readonly Color s_Muted = new Color(0.953f, 0.922f, 0.847f, 0.75f);
        private static readonly Color s_Empty = new Color(0.16f, 0.15f, 0.14f, 1f);

        private readonly List<CardView> m_Cards = new List<CardView>();
        private FeaturedView m_Featured;
        private bool m_Entering;

        private class CardView
        {
            public int Index;
            public Outline Outline;
        }

        private class FeaturedView
        {
            public Image Image;
            public Text Tradition;
            public Text Title;
            public Text Intro;
        }

        private void Start()
        {
            if (manager == null)
                manager = FindAnyObjectByType<MuseumExhibitManager>();

            if (manager == null || cardContainer == null)
            {
                Debug.LogError("WelcomeScreenController: needs a MuseumExhibitManager and a content container.", this);
                return;
            }

            if (enterButton != null)
                enterButton.onClick.AddListener(OnEnterPressed);

            Clear();
            BuildFeatured();
            BuildCollection();

            manager.SelectionChanged += RefreshSelection;
            RefreshSelection();
        }

        private void OnDestroy()
        {
            if (manager != null)
                manager.SelectionChanged -= RefreshSelection;
        }

        private void Clear()
        {
            for (int i = cardContainer.childCount - 1; i >= 0; i--)
                Destroy(cardContainer.GetChild(i).gameObject);
            m_Cards.Clear();
            m_Featured = null;
        }

        // ---------- Featured ----------

        private void BuildFeatured()
        {
            // Plain rectangle: the thumbnail's own backdrop is the frame, so no border or shadow is needed.
            GameObject frame = NewObject("Featured", cardContainer, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var frameImage = frame.GetComponent<Image>();
            frameImage.color = s_RenderBackdrop;
            frameImage.raycastTarget = false;

            var column = frame.GetComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(16, 16, 16, 18);
            column.spacing = 8f;
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            frame.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject imageGo = NewObject("FeaturedImage", frame.transform, typeof(Image), typeof(LayoutElement));
            var imageLayout = imageGo.GetComponent<LayoutElement>();
            imageLayout.preferredHeight = 600f;
            imageLayout.minHeight = 600f;
            var featuredImage = imageGo.GetComponent<Image>();
            featuredImage.preserveAspect = true;
            featuredImage.raycastTarget = false;

            var view = new FeaturedView();
            view.Image = featuredImage;
            view.Tradition = CreateText(frame.transform, "", 15, s_Bronze, true, true);
            view.Title = CreateText(frame.transform, "", 36, s_Gold, true, true);
            view.Intro = CreateText(frame.transform, "", 21, s_Ivory, false, true);
            m_Featured = view;

            CreateText(cardContainer, "THE COLLECTION", 15, s_Gold, true, true).alignment = TextAnchor.MiddleLeft;
        }

        // ---------- Collection ----------

        private void BuildCollection()
        {
            for (int i = 0; i < manager.Count; i++)
            {
                ExhibitData exhibit = manager.GetExhibit(i);
                if (exhibit != null)
                    m_Cards.Add(CreateCard(exhibit, i));
            }
        }

        private CardView CreateCard(ExhibitData exhibit, int index)
        {
            GameObject go = NewObject("Card_" + exhibit.DisplayName, cardContainer,
                typeof(Image), typeof(Button), typeof(Outline), typeof(LayoutElement), typeof(HorizontalLayoutGroup));

            var background = go.GetComponent<Image>();
            background.sprite = cardSprite;
            background.type = cardSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = s_CardBackground;
            background.pixelsPerUnitMultiplier = 1f;

            var outline = go.GetComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredHeight = 250f;
            layout.minHeight = 250f;

            var row = go.GetComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(16, 16, 16, 16);
            row.spacing = 18f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => manager.Select(index));

            CreateThumbnail(go.transform, index);
            CreateCardText(go.transform, exhibit, index);

            return new CardView { Index = index, Outline = outline };
        }

        private void CreateThumbnail(Transform parent, int index)
        {
            GameObject go = NewObject("Thumbnail", parent, typeof(Image), typeof(LayoutElement));
            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 216f;
            layout.minWidth = 216f;
            layout.preferredHeight = 216f;
            layout.minHeight = 216f;

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            ApplyThumbnail(image, index);
        }

        private void CreateCardText(Transform parent, ExhibitData exhibit, int index)
        {
            GameObject go = NewObject("TextColumn", parent, typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var column = go.GetComponent<VerticalLayoutGroup>();
            column.spacing = 4f;
            column.childAlignment = TextAnchor.MiddleLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            CreateText(go.transform, (index + 1).ToString("00") + "  \u00B7  " + TraditionFor(index, exhibit).ToUpperInvariant(), 13, s_Bronze, true, true);
            CreateText(go.transform, exhibit.DisplayName, 28, s_Gold, true, true);
            CreateText(go.transform, BuildShortDescription(exhibit.description, cardDescriptionLength), 21, s_Muted, false, true);
        }

        // ---------- Selection ----------

        private void RefreshSelection()
        {
            int selected = manager != null ? manager.SelectedIndex : -1;
            if (selected < 0)
                selected = 0;

            for (int i = 0; i < m_Cards.Count; i++)
            {
                bool isSelected = m_Cards[i].Index == selected;
                m_Cards[i].Outline.effectColor = isSelected
                    ? new Color(s_Gold.r, s_Gold.g, s_Gold.b, 1f)
                    : new Color(s_Bronze.r, s_Bronze.g, s_Bronze.b, 0.28f);
                m_Cards[i].Outline.effectDistance = isSelected ? new Vector2(1.5f, -1.5f) : new Vector2(1f, -1f);
            }

            UpdateFeatured(selected);
        }

        private void UpdateFeatured(int index)
        {
            if (m_Featured == null || manager == null)
                return;

            ExhibitData exhibit = manager.GetExhibit(index);
            if (exhibit == null)
                return;

            ApplyThumbnail(m_Featured.Image, index);
            m_Featured.Tradition.text = TraditionFor(index, exhibit).ToUpperInvariant();
            m_Featured.Title.text = exhibit.DisplayName;
            m_Featured.Intro.text = BuildShortDescription(exhibit.description, featuredIntroLength);
        }

        // ---------- Helpers ----------

        private void ApplyThumbnail(Image image, int index)
        {
            Sprite sprite = thumbnails != null && index >= 0 && index < thumbnails.Length ? thumbnails[index] : null;
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : s_Empty;
        }

        private string TraditionFor(int index, ExhibitData exhibit)
        {
            if (traditions != null && index >= 0 && index < traditions.Length && !string.IsNullOrEmpty(traditions[index]))
                return traditions[index];
            return exhibit.category ?? string.Empty;
        }

        private static GameObject NewObject(string name, Transform parent, params System.Type[] components)
        {
            var types = new System.Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            for (int i = 0; i < components.Length; i++)
                types[i + 1] = components[i];

            var go = new GameObject(name, types);
            go.transform.SetParent(parent, false);
            return go;
        }

        private Text CreateText(Transform parent, string value, int size, Color color, bool bold, bool wrap)
        {
            GameObject go = NewObject("Label", parent, typeof(CanvasRenderer), typeof(Text), typeof(ContentSizeFitter));

            var text = go.GetComponent<Text>();
            text.font = labelFont != null ? labelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value ?? string.Empty;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.08f;
            text.raycastTarget = false;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return text;
        }

        /// <summary>First sentence when short enough, otherwise a word-safe truncation with an ellipsis.</summary>
        private static string BuildShortDescription(string description, int maxLength)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            string trimmed = description.Trim();
            int sentenceEnd = trimmed.IndexOf('.');
            if (sentenceEnd > 0 && sentenceEnd < maxLength)
                return trimmed.Substring(0, sentenceEnd + 1);

            if (trimmed.Length <= maxLength)
                return trimmed;

            int cut = trimmed.LastIndexOf(' ', maxLength);
            if (cut <= 0)
                cut = maxLength;
            return trimmed.Substring(0, cut).TrimEnd() + "\u2026";
        }

        // ---------- Entry ----------

        private void OnEnterPressed()
        {
            if (m_Entering)
                return;

            m_Entering = true;
            if (enterButton != null)
                enterButton.interactable = false;

            if (gate != null)
                gate.Enter();

            StartCoroutine(FadeOutAndHide());
        }

        private IEnumerator FadeOutAndHide()
        {
            // Keep blocking input during the fade so the Enter tap cannot also place the artifact.
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, fadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }
    }
}
