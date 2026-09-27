using System;
using System.IO;
using Erudition;
using UnityEditor;
using UnityEngine;

public static class AudioAuthoring
{
    public static void Apply(CryptogramGame game)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Audio")) AssetDatabase.CreateFolder("Assets", "Audio");
        Write("Click", new[] { 660.0 }, .11, .09);
        Write("Correct", new[] { 523.25, 659.25, 783.99 }, .30, .15);
        Write("Mistake", new[] { 246.94, 196.0 }, .25, .12);
        Write("Victory", new[] { 523.25, 659.25, 783.99, 1046.5 }, .75, .15);
        Write("LibraryMusic", new[] { 261.63, 329.63, 392.0, 523.25, 440.0, 349.23, 293.66, 392.0,
            261.63, 329.63, 392.0, 493.88, 440.0, 392.0, 329.63, 293.66 }, 16, .09);
        AssetDatabase.Refresh();
        var sound = game.gameObject.AddComponent<GameAudio>();
        sound.musicSource = game.gameObject.AddComponent<AudioSource>();
        sound.effectsSource = game.gameObject.AddComponent<AudioSource>();
        sound.musicSource.clip = Clip("LibraryMusic");
        sound.musicSource.loop = true;
        sound.musicSource.playOnAwake = false;
        sound.musicSource.volume = .28f;
        sound.effectsSource.playOnAwake = false;
        sound.effectsSource.volume = .5f;
        sound.click = Clip("Click"); sound.correct = Clip("Correct"); sound.mistake = Clip("Mistake"); sound.victory = Clip("Victory");
        game.soundPlayer = sound;
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();
    }

    private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");

    private static void Write(string name, double[] notes, double duration, double volume)
    {
        const int rate = 22050;
        var count = (int)(duration * rate);
        using (var stream = File.Create("Assets/Audio/" + name + ".wav"))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
            writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
            var noteDuration = duration / notes.Length;
            for (var sample = 0; sample < count; sample++)
            {
                var t = sample / (double)rate;
                var noteIndex = Math.Min(notes.Length - 1, (int)(t / noteDuration));
                var local = t - noteIndex * noteDuration;
                var envelope = Math.Min(1, local / .012) * Math.Exp(-local / (noteDuration * .35))
                    * Math.Min(1, (noteDuration - local) / .04);
                var phase = 2 * Math.PI * notes[noteIndex] * local;
                var tone = Math.Sin(phase) + .2 * Math.Sin(phase * 2) + .08 * Math.Sin(phase * 3);
                writer.Write((short)(tone * envelope * volume * short.MaxValue));
            }
        }
    }
}
