using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    // Updates detail screens and statistics; collection cards come from the gallery.
    public sealed class ReferenceUiPresenter : MonoBehaviour
    {
        public Text statsLevel, statsNext, statsToday, statsBest;
        public RectTransform statsFill;
        public RectTransform statsTrack, detailTrack;
        public RectTransform[] bars;
        public Text[] days, amounts;
        public GameObject weeklyGraph, yearlyGraph;
        public RectTransform[] weekBars;
        public Text[] weekDays, weekAmounts;
        public Button[] periods;
        public Text coinsReward, likeLabel;
        public Button likeButton;
        public CollectionGallery gallery;
        public Text detailTitle, detailProgress, detailBody;
        public ScrollRect detailScroll;
        public Image detailPicture;
        public RectTransform detailFill;
        public GameObject popup;
        public Text popupTitle, popupBody;
        public Image popupIcon;
        private int period;
        public int SelectedAchievementIndex { get; private set; } = -1;

        public void SetPeriod(int value) { period = Mathf.Clamp(value, 0, 2); }
        public void SetCoinsReward(int count) { if (coinsReward != null) coinsReward.text = "<size=64>+" + count + "</size>\nмонет"; }
        public void ClosePopup()
        {
            SelectedAchievementIndex = -1;
            if (popup != null) popup.SetActive(false);
        }

        public void Refresh(GameSave save, int screen, string completed)
        {
            if (statsLevel != null) statsLevel.text = save.erudition.ToString();
            if (statsNext != null) statsNext.text = "До следующего уровня: " + (50 - save.erudition % 50);
            if (statsToday != null) statsToday.text = save.dailySolved[6].ToString();
            if (statsBest != null) statsBest.text = "Лучший результат: " + save.bestStreak;
            if (statsFill != null && statsTrack != null) statsFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, statsTrack.rect.width * ((save.erudition % 50) / 50f));
            if (likeButton != null)
            {
                var liked = !string.IsNullOrEmpty(completed) && save.likedPuzzleIds.Split('|').Contains(completed);
                likeButton.interactable = !liked;
                likeLabel.text = liked ? "В избранном" : "Мне нравится";
            }
            if (screen != 6 || bars == null) return;
            var today = DateTime.UtcNow.Date;
            int count = period == 2 ? 12 : 7;
            var values = new int[count];
            var labels = new string[count];
            var weekday = new[] { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };
            for (var i = 0; i < count; i++)
            {
                DateTime from, to;
                if (period == 2)
                {
                    from = new DateTime(today.Year, today.Month, 1).AddMonths(i - 11); to = from.AddMonths(1);
                    labels[i] = new[] { "Янв", "Фев", "Мар", "Апр", "Май", "Июн", "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек" }[from.Month - 1];
                }
                else
                {
                    var span = period == 1 ? 4 : 1;
                    from = today.AddDays(-(count - i) * span + 1); to = from.AddDays(span);
                    labels[i] = period == 0 ? weekday[(int)from.DayOfWeek] : from.ToString("dd.MM");
                }
                values[i] = save.activityHistory.Where(day => day.date >= from.Ticks && day.date < to.Ticks).Sum(day => day.solved);
            }
            var max = Mathf.Max(1, values.Max());
            if (weeklyGraph != null) weeklyGraph.SetActive(period != 2);
            if (yearlyGraph != null) yearlyGraph.SetActive(period == 2);
            var activeBars = period == 2 || weekBars == null || weekBars.Length == 0 ? bars : weekBars;
            var activeDays = period == 2 || weekDays == null || weekDays.Length == 0 ? days : weekDays;
            var activeAmounts = period == 2 || weekAmounts == null || weekAmounts.Length == 0 ? amounts : weekAmounts;
            for (var i = 0; i < activeBars.Length; i++)
            {
                var active = i < count;
                activeBars[i].gameObject.SetActive(active); activeDays[i].gameObject.SetActive(active); activeAmounts[i].gameObject.SetActive(active);
                if (!active) continue;
                // The two chart layouts (7 and 12 columns) are authored in the scene.
                // Values change the visible fill, never the position/size of a column.
                activeBars[i].GetComponent<Image>().fillAmount = values[i] == 0 ? .025f : (float)values[i] / max;
                activeDays[i].text = labels[i];
                activeAmounts[i].text = values[i].ToString();
            }
            for (var i = 0; i < periods.Length; i++)
            {
                periods[i].GetComponent<Image>().color = i == period ? new Color(.08f,.48f,1) : new Color(.91f,.92f,.98f);
                periods[i].GetComponentInChildren<Text>().color = i == period ? Color.white : new Color(.34f,.35f,.52f);
            }
        }

        public void ShowCollection(int index, GameSave save, PuzzleEntry[] entries)
        {
            if (gallery == null || index < 0 || index >= gallery.catalog.cards.Length) return;
            var definition = gallery.catalog.cards[index];
            var count = definition.Progress(save, entries);
            var target = definition.Target;
            detailTitle.text = definition.title;
            detailPicture.sprite = definition.picture;
            detailProgress.text = count + " / " + target + " пройдено";
            if (detailTrack != null) detailFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                target > 0 ? detailTrack.rect.width * Mathf.Clamp01((float)count / target) : 0);
            var solved = new System.Collections.Generic.HashSet<string>(save.solvedPuzzleIds.Split('|'));
            var unlocked = entries.Where(entry => solved.Contains(entry.id) && definition.Contains(entry)).GroupBy(entry => PuzzleGenerator.TextKey(entry.text)).Select(group => group.First()).ToArray();
            detailBody.text = unlocked.Length == 0 ? "Здесь появятся разгаданные тексты.\nПроходи уровни и открывай новые истории!" : string.Join("\n\n", unlocked.Select(entry => "«" + entry.text + "»\n<size=27>" + entry.source + (string.IsNullOrEmpty(entry.answer) ? "" : " · Ответ: " + entry.answer) + "</size>"));
            if (detailScroll != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailBody.rectTransform);
                detailScroll.StopMovement(); detailScroll.verticalNormalizedPosition = 1;
            }
        }

        public void ShowAchievement(int index, AchievementCardView[] cards, int progress, bool claimed, string reward)
        {
            if (cards == null || cards.Length == 0 || popup == null) return;
            index = Mathf.Clamp(index, 0, cards.Length - 1);
            SelectedAchievementIndex = index;

            var card = cards[index];
            var title = card.titleText != null ? card.titleText.text : "";
            var description = card.descriptionText != null ? card.descriptionText.text : "";
            var complete = progress >= card.target;

            popupTitle.text = title;

            // The original authored body is only 150 px high, which clipped the
            // reward and status lines. Keep the existing scene object but give
            // its text enough room to show the full achievement state.
            if (popupBody != null)
            {
                popupBody.rectTransform.sizeDelta = new Vector2(770f, 225f);
                popupBody.fontSize = 30;
                popupBody.resizeTextForBestFit = true;
                popupBody.resizeTextMinSize = 22;
                popupBody.resizeTextMaxSize = 30;
                popupBody.horizontalOverflow = HorizontalWrapMode.Wrap;
                popupBody.verticalOverflow = VerticalWrapMode.Truncate;

                var status = claimed
                    ? "<color=#3E8A63>✓ Награда получена</color>"
                    : complete
                        ? "<color=#257BEA>Награда готова — нажмите «Забрать»</color>"
                        : "Выполните условие, чтобы забрать награду.";

                popupBody.text = description
                    + "\nПрогресс: " + Mathf.Min(progress, card.target) + "/" + card.target
                    + "\n\nНаграда:\n" + reward
                    + "\n" + status;
            }

            if (popupIcon != null && card.icon != null)
                popupIcon.sprite = card.icon.sprite;

            var actionButton = popup.GetComponentInChildren<Button>(true);
            if (actionButton != null)
            {
                var actionLabel = actionButton.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(label => label.name == "Text_Label")
                    ?? actionButton.GetComponentInChildren<Text>(true);
                if (actionLabel != null) actionLabel.text = complete && !claimed ? "Забрать" : "Закрыть";
            }

            popup.SetActive(true);
        }
    }
}
