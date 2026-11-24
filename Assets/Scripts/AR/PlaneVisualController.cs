using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARMuseum.AR
{
    /// <summary>
    /// Controls the visual style of detected planes. Planes stay tracked (raycasts still work);
    /// only their rendering changes. Dimming hides edge lines and makes the fill nearly invisible.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ARPlaneManager))]
    public class PlaneVisualController : MonoBehaviour
    {
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material dimmedMaterial;

        private ARPlaneManager m_Planes;
        private bool m_Dimmed;

        private void Awake()
        {
            m_Planes = GetComponent<ARPlaneManager>();
        }

        private void OnEnable()
        {
            m_Planes.planesChanged += OnPlanesChanged;
        }

        private void OnDisable()
        {
            m_Planes.planesChanged -= OnPlanesChanged;
        }

        private void Start()
        {
            foreach (var plane in m_Planes.trackables)
                Apply(plane);
        }

        public void SetDimmed(bool dimmed)
        {
            if (m_Dimmed == dimmed)
                return;

            m_Dimmed = dimmed;
            foreach (var plane in m_Planes.trackables)
                Apply(plane);
        }

        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            foreach (var plane in args.added)
                Apply(plane);
        }

        private void Apply(ARPlane plane)
        {
            if (plane == null)
                return;

            var meshRenderer = plane.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Material target = m_Dimmed ? dimmedMaterial : activeMaterial;
                if (target != null)
                    meshRenderer.sharedMaterial = target;
            }

            foreach (var line in plane.GetComponentsInChildren<LineRenderer>(true))
                line.enabled = !m_Dimmed;
        }
    }
}
