using UnityEngine;

namespace Erudition
{
    public enum UiActionKind
    {
        Home, CollectionDetails, Classic, Turbo, Statistics, Collections, Achievements, Shop,
        Settings, Back, Continue, Hint, Check, RewardVictory, RewardFeather,
        BuyFiveFeathers, BuyFifteenFeathers, BuyFiveHints, PremiumUnavailable,
        CollectionTab, AchievementTab, ToggleSetting, Debug, DebugAddErudition,
        DebugRemoveFeather, DebugZeroFeathers, DebugRefillFeathers, DebugAddHint,
        DebugVictory, DebugDefeat, DebugReset, Retry, DebugOneHeart, DebugUnlockCollections, DebugUnlockAchievements,
        FeatherInfo, CoinInfo, StatisticsPeriod, ClearSelection, LikeQuote, AchievementDetails, ClosePopup,
        BuyHintOffer, RewardHint, CloseHintOffer
    }

    public sealed class UiAction : MonoBehaviour
    {
        [SerializeField] private CryptogramGame game;
        [SerializeField] private UiActionKind action;
        [SerializeField] private int parameter;
        public void SetGame(CryptogramGame owner) { game = owner; }

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
