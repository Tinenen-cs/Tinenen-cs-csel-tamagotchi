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
        [Tooltip("Music played at start if nothing else picks one (SceneMusic switches per scene).")]
        [SerializeField] private string musicName = "music_home";
        [Tooltip("Seconds to fade out the old track and fade in the new one when the music changes.")]
        [SerializeField] private float musicFadeSeconds = 0.5f;

        private const string MuteKey = "Tamagotchi.Muted";

        public bool IsMuted { get; private set; }

        /// <summary>Name of the music track playing (or fading in) now.</summary>
        public string CurrentMusic { get; private set; }

        private string _pendingMusic;   // track to switch to once the old one has faded out
        private float _musicVolume = 1f; // target volume of the current track
        private float _fade = 1f;        // 1 = full volume, fades to 0 before a switch

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

        private void Start()
        {
            if (string.IsNullOrEmpty(CurrentMusic)) PlayMusic(musicName);
        }

        private void Update()
        {
            if (musicSource == null) return;
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, musicFadeSeconds);
            if (_pendingMusic != null)
            {
                _fade = Mathf.Max(0f, _fade - step);           // fade the old track out...
                if (_fade <= 0f) StartTrack(_pendingMusic);   // ...then switch
            }
            else if (_fade < 1f)
            {
                _fade = Mathf.Min(1f, _fade + step);           // fade the new track in
            }
            musicSource.volume = _musicVolume * _fade;
        }

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

        /// <summary>Starts the background music, or crossfades to another track if one is playing.</summary>
        public void PlayMusic(string soundName)
        {
            Sound s = Find(soundName);
            if (s == null || s.clip == null)
            {
                Debug.LogWarning($"[AudioManager] No music clip '{soundName}'.");
                return;
            }
            if (soundName == CurrentMusic && _pendingMusic == null) return; // already playing
            CurrentMusic = soundName;
            if (musicSource.isPlaying && musicSource.clip != null && musicFadeSeconds > 0f && Application.isPlaying)
                _pendingMusic = soundName; // Update fades out, then starts it
            else
                StartTrack(soundName);
        }

        private void StartTrack(string soundName)
        {
            Sound s = Find(soundName);
            _pendingMusic = null;
            musicSource.clip = s.clip;
            musicSource.pitch = s.pitch;
            musicSource.loop = s.loop;
            _musicVolume = s.volume;
            _fade = musicSource.isPlaying ? 0f : (musicFadeSeconds > 0f ? 0f : 1f);
            musicSource.volume = _musicVolume * _fade;
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
