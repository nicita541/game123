using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    // Only updates serialized scene objects. No runtime UI construction.
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
        public CollectionCardView[] collections;
        public Sprite[] collectionPictures;
        public Text detailTitle, detailProgress, detailBody;
        public ScrollRect detailScroll;
        public Image detailPicture;
        public RectTransform detailFill;
        public GameObject popup;
        public Text popupTitle, popupBody;
        public Image popupIcon;
        public Sprite[] achievementIcons;
        private int period;

        public void SetPeriod(int value) { period = Mathf.Clamp(value, 0, 2); }
        public void SetCoinsReward(int count) { if (coinsReward != null) coinsReward.text = "<size=64>+" + count + "</size>\nмонет"; }
        public void ClosePopup() { if (popup != null) popup.SetActive(false); }

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
            index = Mathf.Clamp(index, 0, collections.Length - 1);
            var card = collections[index];
            var count = index < 6 ? save.authorProgress[index] : index < 9 ? save.themeProgress[index - 6] : index < 12 ? save.bookProgress[index - 9] : save.kindProgress[index - 12];
            detailTitle.text = card.title.text;
            detailPicture.sprite = collectionPictures[index];
            detailProgress.text = Mathf.Min(count, card.target) + " / " + card.target + " в коллекции";
            if (detailTrack != null) detailFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, detailTrack.rect.width * Mathf.Clamp01((float)count / card.target));
            var related = entries.Where(entry => index < 6 ? entry.authorIndex == index : index < 9 ? entry.themeIndex == index - 6 : index < 12 ? entry.bookIndex == index - 9 : (int)entry.kind == index - 12);
            var solved = new System.Collections.Generic.HashSet<string>(save.solvedPuzzleIds.Split('|'));
            var unlocked = related.Where(entry => solved.Contains(entry.id)).GroupBy(entry => PuzzleGenerator.TextKey(entry.text)).Select(group => group.First()).ToArray();
            detailBody.text = unlocked.Length == 0 ? "Здесь появятся разгаданные тексты.\nИграй в Классику и пополняй коллекцию!" : string.Join("\n\n", unlocked.Select(entry => "«" + entry.text + "»\n<size=27>" + entry.source + (string.IsNullOrEmpty(entry.answer) ? "" : " · Ответ: " + entry.answer) + "</size>"));
            if (detailScroll != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailBody.rectTransform);
                detailScroll.StopMovement(); detailScroll.verticalNormalizedPosition = 1;
            }
        }

        public void ShowAchievement(int index, AchievementCardView[] cards)
        {
            index = Mathf.Clamp(index, 0, cards.Length - 1);
            var labels = cards[index].GetComponentsInChildren<Text>(true);
            popupTitle.text = labels.First(label => label.name == "Text_Title").text;
            popupBody.text = labels.First(label => label.name == "Text_Description").text + "\n\nПрогресс: " + cards[index].progressText.text;
            popupIcon.sprite = achievementIcons[index];
            popup.SetActive(true);
        }
    }
}
