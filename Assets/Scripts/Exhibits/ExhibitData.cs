using System;
using UnityEngine;

namespace ARMuseum.Exhibits
{
    [Serializable]
    public class SymbolEntry
    {
        public string label;
        [TextArea(1, 3)] public string meaning;
    }

    [CreateAssetMenu(fileName = "ExhibitData", menuName = "AR Museum/Exhibit Data")]
    public class ExhibitData : ScriptableObject
    {
        public string title;
        [Tooltip("Optional short label for the exhibit selector chip, e.g. 'DANCING GIRL'. Uses the title when empty.")]
        public string selectorLabel;
        public string subtitle;
        public string category;
        [TextArea(3, 10)] public string description;
        public SymbolEntry[] symbolism;
        [TextArea(3, 10)] public string historicalContext;
        [TextArea(1, 4)] public string provenanceNote;
        [Tooltip("Spoken narration (one sentence per line or per sentence). Read aloud by the device text-to-speech engine.")]
        [TextArea(3, 14)] public string narration;
        [Tooltip("Optional recorded narration. If set, it is played instead of text-to-speech.")]
        public AudioClip narrationClip;
        [Tooltip("Unique, stable identifier for this exhibit (e.g. 'nataraja'). Used for lookups and logs.")]
        public string exhibitId;
        [Tooltip("Thumbnail shown on the exhibit selector. Optional.")]
        public Sprite thumbnail;
        [Tooltip("The artifact prefab spawned when this exhibit is placed. Must have ExhibitArtifact and a BoxCollider.")]
        public GameObject artifactPrefab;
        [Tooltip("Suggested real-world height in metres. 0 = leave the prefab's scale unchanged.")]
        [Min(0f)] public float displayHeightMeters;

        [Tooltip("Historical period shown on the details screen, e.g. 'c. 3rd century CE'.")]
        public string period;
        [Tooltip("Short introduction (one or two sentences) shown on the details screen.")]
        [TextArea(2, 4)] public string introduction;
        [Tooltip("Cultural significance text shown on the details screen.")]
        [TextArea(3, 10)] public string culturalSignificance;
        [Tooltip("High-resolution render of the artifact for the details screen.")]
        public Sprite detailImage;

        /// <summary>Name shown in the UI. Uses the title field.</summary>
        public string DisplayName => string.IsNullOrEmpty(title) ? name : title;
    }
}
