using UnityEngine;

namespace ARMuseum.Exhibits
{
    /// <summary>Marks a placed artifact as interactive and links it to its exhibit content.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class ExhibitArtifact : MonoBehaviour
    {
        [SerializeField] private ExhibitData data;
        [Tooltip("The child transform that holds the visible model. Rotation turns this, never the root. Must not be the root itself.")]
        [SerializeField] private Transform visualRoot;

        private BoxCollider m_Box;

        public ExhibitData Data => data;

        public Transform VisualRoot => visualRoot;

        /// <summary>
        /// Attaches exhibit content to this placed artifact and makes it turnable. Called by the placer
        /// whenever an artifact is spawned for an exhibit. Safe to call on any artifact prefab.
        /// </summary>
        public void Bind(ExhibitData exhibit)
        {
            data = exhibit;

            var turntable = GetComponent<ArtifactTurntable>();
            if (turntable == null)
                turntable = gameObject.AddComponent<ArtifactTurntable>();
            if (visualRoot == null)
                Debug.LogWarning("ExhibitArtifact '" + name + "' has no Visual Root assigned; rotation will turn the artifact root. Assign its model child.", this);
            turntable.Configure(visualRoot);
        }

        private BoxCollider Box
        {
            get
            {
                if (m_Box == null)
                    m_Box = GetComponent<BoxCollider>();
                return m_Box;
            }
        }

        /// <summary>World-space centre of the artifact's collider, i.e. the visual middle of the statue.</summary>
        public Vector3 AnchorPoint => Box.transform.TransformPoint(Box.center);

        /// <summary>
        /// Half-extent of the artifact's (oriented) collider measured along a world-space direction.
        /// Used to keep world-space UI a fixed gap away from the model's silhouette.
        /// </summary>
        public float ProjectedHalfExtent(Vector3 worldDirection)
        {
            var box = Box;
            Transform t = box.transform;
            Vector3 s = t.lossyScale;
            Vector3 e = new Vector3(
                Mathf.Abs(box.size.x * s.x),
                Mathf.Abs(box.size.y * s.y),
                Mathf.Abs(box.size.z * s.z)) * 0.5f;

            Vector3 d = worldDirection.sqrMagnitude > 0f ? worldDirection.normalized : Vector3.right;
            return Mathf.Abs(Vector3.Dot(d, t.right)) * e.x
                 + Mathf.Abs(Vector3.Dot(d, t.up)) * e.y
                 + Mathf.Abs(Vector3.Dot(d, t.forward)) * e.z;
        }
    }
}
