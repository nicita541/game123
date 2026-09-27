using System;
using UnityEngine;

namespace Erudition
{
    public sealed class AdsBridge : MonoBehaviour
    {
        public bool IsAvailable
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        public void ShowRewarded(Action<bool> complete)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            complete?.Invoke(true);
#else
            complete?.Invoke(false);
#endif
        }
    }
}
