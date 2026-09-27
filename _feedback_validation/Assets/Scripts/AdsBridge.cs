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
        [SerializeField] private string interstitialAdUnitId = "demo-interstitial-yandex";
        public bool IsLoading { get; private set; }
        public bool IsShowing { get; private set; }
        public string Status { get; private set; } = "";
        private Action<bool> completion;
#if UNITY_EDITOR
        // Automated tests opt in explicitly. Ordinary editor clicks never fake an ad.
        public static bool EditorTestRewards;
        public static Func<bool> EditorTestInterstitial;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        private RewardedAdLoader loader;
        private RewardedAd ad;
        private InterstitialAdLoader interstitialLoader;
        private Interstitial interstitial;
        private bool interstitialLoading;
        private float nextInterstitialLoad;
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
            LoadInterstitial();
#endif
        }

        public bool TryShowInterstitial()
        {
            if (IsShowing || completion != null) return false;
#if UNITY_EDITOR
            return EditorTestInterstitial != null && EditorTestInterstitial();
#elif UNITY_ANDROID
            if (interstitial == null) { LoadInterstitial(); return false; }
            var showing = interstitial;
            interstitial = null;
            IsShowing = true;
            showing.OnAdDismissed += (sender, args) => FinishInterstitial(showing);
            showing.OnAdFailedToShow += (sender, args) => FinishInterstitial(showing);
            // Keep ownership for cleanup if the game exits while the ad is open.
            showingInterstitial = showing;
            showing.Show();
            return true;
#else
            return false;
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
        private Interstitial showingInterstitial;

        private void Update()
        {
            if (!disposed && !IsShowing && interstitial == null && !interstitialLoading
                && Time.unscaledTime >= nextInterstitialLoad) LoadInterstitial();
        }

        private void LoadInterstitial()
        {
            if (disposed || interstitialLoading || interstitial != null || IsShowing
                || Time.unscaledTime < nextInterstitialLoad) return;
            if (interstitialLoader == null) interstitialLoader = new InterstitialAdLoader();
            interstitialLoading = true;
            interstitialLoader.LoadAd(new AdRequest(interstitialAdUnitId), loaded =>
            {
                if (disposed) { loaded.Destroy(); return; }
                interstitialLoading = false;
                interstitial = loaded;
                // Loading only prepares the next level boundary; never auto-show here.
            }, failure =>
            {
                if (disposed) return;
                interstitialLoading = false;
                nextInterstitialLoad = Time.unscaledTime + 60;
            });
        }

        private void FinishInterstitial(Interstitial showing)
        {
            if (disposed || showingInterstitial != showing) return;
            showingInterstitial = null;
            showing.Destroy();
            IsShowing = false;
            nextInterstitialLoad = Time.unscaledTime + 1;
        }

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
            interstitialLoader?.CancelLoading();
            interstitial?.Destroy();
            showingInterstitial?.Destroy();
            completion = null;
        }
#endif
    }
}
