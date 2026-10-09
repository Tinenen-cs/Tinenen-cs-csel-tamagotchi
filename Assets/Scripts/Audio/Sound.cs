using System;
using UnityEngine;

namespace Tamagotchi.Audio
{
    /// <summary>
    /// One named sound and how it should be played. Based on the project's original Sound class;
    /// the per-sound AudioSource and mixer group were dropped because AudioManager now plays
    /// everything through one SFX source and one music source.
    /// </summary>
    [Serializable]
    public class Sound
    {
        [Tooltip("Name used in code, e.g. \"eat\" or \"click\".")]
        public string name;

        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 0.75f;

        [Tooltip("Random +/- change in volume each time it plays, so repeats sound less robotic.")]
        [Range(0f, 1f)]
        public float volumeVariance = 0.1f;

        [Range(0.1f, 3f)]
        public float pitch = 1f;

        [Tooltip("Random +/- change in pitch each time it plays.")]
        [Range(0f, 1f)]
        public float pitchVariance = 0.1f;

        [Tooltip("Only used when this sound is played as music.")]
        public bool loop;
    }
}
