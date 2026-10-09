using UnityEngine;

namespace Tamagotchi.Pet
{
    /// <summary>
    /// All tuning numbers for the pet's stats in one asset, editable in the Inspector.
    /// Asset: Assets/Data/PetStatsConfig.asset  (Create > Tamagotchi > Pet Stats Config for a new one).
    /// Stats range 0-100. The activity rules (Monster Simulator) are marked [Rule].
    /// </summary>
    [CreateAssetMenu(fileName = "PetStatsConfig", menuName = "Tamagotchi/Pet Stats Config")]
    public class PetStatsConfig : ScriptableObject
    {
        [Header("Global")]
        [Tooltip("Speeds up everything that happens over time. 1 = normal, 10 = demo mode.")]
        [Min(0f)] public float timeScale = 1f;

        [Header("Starting values (new game / restart)")]
        [Range(0, 100)] public float startHunger = 80f;
        [Range(0, 100)] public float startHappiness = 90f;
        [Range(0, 100)] public float startStamina = 60f;
        [Range(0, 100)] public float startIntelligence = 10f;
        [Range(0, 100)] public float startHealth = 100f;

        [Header("[Rule] Passive happiness decay")]
        [Tooltip("Length of one game tick in seconds (0.02 = 50 ticks per second, Unity's fixed step).")]
        [Min(0.001f)] public float tickSeconds = 0.02f;
        [Tooltip("Happiness lost every game tick.")]
        [Min(0f)] public float happinessDecayPerTick = 0.01f;

        [Header("Other decay, per minute (awake)")]
        [Min(0f)] public float hungerDecay = 6f;
        [Min(0f)] public float staminaDecay = 3f;
        [Min(0f)] public float intelligenceDecay = 1f;

        [Header("Sleeping")]
        [Tooltip("How long the pet naps after Sleep before waking up by itself (game seconds).")]
        [Min(0f)] public float napSeconds = 3f;
        [Tooltip("Decay of the other stats is multiplied by this while asleep.")]
        [Range(0, 1)] public float sleepDecayMultiplier = 0.5f;

        [Header("Health")]
        [Tooltip("Health lost per minute while hunger is at 0.")]
        [Min(0f)] public float healthDrainWhenStarving = 12f;
        [Tooltip("Health regained per minute while hunger is above the 'well fed' level.")]
        [Min(0f)] public float healthRegen = 3f;
        [Range(0, 100)] public float wellFedLevel = 50f;

        [Header("[Rule] Thresholds")]
        [Tooltip("Below this stamina the pet cannot Play or Study, and Sleep becomes available.")]
        [Range(0, 100)] public float staminaLock = 20f;
        [Tooltip("At or below this happiness the pet is SAD.")]
        [Range(0, 100)] public float sadThreshold = 50f;
        [Tooltip("Below this hunger the pet is HANGRY.")]
        [Range(0, 100)] public float hangryThreshold = 25f;
        [Tooltip("Below this health the pet cries.")]
        [Range(0, 100)] public float lowThreshold = 25f;
        [Tooltip("Seconds hunger may stay at 0 before the pet gets sick (game over).")]
        [Min(0f)] public float starvingSecondsUntilSick = 30f;

        [Header("Offline (time while the game is closed)")]
        [Tooltip("How fast stats change while the game is closed, compared to playing. 0.05 = 1 hour away counts as 3 minutes.")]
        [Range(0, 1)] public float offlineDecayMultiplier = 0.05f;
        [Tooltip("Longer absences are capped at this many hours.")]
        [Min(0f)] public float maxOfflineHours = 72f;

        [Header("[Rule] Action effects")]
        [Tooltip("Play: happiness gained.")]
        public float playHappiness = 0.5f;
        [Tooltip("Study: happiness lost.")]
        public float studyHappinessCost = 0.5f;
        [Tooltip("Eat: stamina gained.")]
        public float feedStamina = 5f;
        [Tooltip("Eat: happiness gained.")]
        public float feedHappiness = 2f;
        [Tooltip("Eat: hunger filled.")]
        public float feedHunger = 25f;

        [Header("Other action effects")]
        public float playStaminaCost = 2f;
        public float playHungerCost = 1f;
        public float studyStaminaCost = 5f;
        public float studyIntelligence = 5f;
        [Tooltip("Happiness gained when the player taps (pets) the hamster.")]
        public float petHappiness = 3f;
        [Tooltip("Happiness lost when the pet is scolded.")]
        public float scoldHappinessCost = 10f;
    }
}
