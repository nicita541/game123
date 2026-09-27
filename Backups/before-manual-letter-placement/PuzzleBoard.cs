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
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform content;
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text owlGuide;

        private readonly Dictionary<char, int> cipher = new Dictionary<char, int>();
        private readonly Dictionary<int, char> answers = new Dictionary<int, char>();
        private readonly HashSet<int> revealed = new HashSet<int>();
        private readonly HashSet<string> attempted = new HashSet<string>();
        private PuzzleEntry entry;
        private int puzzleIndex = -1;
        private int selectedCode;
        private int selectedSlot = -1;
        private int remainingHearts;
        private int mistakesInLevel;

        public PuzzleMode Mode => mode;
        public int PuzzleIndex => puzzleIndex;
        public PuzzleEntry Entry => entry;
        public int RemainingHearts => remainingHearts;
        public int MistakesInLevel => mistakesInLevel;
        public int HiddenKinds => answers.Count - revealed.Count;
        public void SetGame(CryptogramGame owner) { game = owner; }
        public void ConfigureReading(ScrollRect reader, RectTransform page, Text progress, Image fill, Text guide)
        { scroll = reader; content = page; progressText = progress; progressFill = fill; owlGuide = guide; }

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
            selectedSlot = saved.selectedSlot;
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
                if (int.TryParse(part, out var code) && answers.ContainsKey(code)) revealed.Add(code);
            if (revealed.Count == 0 || saved.helpRevision < 2)
            {
                var hidden = Mathf.Clamp(PuzzleGenerator.HiddenLetters(erudition, mode), 1, Mathf.Max(1, distinct.Length - 2));
                var count = distinct.Length - hidden;
                var startingLetters = phrase.Where(char.IsLetter)
                    .GroupBy(character => character).OrderByDescending(group => group.Count())
                    .ThenBy(group => group.Key).Select(group => group.Key);
                // At higher tiers hide common letters too, not just rare one-off characters.
                if (PuzzleGenerator.Tier(erudition) >= 2) startingLetters = startingLetters.OrderBy(character => rng.Next());
                foreach (var character in startingLetters.Take(count))
                    revealed.Add(cipher[character]);
            }
            foreach (var part in saved.attemptedPairs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                attempted.Add(part);

            foreach (var cell in cells) cell.gameObject.SetActive(false);
            var usedRows = layout.Count == 0 ? 1 : layout.Max(item => item.slot / columns) + 1;
            if (content != null)
            {
                var viewHeight = scroll == null ? 660 : scroll.viewport.rect.height;
                content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(viewHeight, usedRows * 104 + 32));
                foreach (var item in layout)
                {
                    var rect = cells[item.slot].GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
                    rect.anchoredPosition = new Vector2((item.slot % columns - (columns - 1) / 2f) * 58,
                        -52 - (content.sizeDelta.y - usedRows * 104) / 2 - item.slot / columns * 104);
                }
                if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
            }
            foreach (var item in layout)
            {
                var cell = cells[item.slot];
                var code = char.IsLetter(item.character) ? cipher[item.character] : 0;
                cell.Bind(item.character, code, code == 0 || revealed.Contains(code));
            }
            RefreshKeyboard();
            if (owlGuide != null) owlGuide.text = erudition < 60
                ? "Я Совушка. Давай начнём!\nОдно число — одна буква.\nБольшую часть я уже открыла."
                : usedRows > 6 ? "Эта история чуть длиннее.\nЛистай свиток пальцем.\nС одинаковым числом — одна буква!" : "Не спеши. Прочитай открытые слова —\nони помогут найти ответ.";
            UpdateHearts();
            SetHintCount(hints);
            var restored = selectedSlot >= 0 && selectedSlot < cells.Length && cells[selectedSlot].gameObject.activeSelf && cells[selectedSlot].IsHiddenLetter;
            if (restored) SelectCell(selectedSlot); else SelectFirstHidden();
            UpdateProgress();
            game?.StorePuzzleProgress(this);
        }

        public void SelectCell(int index)
        {
            if (index < 0 || index >= cells.Length || !cells[index].IsHiddenLetter) return;
            selectedCode = cells[index].Code;
            selectedSlot = index;
            foreach (var cell in cells) if (cell.gameObject.activeSelf) cell.SetSelected(cell.Code == selectedCode, cell == cells[index]);
            feedbackText.text = "Выбрано число " + selectedCode + ". Какую букву спрятали?";
            EnsureVisible(index);
            game?.StorePuzzleProgress(this);
        }

        public void Guess(char letter, KeyboardKeyView key)
        {
            if (entry == null) return;
            if (selectedCode == 0) SelectFirstHidden();
            if (selectedCode == 0) return;
            letter = char.ToUpperInvariant(letter);
            if (cipher.TryGetValue(letter, out var usedCode) && revealed.Contains(usedCode))
            { feedbackText.text = "«" + letter + "» уже открыта во всём тексте — выбери другую."; return; }
            var pair = selectedCode + "-" + letter;
            if (attempted.Contains(pair))
            {
                feedbackText.text = "Эту букву для шифра " + selectedCode + " уже пробовали";
                return;
            }
            attempted.Add(pair);
            var correct = answers[selectedCode] == letter;
            key?.ShowResult(correct);
            if (Application.isPlaying) StartCoroutine(ResetKeyLater(key));
            game.RecordGuess(!correct);
            if (correct)
            {
                RevealCode(selectedCode);
                if (AllSolved()) { game.CompletePuzzle(this); return; }
                SelectFirstHidden();
                feedbackText.text = "Верно! «" + letter + "» открыта во всём тексте.";
            }
            else
            {
                mistakesInLevel++;
                remainingHearts = Mathf.Max(0, remainingHearts - 1);
                UpdateHearts();
                feedbackText.text = "Пока не подходит. Попробуй другую букву.\nОсталось сердечек: " + remainingHearts;
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
            selectedSlot = -1;
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
                selectedSlot = selectedSlot,
                helpRevision = 2,
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
            RefreshKeyboard();
            UpdateProgress();
        }

        private bool AllSolved() => cells.All(cell => !cell.gameObject.activeSelf || !cell.IsHiddenLetter);

        private void SelectFirstHidden()
        {
            var index = Array.FindIndex(cells, cell => cell.gameObject.activeSelf && cell.IsHiddenLetter);
            if (index < 0) { selectedCode = 0; return; }
            SelectCell(index);
        }

        private void RefreshKeyboard()
        {
            foreach (var key in keys) key.SetUsed(cipher.TryGetValue(key.Letter, out var code) && revealed.Contains(code));
        }

        private void UpdateProgress()
        {
            var total = cells.Count(cell => cell.gameObject.activeSelf && cell.Code > 0);
            var opened = cells.Count(cell => cell.gameObject.activeSelf && cell.Code > 0 && !cell.IsHiddenLetter);
            if (progressText != null) progressText.text = "Открыто " + opened + " из " + total + " букв";
            if (progressFill != null)
            {
                var fraction = total == 0 ? 0 : (float)opened / total;
                progressFill.fillAmount = fraction;
                var rect = progressFill.rectTransform;
                var track = rect.parent as RectTransform;
                if (track != null)
                {
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, .5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, track.rect.width * fraction);
                }
            }
            var words = PuzzleGenerator.WordCount(entry.text);
            var noun = words % 100 >= 11 && words % 100 <= 14 ? "слов" : words % 10 == 1 ? "слово" : words % 10 >= 2 && words % 10 <= 4 ? "слова" : "слов";
            if (instructionText != null) instructionText.text = "Найди " + HiddenKinds + " " + (HiddenKinds == 1 ? "букву" : HiddenKinds < 5 ? "буквы" : "букв") + " · " + words + " " + noun;
        }

        private void EnsureVisible(int index)
        {
            if (scroll == null || content == null || content.rect.height <= scroll.viewport.rect.height) return;
            var center = -cells[index].GetComponent<RectTransform>().anchoredPosition.y;
            var offset = content.anchoredPosition.y;
            var height = scroll.viewport.rect.height;
            if (center < offset + 60 || center > offset + height - 60)
            {
                scroll.StopMovement();
                content.anchoredPosition = new Vector2(0, Mathf.Clamp(center - height / 2, 0, content.rect.height - height));
            }
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
            var rowOffset = content == null ? Mathf.Max(0, (rows - row - 1) / 2) : 0;
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
