using UnityEngine;

namespace Erudition
{
    public enum UiActionKind
    {
        Home, Modes, Classic, Turbo, Statistics, Collections, Achievements, Shop,
        Settings, Back, Continue, Hint, Check, RewardVictory, RewardFeather,
        BuyFiveFeathers, BuyFifteenFeathers, BuyFiveHints, PremiumUnavailable,
        CollectionTab, AchievementTab, ToggleSetting, Debug, DebugAddErudition,
        DebugRemoveFeather, DebugZeroFeathers, DebugRefillFeathers, DebugAddHint,
        DebugVictory, DebugDefeat, DebugReset, Retry, DebugOneHeart, DebugUnlockCollections, DebugUnlockAchievements,
        FeatherInfo, CoinInfo
    }

    public sealed class UiAction : MonoBehaviour
    {
        [SerializeField] private CryptogramGame game;
        [SerializeField] private UiActionKind action;
        [SerializeField] private int parameter;

        public void Configure(CryptogramGame owner, UiActionKind kind, int value = 0)
        {
            game = owner;
            action = kind;
            parameter = value;
        }

        public void Press()
        {
            if (game != null) game.Execute(action, parameter);
        }
    }
}
