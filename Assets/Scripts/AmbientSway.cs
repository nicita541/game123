using UnityEngine;

namespace Erudition
{
    // Subtle motion for an existing authored RectTransform.
    // No scene/UI objects are created at runtime.
    public sealed class AmbientSway : MonoBehaviour
    {
        public Vector2 amplitude = new Vector2(4f, 1f);
        public float rotationDegrees = .6f;
        public float speed = 1f;
        public float phase;

        private RectTransform rect;
        private Vector2 restingPosition;
        private Quaternion restingRotation;
        private bool initialized;

        private void OnEnable()
        {
            rect = transform as RectTransform;
            if (rect == null)
                return;

            restingPosition = rect.anchoredPosition;
            restingRotation = rect.localRotation;
            initialized = true;
        }

        private void OnDisable()
        {
            if (!initialized || rect == null)
                return;

            rect.anchoredPosition = restingPosition;
            rect.localRotation = restingRotation;
        }

        private void Update()
        {
            if (!initialized || rect == null)
                return;

            var wave = Mathf.Sin(Time.unscaledTime * Mathf.Max(.01f, speed) + phase);
            rect.anchoredPosition = restingPosition + amplitude * wave;
            rect.localRotation = restingRotation * Quaternion.Euler(0f, 0f, rotationDegrees * wave);
        }
    }
}
