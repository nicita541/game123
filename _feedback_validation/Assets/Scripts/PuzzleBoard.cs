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
        private readonly HashSet<int> filledSlots = new HashSet<int>();
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
        public int HiddenKinds => cells.Where(cell => cell.gameObject.activeSelf && cell.IsHiddenLetter)
            .Select(cell => cell.Code).Distinct().Count();
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
            filledSlots.Clear();
            attempted.Clear();
            selectedCode = saved.selectedCode;
            selectedSlot = saved.selectedSlot;
            remainingHearts = saved.remainingHearts > 0 ? saved.remainingHearts : 5;
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
            foreach (var part in saved.attemptedPairs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                attempted.Add(part);
            foreach (var part in (saved.filledSlots ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(part, out var slot) && layout.Any(item => item.slot == slot && char.IsLetter(item.character)))
                    filledSlots.Add(slot);

            // Only a handful of individual cells are given, never every occurrence
            // of a letter. Already-started games keep their exact saved progress.
            if (saved.helpRevision == 0 && revealed.Count == 0 && filledSlots.Count == 0)
            {
                var letterCells = layout.Where(item => char.IsLetter(item.character)).ToArray();
                var budget = PuzzleGenerator.StartingClues(letterCells.Length, erudition);
                // Divide the text into bands so the clues are spread across the
                // whole phrase. Prefer different letters, rather than filling
                // all occurrences of one cipher. Never give away a whole word.
                var wordBySlot = new Dictionary<int, int>();
                var word = -1;
                var inWord = false;
                var previousRow = -1;
                foreach (var item in layout)
                {
                    if (item.slot / columns != previousRow) inWord = false;
                    previousRow = item.slot / columns;
                    if (!char.IsLetter(item.character)) { inWord = false; continue; }
                    if (!inWord) word++;
                    inWord = true;
                    wordBySlot[item.slot] = word;
                }
                var hiddenPerWord = wordBySlot.Values.GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count());
                var givenLetters = new HashSet<char>();
                for (var band = 0; band < budget; band++)
                {
                    var start = band * letterCells.Length / budget;
                    var end = (band + 1) * letterCells.Length / budget;
                    var candidates = letterCells.Skip(start).Take(end - start)
                        .Where(item => hiddenPerWord[wordBySlot[item.slot]] > 1)
                        .OrderBy(item => givenLetters.Contains(item.character) ? 1 : 0)
                        .ThenBy(item => rng.Next()).ToArray();
                    if (candidates.Length == 0) continue;
                    var clue = candidates[0];
                    filledSlots.Add(clue.slot);
                    givenLetters.Add(clue.character);
                    hiddenPerWord[wordBySlot[clue.slot]]--;
                }
            }

            foreach (var cell in cells) cell.gameObject.SetActive(false);
            if (content != null)
            {
                var viewHeight = scroll == null ? 660 : scroll.viewport.rect.height;
                // Cell/row positions come entirely from the scene. Only the scroll
                // extent follows the last occupied authored cell.
                var bottom = layout.Max(item =>
                {
                    var rect = (RectTransform)cells[item.slot].transform;
                    var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                    return -content.InverseTransformPoint(corners[0]).y;
                });
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(viewHeight, bottom));
                if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
            }
            foreach (var item in layout)
            {
                var cell = cells[item.slot];
                var code = char.IsLetter(item.character) ? cipher[item.character] : 0;
                cell.Bind(item.character, code, code == 0 || revealed.Contains(code) || filledSlots.Contains(item.slot));
            }
            RefreshKeyboard();
            if (owlGuide != null) owlGuide.text = "";
            UpdateHearts();
            SetHintCount(hints);
            var restored = selectedSlot >= 0 && selectedSlot < cells.Length && cells[selectedSlot].gameObject.activeSelf && cells[selectedSlot].IsHiddenLetter;
            if (restored) SelectCell(selectedSlot); else SelectFirstHidden();
            UpdateProgress();
            game?.StorePuzzleProgress(this);
        }

        public void SelectCell(int index)
        {
            if (index < 0 || index >= cells.Length || !cells[index].gameObject.activeSelf || !cells[index].IsHiddenLetter) return;
            selectedCode = cells[index].Code;
            selectedSlot = index;
            foreach (var cell in cells) if (cell.gameObject.activeSelf) cell.SetSelected(cell.Code == selectedCode, cell == cells[index]);
            feedbackText.text = "";
            EnsureVisible(index);
            game?.StorePuzzleProgress(this);
        }

        public void Guess(char letter, KeyboardKeyView key)
        {
            if (entry == null) return;
            if (!EnsureSelection()) return;
            letter = char.ToUpperInvariant(letter);
            if (cipher.TryGetValue(letter, out var usedCode) && !cells.Any(cell => cell.gameObject.activeSelf && cell.Code == usedCode && cell.IsHiddenLetter))
            { feedbackText.text = "«" + letter + "» уже открыта во всём тексте — выбери другую."; return; }
            var pair = selectedCode + "-" + letter;
            if (attempted.Contains(pair))
            {
                feedbackText.text = "Эту букву для шифра " + selectedCode + " уже пробовали";
                return;
            }
            var correct = answers[selectedCode] == letter;
            key?.ShowResult(correct);
            if (Application.isPlaying) StartCoroutine(ResetKeyLater(key));
            game.RecordGuess(!correct);
            if (correct)
            {
                RevealSelected();
                if (AllSolved()) { game.CompletePuzzle(this); return; }
                SelectNextHidden();
                feedbackText.text = "";
            }
            else
            {
                attempted.Add(pair);
                mistakesInLevel++;
                remainingHearts = Mathf.Max(0, remainingHearts - 1);
                UpdateHearts();
                feedbackText.text = "Не подходит · −1 сердце";
                if (remainingHearts == 0) { game.FailPuzzle(this); return; }
            }
            game.StorePuzzleProgress(this);
        }

        public void UseHint()
        {
            if (entry == null) return;
            if (!EnsureSelection()) return;
            if (!game.ConsumeHint())
            {
                feedbackText.text = "Подсказки закончились — их можно купить в магазине";
                return;
            }
            var answer = answers[selectedCode];
            var code = selectedCode;
            RevealSelected();
            SetHintCount(game.Hints);
            if (AllSolved()) { game.CompletePuzzle(this); return; }
            SelectNextHidden();
            feedbackText.text = "Подсказка: " + code + " — «" + answer + "». Открыта одна клетка.";
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
                filledSlots = string.Join(",", filledSlots.OrderBy(slot => slot)),
                selectedCode = selectedCode,
                selectedSlot = selectedSlot,
                helpRevision = 5,
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
            remainingHearts = Mathf.Clamp(count, 1, 5);
            UpdateHearts();
            game.StorePuzzleProgress(this);
        }

        private bool EnsureSelection()
        {
            if (selectedSlot < 0 || selectedSlot >= cells.Length || !cells[selectedSlot].gameObject.activeSelf || !cells[selectedSlot].IsHiddenLetter)
                SelectFirstHidden();
            return selectedCode != 0;
        }

        private void RevealSelected()
        {
            // Initial/legacy reveals apply to a cipher; player answers apply to one authored cell only.
            filledSlots.Add(selectedSlot);
            cells[selectedSlot].Reveal();
            RefreshKeyboard();
            UpdateProgress();
        }

        private bool AllSolved() => cells.All(cell => !cell.gameObject.activeSelf || !cell.IsHiddenLetter);

        private void SelectFirstHidden()
        {
            var index = Array.FindIndex(cells, cell => cell.gameObject.activeSelf && cell.IsHiddenLetter);
            if (index < 0) { selectedCode = 0; selectedSlot = -1; return; }
            SelectCell(index);
        }

        private void SelectNextHidden()
        {
            // Continue reading from the cell just filled. Earlier gaps are only
            // revisited after reaching the end, so a lower row stays on screen.
            var index = Array.FindIndex(cells, selectedSlot + 1,
                cell => cell.gameObject.activeSelf && cell.IsHiddenLetter);
            if (index >= 0) SelectCell(index);
            else SelectFirstHidden();
        }

        private void RefreshKeyboard()
        {
            foreach (var key in keys)
            {
                if (!cipher.TryGetValue(key.Letter, out var code)) { key.SetState(false, false); continue; }
                var matching = cells.Where(cell => cell.gameObject.activeSelf && cell.Code == code).ToArray();
                key.SetState(matching.Any(cell => !cell.IsHiddenLetter), matching.All(cell => !cell.IsHiddenLetter));
            }
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
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, track.rect.width * fraction);
                }
            }
            var words = PuzzleGenerator.WordCount(entry.text);
            var noun = words % 100 >= 11 && words % 100 <= 14 ? "слов" : words % 10 == 1 ? "слово" : words % 10 >= 2 && words % 10 <= 4 ? "слова" : "слов";
            if (instructionText != null) instructionText.text = "Заполни клетки: " + (total - opened) + " · " + words + " " + noun;
        }

        private void EnsureVisible(int index)
        {
            if (scroll == null || content == null || content.rect.height <= scroll.viewport.rect.height) return;
            var center = -content.InverseTransformPoint(cells[index].transform.position).y;
            var offset = content.anchoredPosition.y;
            var height = scroll.viewport.rect.height;
            if (center < offset + 60 || center > offset + height - 60)
            {
                scroll.StopMovement();
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(center - height / 2, 0, content.rect.height - height));
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
