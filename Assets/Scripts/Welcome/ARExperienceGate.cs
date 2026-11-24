using System;
using UnityEngine;
using ARMuseum.Artifacts;

namespace ARMuseum.Welcome
{
    /// <summary>
    /// Holds the AR experience back until the user presses ENTER AR MUSEUM.
    /// Before entry: AR Session is inactive (ARCore does not start, no camera permission prompt),
    /// the artifact placer is disabled, and the exhibit selector is hidden.
    /// On entry: AR Session is activated (ARCore initializes), then the selector and placer are enabled.
    /// XR Origin, the AR camera and the camera configuration are never changed here.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARExperienceGate : MonoBehaviour
    {
        [Tooltip("The 'AR Session' GameObject. It is activated on entry so ARCore starts only then.")]
        [SerializeField] private GameObject arSessionRoot;

        [SerializeField] private ArtifactPlacer placer;

        [Tooltip("The ExhibitSelectorCanvas root. Shown on entry.")]
        [SerializeField] private GameObject exhibitSelectorRoot;

        private bool m_Entered;

        public bool HasEntered => m_Entered;

        /// <summary>Raised once, when the AR experience has been started.</summary>
        public event Action Entered;

        private void Awake()
        {
            if (arSessionRoot != null)
                arSessionRoot.SetActive(false);

            if (placer != null)
                placer.enabled = false;

            if (exhibitSelectorRoot != null)
                exhibitSelectorRoot.SetActive(false);
        }

        /// <summary>Starts AR. Safe to call more than once; only the first call has effect.</summary>
        public void Enter()
        {
            if (m_Entered)
                return;

            m_Entered = true;

            if (arSessionRoot != null)
                arSessionRoot.SetActive(true);

            if (exhibitSelectorRoot != null)
                exhibitSelectorRoot.SetActive(true);

            if (placer != null)
                placer.enabled = true;

            Entered?.Invoke();
        }
    }
}
