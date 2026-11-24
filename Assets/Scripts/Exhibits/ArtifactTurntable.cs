using UnityEngine;

namespace ARMuseum.Exhibits
{
    /// <summary>
    /// Reusable turntable for an exhibit artifact. Spins the artifact's visual model around its own
    /// vertical (Y) axis in discrete steps, animating smoothly between them. It only changes the model's
    /// local rotation, so the artifact root keeps its world position and placement orientation, and the
    /// model stays upright. Add to any artifact prefab root and assign its model transform.
    /// </summary>
    [DisallowMultipleComponent]
    public class ArtifactTurntable : MonoBehaviour
    {
        [Tooltip("Transform of the visual model to spin. Its origin should sit on the base of the artifact.")]
        [SerializeField] private Transform modelRoot;
        [Tooltip("Degrees per rotate button press.")]
        [SerializeField] private float stepDegrees = 30f;
        [Tooltip("Maximum spin speed in degrees per second.")]
        [SerializeField] private float maxDegreesPerSecond = 180f;

        private Quaternion m_BaseLocalRotation;
        private float m_TargetYaw;
        private float m_CurrentYaw;
        private bool m_Initialised;

        public float CurrentYaw => m_CurrentYaw;
        public float TargetYaw => m_TargetYaw;

        private void Awake()
        {
            Initialise();
        }

        private void Initialise()
        {
            if (m_Initialised)
                return;

            if (modelRoot == null)
                modelRoot = transform;

            // The authored model pose is the "initial orientation" that reset returns to.
            m_BaseLocalRotation = modelRoot.localRotation;
            m_Initialised = true;
        }

        private void Update()
        {
            if (!m_Initialised)
                Initialise();

            float next = Mathf.MoveTowards(m_CurrentYaw, m_TargetYaw, maxDegreesPerSecond * Time.deltaTime);
            if (Mathf.Approximately(next, m_CurrentYaw))
                return;

            m_CurrentYaw = next;
            modelRoot.localRotation = Quaternion.AngleAxis(m_CurrentYaw, Vector3.up) * m_BaseLocalRotation;
        }

        /// <summary>Turns the model by one step. direction: negative = left, positive = right.</summary>
        public void RotateStep(int direction)
        {
            if (direction == 0)
                return;

            m_TargetYaw += Mathf.Sign(direction) * stepDegrees;
        }

        /// <summary>Animates the model back to its authored orientation.</summary>
        /// <summary>
        /// Sets the model to turn and captures its current pose as the authored orientation.
        /// Ignores null, and refuses the root transform (rotating the root would fight the placer's facing).
        /// </summary>
        public void Configure(Transform model)
        {
            Initialise();
            if (model == null)
                return;

            if (model == transform)
            {
                Debug.LogWarning("ArtifactTurntable: the visual model must be a child of the artifact root, not the root itself.", this);
                return;
            }

            modelRoot = model;
            m_BaseLocalRotation = modelRoot.localRotation;
            m_TargetYaw = 0f;
            m_CurrentYaw = 0f;
        }

        public void ResetRotation()
        {
            m_TargetYaw = 0f;
        }
    }
}
