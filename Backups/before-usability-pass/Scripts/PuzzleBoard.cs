using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public sealed class PuzzleBoard : MonoBehaviour
    {
        [SerializeField] private CryptogramGame game;
        [SerializeField] private PuzzleMode mode;
        [SerializeField] private int columns;
        [SerializeField] private LetterCellView[] cells;
        [SerializeField] private KeyboardKeyView[] keys;
        [SerializeField] private GameObject[] hearts;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Text hintCountText;

        private readonly Dictionary<char, int> cipher = new Dictionary<char, int>();
        private readonly Dictionary<int, char> answers = new Dictionary<int, char>();
        private readonly HashSet<int> revealed = new HashSet<int>();
        private readonly HashSet<string> attempted = new HashSet<string>();
        private PuzzleEntry entry;
        private int puzzleIndex = -1;
        private int selectedCode;
        private int remainingHearts;
        private int mistakesInLevel;

        public PuzzleMode Mode => mode;
        public int PuzzleIndex => puzzleIndex;
        public PuzzleEntry Entry => entry;
        public int RemainingHearts => remainingHearts;
        public int MistakesInLevel => mistakesInLevel;

        public void Configure(CryptogramGame owner, PuzzleMode puzzleMode, int gridColumns,
            LetterCellView[] letterCells, KeyboardKeyView[] keyboardKeys, GameObject[] heartObjects,
            Text instruction, Text feedback, Text hintLabel)
        {
            game = owner;
            mode = puzzleMode;
            columns = gridColumns;
            cells = letterCells;
            keys = keyboardKeys;
            hearts = heartObjects;
            instructionText = instruction;
            feedbackText = feedback;
            hintCountText = hintLabel;
        }

        public bool CanFit(string phrase)
        {
            return Layout(phrase) != null;
        }

        public void StartPuzzle(PuzzleEntry puzzle, int index, PuzzleProgress saved, int erudition, int hints)
        {
            entry = puzzle;
            puzzleIndex = index;
            cipher.Clear();
            answers.Clear();
            revealed.Clear();
            attempted.Clear();
            selectedCode = saved.selectedCode;
            remainingHearts = saved.remainingHearts > 0 ? saved.remainingHearts : (mode == PuzzleMode.Classic ? 5 : 3);
            mistakesInLevel = saved.mistakesInLevel;

            var phrase = puzzle.text.Trim().ToUpperInvariant().Replace('\n', ' ');
            var layout = Layout(phrase);
            if (layout == null) throw new InvalidOperationException("Puzzle does not fit the scene grid: " + puzzle.id);

            var distinct = phrase.Where(char.IsLetter).Distinct().OrderBy(character => character).ToArray();
            var numbers = Enumerable.Range(1, distinct.Length).ToArray();
            var rng = new System.Random(StableHash(puzzle.id));
            for (var i = numbers.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = numbers[i]; numbers[i] = numbers[j]; numbers[j] = swap;
            }
            for (var i = 0; i < distinct.Length; i++)
            {
                cipher[distinct[i]] = numbers[i];
                answers[numbers[i]] = distinct[i];
            }

            foreach (var part in saved.revealedCodes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(part, out var code)) revealed.Add(code);
            if (revealed.Count == 0)
            {
                var count = Mathf.Clamp(3 - erudition / 180, 1, mode == PuzzleMode.Classic ? 3 : 2);
                foreach (var character in phrase.Where(char.IsLetter)
                    .GroupBy(character => character).OrderByDescending(group => group.Count())
                    .ThenBy(group => group.Key).Take(count).Select(group => group.Key))
                    revealed.Add(cipher[character]);
            }
            foreach (var part in saved.attemptedPairs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                attempted.Add(part);

            foreach (var cell in cells) cell.gameObject.SetActive(false);
            foreach (var item in layout)
            {
                var cell = cells[item.slot];
                var code = char.IsLetter(item.character) ? cipher[item.character] : 0;
                cell.Bind(item.character, code, code == 0 || revealed.Contains(code));
            }
            foreach (var key in keys) key.ResetVisual();
            instructionText.text = mode == PuzzleMode.Classic ? "Восстановите литературную цитату" : "Разгадайте короткую фразу";
            feedbackText.text = "Нажмите на скрытую букву, затем выберите клавишу";
            UpdateHearts();
            SetHintCount(hints);
            SelectFirstHidden();
            game.StorePuzzleProgress(this);
        }

        public void SelectCell(int index)
        {
            if (index < 0 || index >= cells.Length || !cells[index].IsHiddenLetter) return;
            selectedCode = cells[index].Code;
            foreach (var cell in cells) if (cell.gameObject.activeSelf) cell.SetSelected(cell.Code == selectedCode);
            feedbackText.text = "Шифр " + selectedCode + ": выберите букву";
            game.StorePuzzleProgress(this);
        }

        public void Guess(char letter, KeyboardKeyView key)
        {
            if (entry == null) return;
            if (selectedCode == 0) SelectFirstHidden();
            if (selectedCode == 0) return;
            letter = char.ToUpperInvariant(letter);
            var pair = selectedCode + "-" + letter;
            if (attempted.Contains(pair))
            {
                feedbackText.text = "Эту букву для шифра " + selectedCode + " уже пробовали";
                return;
            }
            attempted.Add(pair);
            var correct = answers[selectedCode] == letter;
            key.ShowResult(correct);
            StartCoroutine(ResetKeyLater(key));
            game.RecordGuess(!correct);
            if (correct)
            {
                RevealCode(selectedCode);
                feedbackText.text = "Верно! Открыта буква «" + letter + "»";
                if (AllSolved()) { game.CompletePuzzle(this); return; }
                SelectFirstHidden();
            }
            else
            {
                mistakesInLevel++;
                remainingHearts = Mathf.Max(0, remainingHearts - 1);
                UpdateHearts();
                feedbackText.text = "Не подходит. Осталось ошибок: " + remainingHearts;
                if (remainingHearts == 0) { game.FailPuzzle(this); return; }
            }
            game.StorePuzzleProgress(this);
        }

        public void UseHint()
        {
            if (entry == null) return;
            if (selectedCode == 0) SelectFirstHidden();
            if (selectedCode == 0) return;
            if (!game.ConsumeHint())
            {
                feedbackText.text = "Подсказки закончились — их можно купить в магазине";
                return;
            }
            var answer = answers[selectedCode];
            RevealCode(selectedCode);
            feedbackText.text = "Подсказка: «" + answer + "»";
            SetHintCount(game.Hints);
            if (AllSolved()) { game.CompletePuzzle(this); return; }
            SelectFirstHidden();
            game.StorePuzzleProgress(this);
        }

        public void Check()
        {
            if (entry == null) return;
            if (AllSolved()) game.CompletePuzzle(this);
            else feedbackText.text = "Осталось открыть букв: " + cells.Count(cell => cell.gameObject.activeSelf && cell.IsHiddenLetter);
        }

        public void ClearSelection()
        {
            selectedCode = 0;
            foreach (var cell in cells) if (cell.gameObject.activeSelf) cell.SetSelected(false);
            feedbackText.text = "Выберите другую скрытую букву";
            game.StorePuzzleProgress(this);
        }

        public PuzzleProgress ExportProgress()
        {
            return new PuzzleProgress
            {
                puzzleIndex = puzzleIndex,
                remainingHearts = remainingHearts,
                revealedCodes = string.Join(",", revealed.OrderBy(code => code)),
                selectedCode = selectedCode,
                mistakesInLevel = mistakesInLevel,
                attemptedPairs = string.Join(";", attempted.OrderBy(pair => pair))
            };
        }

        public void SetHintCount(int count)
        {
            if (hintCountText != null) hintCountText.text = count.ToString();
        }

        public void SetRemainingHeartsForDebug(int count)
        {
            remainingHearts = Mathf.Clamp(count, 1, mode == PuzzleMode.Classic ? 5 : 3);
            UpdateHearts();
            game.StorePuzzleProgress(this);
        }

        private void RevealCode(int code)
        {
            revealed.Add(code);
            foreach (var cell in cells)
                if (cell.gameObject.activeSelf && cell.Code == code) cell.Reveal();
        }

        private bool AllSolved() => cells.All(cell => !cell.gameObject.activeSelf || !cell.IsHiddenLetter);

        private void SelectFirstHidden()
        {
            var index = Array.FindIndex(cells, cell => cell.gameObject.activeSelf && cell.IsHiddenLetter);
            if (index < 0) { selectedCode = 0; return; }
            SelectCell(index);
        }

        private void UpdateHearts()
        {
            for (var i = 0; i < hearts.Length; i++)
            {
                hearts[i].SetActive(true);
                var image = hearts[i].GetComponent<Image>();
                if (image != null) image.color = i < remainingHearts ? Color.white : new Color(.45f,.4f,.5f,.45f);
            }
        }

        private IEnumerator ResetKeyLater(KeyboardKeyView key)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            if (key != null) key.ResetVisual();
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 23;
                foreach (var character in value) hash = hash * 31 + character;
                return hash;
            }
        }

        private struct LayoutItem
        {
            public int slot;
            public char character;
        }

        private List<LayoutItem> Layout(string phrase)
        {
            if (columns <= 0 || cells == null || cells.Length == 0) return null;
            var rows = cells.Length / columns;
            var items = new List<LayoutItem>();
            var row = 0;
            var column = 0;
            foreach (var word in phrase.ToUpperInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length > columns) return null;
                if (column > 0 && column + 1 + word.Length > columns) { row++; column = 0; }
                else if (column > 0) { items.Add(new LayoutItem { slot = row * columns + column, character = ' ' }); column++; }
                if (row >= rows) return null;
                foreach (var character in word)
                {
                    if (column >= columns) return null;
                    items.Add(new LayoutItem { slot = row * columns + column, character = character });
                    column++;
                }
            }
            var rowOffset = Mathf.Max(0, (rows - row - 1) / 2);
            var centered = new List<LayoutItem>();
            foreach (var line in items.GroupBy(item => item.slot / columns))
            {
                var used = line.Max(item => item.slot % columns) + 1;
                var horizontalOffset = (columns - used) / 2;
                foreach (var item in line)
                    centered.Add(new LayoutItem { slot = item.slot + rowOffset * columns + horizontalOffset, character = item.character });
            }
            return centered;
        }
    }
}
