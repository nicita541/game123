using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Erudition
{
    public sealed class CryptogramGame : MonoBehaviour
    {
        private const int MaxFeathers = 5;
        private const int FeatherRestoreMinutes = 20;

        public PuzzleLibrary library;
        public AdsBridge ads;
        public GameAudio soundPlayer;
        public ReferenceUiPresenter presentation;
        public GameObject[] screens;
        public PuzzleBoard classicBoard;
        public PuzzleBoard turboBoard;

        public Text mainErudition;
        public Text mainFeathers;
        public Text classicErudition;
        public Text turboErudition;
        public Text shopFeathers;
        public Text shopCoins;
        public Text noFeathersCounter;
        public Text noFeathersCounterRestored;
        public Text noFeathersTimer;
        public Text noFeathersNote;
        public Text classicStartLabel;
        public Text turboStartLabel;

        public Text victoryQuote;
        public Text victorySource;
        public Text victoryEruditionReward;
        public Text victoryCollectionReward;
        public Text victoryBonusNote;
        public Button victoryAdButton;
        public Button noFeathersAdButton;

        public Text defeatQuote;
        public Text defeatSource;
        public Text defeatMessage;

        public Text statsLevel;
        public Text statsSolved;
        public Text statsAccuracy;
        public Text statsStreak;
        public Text statsBest;
        public RectTransform statsProgressFill;
        public RectTransform[] activityBars;
        public Text[] activityDayLabels;

        public GameObject[] collectionGroups;
        public Button[] collectionTabs;
        public CollectionCardView[] authorCards;
        public CollectionCardView[] themeCards;
        public CollectionCardView[] bookCards;
        public Button[] achievementTabs;
        public AchievementCardView[] achievementCards;
        public Text[] settingValues;
        public Text shopMessage;
        public Text debugValues;
        public GameObject debugOpenButton;

        private GameSave save;
        private int currentScreen;
        private int collectionTab;
        private int achievementTab;
        private bool victoryBonusClaimed;
        private float nextTimerUpdate;
        private Dictionary<Text, int> baseFontSizes;
        private string lastCompletedPuzzleId;
        private PuzzleEntry[] puzzles;
        private PuzzleMode? pendingMode;
        private int pendingIndex = -1;
        public static CryptogramGame Current { get; private set; }
        public PuzzleEntry[] Entries => puzzles ?? (puzzles = PuzzleGenerator.Build(library));

        public int Hints => save == null ? 0 : save.hints;

        private void Awake()
        {
            Current = this;
            baseFontSizes = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .ToDictionary(label => label, label => label.fontSize);
            save = SaveStore.Load();
            RollDailyWindow();
            if (save.activityHistory.Count == 0)
                for (var i = 0; i < 7; i++)
                    if (save.dailySolved[i] > 0) save.activityHistory.Add(new ActivityDay { date = DateTime.UtcNow.Date.AddDays(i - 6).Ticks, solved = save.dailySolved[i] });
            RestoreEnergy();
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (debugOpenButton != null) debugOpenButton.SetActive(false);
#endif
            ShowScreen(0);
            ApplyTextScale();
            UpdateAllUi();
        }

        private IEnumerator Start()
        {
            if (!SceneManager.GetSceneByName("GameplayScene").isLoaded)
                yield return SceneManager.LoadSceneAsync("GameplayScene", LoadSceneMode.Additive);
            var link = FindFirstObjectByType<GameplaySceneLink>();
            if (link != null) RegisterGameplay(link);
        }

        private void OnDestroy() { if (Current == this) Current = null; }

        public void RegisterGameplay(GameplaySceneLink link)
        {
            classicBoard = link.classicBoard; turboBoard = link.turboBoard;
            screens[2] = link.classicScreen; screens[3] = link.turboScreen;
            classicErudition = link.classicErudition; turboErudition = link.turboErudition;
            classicBoard.SetGame(this); turboBoard.SetGame(this);
            foreach (var action in link.actions) action.SetGame(this);
            if (baseFontSizes != null)
                foreach (var label in link.GetComponentsInChildren<Text>(true))
                    if (!baseFontSizes.ContainsKey(label)) baseFontSizes[label] = label.fontSize;
            screens[2].SetActive(currentScreen == 2); screens[3].SetActive(currentScreen == 3);
            ApplyTextScale(); UpdateAllUi();
            if (pendingMode.HasValue)
            { var mode = pendingMode.Value; var index = pendingIndex; pendingMode = null; StartMode(mode, index); }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextTimerUpdate) return;
            nextTimerUpdate = Time.unscaledTime + 1f;
            if (RestoreEnergy()) UpdateAllUi();
            UpdateTimer();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && save != null) SaveStore.Save(save);
            else if (!paused && save != null) { if (RestoreEnergy()) UpdateAllUi(); }
        }

        private void OnApplicationQuit()
        {
            if (save != null) SaveStore.Save(save);
        }

        public void Execute(UiActionKind action, int parameter)
        {
            soundPlayer?.Click();
            switch (action)
            {
                case UiActionKind.Home: pendingMode = null; ShowScreen(0); break;
                case UiActionKind.CollectionDetails: presentation?.ShowCollection(parameter, save, Entries); ShowScreen(1); break;
                case UiActionKind.StatisticsPeriod: presentation?.SetPeriod(parameter); UpdateAllUi(); break;
                case UiActionKind.ClearSelection: ActiveBoard()?.ClearSelection(); break;
                case UiActionKind.AchievementDetails: presentation?.ShowAchievement(parameter, achievementCards); break;
                case UiActionKind.ClosePopup: presentation?.ClosePopup(); break;
                case UiActionKind.LikeQuote:
                    if (!string.IsNullOrEmpty(lastCompletedPuzzleId) && !save.likedPuzzleIds.Split('|').Contains(lastCompletedPuzzleId))
                    { save.likedPuzzleIds += lastCompletedPuzzleId + "|"; Persist(); }
                    break;
                case UiActionKind.Classic: StartMode(PuzzleMode.Classic); break;
                case UiActionKind.Turbo: StartMode(PuzzleMode.Turbo); break;
                case UiActionKind.Statistics: UpdateStats(); ShowScreen(6); break;
                case UiActionKind.Collections: ShowCollectionTab(0); ShowScreen(7); break;
                case UiActionKind.Achievements: ShowAchievementTab(0); ShowScreen(8); break;
                case UiActionKind.Shop: UpdateAllUi(); ShowScreen(9); break;
                case UiActionKind.Settings: ShowScreen(10); break;
                case UiActionKind.Back: GoBack(); break;
                case UiActionKind.Continue: ShowScreen(0); break;
                case UiActionKind.Hint: ActiveBoard()?.UseHint(); break;
                case UiActionKind.Check: ActiveBoard()?.Check(); break;
                case UiActionKind.RewardVictory: RewardVictory(); break;
                case UiActionKind.RewardFeather: RewardFeather(); break;
                case UiActionKind.BuyFiveFeathers: BuyFeathers(5, 100); break;
                case UiActionKind.BuyFifteenFeathers: BuyFeathers(15, 250); break;
                case UiActionKind.BuyFiveHints: BuyHints(); break;
                case UiActionKind.PremiumUnavailable: SayShop("Этот товар появится после подключения платежей"); break;
                case UiActionKind.FeatherInfo: SayShop("Выберите пачку ниже. Лишние перья останутся в запасе."); break;
                case UiActionKind.CoinInfo: SayShop("Монеты выдаются за решение криптограмм."); break;
                case UiActionKind.CollectionTab: ShowCollectionTab(parameter); break;
                case UiActionKind.AchievementTab: ShowAchievementTab(parameter); break;
                case UiActionKind.ToggleSetting: ToggleSetting(parameter); break;
                case UiActionKind.Debug: if (DebugAllowed()) ShowScreen(11); break;
                case UiActionKind.DebugAddErudition: if (DebugAllowed()) { save.erudition += 10; Persist(); } break;
                case UiActionKind.DebugRemoveFeather: if (DebugAllowed()) { SpendFeather(); Persist(); } break;
                case UiActionKind.DebugZeroFeathers: if (DebugAllowed()) { save.feathers = 0; StartRecoveryClock(); Persist(); ShowScreen(5); } break;
                case UiActionKind.DebugRefillFeathers: if (DebugAllowed()) { save.feathers = 5; save.nextFeatherUtcTicks = 0; Persist(); } break;
                case UiActionKind.DebugAddHint: if (DebugAllowed()) { save.hints++; Persist(); } break;
                case UiActionKind.DebugVictory: if (DebugAllowed()) DebugVictory(); break;
                case UiActionKind.DebugDefeat: if (DebugAllowed()) DebugDefeat(); break;
                case UiActionKind.DebugReset: if (DebugAllowed()) ResetProgress(); break;
                case UiActionKind.Retry: RetryFailedPuzzle(); break;
                case UiActionKind.DebugOneHeart: if (DebugAllowed()) { var board = EnsureDebugPuzzle(); board?.SetRemainingHeartsForDebug(1); } break;
                case UiActionKind.DebugUnlockCollections:
                    if (DebugAllowed())
                    {
                        for (var i = 0; i < save.authorProgress.Length; i++) save.authorProgress[i] = authorCards[i].target;
                        for (var i = 0; i < save.themeProgress.Length; i++) save.themeProgress[i] = themeCards[i].target;
                        for (var i = 0; i < save.bookProgress.Length; i++) save.bookProgress[i] = bookCards[i].target;
                        Persist();
                    }
                    break;
                case UiActionKind.DebugUnlockAchievements:
                    if (DebugAllowed()) { save.solved = Mathf.Max(50, save.solved); save.classicSolved = Mathf.Max(20, save.classicSolved);
                        save.erudition = Mathf.Max(250, save.erudition); save.perfectWins = Mathf.Max(30, save.perfectWins);
                        save.bestEveningStreak = 5; save.likedPuzzleIds = string.Join("|", Enumerable.Range(0, 20).Select(i => "debug-" + i)) + "|"; Persist(); }
                    break;
            }
        }

        public void StorePuzzleProgress(PuzzleBoard board)
        {
            if (save == null || board == null || board.PuzzleIndex < 0) return;
            save.activePuzzle = board.ExportProgress();
            SaveStore.Save(save);
        }

        public void RecordGuess(bool wrong)
        {
            soundPlayer?.Guess(!wrong);
#if UNITY_ANDROID || UNITY_IOS
            if (wrong && save.vibration) Handheld.Vibrate();
#endif
            save.guesses++;
            if (wrong) save.mistakes++;
            SaveStore.Save(save);
        }

        public bool ConsumeHint()
        {
            if (save.hints <= 0) return false;
            save.hints--;
            Persist();
            return true;
        }

        public void CompletePuzzle(PuzzleBoard board)
        {
            if (board == null || board.PuzzleIndex < 0 || save.activePuzzle.puzzleIndex != board.PuzzleIndex) return;
            var entry = board.Entry;
            var eruditionReward = board.Mode == PuzzleMode.Classic ? 20 : 12;
            var coinReward = board.Mode == PuzzleMode.Classic ? 30 : 20;
            save.erudition += eruditionReward;
            save.coins += coinReward;
            save.solved++;
            save.streak++;
            save.bestStreak = Mathf.Max(save.bestStreak, save.streak);
            if (board.Mode == PuzzleMode.Classic) save.classicSolved++; else save.turboSolved++;
            if (board.MistakesInLevel == 0) save.perfectWins++;
            if (entry.authorIndex >= 0 && entry.authorIndex < save.authorProgress.Length) save.authorProgress[entry.authorIndex]++;
            if (entry.themeIndex >= 0 && entry.themeIndex < save.themeProgress.Length) save.themeProgress[entry.themeIndex]++;
            if (entry.bookIndex >= 0 && entry.bookIndex < save.bookProgress.Length) save.bookProgress[entry.bookIndex]++;
            RollDailyWindow();
            save.dailySolved[6]++;
            var day = save.activityHistory.FirstOrDefault(item => item.date == DateTime.UtcNow.Date.Ticks);
            if (day == null) { day = new ActivityDay { date = DateTime.UtcNow.Date.Ticks }; save.activityHistory.Add(day); }
            day.solved++;
            save.activityHistory.RemoveAll(item => item.date < DateTime.UtcNow.Date.AddYears(-2).Ticks);
            save.eveningStreak = DateTime.Now.Hour >= 18 ? save.eveningStreak + 1 : 0;
            save.bestEveningStreak = Mathf.Max(save.bestEveningStreak, save.eveningStreak);
            lastCompletedPuzzleId = entry.id;
            if (!save.solvedPuzzleIds.Split('|').Contains(entry.id)) save.solvedPuzzleIds += entry.id + "|";
            save.activePuzzle = new PuzzleProgress();
            soundPlayer?.Win();
            victoryQuote.text = "«" + entry.text + "»";
            victorySource.text = "— " + entry.source;
            victoryEruditionReward.text = "<size=64>+" + eruditionReward + "</size>\nк эрудиции";
            victoryCollectionReward.text = "<size=64>+1</size>\nв коллекцию";
            presentation?.SetCoinsReward(coinReward);
            victoryBonusClaimed = false;
            if (victoryBonusNote != null) victoryBonusNote.gameObject.SetActive(false);
            if (victoryAdButton != null) victoryAdButton.interactable = ads != null && ads.IsAvailable;
            Persist();
            ShowScreen(4);
        }

        public void FailPuzzle(PuzzleBoard board)
        {
            if (board == null || board.Entry == null) return;
            save.lastFailedPuzzleIndex = board.PuzzleIndex;
            save.streak = 0;
            save.eveningStreak = 0;
            save.activePuzzle = new PuzzleProgress();
            if (defeatQuote != null) defeatQuote.text = "«" + board.Entry.text + "»";
            if (defeatSource != null) defeatSource.text = "— " + board.Entry.source;
            if (defeatMessage != null) defeatMessage.text = "Сердечки закончились, но всё получится.\nТеперь история открыта — попробуем ещё?";
            Persist();
            ShowScreen(12);
        }

        private void StartMode(PuzzleMode mode, int requestedIndex = -1)
        {
            RestoreEnergy();
            var board = mode == PuzzleMode.Classic ? classicBoard : turboBoard;
            if (board == null) { pendingMode = mode; pendingIndex = requestedIndex; return; }
            var progress = save.activePuzzle;
            if (requestedIndex < 0 && progress.puzzleIndex >= 0 && progress.puzzleIndex < Entries.Length
                && Entries[progress.puzzleIndex].mode == mode && progress.remainingHearts > 0)
            {
                board.StartPuzzle(Entries[progress.puzzleIndex], progress.puzzleIndex, progress, save.erudition, save.hints);
                ShowScreen(mode == PuzzleMode.Classic ? 2 : 3);
                return;
            }
            if (save.feathers <= 0)
            {
                UpdateEnergyUi();
                ShowScreen(5);
                return;
            }
            var index = requestedIndex >= 0 ? requestedIndex : ChoosePuzzle(mode, board);
            if (index < 0) return;
            SpendFeather();
            save.activePuzzle = new PuzzleProgress
            {
                puzzleIndex = index,
                remainingHearts = mode == PuzzleMode.Classic ? 5 : 3
            };
            if (mode == PuzzleMode.Classic) save.lastClassicIndex = index; else save.lastTurboIndex = index;
            board.StartPuzzle(Entries[index], index, save.activePuzzle, save.erudition, save.hints);
            Persist();
            ShowScreen(mode == PuzzleMode.Classic ? 2 : 3);
        }

        private int ChoosePuzzle(PuzzleMode mode, PuzzleBoard board)
        {
            var eligible = Enumerable.Range(0, Entries.Length)
                .Where(index => Entries[index].mode == mode
                    && Entries[index].minimumErudition <= save.erudition
                    && board.CanFit(Entries[index].text)).ToList();
            if (eligible.Count == 0) return -1;
            // Periodic author reviews keep every collection attainable after harder tiers unlock.
            if (mode == PuzzleMode.Classic && save.classicSolved > 0 && save.classicSolved % 3 == 0)
            {
                var review = eligible.Where(index => Entries[index].authorIndex >= 0)
                    .OrderBy(index => (float)save.authorProgress[Entries[index].authorIndex]
                        / authorCards[Entries[index].authorIndex].target)
                    .ThenBy(index => index == save.lastClassicIndex ? 1 : 0).ToList();
                if (review.Count > 0) return review[0];
            }
            var generated = eligible.Where(index => Entries[index].id.StartsWith("generated_v1_")).ToList();
            var highestTier = generated.Max(index => Entries[index].minimumErudition);
            var tier = generated.Where(index => Entries[index].minimumErudition == highestTier).ToList();
            var last = mode == PuzzleMode.Classic ? save.lastClassicIndex : save.lastTurboIndex;
            return tier.Where(index => index > last).DefaultIfEmpty(tier[0]).First();
        }

        private void RetryFailedPuzzle()
        {
            var index = save.lastFailedPuzzleIndex;
            if (index < 0 || index >= Entries.Length) { ShowScreen(0); return; }
            StartMode(Entries[index].mode, index);
        }

        private void RewardVictory()
        {
            if (victoryBonusClaimed || ads == null || !ads.IsAvailable) return;
            ads.ShowRewarded(success =>
            {
                if (!success) return;
                victoryBonusClaimed = true;
                save.erudition += 10;
                save.hints++;
                if (victoryBonusNote != null)
                {
                    victoryBonusNote.text = "Бонус: +10 эрудиции и +1 подсказка";
                    victoryBonusNote.gameObject.SetActive(true);
                }
                if (victoryAdButton != null) victoryAdButton.interactable = false;
                Persist();
            });
        }

        private void RewardFeather()
        {
            if (ads == null || !ads.IsAvailable || save.feathers >= MaxFeathers) return;
            ads.ShowRewarded(success =>
            {
                if (!success) return;
                save.feathers = Mathf.Min(MaxFeathers, save.feathers + 1);
                if (save.feathers >= MaxFeathers) save.nextFeatherUtcTicks = 0;
                else StartRecoveryClock();
                if (noFeathersNote != null)
                {
                    noFeathersNote.text = "Перо восстановлено! Можно начать уровень.";
                    noFeathersNote.gameObject.SetActive(true);
                }
                Persist();
            });
        }

        private void BuyFeathers(int amount, int cost)
        {
            if (save.coins < cost) { SayShop("Недостаточно монет"); return; }
            save.coins -= cost;
            save.reserveFeathers += amount;
            FillFromReserve();
            SayShop("Перья добавлены! В запасе: " + save.reserveFeathers);
            Persist();
        }

        private void FillFromReserve()
        {
            var needed = MaxFeathers - save.feathers;
            var amount = Mathf.Min(needed, save.reserveFeathers);
            save.feathers += amount;
            save.reserveFeathers -= amount;
            if (save.feathers >= MaxFeathers) save.nextFeatherUtcTicks = 0;
            else StartRecoveryClock();
        }

        private void BuyHints()
        {
            if (save.coins < 150) { SayShop("Недостаточно монет"); return; }
            save.coins -= 150;
            save.hints += 5;
            SayShop("Добавлено 5 подсказок!");
            Persist();
        }

        private void SayShop(string message)
        {
            if (shopMessage != null) shopMessage.text = message;
        }

        private bool SpendFeather()
        {
            if (save.feathers <= 0) return false;
            save.feathers--;
            if (save.reserveFeathers > 0) FillFromReserve();
            else StartRecoveryClock();
            return true;
        }

        private void StartRecoveryClock()
        {
            if (save.feathers >= MaxFeathers) { save.nextFeatherUtcTicks = 0; return; }
            if (save.nextFeatherUtcTicks <= 0) save.nextFeatherUtcTicks = DateTime.UtcNow.AddMinutes(FeatherRestoreMinutes).Ticks;
        }

        private bool RestoreEnergy()
        {
            if (save.reserveFeathers > 0 && save.feathers < MaxFeathers)
            {
                FillFromReserve();
                SaveStore.Save(save);
                return true;
            }
            if (save.feathers >= MaxFeathers)
            {
                if (save.nextFeatherUtcTicks == 0) return false;
                save.nextFeatherUtcTicks = 0;
                SaveStore.Save(save);
                return true;
            }
            if (save.nextFeatherUtcTicks <= 0)
            {
                StartRecoveryClock();
                SaveStore.Save(save);
                return true;
            }
            var changed = false;
            var now = DateTime.UtcNow.Ticks;
            while (save.feathers < MaxFeathers && now >= save.nextFeatherUtcTicks)
            {
                save.feathers++;
                save.nextFeatherUtcTicks += TimeSpan.FromMinutes(FeatherRestoreMinutes).Ticks;
                changed = true;
            }
            if (save.feathers >= MaxFeathers) save.nextFeatherUtcTicks = 0;
            if (changed) SaveStore.Save(save);
            return changed;
        }

        private void UpdateTimer()
        {
            if (noFeathersTimer == null) return;
            if (save.feathers >= MaxFeathers || save.nextFeatherUtcTicks <= 0)
            {
                noFeathersTimer.text = "Готово";
                return;
            }
            var remaining = Math.Max(0, save.nextFeatherUtcTicks - DateTime.UtcNow.Ticks);
            var span = TimeSpan.FromTicks(remaining);
            noFeathersTimer.text = ((int)span.TotalMinutes).ToString("00") + ":" + span.Seconds.ToString("00");
        }

        private void RollDailyWindow()
        {
            var today = DateTime.UtcNow.Date.Ticks;
            if (save.lastDailyUpdateUtcTicks <= 0) { save.lastDailyUpdateUtcTicks = today; return; }
            var days = Mathf.Clamp((int)((today - save.lastDailyUpdateUtcTicks) / TimeSpan.TicksPerDay), 0, 7);
            if (days == 0) return;
            for (var i = 0; i < 7; i++) save.dailySolved[i] = i + days < 7 ? save.dailySolved[i + days] : 0;
            save.lastDailyUpdateUtcTicks = today;
            SaveStore.Save(save);
        }

        private void GoBack()
        {
            if (currentScreen == 11) ShowScreen(10);
            else if (currentScreen == 1) ShowScreen(7);
            else ShowScreen(0);
        }

        private PuzzleBoard ActiveBoard()
        {
            return currentScreen == 2 ? classicBoard : currentScreen == 3 ? turboBoard : null;
        }

        private void ShowScreen(int index)
        {
            if (screens == null || index < 0 || index >= screens.Length) return;
            currentScreen = index;
            presentation?.ClosePopup();
            for (var i = 0; i < screens.Length; i++) if (screens[i] != null) screens[i].SetActive(i == index);
            if (index == 7) ShowCollectionTab(collectionTab);
            if (index == 8) ShowAchievementTab(achievementTab);
            if (index == 5 && noFeathersNote != null) noFeathersNote.gameObject.SetActive(false);
            UpdateAllUi();
        }

        private void UpdateAllUi()
        {
            if (save == null) return;
            if (mainErudition != null) mainErudition.text = save.erudition.ToString();
            if (classicErudition != null) classicErudition.text = save.erudition.ToString();
            if (turboErudition != null) turboErudition.text = save.erudition.ToString();
            if (classicStartLabel != null) classicStartLabel.text = IsCurrentMode(PuzzleMode.Classic)
                ? "Продолжить" : "Классика";
            if (turboStartLabel != null) turboStartLabel.text = IsCurrentMode(PuzzleMode.Turbo)
                ? "Продолжить" : "Турбо";
            classicBoard?.SetHintCount(save.hints);
            turboBoard?.SetHintCount(save.hints);
            UpdateEnergyUi();
            UpdateStats();
            UpdateCollections();
            UpdateAchievements();
            UpdateSettings();
            presentation?.Refresh(save, currentScreen, lastCompletedPuzzleId);
            if (debugValues != null) debugValues.text = "Эрудиция: " + save.erudition + "    Перья: " + save.feathers + "/5    Подсказки: " + save.hints + "    Ошибки: " + save.mistakes;
        }

        private bool IsCurrentMode(PuzzleMode mode)
        {
            var index = save.activePuzzle.puzzleIndex;
            return index >= 0 && index < Entries.Length && Entries[index].mode == mode;
        }

        private void UpdateEnergyUi()
        {
            var featherValue = save.feathers + "/" + MaxFeathers;
            if (mainFeathers != null) mainFeathers.text = featherValue;
            if (shopFeathers != null) shopFeathers.text = featherValue
                + (save.reserveFeathers > 0 ? "  +" + save.reserveFeathers : "");
            if (shopCoins != null) shopCoins.text = save.coins.ToString();
            if (noFeathersCounter != null) noFeathersCounter.text = featherValue;
            if (noFeathersCounterRestored != null) noFeathersCounterRestored.text = featherValue;
            if (noFeathersAdButton != null) noFeathersAdButton.interactable = ads != null && ads.IsAvailable && save.feathers < MaxFeathers;
            UpdateTimer();
        }

        private void UpdateStats()
        {
            if (statsLevel != null) statsLevel.text = "Эрудиция " + save.erudition;
            if (statsSolved != null) statsSolved.text = save.solved.ToString();
            if (statsAccuracy != null) statsAccuracy.text = (save.guesses == 0 ? 100 : Mathf.RoundToInt(100f * (save.guesses - save.mistakes) / save.guesses)) + "%";
            if (statsStreak != null) statsStreak.text = save.streak.ToString();
            if (statsBest != null) statsBest.text = save.bestStreak.ToString();
            if (statsProgressFill != null)
            {
                var size = statsProgressFill.sizeDelta;
                size.x = 760 * Mathf.Clamp01((save.erudition % 100) / 100f);
                statsProgressFill.sizeDelta = size;
            }
            if (activityBars != null)
                for (var i = 0; i < Math.Min(7, activityBars.Length); i++)
                {
                    var height = Mathf.Clamp(50 + save.dailySolved[i] * 40, 50, 270);
                    var size = activityBars[i].sizeDelta;
                    size.y = height;
                    activityBars[i].sizeDelta = size;
                    var position = activityBars[i].anchoredPosition;
                    position.y = -145 + height / 2f;
                    activityBars[i].anchoredPosition = position;
                }
            var days = new[] { "ВС", "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ" };
            for (var i = 0; activityDayLabels != null && i < activityDayLabels.Length; i++)
                activityDayLabels[i].text = days[(int)DateTime.UtcNow.Date.AddDays(i - 6).DayOfWeek];
        }

        private void UpdateCollections()
        {
            for (var i = 0; authorCards != null && i < Math.Min(authorCards.Length, save.authorProgress.Length); i++) authorCards[i].SetProgress(save.authorProgress[i]);
            for (var i = 0; themeCards != null && i < Math.Min(themeCards.Length, save.themeProgress.Length); i++) themeCards[i].SetProgress(save.themeProgress[i]);
            for (var i = 0; bookCards != null && i < Math.Min(bookCards.Length, save.bookProgress.Length); i++) bookCards[i].SetProgress(save.bookProgress[i]);
        }

        private void ShowCollectionTab(int tab)
        {
            collectionTab = Mathf.Clamp(tab, 0, 2);
            for (var i = 0; collectionGroups != null && i < collectionGroups.Length; i++) collectionGroups[i].SetActive(i == collectionTab);
            for (var i = 0; collectionTabs != null && i < collectionTabs.Length; i++)
            {
                var image = collectionTabs[i].GetComponent<Image>();
                if (image != null) image.color = i == collectionTab ? new Color(0.16f, 0.49f, 0.95f) : new Color(0.95f, 0.96f, 0.99f);
                var text = collectionTabs[i].GetComponentInChildren<Text>();
                if (text != null) text.color = i == collectionTab ? Color.white : new Color(0.2f, 0.19f, 0.38f);
            }
        }

        private void UpdateAchievements()
        {
            if (achievementCards == null) return;
            for (var i = 0; i < achievementCards.Length; i++) achievementCards[i].SetProgress(AchievementProgress(i));
        }

        private int AchievementProgress(int index) => index < 2 ? save.solved : index == 2 ? save.erudition / 50
            : index == 3 ? save.classicSolved : index == 4 ? save.likedPuzzleIds.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Length
            : index == 5 ? save.perfectWins : save.bestEveningStreak;

        private void ShowAchievementTab(int tab)
        {
            achievementTab = Mathf.Clamp(tab, 0, 2);
            var visibleIndex = 0;
            for (var i = 0; achievementCards != null && i < achievementCards.Length; i++)
            {
                var count = AchievementProgress(i);
                var show = achievementTab == 0 || achievementTab == 1 && count < achievementCards[i].target || achievementTab == 2 && i >= 3;
                achievementCards[i].gameObject.SetActive(show);
                if (show && presentation != null)
                    achievementCards[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 413 - visibleIndex++ * 204);
            }
            for (var i = 0; achievementTabs != null && i < achievementTabs.Length; i++)
            {
                var image = achievementTabs[i].GetComponent<Image>();
                if (image != null) image.color = i == achievementTab ? new Color(0.16f, 0.49f, 0.95f) : new Color(0.95f, 0.96f, 0.99f);
                var text = achievementTabs[i].GetComponentInChildren<Text>();
                if (text != null) text.color = i == achievementTab ? Color.white : new Color(0.2f, 0.19f, 0.38f);
            }
        }

        private void ToggleSetting(int index)
        {
            switch (index)
            {
                case 0: save.music = !save.music; break;
                case 1: save.sound = !save.sound; break;
                case 2: save.vibration = !save.vibration; break;
                case 3: save.largeText = !save.largeText; ApplyTextScale(); break;
            }
            Persist();
        }

        private void UpdateSettings()
        {
            soundPlayer?.ApplySettings(save.music, save.sound);
            var values = new[] { save.music, save.sound, save.vibration, save.largeText };
            for (var i = 0; settingValues != null && i < Math.Min(settingValues.Length, values.Length); i++)
            {
                settingValues[i].text = values[i] ? "●  ВКЛ" : "ВЫКЛ  ●";
                settingValues[i].color = values[i] ? Color.white : new Color(.27f, .25f, .4f);
                var image = settingValues[i].transform.parent.GetComponent<Image>();
                if (image != null) image.color = values[i] ? new Color(0.08f, 0.72f, 0.48f) : new Color(0.78f, 0.79f, 0.85f);
            }
        }

        private void ApplyTextScale()
        {
            if (baseFontSizes == null) return;
            foreach (var pair in baseFontSizes)
                if (pair.Key != null) pair.Key.fontSize = save.largeText
                    ? Mathf.RoundToInt(pair.Value * 1.12f) : pair.Value;
        }

        private void DebugVictory()
        {
            var board = EnsureDebugPuzzle();
            if (board != null && board.Entry != null) { CompletePuzzle(board); return; }
            victoryQuote.text = "«Книга — лучший друг.»";
            victorySource.text = "— народная мудрость";
            victoryEruditionReward.text = "+20 к эрудиции";
            victoryCollectionReward.text = "+1 в коллекцию";
            ShowScreen(4);
        }

        private void DebugDefeat()
        {
            var board = EnsureDebugPuzzle();
            if (board != null && board.Entry != null) { FailPuzzle(board); return; }
            if (defeatQuote != null) defeatQuote.text = "«Попробуйте ещё раз»";
            if (defeatSource != null) defeatSource.text = "— Эрудиция";
            if (defeatMessage != null) defeatMessage.text = "Тестовый экран поражения";
            ShowScreen(12);
        }

        private PuzzleBoard EnsureDebugPuzzle()
        {
            var index = save.activePuzzle.puzzleIndex;
            var mode = index >= 0 && index < Entries.Length ? Entries[index].mode : PuzzleMode.Classic;
            if (save.feathers == 0) save.feathers = 1;
            StartMode(mode);
            return ActiveBoard();
        }

        private void ResetProgress()
        {
            SaveStore.Reset();
            save = new GameSave();
            save.Normalize();
            ApplyTextScale();
            Persist();
            ShowScreen(0);
        }

        private void Persist()
        {
            SaveStore.Save(save);
            UpdateAllUi();
        }

        private static bool DebugAllowed()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }
}
