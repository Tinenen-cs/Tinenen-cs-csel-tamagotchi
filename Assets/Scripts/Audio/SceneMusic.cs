using System;
using Tamagotchi.World;
using UnityEngine;

namespace Tamagotchi.Audio
{
    /// <summary>One background and the music track that goes with it.</summary>
    [Serializable]
    public class SceneTrack
    {
        public Sprite background;
        [Tooltip("Name of a music entry in AudioManager > Sounds, e.g. \"music_beach\".")]
        public string music;
    }

    /// <summary>
    /// Plays the music that belongs to the background on screen: each scene has its own track,
    /// and the moonlit bedroom (also shown while the pet sleeps) has a lullaby. AudioManager
    /// crossfades between tracks.
    /// </summary>
    public class SceneMusic : MonoBehaviour
    {
        [SerializeField] private BackgroundSwitcher backgrounds;
        [SerializeField] private SceneTrack[] tracks = new SceneTrack[0];

        private void OnEnable() => backgrounds.DisplayChanged += Play;
        private void OnDisable() => backgrounds.DisplayChanged -= Play;

        private void Start() => Play(backgrounds.Current);

        /// <summary>Music name for a background (null if it has none).</summary>
        public string MusicFor(Sprite background) => Array.Find(tracks, t => t.background == background)?.music;

        private void Play(Sprite background)
        {
            string music = MusicFor(background);
            if (music != null && AudioManager.Instance != null) AudioManager.Instance.PlayMusic(music);
        }
    }
}
