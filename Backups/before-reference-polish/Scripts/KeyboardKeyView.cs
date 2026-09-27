using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public sealed class KeyboardKeyView : MonoBehaviour
    {
        [SerializeField] private PuzzleBoard board;
        [SerializeField] private string letter;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color correctColor = new Color(0.76f, 0.96f, 0.84f);
        [SerializeField] private Color errorColor = new Color(1f, 0.76f, 0.78f);

        public void Configure(PuzzleBoard owner, string value, Image image)
        {
            board = owner;
            letter = value;
            background = image;
            if (background != null) normalColor = background.color;
        }

        public void Press()
        {
            if (board != null && !string.IsNullOrEmpty(letter)) board.Guess(letter[0], this);
        }

        public void ShowResult(bool correct)
        {
            if (background != null) background.color = correct ? correctColor : errorColor;
        }

        public void ResetVisual()
        {
            if (background != null) background.color = normalColor;
        }

        public void CaptureNormalColor()
        {
            if (background != null) normalColor = background.color;
        }
    }
}
