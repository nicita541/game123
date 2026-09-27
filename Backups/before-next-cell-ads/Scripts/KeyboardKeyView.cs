using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public sealed class KeyboardKeyView : MonoBehaviour
    {
        [SerializeField] private PuzzleBoard board;
        [SerializeField] private string letter;
        [SerializeField] private Image background;
        [SerializeField] private Image strike;
        [SerializeField] private Text label;
        [SerializeField] private Button button;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color correctColor = new Color(0.76f, 0.96f, 0.84f);
        [SerializeField] private Color errorColor = new Color(1f, 0.76f, 0.78f);
        public char Letter => string.IsNullOrEmpty(letter) ? '\0' : letter[0];
        public bool Used { get; private set; }
        public bool Found { get; private set; }

        public void ConfigureUsage(Image line, Text text, Button control) { strike = line; label = text; button = control; }
        public void SetUsed(bool value)
        {
            SetState(value, value);
        }

        public void SetState(bool found, bool exhausted)
        {
            Found = found;
            Used = exhausted;
            if (strike != null) strike.enabled = exhausted;
            if (button != null) button.interactable = !exhausted;
            if (label != null) label.color = exhausted ? new Color(.48f, .46f, .48f, .85f)
                : found ? new Color(.08f, .37f, .22f) : new Color(.15f, .12f, .26f);
            ResetVisual();
        }

        public void Configure(PuzzleBoard owner, string value, Image image)
        {
            board = owner;
            letter = value;
            background = image;
            if (background != null) normalColor = background.color;
        }

        public void Press()
        {
            if (!Used && board != null && !string.IsNullOrEmpty(letter)) board.Guess(letter[0], this);
        }

        public void ShowResult(bool correct)
        {
            if (background != null) background.color = correct ? correctColor : errorColor;
        }

        public void ResetVisual()
        {
            if (background != null) background.color = Used ? new Color(.82f, .8f, .75f, 1) : Found ? correctColor : normalColor;
        }

        public void CaptureNormalColor()
        {
            if (background != null) normalColor = background.color;
        }
    }
}
