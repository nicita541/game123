using System;
using UnityEngine;

namespace Erudition
{
    // Value 1 and its catalogue entries are retained only to read existing saves.
    // New games use Classic; the Turbo screen and controls have been removed.
    public enum PuzzleMode { Classic, Turbo }

    [Serializable]
    public sealed class PuzzleEntry
    {
        public string id;
        public PuzzleMode mode;
        [TextArea(2, 5)] public string text;
        public string source;
        public int minimumErudition;
        [Range(-1, 5)] public int authorIndex = -1;
        [Range(0, 2)] public int themeIndex;
        [Range(0, 2)] public int bookIndex;
    }

    [CreateAssetMenu(fileName = "PuzzleLibrary", menuName = "Erudition/Puzzle Library")]
    public sealed class PuzzleLibrary : ScriptableObject
    {
        public PuzzleEntry[] entries = Array.Empty<PuzzleEntry>();
    }
}
