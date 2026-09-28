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
        public CollectionGallery gallery;
        public GameObject hintOffer;
        public Text hintOfferMessage;
        public Button hintOfferBuy, hintOfferAd, hintOfferClose;
        public AdsBridge ads;
        public GameAudio soundPlayer;
        public ReferenceUiPresenter presentation;
        public GameObject[] screens;
        public PuzzleBoard classicBoard;

        public Text mainErudition;
        public Text mainFeathers;
        public Text classicErudition;
        public Text shopFeathers;
        public Text shopCoins;
        public Text noFeathersCounter;
        public Text noFeathersCounterRestored;
        public Text noFeathersTimer;
        public Text noFeathersNote;
        public Text classicStartLabel;

        public Text victoryQuote;
        public Text victorySource;
        public Text victoryEruditionReward;
        public Text victoryCollectionReward;
        public Text victoryRewardDetails;
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
        public CollectionCardView[] kindCards;
        public Button[] achievementTabs;
        public AchievementCardView[] achievementCards;
        public Text[] settingValues;
        public Image[] settingTracks;
        public RectTransform[] settingThumbs;
        public RectTransform[] settingOnStops;
        public RectTransform[] settingOffStops;
        public Text shopMessage;
        public Text debugValues;
        public GameObject debugOpenButton;

        private GameSave save;
        private int currentScreen;
        private int collectionTab;
        private int achievementTab;
        private int victorySequence;
        private float nextTimerUpdate;
        private Dictionary<Text, int> baseFontSizes;
        private string lastCompletedPuzzleId;
        private PuzzleEntry[] puzzles;
        private bool pendingStart;
        private bool applicationPaused;
        private float nextProgressSave;
        private int pendingIndex = -1;
        private Vector2[] achievementPositions;
        public static CryptogramGame Current { get; private set; }
        public PuzzleEntry[] Entries => puzzles ?? (puzzles = PuzzleGenerator.Build(library, gallery == null ? null : gallery.catalog));
        private bool hintRewardPending;

        public int Hints => save == null ? 0 : save.hints;

        private void Awake()
        {
            Current = this;
            if (gallery != null) gallery.Build(this);
            achievementPositions = achievementCards.Select(card => ((RectTransform)card.transform).anchoredPosition).ToArray();
            baseFontSizes = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .ToDictionary(label => label, label => label.fontSize);
            save = SaveStore.Load();
            ResolveSavedPuzzles();
            if (!save.kindProgressInitialized)
            {
                var solvedIds = new HashSet<string>(save.solvedPuzzleIds.Split('|'));
                foreach (var entry in Entries.Where(entry => solvedIds.Contains(entry.id))) save.kindProgress[(int)entry.kind]++;
                save.kindProgressInitialized = true;
                SaveStore.Save(save);
            }
            if (save.winsUntilInterstitial == 0) save.winsUntilInterstitial = UnityEngine.Random.Range(2, 5);
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
            classicBoard = link.classicBoard;
            screens[2] = link.classicScreen;
            classicErudition = link.classicErudition;
            classicBoard.SetGame(this);
            foreach (var action in link.actions)
            {
                if (action != null)
                    action.SetGame(this);
            }
            if (baseFontSizes != null)
                foreach (var label in link.GetComponentsInChildren<Text>(true))
                    if (!baseFontSizes.ContainsKey(label)) baseFontSizes[label] = label.fontSize;
            screens[2].SetActive(currentScreen == 2);
            ApplyTextScale(); UpdateAllUi();
            if (pendingStart)
            { var index = pendingIndex; pendingStart = false; StartClassicPuzzle(index); }
        }

        private void Update()
        {
            if (!applicationPaused && Application.isFocused && currentScreen == 2 && HasActivePuzzle()
                && (hintOffer == null || !hintOffer.activeSelf) && (ads == null || !ads.IsShowing))
            {
                classicBoard?.TickPlayTime(Time.unscaledDeltaTime);
                if (Time.unscaledTime >= nextProgressSave)
                { nextProgressSave = Time.unscaledTime + 10; StorePuzzleProgress(classicBoard); }
            }
            if (Time.unscaledTime < nextTimerUpdate) return;
            nextTimerUpdate = Time.unscaledTime + 1f;
            if (RestoreEnergy()) UpdateAllUi();
            UpdateTimer();
            UpdateEnergyUi();
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (paused) StoreActiveProgress();
            if (paused && save != null) SaveStore.Save(save);
            else if (!paused && save != null) { if (RestoreEnergy()) UpdateAllUi(); }
        }

        private void OnApplicationQuit()
        {
            StoreActiveProgress();
            if (save != null) SaveStore.Save(save);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) StoreActiveProgress();
        }

        public void Execute(UiActionKind action, int parameter)
        {
            soundPlayer?.Click();
            switch (action)
            {
                case UiActionKind.Home: pendingStart = false; ShowScreen(0); break;
                case UiActionKind.CollectionDetails: presentation?.ShowCollection(parameter, save, Entries); ShowScreen(1); break;
                case UiActionKind.StatisticsPeriod: presentation?.SetPeriod(parameter); UpdateAllUi(); break;
                case UiActionKind.ClearSelection: ActiveBoard()?.ClearSelection(); break;
                case UiActionKind.AchievementDetails: presentation?.ShowAchievement(parameter, achievementCards); break;
                case UiActionKind.ClosePopup: presentation?.ClosePopup(); break;
                case UiActionKind.LikeQuote:
                    if (!string.IsNullOrEmpty(lastCompletedPuzzleId) && !save.likedPuzzleIds.Split('|').Contains(lastCompletedPuzzleId))
                    { save.likedPuzzleIds += lastCompletedPuzzleId + "|"; Persist(); }
                    break;
                case UiActionKind.Classic:
                case UiActionKind.Turbo: // Reserved serialized value: old controls now open the only game mode.
                    StartClassicPuzzle(); break;
                case UiActionKind.Statistics: UpdateStats(); ShowScreen(6); break;
                case UiActionKind.Collections: ShowCollectionTab(0); ShowScreen(7); break;
                case UiActionKind.Achievements: ShowAchievementTab(0); ShowScreen(8); break;
                case UiActionKind.Shop: UpdateAllUi(); ShowScreen(9); break;
                case UiActionKind.Settings: ShowScreen(10); break;
                case UiActionKind.Back: GoBack(); break;
                case UiActionKind.Continue: ShowScreen(0); break;
                case UiActionKind.Hint: ActiveBoard()?.UseHint(); break;
                case UiActionKind.BuyHintOffer: BuyHintOffer(); break;
                case UiActionKind.RewardHint: RewardHint(); break;
                case UiActionKind.CloseHintOffer: if (!hintRewardPending) hintOffer?.SetActive(false); break;
                case UiActionKind.Check: ActiveBoard()?.Check(); break;
                case UiActionKind.RewardVictory: break; // Retired action; keep serialized enum indices stable.
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
                        save.solvedPuzzleIds = string.Join("|", Entries.Select(e => e.id)) + "|";
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

        private void StoreActiveProgress()
        {
            if (save != null && classicBoard != null && save.activePuzzle.puzzleIndex >= 0
                && save.activePuzzle.puzzleIndex == classicBoard.PuzzleIndex)
                StorePuzzleProgress(classicBoard);
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

        private void ResolveSavedPuzzles()
        {
            var progress = save.activePuzzle;
            if (!string.IsNullOrEmpty(progress.puzzleId))
                progress.puzzleIndex = Array.FindIndex(Entries, e => e.id == progress.puzzleId);
            else if (progress.puzzleIndex >= 0 && progress.puzzleIndex < Entries.Length)
                progress.puzzleId = Entries[progress.puzzleIndex].id;
            if (progress.puzzleIndex < 0 || progress.puzzleIndex >= Entries.Length)
                save.activePuzzle = new PuzzleProgress();
            else progress.remainingHearts = Mathf.Min(progress.remainingHearts,
                Mathf.Max(1, PuzzleBoard.MaxHearts - progress.mistakesInLevel));
            if (!string.IsNullOrEmpty(save.lastFailedPuzzleId))
                save.lastFailedPuzzleIndex = Array.FindIndex(Entries, e => e.id == save.lastFailedPuzzleId);
            else if (save.lastFailedPuzzleIndex >= 0 && save.lastFailedPuzzleIndex < Entries.Length)
                save.lastFailedPuzzleId = Entries[save.lastFailedPuzzleIndex].id;
            SaveStore.Save(save);
        }

        public void OfferHints()
        {
            if (currentScreen != 2 || !HasActivePuzzle() || hintOffer == null) return;
            StoreActiveProgress();
            hintOfferMessage.text = "Купить 5 подсказок за 150 монет\nили посмотреть видео за 1 подсказку.\n\nВаши монеты: " + save.coins;
            hintOffer.SetActive(true);
        }

        private void BuyHintOffer()
        {
            if (hintRewardPending || hintOffer == null || !hintOffer.activeSelf) return;
            if (save.coins < 150)
            { hintOfferMessage.text = "Недостаточно монет: " + save.coins + " из 150.\nМожно получить подсказку за видео."; return; }
            save.coins -= 150;
            save.hints += 5;
            Persist();
            hintOffer.SetActive(false);
            ActiveBoard()?.UseHint();
        }

        private void RewardHint()
        {
            if (hintRewardPending || hintOffer == null || !hintOffer.activeSelf) return;
            if (ads == null || !ads.IsAvailable)
            { hintOfferMessage.text = "Видео пока недоступно. Попробуйте ещё раз через несколько секунд."; return; }
            hintRewardPending = true;
            hintOfferBuy.interactable = hintOfferAd.interactable = hintOfferClose.interactable = false;
            hintOfferMessage.text = "Загружаем видео…";
            var recipient = save;
            var puzzleId = classicBoard.Entry.id;
            ads.ShowRewarded(success =>
            {
                if (this == null) return;
                hintRewardPending = false;
                hintOfferBuy.interactable = hintOfferAd.interactable = hintOfferClose.interactable = true;
                if (recipient != save) return;
                if (!success)
                { hintOfferMessage.text = ads.Status + "\nПодсказка выдаётся после полного просмотра."; return; }
                save.hints++;
                Persist();
                var apply = hintOffer.activeSelf && currentScreen == 2 && HasActivePuzzle() && classicBoard.Entry.id == puzzleId;
                hintOffer.SetActive(false);
                if (apply) classicBoard.UseHint();
            });
        }

        public void CompletePuzzle(PuzzleBoard board)
        {
            if (board == null || board.PuzzleIndex < 0 || save.activePuzzle.puzzleIndex != board.PuzzleIndex) return;
            var entry = board.Entry;
            var reward = PuzzleReward.Calculate(entry.minimumErudition, board.ExportProgress());
            var eruditionReward = reward.points;
            const int coinReward = 30;
            save.erudition += eruditionReward;
            save.coins += coinReward;
            save.solved++;
            save.streak++;
            save.bestStreak = Mathf.Max(save.bestStreak, save.streak);
            save.classicSolved++;
            save.kindProgress[(int)entry.kind]++;
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
            victorySource.text = "— " + entry.source + (string.IsNullOrEmpty(entry.answer) ? "" : "\nОтвет: " + entry.answer);
            if (victoryRewardDetails != null) victoryRewardDetails.text = PuzzleGenerator.DifficultyName(entry.minimumErudition) + " · " + reward.Explanation;
            victoryEruditionReward.text = "<size=64>+" + eruditionReward + "</size>\nк эрудиции";
            victoryCollectionReward.text = "<size=64>+1</size>\nв коллекцию";
            presentation?.SetCoinsReward(coinReward);
            victorySequence++;
            save.winsUntilInterstitial = Mathf.Max(0, save.winsUntilInterstitial - 1);
            Persist();
            ShowScreen(4);
            if (Application.isPlaying && save.winsUntilInterstitial == 0)
                StartCoroutine(ShowLevelEndAd(victorySequence));
        }

        public void FailPuzzle(PuzzleBoard board)
        {
            if (board == null || board.Entry == null || save.activePuzzle.puzzleIndex != board.PuzzleIndex) return;
            save.lastFailedPuzzleIndex = board.PuzzleIndex;
            save.lastFailedPuzzleId = board.Entry.id;
            save.streak = 0;
            save.eveningStreak = 0;
            save.activePuzzle = new PuzzleProgress();
            UpdateDefeatMessage();
            Persist();
            ShowScreen(12);
            // Every defeat requests an ad, independently of the victory cooldown.
            if (ads != null && ads.TryShowInterstitial())
            {
                save.lastInterstitialUtcTicks = DateTime.UtcNow.Ticks;
                SaveStore.Save(save);
            }
        }

        private void UpdateDefeatMessage()
        {
            // A failed attempt must not disclose the answer or its attribution.
            // The same puzzle can still be retried for the usual one-feather cost.
            if (defeatQuote != null) { defeatQuote.text = ""; defeatQuote.gameObject.SetActive(false); }
            if (defeatSource != null) { defeatSource.text = ""; defeatSource.gameObject.SetActive(false); }
            if (defeatMessage != null) defeatMessage.text = "Сердечки закончились.\nПопробуй ещё раз — у тебя получится!";
        }

        private void StartClassicPuzzle(int requestedIndex = -1)
        {
            RestoreEnergy();
            var board = classicBoard;
            if (board == null) { pendingStart = true; pendingIndex = requestedIndex; return; }
            var progress = save.activePuzzle;
            // Keep a previously started Turbo puzzle playable in the single
            // remaining board, without charging again or losing filled cells.
            if (requestedIndex < 0 && progress.puzzleIndex >= 0 && progress.puzzleIndex < Entries.Length
                && progress.remainingHearts > 0)
            {
                board.StartPuzzle(Entries[progress.puzzleIndex], progress.puzzleIndex, progress, save.erudition, save.hints);
                ShowScreen(2);
                return;
            }
            if (save.feathers <= 0)
            {
                UpdateEnergyUi();
                ShowScreen(5);
                return;
            }
            var index = requestedIndex >= 0 ? requestedIndex : ChoosePuzzle(PuzzleMode.Classic, board);
            if (index < 0 || index >= Entries.Length) return;
            SpendFeather();
            save.recentTexts.Add(PuzzleGenerator.TextKey(Entries[index].text));
            while (save.recentTexts.Count > 10) save.recentTexts.RemoveAt(0);
            save.activePuzzle = new PuzzleProgress
            {
                puzzleIndex = index,
                puzzleId = Entries[index].id,
                scoringRevision = 1,
                playerTierAtStart = PuzzleGenerator.Tier(save.erudition),
                remainingHearts = PuzzleBoard.MaxHearts
            };
            save.lastClassicIndex = index;
            board.StartPuzzle(Entries[index], index, save.activePuzzle, save.erudition, save.hints);
            Persist();
            ShowScreen(2);
        }

        private int ChoosePuzzle(PuzzleMode mode, PuzzleBoard board)
        {
            return PuzzleSelection.Choose(Entries, mode, save.erudition, save.recentTexts,
                board.CanFit, count => UnityEngine.Random.Range(0, count));
        }

        private void RetryFailedPuzzle()
        {
            var index = save.lastFailedPuzzleIndex;
            if (index < 0 || index >= Entries.Length) { ShowScreen(0); return; }
            StartClassicPuzzle(index);
        }

        private IEnumerator ShowLevelEndAd(int sequence)
        {
            // Give the result screen a moment to appear. Never show a late ad
            // after navigation or wait for a network load while the player moves on.
            yield return new WaitForSecondsRealtime(.8f);
            if (sequence != victorySequence || currentScreen != 4 || save.activePuzzle.puzzleIndex >= 0) yield break;
            if (save.lastInterstitialUtcTicks > 0
                && DateTime.UtcNow.Ticks - save.lastInterstitialUtcTicks < TimeSpan.FromSeconds(90).Ticks) yield break;
            if (ads == null || !ads.TryShowInterstitial()) yield break;
            save.lastInterstitialUtcTicks = DateTime.UtcNow.Ticks;
            save.winsUntilInterstitial = UnityEngine.Random.Range(2, 5);
            SaveStore.Save(save);
        }

        private void RewardFeather()
        {
            if (ads == null || !ads.IsAvailable || save.feathers >= MaxFeathers) return;
            ads.ShowRewarded(success =>
            {
                if (!success)
                {
                    if (noFeathersNote != null) { noFeathersNote.text = ads.Status; noFeathersNote.gameObject.SetActive(true); }
                    return;
                }
                save.feathers = Mathf.Min(MaxFeathers, save.feathers + 1);
                if (save.feathers >= MaxFeathers) save.nextFeatherUtcTicks = 0;
                else StartRecoveryClock();
                if (noFeathersNote != null)
                {
                    noFeathersNote.text = "Перо восстановлено! Можно начать уровень.";
                    noFeathersNote.gameObject.SetActive(true);
                }
                Persist();

                if (save.activePuzzle.puzzleIndex >= 0 && classicBoard != null)
                {
                    classicBoard.StartPuzzle(
                        Entries[save.activePuzzle.puzzleIndex],
                        save.activePuzzle.puzzleIndex,
                        save.activePuzzle,
                        save.erudition,
                        save.hints
                    );

                    ShowScreen(2);
                }
                else
                {
                    ShowScreen(0);
                }
            });
        }

        private void BuyFeathers(int amount, int cost)
        {
            if (save.coins < cost) { SayShop("Недостаточно монет"); return; }
            save.coins -= cost;
            save.feathers += amount;
            StartRecoveryClock();
            SayShop("+" + amount + " перьев · Всего: " + save.feathers);
            Persist();
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
            StartRecoveryClock();
            return true;
        }

        private void StartRecoveryClock()
        {
            if (save.feathers >= MaxFeathers) { save.nextFeatherUtcTicks = 0; return; }
            if (save.nextFeatherUtcTicks <= 0) save.nextFeatherUtcTicks = DateTime.UtcNow.AddMinutes(FeatherRestoreMinutes).Ticks;
        }

        private bool RestoreEnergy()
        {
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
            return currentScreen == 2 ? classicBoard : null;
        }

        private void ShowScreen(int index)
        {
            if (screens == null || index < 0 || index >= screens.Length) return;
            hintOffer?.SetActive(false);
            if (currentScreen == 2 && index != 2) StoreActiveProgress();
            currentScreen = index;
            presentation?.ClosePopup();
            for (var i = 0; i < screens.Length; i++) if (screens[i] != null) screens[i].SetActive(i == index);
            if (index == 7) ShowCollectionTab(collectionTab);
            if (index == 8) ShowAchievementTab(achievementTab);
            if (index == 5 && noFeathersNote != null) noFeathersNote.gameObject.SetActive(false);
            if (index == 12) UpdateDefeatMessage();
            UpdateAllUi();
        }

        private void UpdateAllUi()
        {
            if (save == null) return;
            if (mainErudition != null) mainErudition.text = save.erudition.ToString();
            if (classicErudition != null) classicErudition.text = save.erudition.ToString();
            if (classicStartLabel != null) classicStartLabel.text = HasActivePuzzle()
                ? "Продолжить" : "Классика";
            classicBoard?.SetHintCount(save.hints);
            UpdateEnergyUi();
            UpdateStats();
            UpdateCollections();
            UpdateAchievements();
            UpdateSettings();
            presentation?.Refresh(save, currentScreen, lastCompletedPuzzleId);
            if (debugValues != null) debugValues.text = "Эрудиция: " + save.erudition + "    Перья: " + save.feathers + "/5    Подсказки: " + save.hints + "    Ошибки: " + save.mistakes;
        }

        private bool HasActivePuzzle()
        {
            var index = save.activePuzzle.puzzleIndex;
            return index >= 0 && index < Entries.Length && save.activePuzzle.remainingHearts > 0;
        }

        private void UpdateEnergyUi()
        {
            var featherValue = save.feathers.ToString();
            if (mainFeathers != null) mainFeathers.text = featherValue;
            if (shopFeathers != null) shopFeathers.text = featherValue;
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
            var days = new[] { "ВС", "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ" };
            for (var i = 0; activityDayLabels != null && i < activityDayLabels.Length; i++)
                activityDayLabels[i].text = days[(int)DateTime.UtcNow.Date.AddDays(i - 6).DayOfWeek];
        }

        private void UpdateCollections()
        {
            if (gallery != null) { gallery.Refresh(save, Entries); return; }
            for (var i = 0; kindCards != null && i < Math.Min(kindCards.Length, save.kindProgress.Length); i++) kindCards[i].SetProgress(save.kindProgress[i]);
            for (var i = 0; authorCards != null && i < Math.Min(authorCards.Length, save.authorProgress.Length); i++) authorCards[i].SetProgress(save.authorProgress[i]);
            for (var i = 0; themeCards != null && i < Math.Min(themeCards.Length, save.themeProgress.Length); i++) themeCards[i].SetProgress(save.themeProgress[i]);
            for (var i = 0; bookCards != null && i < Math.Min(bookCards.Length, save.bookProgress.Length); i++) bookCards[i].SetProgress(save.bookProgress[i]);
        }

        private void ShowCollectionTab(int tab)
        {
            collectionTab = Mathf.Clamp(tab, 0, collectionGroups.Length - 1);
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
            : index == 3 ? save.classicSolved : index == 4 ? save.solvedPuzzleIds.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Distinct().Count()
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
                if (show && achievementPositions != null)
                {
                    // Filtering compacts into scene-authored slots, never hard-coded coordinates.
                    var slot = achievementTab == 0 ? i : visibleIndex++;
                    achievementCards[i].GetComponent<RectTransform>().anchoredPosition = achievementPositions[slot];
                }
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
                var modern = settingTracks != null && i < settingTracks.Length && settingTracks[i] != null;
                settingValues[i].text = modern ? (values[i] ? "Включено" : "Выключено") : (values[i] ? "●  ВКЛ" : "ВЫКЛ  ●");
                settingValues[i].color = modern ? (values[i] ? new Color(.2f,.46f,.34f) : new Color(.47f,.43f,.5f)) : values[i] ? Color.white : new Color(.27f, .25f, .4f);
                var image = modern ? settingTracks[i] : settingValues[i].transform.parent.GetComponent<Image>();
                if (image != null) image.color = values[i] ? new Color(.30f,.64f,.47f) : new Color(.74f,.71f,.77f);
                var stops = values[i] ? settingOnStops : settingOffStops;
                if (settingThumbs != null && i < settingThumbs.Length && settingThumbs[i] != null
                    && stops != null && i < stops.Length && stops[i] != null)
                    settingThumbs[i].anchoredPosition = stops[i].anchoredPosition;
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
            victoryEruditionReward.text = "+10 к эрудиции";
            if (victoryRewardDetails != null) victoryRewardDetails.text = "Пример награды за лёгкое задание";
            victoryCollectionReward.text = "+1 в коллекцию";
            ShowScreen(4);
        }

        private void DebugDefeat()
        {
            var board = EnsureDebugPuzzle();
            if (board != null && board.Entry != null) { FailPuzzle(board); return; }
            UpdateDefeatMessage();
            ShowScreen(12);
        }

        private PuzzleBoard EnsureDebugPuzzle()
        {
            if (save.feathers == 0) save.feathers = 1;
            StartClassicPuzzle();
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
