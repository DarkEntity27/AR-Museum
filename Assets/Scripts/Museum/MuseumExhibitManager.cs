using System;
using System.Collections.Generic;
using UnityEngine;
using ARMuseum.Artifacts;
using ARMuseum.Exhibits;

namespace ARMuseum.Museum
{
    /// <summary>
    /// Owns the museum's exhibit list and which exhibit is selected for placement.
    ///
    /// Selecting an exhibit tells the ArtifactPlacer which prefab to spawn next. If an artifact is already
    /// placed, the placer replaces it in the same spot and orientation, closes the placard (which stops
    /// its narration), and removes the old artifact with its turntable and interaction state. Nothing here
    /// is specific to any one exhibit: to add one, create an ExhibitData asset and add it to the list.
    /// </summary>
    [DisallowMultipleComponent]
    public class MuseumExhibitManager : MonoBehaviour
    {
        [Tooltip("Exhibits in display order. Add a new ExhibitData asset here to make it available.")]
        [SerializeField] private ExhibitData[] exhibits = Array.Empty<ExhibitData>();

        [Tooltip("Placement target used for selection. Assigned automatically if left empty.")]
        [SerializeField] private ArtifactPlacer placer;

        [Tooltip("Index in the list of the exhibit selected when the museum starts.")]
        [SerializeField] private int defaultExhibitIndex;

        private int m_Selected = -1;

        /// <summary>Raised after the selected exhibit changes.</summary>
        public event Action SelectionChanged;

        public int Count => exhibits.Length;

        /// <summary>Index of the selected exhibit, or -1 if none.</summary>
        public int SelectedIndex => m_Selected;

        public ExhibitData SelectedExhibit => IsValidIndex(m_Selected) ? exhibits[m_Selected] : null;

        public ExhibitData GetExhibit(int index) => IsValidIndex(index) ? exhibits[index] : null;

        private void Awake()
        {
            if (placer == null)
                placer = FindFirstObjectByType<ArtifactPlacer>();
        }

        private void Start()
        {
            if (exhibits.Length == 0)
            {
                Debug.LogError("MuseumExhibitManager: no exhibits assigned.", this);
                return;
            }

            Select(Mathf.Clamp(defaultExhibitIndex, 0, exhibits.Length - 1));
        }

        /// <summary>Selects an exhibit for placement. Returns false if the index or exhibit is invalid.</summary>
        public bool Select(int index)
        {
            if (!IsValidIndex(index))
            {
                Debug.LogWarning("MuseumExhibitManager: exhibit index " + index + " is out of range.", this);
                return false;
            }

            ExhibitData exhibit = exhibits[index];
            if (exhibit == null || exhibit.artifactPrefab == null)
            {
                Debug.LogError("MuseumExhibitManager: exhibit at index " + index + " is missing or has no artifact prefab.", this);
                return false;
            }

            if (index == m_Selected)
                return true;

            if (placer == null)
            {
                Debug.LogError("MuseumExhibitManager: no ArtifactPlacer found.", this);
                return false;
            }

            m_Selected = index;
            placer.SetPendingExhibit(exhibit);
            SelectionChanged?.Invoke();
            return true;
        }

        private bool IsValidIndex(int index) => index >= 0 && index < exhibits.Length;

        private void OnValidate()
        {
            // Two exhibits with the same ID would be confusing to look up; warn in the editor.
            var seen = new HashSet<string>();
            foreach (ExhibitData exhibit in exhibits)
            {
                if (exhibit == null)
                    continue;

                string id = string.IsNullOrEmpty(exhibit.exhibitId) ? exhibit.name : exhibit.exhibitId;
                if (!seen.Add(id))
                    Debug.LogWarning("MuseumExhibitManager: duplicate exhibit ID '" + id + "'.", this);
            }
        }
    }
}
