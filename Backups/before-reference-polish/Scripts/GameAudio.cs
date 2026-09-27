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

        public void ApplySettings(bool music, bool sound)
        {
            effectsEnabled = sound;
            if (musicSource == null) return;
            if (music && !musicSource.isPlaying) musicSource.Play();
            else if (!music && musicSource.isPlaying) musicSource.Pause();
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
