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
        public const int MaxHearts = 3;
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
        private PuzzleProgress scoring;

        public void TickPlayTime(float seconds)
        { if (scoring != null) scoring.activeSeconds += Mathf.Max(0, seconds); }

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
            remainingHearts = saved.remainingHearts > 0
                ? Mathf.Min(saved.remainingHearts, Mathf.Max(1, MaxHearts - saved.mistakesInLevel)) : MaxHearts;
            mistakesInLevel = saved.mistakesInLevel;
            scoring = new PuzzleProgress {
                scoringRevision = saved.scoringRevision,
                playerTierAtStart = saved.scoringRevision > 0 ? saved.playerTierAtStart : PuzzleGenerator.Tier(erudition),
                initialHiddenLetters = saved.initialHiddenLetters, hintsUsed = saved.hintsUsed,
                activeSeconds = saved.activeSeconds
            };

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
                var budget = PuzzleGenerator.StartingClues(letterCells.Length, puzzle.minimumErudition);
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
            foreach (var cell in cells)
            {
                if (cell.gameObject.activeSelf && !cell.IsHiddenLetter)
                {
                    HideCodeIfSolved(cell.Code);
                }
            }
            RefreshKeyboard();
            if (scoring.initialHiddenLetters == 0)
                scoring.initialHiddenLetters = cells.Count(cell => cell.gameObject.activeSelf && cell.IsHiddenLetter);
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
            foreach (var cell in cells) if (cell.gameObject.activeSelf) cell.SetSelected(cell == cells[index], cell == cells[index]);
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
                game.OfferHints();
                return;
            }
            var answer = answers[selectedCode];
            scoring.hintsUsed++;
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
                puzzleId = entry.id,
                remainingHearts = remainingHearts,
                revealedCodes = string.Join(",", revealed.OrderBy(code => code)),
                filledSlots = string.Join(",", filledSlots.OrderBy(slot => slot)),
                selectedCode = selectedCode,
                selectedSlot = selectedSlot,
                helpRevision = 5,
                mistakesInLevel = mistakesInLevel,
                scoringRevision = scoring.scoringRevision,
                playerTierAtStart = scoring.playerTierAtStart,
                initialHiddenLetters = scoring.initialHiddenLetters,
                hintsUsed = scoring.hintsUsed,
                activeSeconds = scoring.activeSeconds,
                attemptedPairs = string.Join(";", attempted.OrderBy(pair => pair))
            };
        }

        public void SetHintCount(int count)
        {
            if (hintCountText != null) hintCountText.text = count.ToString();
        }

        public void SetRemainingHeartsForDebug(int count)
        {
            remainingHearts = Mathf.Clamp(count, 1, MaxHearts);
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
            filledSlots.Add(selectedSlot);

            int openedCode = cells[selectedSlot].Code;

            cells[selectedSlot].Reveal();

            HideCodeIfSolved(openedCode);

            RefreshKeyboard();
            UpdateProgress();
        }

        private void HideCodeIfSolved(int code)
        {
            bool stillHidden = cells.Any(cell =>
                cell.gameObject.activeSelf &&
                cell.Code == code &&
                cell.IsHiddenLetter
            );

            // Есть ещё закрытые буквы с этим номером — оставляем цифру
            if (stillHidden)
                return;

            // Все буквы этого номера открыты — убираем цифры везде
            foreach (var cell in cells)
            {
                if (cell.gameObject.activeSelf && cell.Code == code)
                {
                    cell.HideCode();
                }
            }
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
            if (instructionText != null) instructionText.text = PuzzleGenerator.DifficultyName(entry.minimumErudition)
                + " · " + PuzzleGenerator.KindName(entry.kind) + "\nОсталось " + (total - opened) + " букв · " + words + " " + noun;
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
                hearts[i].SetActive(i < MaxHearts);
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
            var words = phrase.ToUpperInvariant()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0 || words.Any(word => word.Length > columns)) return null;

            // Keep one-letter Russian words/prepositions together with the word
            // that follows them. This prevents lonely "И", "А", "В", "К", etc.
            // from hanging at the far edge of one row while the related word
            // starts on the next row.
            var units = new List<string>();
            for (var i = 0; i < words.Length; i++)
            {
                if (IsSingleLetterWord(words[i]) && i + 1 < words.Length)
                {
                    var combined = words[i];
                    var end = i;
                    while (end + 1 < words.Length && IsSingleLetterWord(words[end]))
                    {
                        var candidate = combined + " " + words[end + 1];
                        if (candidate.Length > columns) break;
                        combined = candidate;
                        end++;
                    }

                    if (end > i)
                    {
                        units.Add(combined);
                        i = end;
                        continue;
                    }
                }

                units.Add(words[i]);
            }

            // First determine the minimum number of rows required by these
            // glued units. Then choose better line breaks with dynamic
            // programming so the rows are visually balanced instead of using
            // the old purely greedy wrapping.
            var lineCount = 1;
            var used = 0;
            foreach (var unit in units)
            {
                if (used > 0 && used + 1 + unit.Length > columns)
                {
                    lineCount++;
                    used = unit.Length;
                }
                else
                {
                    used += (used > 0 ? 1 : 0) + unit.Length;
                }
            }
            if (lineCount > rows) return null;

            const int infinity = int.MaxValue / 4;
            var cost = new int[units.Count + 1, lineCount + 1];
            var next = new int[units.Count + 1, lineCount + 1];
            for (var i = 0; i <= units.Count; i++)
                for (var remaining = 0; remaining <= lineCount; remaining++)
                {
                    cost[i, remaining] = infinity;
                    next[i, remaining] = -1;
                }
            cost[units.Count, 0] = 0;

            for (var remaining = 1; remaining <= lineCount; remaining++)
            {
                for (var start = units.Count - 1; start >= 0; start--)
                {
                    var lineLength = 0;
                    for (var end = start; end < units.Count; end++)
                    {
                        lineLength += (end > start ? 1 : 0) + units[end].Length;
                        if (lineLength > columns) break;
                        if (cost[end + 1, remaining - 1] >= infinity) continue;

                        var slack = columns - lineLength;
                        var candidateCost = slack * slack + cost[end + 1, remaining - 1];
                        if (candidateCost >= cost[start, remaining]) continue;
                        cost[start, remaining] = candidateCost;
                        next[start, remaining] = end + 1;
                    }
                }
            }

            if (next[0, lineCount] < 0) return null;

            var lines = new List<string>(lineCount);
            var unitIndex = 0;
            var linesLeft = lineCount;
            while (linesLeft > 0)
            {
                var end = next[unitIndex, linesLeft];
                if (end <= unitIndex) return null;
                lines.Add(string.Join(" ", units.Skip(unitIndex).Take(end - unitIndex)));
                unitIndex = end;
                linesLeft--;
            }

            var rowOffset = content == null ? Mathf.Max(0, (rows - lines.Count) / 2) : 0;
            var result = new List<LayoutItem>();
            for (var row = 0; row < lines.Count; row++)
            {
                var line = lines[row];
                var horizontalOffset = (columns - line.Length) / 2;
                for (var column = 0; column < line.Length; column++)
                {
                    result.Add(new LayoutItem
                    {
                        slot = (row + rowOffset) * columns + horizontalOffset + column,
                        character = line[column]
                    });
                }
            }
            return result;
        }

        private static bool IsSingleLetterWord(string word)
        {
            return word.Count(char.IsLetter) == 1;
        }
    }
}
