using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ARMuseum.Exhibits;

namespace ARMuseum.Museum
{
    /// <summary>
    /// Compact exhibit picker along the top of the screen. Builds one chip per exhibit from the
    /// MuseumExhibitManager at runtime, so adding an exhibit to the manager adds its chip with no UI edits.
    /// The selected chip is highlighted in antique gold. Chips are UI, so taps on them never reach AR
    /// placement.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExhibitSelectorUI : MonoBehaviour
    {
        [SerializeField] private MuseumExhibitManager manager;

        [Tooltip("Content transform of the horizontal ScrollRect that holds the chips.")]
        [SerializeField] private RectTransform chipContainer;

        [Tooltip("Rounded rectangle sprite used for chip backgrounds (the same one as the placard).")]
        [SerializeField] private Sprite chipSprite;

        [SerializeField] private Font labelFont;
        [SerializeField] private int labelFontSize = 18;

        private static readonly Color s_BackgroundIdle = new Color(0.14f, 0.13f, 0.12f, 0.88f);
        private static readonly Color s_BackgroundSelected = new Color(0.30f, 0.24f, 0.16f, 0.96f);
        private static readonly Color s_Gold = new Color(0.79f, 0.66f, 0.43f, 1f);
        private static readonly Color s_Cream = new Color(0.953f, 0.922f, 0.847f, 1f);

        private readonly List<Chip> m_Chips = new List<Chip>();

        private struct Chip
        {
            public int Index;
            public Image Background;
            public Outline Outline;
            public Text Label;
        }

        private void Start()
        {
            if (manager == null)
                manager = FindFirstObjectByType<MuseumExhibitManager>();

            if (manager == null || chipContainer == null)
            {
                Debug.LogError("ExhibitSelectorUI: needs a MuseumExhibitManager and a chip container.", this);
                return;
            }

            BuildChips();
            manager.SelectionChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (manager != null)
                manager.SelectionChanged -= Refresh;
        }

        private void BuildChips()
        {
            for (int i = chipContainer.childCount - 1; i >= 0; i--)
                Destroy(chipContainer.GetChild(i).gameObject);
            m_Chips.Clear();

            for (int i = 0; i < manager.Count; i++)
            {
                ExhibitData exhibit = manager.GetExhibit(i);
                if (exhibit == null)
                    continue;

                m_Chips.Add(CreateChip(exhibit, i));
            }
        }

        private Chip CreateChip(ExhibitData exhibit, int index)
        {
            var go = new GameObject("Chip_" + exhibit.DisplayName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button),
                typeof(Outline), typeof(LayoutElement), typeof(ContentSizeFitter), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(chipContainer, false);

            var background = go.GetComponent<Image>();
            background.sprite = chipSprite;
            background.type = chipSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = s_BackgroundIdle;

            var outline = go.GetComponent<Outline>();
            outline.effectColor = s_Gold;
            outline.effectDistance = new Vector2(1f, -1f);

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 52f;
            layout.preferredHeight = 52f;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var group = go.GetComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(14, 14, 6, 6);
            group.spacing = 10f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            if (exhibit.thumbnail != null)
                CreateThumbnail(go.transform, exhibit.thumbnail);

            Text label = CreateLabel(go.transform, FormatChipLabel(exhibit, index));

            var button = go.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.85f, 1f);
            colors.pressedColor = new Color(0.75f, 0.65f, 0.5f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => manager.Select(index));

            return new Chip { Index = index, Background = background, Outline = outline, Label = label };
        }

        private static void CreateThumbnail(Transform parent, Sprite sprite)
        {
            var go = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 36f;
            layout.preferredHeight = 36f;
        }

        private Text CreateLabel(Transform parent, string text)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);

            var label = go.GetComponent<Text>();
            label.font = labelFont != null ? labelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = labelFontSize;
            label.fontStyle = FontStyle.Bold;
            label.color = s_Cream;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            return label;
        }

        private void Refresh()
        {
            int selected = manager != null ? manager.SelectedIndex : -1;
            for (int i = 0; i < m_Chips.Count; i++)
            {
                Chip chip = m_Chips[i];
                bool isSelected = chip.Index == selected;

                chip.Background.color = isSelected ? s_BackgroundSelected : s_BackgroundIdle;
                chip.Outline.effectColor = new Color(s_Gold.r, s_Gold.g, s_Gold.b, isSelected ? 1f : 0.35f);
                chip.Outline.effectDistance = isSelected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
                chip.Label.color = isSelected ? s_Gold : s_Cream;
            }
        }
    

/// <summary>
        /// Builds the chip text from the exhibit's position in the museum list, e.g. "01 · NATARAJA".
        /// The number comes from the list index (1-based), so nothing is hardcoded per exhibit.
        /// </summary>
        private static string FormatChipLabel(ExhibitData exhibit, int index)
        {
            return (index + 1).ToString("00") + " · " + exhibit.DisplayName;
        }
}
}
