using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public enum SettingKind
    {
        Music = 0,
        Sound = 1,
        Vibration = 2,
        LargeText = 3
    }

    public sealed class SettingRowView : MonoBehaviour
    {
        public SettingKind kind;
        public Text stateText;
        public Image track;
        public RectTransform thumb;
        public RectTransform onPosition;
        public RectTransform offPosition;

        public void Render(bool enabled)
        {
            if (stateText != null)
            {
                stateText.text = enabled ? "Включено" : "Выключено";
                stateText.color = enabled ? new Color(.2f, .46f, .34f) : new Color(.47f, .43f, .5f);
            }

            if (track != null)
                track.color = enabled ? new Color(.30f, .64f, .47f) : new Color(.74f, .71f, .77f);

            var target = enabled ? onPosition : offPosition;
            if (thumb != null && target != null)
                thumb.anchoredPosition = target.anchoredPosition;
        }
    }
}
