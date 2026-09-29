using System;
using UnityEngine;

namespace Erudition
{
    // Value 1 and its catalogue entries are retained only to read existing saves.
    // New games use Classic; the Turbo screen and controls have been removed.
    public enum PuzzleMode { Classic, Turbo }
    public enum PuzzleKind { Quotes, Stories, Proverbs, Riddles }

    [Serializable]
    public sealed class PuzzleEntry
    {
        public string id;
        public string collectionId;
        public PuzzleMode mode;
        [TextArea(2, 5)] public string text;
        public string source;
        public int minimumErudition;
        public PuzzleKind kind;
        public string answer;
    }
}
