using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using ARMuseum.AR;
using ARMuseum.Exhibits;
using ARMuseum.UI;

namespace ARMuseum.Artifacts
{
    /// <summary>
    /// Routes screen taps in priority order:
    ///   1. Taps on UI are ignored by AR (no placement, no artifact interaction).
    ///   2. While the info card is open, AR taps are ignored.
    ///   3. A tap on the placed artifact opens its information (never moves it).
    ///   4. A tap on a detected plane places the artifact once, then moves that same instance.
    /// After the first placement, plane visuals are dimmed (planes stay tracked for raycasts).
    /// </summary>
    [DisallowMultipleComponent]
    public class ArtifactPlacer : MonoBehaviour
    {
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private Transform arCamera;
        [SerializeField] private GameObject artifactPrefab;
        [SerializeField] private ExhibitInfoPanel infoPanel;
        [SerializeField] private PlaneVisualController planeVisuals;

        private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
        private static readonly RaycastHit[] s_PhysicsHits = new RaycastHit[16];
        private Camera m_Camera;
        private GameObject m_Spawned;

        public GameObject SpawnedArtifact => m_Spawned;

        private ExhibitData m_PendingExhibit;
        private GameObject m_PendingPrefab;

        /// <summary>The exhibit that the next placement will spawn.</summary>
        public ExhibitData PendingExhibit => m_PendingExhibit;

private void Awake()
        {
            if (raycastManager == null)
                raycastManager = FindFirstObjectByType<ARRaycastManager>();

            if (arCamera == null)
            {
                var cameraManager = FindFirstObjectByType<ARCameraManager>();
                if (cameraManager != null)
                    arCamera = cameraManager.transform;
            }

            m_Camera = arCamera != null ? arCamera.GetComponent<Camera>() : null;
            if (m_Camera == null)
                m_Camera = Camera.main;

            // Until an exhibit is selected, the serialized prefab is the fallback.
            m_PendingPrefab = artifactPrefab;
        }

private void Update()
        {
            if (!TryGetTap(out Vector2 screenPosition, out int pointerId))
                return;

            if (IsPointerOverUI(pointerId))
                return;

            if (infoPanel != null && infoPanel.IsOpen)
                return;

            if (m_Spawned != null && m_Camera != null && IsArtifactHit(m_Camera.ScreenPointToRay(screenPosition)))
            {
                OpenInfo();
                return;
            }

            if (raycastManager == null || m_PendingPrefab == null)
                return;

            if (raycastManager.Raycast(screenPosition, s_Hits, TrackableType.PlaneWithinPolygon))
                PlaceOrMove(s_Hits[0].pose);
        }

        private bool IsPointerOverUI(int pointerId)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            return eventSystem.IsPointerOverGameObject(pointerId) || eventSystem.IsPointerOverGameObject();
        }

        private bool IsArtifactHit(Ray ray)
        {
            int count = Physics.RaycastNonAlloc(ray, s_PhysicsHits, 10f);
            for (int i = 0; i < count; i++)
            {
                if (s_PhysicsHits[i].transform.IsChildOf(m_Spawned.transform))
                    return true;
            }
            return false;
        }

        private void OpenInfo()
        {
            if (infoPanel == null)
                return;

            var artifact = m_Spawned.GetComponent<ExhibitArtifact>();
            if (artifact != null && artifact.Data != null)
                infoPanel.Show(artifact.Data, artifact);
        }

        private static bool TryGetTap(out Vector2 position, out int pointerId)
        {
            position = default;
            pointerId = -1;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    position = touch.position.ReadValue();
                    pointerId = touch.touchId.ReadValue();
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                position = mouse.position.ReadValue();
                pointerId = -1;
                return true;
            }

            return false;
        }

private void PlaceOrMove(Pose pose)
        {
            Quaternion rotation = GetFacingRotation(pose.position);

            if (m_Spawned == null)
            {
                m_Spawned = Spawn(m_PendingPrefab, m_PendingExhibit, pose.position, rotation);
                if (planeVisuals != null)
                    planeVisuals.SetDimmed(true);
            }
            else
            {
                m_Spawned.transform.SetPositionAndRotation(pose.position, rotation);
            }
        }

        /// <summary>Yaw-only rotation so the artifact's front faces the viewer, staying upright on the plane.</summary>
        private Quaternion GetFacingRotation(Vector3 position)
        {
            if (arCamera == null)
                return Quaternion.identity;

            Vector3 toCamera = arCamera.position - position;
            toCamera.y = 0f;

            if (toCamera.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    

/// <summary>
        /// Chooses the exhibit to place next. With no artifact placed, only the pending choice changes.
        /// With an artifact placed, it is replaced in the same spot and orientation and the old one is removed.
        /// </summary>
        public void SetPendingExhibit(ExhibitData exhibit)
        {
            if (exhibit == null || exhibit.artifactPrefab == null)
            {
                Debug.LogError("ArtifactPlacer: the exhibit has no artifact prefab assigned.", this);
                return;
            }

            if (exhibit == m_PendingExhibit)
                return;

            m_PendingExhibit = exhibit;
            m_PendingPrefab = exhibit.artifactPrefab;

            if (m_Spawned != null)
                ReplaceSpawned();
        }

        private void ReplaceSpawned()
        {
            Vector3 position = m_Spawned.transform.position;
            Quaternion rotation = m_Spawned.transform.rotation;

            // Close the card (this also stops its narration) before the old artifact is removed.
            if (infoPanel != null && infoPanel.IsOpen)
                infoPanel.Hide();

            GameObject old = m_Spawned;
            m_Spawned = null;
            old.SetActive(false);   // cannot be tapped while it is being removed
            Destroy(old);

            m_Spawned = Spawn(m_PendingPrefab, m_PendingExhibit, position, rotation);
        }

        private static GameObject Spawn(GameObject prefab, ExhibitData exhibit, Vector3 position, Quaternion rotation)
        {
            GameObject go = Instantiate(prefab, position, rotation);
            go.name = "Artifact";

            var artifact = go.GetComponent<ExhibitArtifact>();
            if (artifact == null)
                Debug.LogError("ArtifactPlacer: artifact prefab '" + prefab.name + "' has no ExhibitArtifact component.", prefab);
            else if (exhibit != null)
                artifact.Bind(exhibit);

            ApplyDisplayHeight(go, exhibit);
            return go;
        }

        /// <summary>Scales the artifact to the exhibit's suggested height. Leaves it alone if already close.</summary>
        private static void ApplyDisplayHeight(GameObject go, ExhibitData exhibit)
        {
            if (exhibit == null || exhibit.displayHeightMeters <= 0f)
                return;

            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float height = bounds.size.y;
            if (height <= 0.0001f)
                return;

            float factor = exhibit.displayHeightMeters / height;
            if (Mathf.Abs(factor - 1f) < 0.02f)
                return;

            go.transform.localScale *= factor;
        }
}
}
