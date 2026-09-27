using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using YandexMobileAds;
using YandexMobileAds.Base;
#endif

namespace Erudition
{
    public sealed class AdsBridge : MonoBehaviour
    {
        [SerializeField] private string rewardedAdUnitId = "demo-rewarded-yandex";
        public bool IsLoading { get; private set; }
        public bool IsShowing { get; private set; }
        public string Status { get; private set; } = "";
        private Action<bool> completion;
#if UNITY_EDITOR
        // Automated tests opt in explicitly. Ordinary editor clicks never fake an ad.
        public static bool EditorTestRewards;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        private RewardedAdLoader loader;
        private RewardedAd ad;
        private bool rewarded, disposed;
#endif
        public bool IsAvailable => !IsLoading && !IsShowing;

        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            YandexAds.SetUserConsent(false);
            YandexAds.SetLocationTracking(false);
            loader = new RewardedAdLoader();
            Load();
#endif
        }

        public void ShowRewarded(Action<bool> complete)
        {
#if UNITY_EDITOR
            Status = EditorTestRewards ? "" : "Видео доступно в Android-версии";
            complete?.Invoke(EditorTestRewards);
#elif UNITY_ANDROID
            if (!IsAvailable) { complete?.Invoke(false); return; }
            completion = complete;
            if (ad != null) Show();
            else Load();
#else
            Status = "Видео доступно в Android-версии";
            complete?.Invoke(false);
#endif
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        private void Load()
        {
            if (disposed || IsLoading) return;
            if (loader == null) loader = new RewardedAdLoader();
            IsLoading = true;
            Status = "Загружаем видео…";
            loader.LoadAd(new AdRequest(rewardedAdUnitId), loaded =>
            {
                if (disposed) { loaded.Destroy(); return; }
                IsLoading = false;
                ad = loaded;
                Status = "";
                if (completion != null) Show();
            }, failure =>
            {
                if (disposed) return;
                IsLoading = false;
                Status = "Видео недоступно. Попробовать ещё раз";
                Finish(false);
            });
        }
        private void Show()
        {
            IsShowing = true;
            rewarded = false;
            ad.OnRewarded += (sender, reward) => { rewarded = true; };
            ad.OnAdDismissed += (sender, args) =>
            {
                IsShowing = false;
                ad.Destroy(); ad = null;
                Status = rewarded ? "" : "Просмотр не завершён";
                Finish(rewarded);
                // Prepare one next ad; failures wait for an explicit new request.
                Load();
            };
            ad.OnAdFailedToShow += (sender, args) =>
            {
                IsShowing = false;
                ad.Destroy(); ad = null;
                Status = "Видео недоступно. Попробовать ещё раз";
                Finish(false);
            };
            ad.Show();
        }
        private void Finish(bool success)
        {
            var callback = completion;
            completion = null;
            callback?.Invoke(success);
        }
        private void OnDestroy()
        {
            disposed = true;
            loader?.CancelLoading();
            ad?.Destroy();
            completion = null;
        }
#endif
    }
}
