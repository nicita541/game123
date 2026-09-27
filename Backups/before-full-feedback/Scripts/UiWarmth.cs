using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Erudition
{
    // Subtle motion on existing scene objects only; no particles or generated UI.
    public sealed class UiWarmth : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool breathe;
        public float amplitude = .006f;
        private bool pressed;
        private Vector3 resting;
        private bool initialized;
        private void OnEnable()
        {
            if (!initialized) { resting = transform.localScale; initialized = true; }
            pressed = false;
        }
        private void OnDisable() { if (initialized) transform.localScale = resting; }
        private void Update()
        {
            var scale = pressed ? .975f : breathe ? 1 + Mathf.Sin(Time.unscaledTime * 1.5f) * amplitude : 1;
            transform.localScale = Vector3.Lerp(transform.localScale, resting * scale, 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
        }
        public void OnPointerDown(PointerEventData data)
        {
            var control = GetComponent<Selectable>();
            pressed = control == null || control.IsInteractable();
        }
        public void OnPointerUp(PointerEventData data) { pressed = false; }
        public void OnPointerExit(PointerEventData data) { pressed = false; }
    }
}
