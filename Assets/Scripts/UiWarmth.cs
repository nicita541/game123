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

        [Header("Idle blink")]
        public bool blink;
        public float blinkMinInterval = 2.8f;
        public float blinkMaxInterval = 5.8f;
        [Range(.02f, .3f)] public float blinkDuration = .09f;
        [Range(.8f, 1f)] public float blinkScaleY = .9f;

        private bool pressed;
        private Vector3 resting;
        private bool initialized;
        private bool blinking;
        private float blinkEndsAt;
        private float nextBlinkAt;

        private void OnEnable()
        {
            if (!initialized)
            {
                resting = transform.localScale;
                initialized = true;
            }

            pressed = false;
            blinking = false;
            ScheduleBlink();
        }

        private void OnDisable()
        {
            if (initialized && (pressed || breathe || blink))
                transform.localScale = resting;

            pressed = false;
            blinking = false;
        }

        private void Update()
        {
            UpdateBlink();

            if (!pressed && !breathe && !blink)
                return;

            var uniformScale = pressed
                ? .975f
                : breathe
                    ? 1f + Mathf.Sin(Time.unscaledTime * 1.5f) * amplitude
                    : 1f;

            var blinkY = blinking ? blinkScaleY : 1f;
            var targetScale = Vector3.Scale(
                resting * uniformScale,
                new Vector3(1f, blinkY, 1f));

            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        }

        private void UpdateBlink()
        {
            if (!blink)
                return;

            var now = Time.unscaledTime;
            if (blinking)
            {
                if (now < blinkEndsAt)
                    return;

                blinking = false;
                ScheduleBlink();
                return;
            }

            if (now < nextBlinkAt)
                return;

            blinking = true;
            blinkEndsAt = now + Mathf.Max(.02f, blinkDuration);
        }

        private void ScheduleBlink()
        {
            if (!blink)
            {
                nextBlinkAt = float.PositiveInfinity;
                return;
            }

            var min = Mathf.Max(.25f, blinkMinInterval);
            var max = Mathf.Max(min, blinkMaxInterval);
            nextBlinkAt = Time.unscaledTime + Random.Range(min, max);
        }

        public void OnPointerDown(PointerEventData data)
        {
            var control = GetComponent<Selectable>();
            if (!pressed)
                resting = transform.localScale;

            pressed = control == null || control.IsInteractable();
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (pressed)
                transform.localScale = resting;

            pressed = false;
        }

        public void OnPointerExit(PointerEventData data)
        {
            OnPointerUp(data);
        }
    }
}
