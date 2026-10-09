using System;
using UnityEngine;

namespace Tamagotchi.Audio
{
    /// <summary>
    /// Plays all game audio through two sources: one for sound effects, one for music.
    /// Sounds are looked up by name from the <see cref="sounds"/> list (edit it in the Inspector
    /// to swap clips). The mute setting is remembered between sessions.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        [Tooltip("Every sound effect and the music track, by name.")]
        [SerializeField] private Sound[] sounds = new Sound[0];
        [Tooltip("Name of the entry in Sounds that is played as background music.")]
        [SerializeField] private string musicName = "music";

        private const string MuteKey = "Tamagotchi.Muted";

        public bool IsMuted { get; private set; }

        /// <summary>Raised when mute is switched on (true) or off (false).</summary>
        public event Action<bool> MuteChanged;

        /// <summary>Raised with the name of every sound effect played (handy for tests and debugging).</summary>
        public event Action<string> SoundPlayed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            IsMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            ApplyMute();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start() => PlayMusic(musicName);

        /// <summary>Plays a sound effect once (with a little random pitch/volume variation).</summary>
        public void Play(string soundName)
        {
            Sound s = Find(soundName);
            if (s == null || s.clip == null)
            {
                Debug.LogWarning($"[AudioManager] No clip for sound '{soundName}'.");
                return;
            }
            sfxSource.pitch = s.pitch + UnityEngine.Random.Range(-s.pitchVariance, s.pitchVariance) * 0.5f;
            float volume = s.volume * (1f + UnityEngine.Random.Range(-s.volumeVariance, s.volumeVariance) * 0.5f);
            sfxSource.PlayOneShot(s.clip, Mathf.Clamp01(volume));
            SoundPlayed?.Invoke(soundName);
        }

        /// <summary>Starts (or switches) the background music.</summary>
        public void PlayMusic(string soundName)
        {
            Sound s = Find(soundName);
            if (s == null || s.clip == null) return;
            musicSource.clip = s.clip;
            musicSource.volume = s.volume;
            musicSource.pitch = s.pitch;
            musicSource.loop = s.loop;
            musicSource.Play();
        }

        public void ToggleMute() => SetMuted(!IsMuted);

        /// <summary>Mutes or unmutes music and sound effects, and remembers the choice.</summary>
        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMute();
            MuteChanged?.Invoke(muted);
        }

        /// <summary>True if a sound with this name has a clip assigned.</summary>
        public bool Has(string soundName) => Find(soundName)?.clip != null;

        private void ApplyMute()
        {
            if (sfxSource != null) sfxSource.mute = IsMuted;
            if (musicSource != null) musicSource.mute = IsMuted;
        }

        private Sound Find(string soundName) => Array.Find(sounds, s => s.name == soundName);
    }
}
