using System;
using Tamagotchi.Pet;
using Tamagotchi.World;
using UnityEngine;

namespace Tamagotchi
{
    /// <summary>
    /// Saves the pet to PlayerPrefs (one small JSON entry) and loads it on start, including the
    /// time spent away: offline time is replayed at <see cref="PetStatsConfig.offlineDecayMultiplier"/>
    /// speed, capped at <see cref="PetStatsConfig.maxOfflineHours"/>.
    /// Saves every few seconds, when the app is paused/minimised, and when it quits.
    /// Runs before other scripts' Start so they all see the loaded state.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SaveSystem : MonoBehaviour
    {
        [Serializable]
        private class SaveData
        {
            public int version = 1;
            public float hunger, happiness, energy, intelligence, health, starvingSeconds;
            public bool sleeping;
            public int background;
            public long savedAtUtcTicks;
        }

        [SerializeField] private PetStats stats;
        [SerializeField] private BackgroundSwitcher backgrounds;
        [Tooltip("Seconds between automatic saves.")]
        [SerializeField] private float autosaveSeconds = 5f;

        /// <summary>PlayerPrefs key prefix (tests use their own so they never touch a real save).</summary>
        public static string KeyPrefix = "Tamagotchi.";

        /// <summary>Clock used for offline time (tests can replace it).</summary>
        public static Func<DateTime> UtcNow = () => DateTime.UtcNow;

        private static string Key => KeyPrefix + "Save";

        /// <summary>Real seconds the game was closed before this session (0 for a new game).</summary>
        public float LastOfflineSeconds { get; private set; }

        /// <summary>True if this session continued from a save.</summary>
        public bool LoadedFromSave { get; private set; }

        public static bool HasSave => PlayerPrefs.HasKey(Key);

        private float _nextAutosave;

        private void Start()
        {
            Load();
            _nextAutosave = Time.unscaledTime + autosaveSeconds;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextAutosave) return;
            _nextAutosave = Time.unscaledTime + autosaveSeconds;
            Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        /// <summary>Writes the current pet to PlayerPrefs.</summary>
        public void Save()
        {
            var data = new SaveData
            {
                hunger = stats.Hunger,
                happiness = stats.Happiness,
                energy = stats.Energy,
                intelligence = stats.Intelligence,
                health = stats.Health,
                starvingSeconds = stats.StarvingSeconds,
                sleeping = stats.IsSleeping,
                background = backgrounds != null ? backgrounds.Index : 0,
                savedAtUtcTicks = UtcNow().Ticks,
            };
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        /// <summary>Restores the saved pet and applies the time spent away. Returns false if there is no save.</summary>
        public bool Load()
        {
            LastOfflineSeconds = 0f;
            LoadedFromSave = false;
            if (!HasSave) return false;

            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)); }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] Save data unreadable, starting fresh: " + e.Message);
                return false;
            }
            if (data == null) return false;

            if (backgrounds != null) backgrounds.SetIndex(data.background);
            stats.SetValues(data.hunger, data.happiness, data.energy, data.intelligence, data.health,
                            data.sleeping, data.starvingSeconds);

            // Time away. A clock set backwards counts as no time.
            double away = (UtcNow() - new DateTime(data.savedAtUtcTicks, DateTimeKind.Utc)).TotalSeconds;
            PetStatsConfig cfg = stats.Config;
            away = Math.Max(0.0, Math.Min(away, cfg.maxOfflineHours * 3600.0));
            LastOfflineSeconds = (float)away;
            if (!stats.IsSick && away > 0)
                stats.Simulate(LastOfflineSeconds * cfg.offlineDecayMultiplier, applyTimeScale: false);

            LoadedFromSave = true;
            return true;
        }

        /// <summary>Deletes the save (menu: Tamagotchi > Clear Save Data).</summary>
        public static void ClearSave()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        /// <summary>"2h 5m", "12m", "45s".</summary>
        public static string FormatDuration(float seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d {t.Hours}h";
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}m";
            return $"{(int)t.TotalSeconds}s";
        }
    }
}
