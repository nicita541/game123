using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Erudition
{
    // A serialized bridge between two authored scenes; it never creates UI objects.
    public sealed class GameplaySceneLink : MonoBehaviour
    {
        public GameObject classicScreen, turboScreen;
        public PuzzleBoard classicBoard, turboBoard;
        public Text classicErudition, turboErudition;
        public UiAction[] actions;
        private IEnumerator Start()
        {
            var openedDirectly = CryptogramGame.Current == null;
            if (openedDirectly && !SceneManager.GetSceneByName("MainScene").isLoaded)
                yield return SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Additive);
            if (CryptogramGame.Current != null)
            {
                CryptogramGame.Current.RegisterGameplay(this);
                if (openedDirectly) CryptogramGame.Current.Execute(UiActionKind.Classic, 0);
            }
        }
    }
}
