using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace Erudition
{
    [Serializable]
    public sealed class ActivityDay
    {
        public long date;
        public int solved;
    }
    [Serializable]
    public sealed class PuzzleProgress
    {
        public int puzzleIndex = -1;
        public int remainingHearts;
        public string revealedCodes = "";
        public int selectedCode;
        public int selectedSlot = -1;
        public int helpRevision;
        public int mistakesInLevel;
        public string attemptedPairs = "";
    }

    [Serializable]
    public sealed class GameSave
    {
        public int version = 2;
        public int erudition;
        public int feathers = 5;
        public int reserveFeathers;
        public int hints = 2;
        public int coins = 1200;
        public long nextFeatherUtcTicks;
        public int solved;
        public int guesses;
        public int mistakes;
        public int streak;
        public int bestStreak;
        public int classicSolved;
        public int turboSolved;
        public int perfectWins;
        public int eveningStreak;
        public int bestEveningStreak;
        public string likedPuzzleIds = "";
        public string solvedPuzzleIds = "";
        public List<ActivityDay> activityHistory = new List<ActivityDay>();
        public int lastClassicIndex = -1;
        public int lastTurboIndex = -1;
        public int lastFailedPuzzleIndex = -1;
        public int[] authorProgress = new int[6];
        public int[] themeProgress = new int[3];
        public int[] bookProgress = new int[3];
        public int[] dailySolved = new int[7];
        public long lastDailyUpdateUtcTicks;
        public bool music = true;
        public bool sound = true;
        public bool vibration = true;
        public bool largeText;
        public PuzzleProgress activePuzzle = new PuzzleProgress();

        public void Normalize()
        {
            // Version 1 started with a decorative 101. Keep earned points, remove that bonus.
            if (version < 2) { erudition = Mathf.Max(0, erudition - 101); version = 2; }
            erudition = Mathf.Max(0, erudition);
            feathers = Mathf.Clamp(feathers, 0, 5);
            reserveFeathers = Mathf.Max(0, reserveFeathers);
            hints = Mathf.Max(0, hints);
            coins = Mathf.Max(0, coins);
            if (activityHistory == null) activityHistory = new List<ActivityDay>();
            if (likedPuzzleIds == null) likedPuzzleIds = "";
            if (solvedPuzzleIds == null) solvedPuzzleIds = "";
            if (authorProgress == null || authorProgress.Length != 6) authorProgress = new int[6];
            if (themeProgress == null || themeProgress.Length != 3) themeProgress = new int[3];
            if (bookProgress == null || bookProgress.Length != 3) bookProgress = new int[3];
            if (dailySolved == null || dailySolved.Length != 7) dailySolved = new int[7];
            if (activePuzzle == null) activePuzzle = new PuzzleProgress();
            if (activePuzzle.revealedCodes == null) activePuzzle.revealedCodes = "";
            if (activePuzzle.attemptedPairs == null) activePuzzle.attemptedPairs = "";
        }
    }

    public static class SaveStore
    {
        private static string Key
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(EditorTestSlot)) return EditorTestSlot;
#endif
                return "erudition.save.v1";
            }
        }
#if UNITY_EDITOR
        public static string EditorTestSlot;
#endif
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(GameSave));

        public static GameSave Load()
        {
            var xml = PlayerPrefs.GetString(Key, "");
            GameSave save;
            try
            {
                using (var reader = new StringReader(xml))
                    save = string.IsNullOrEmpty(xml) ? new GameSave() : (GameSave)Serializer.Deserialize(reader);
            }
            catch { save = new GameSave(); }
            if (save == null) save = new GameSave();
            save.Normalize();
            return save;
        }

        public static void Save(GameSave save)
        {
            using (var writer = new StringWriter())
            {
                Serializer.Serialize(writer, save);
                PlayerPrefs.SetString(Key, writer.ToString());
            }
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
