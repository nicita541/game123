using UnityEngine;

namespace Erudition
{
    public sealed class GameAudio : MonoBehaviour
    {
        public AudioSource musicSource;
        public AudioSource effectsSource;
        public AudioClip click;
        public AudioClip correct;
        public AudioClip mistake;
        public AudioClip victory;
        private bool effectsEnabled = true;
        private bool musicPaused;

        public void ApplySettings(bool music, bool sound)
        {
            effectsEnabled = sound;
            if (musicSource == null) return;
            if (music && !musicSource.isPlaying)
            {
                if (musicPaused) musicSource.UnPause(); else musicSource.Play();
                musicPaused = false;
            }
            else if (!music && musicSource.isPlaying) { musicSource.Pause(); musicPaused = true; }
        }

        private void Update()
        {
            var ads = CryptogramGame.Current == null ? null : CryptogramGame.Current.ads;
            var muted = ads != null && ads.IsShowing;
            if (musicSource != null) musicSource.mute = muted;
            if (effectsSource != null) effectsSource.mute = muted;
        }

        public void Click() => Play(click);
        public void Guess(bool success) => Play(success ? correct : mistake);
        public void Win() => Play(victory);

        private void Play(AudioClip clip)
        {
            if (effectsEnabled && effectsSource != null && clip != null) effectsSource.PlayOneShot(clip);
        }
    }
}
