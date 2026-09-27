using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public sealed class LetterCellView : MonoBehaviour
    {
        [SerializeField] private PuzzleBoard board;
        [SerializeField] private int index;
        [SerializeField] private Text letterText;
        [SerializeField] private Text codeText;
        [SerializeField] private Image underline;
        [SerializeField] private Button button;
        [SerializeField] private Color normalColor = new Color(0.13f, 0.2f, 0.42f);
        [SerializeField] private Color selectedColor = new Color(0.54f, 0.35f, 0.94f);
        [SerializeField] private Color revealedColor = new Color(0.09f, 0.58f, 0.43f);

        private char answer;
        private int code;
        private bool isLetter;
        private bool revealed;

        public int Code => code;
        public bool IsHiddenLetter => isLetter && !revealed;
        public char Answer => answer;

        public void Configure(PuzzleBoard owner, int cellIndex, Text letter, Text number, Image line, Button cellButton)
        {
            board = owner;
            index = cellIndex;
            letterText = letter;
            codeText = number;
            underline = line;
            button = cellButton;
        }

        public void Bind(char value, int cipherCode, bool showLetter)
        {
            answer = value;
            code = cipherCode;
            isLetter = cipherCode > 0;
            revealed = showLetter || !isLetter;
            gameObject.SetActive(true);
            if (button != null) button.interactable = isLetter && !revealed;
            letterText.text = revealed ? value.ToString() : "";
            codeText.text = isLetter ? code.ToString() : "";
            codeText.gameObject.SetActive(isLetter);
            underline.gameObject.SetActive(isLetter);
            underline.color = revealed ? revealedColor : normalColor;
            letterText.color = revealed && isLetter ? revealedColor : normalColor;
        }

        public void Reveal()
        {
            if (!isLetter) return;
            revealed = true;
            letterText.text = answer.ToString();
            letterText.color = revealedColor;
            underline.color = revealedColor;
            if (button != null) button.interactable = false;
        }

        public void SetSelected(bool selected)
        {
            if (IsHiddenLetter) underline.color = selected ? selectedColor : normalColor;
        }

        public void Press()
        {
            if (board != null && IsHiddenLetter) board.SelectCell(index);
        }
    }
}
