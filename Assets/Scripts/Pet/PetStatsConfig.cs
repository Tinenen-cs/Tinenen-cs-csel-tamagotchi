using UnityEngine;

namespace Tamagotchi.Pet
{
    /// <summary>
    /// All tuning numbers for the pet's stats in one asset, editable in the Inspector.
    /// Asset: Assets/Data/PetStatsConfig.asset  (Create > Tamagotchi > Pet Stats Config for a new one).
    /// Rates are "points per minute" unless the name says otherwise. Stats range 0-100.
    /// </summary>
    [CreateAssetMenu(fileName = "PetStatsConfig", menuName = "Tamagotchi/Pet Stats Config")]
    public class PetStatsConfig : ScriptableObject
    {
        [Header("Global")]
        [Tooltip("Speeds up all decay/regeneration. 1 = normal, 10 = demo mode to see hangry/sick quickly.")]
        [Min(0f)] public float timeScale = 1f;

        [Header("Starting values (new game / restart)")]
        [Range(0, 100)] public float startHunger = 80f;
        [Range(0, 100)] public float startHappiness = 80f;
        [Range(0, 100)] public float startEnergy = 80f;
        [Range(0, 100)] public float startIntelligence = 10f;
        [Range(0, 100)] public float startHealth = 100f;

        [Header("Decay per minute (awake)")]
        [Min(0f)] public float hungerDecay = 6f;
        [Min(0f)] public float happinessDecay = 4f;
        [Min(0f)] public float energyDecay = 3f;
        [Min(0f)] public float intelligenceDecay = 1f;

        [Header("Sleeping")]
        [Tooltip("Energy gained per SECOND while sleeping. The pet wakes up by itself at 100.")]
        [Min(0f)] public float sleepEnergyPerSecond = 4f;
        [Tooltip("Hunger/happiness/intelligence decay is multiplied by this while asleep.")]
        [Range(0, 1)] public float sleepDecayMultiplier = 0.5f;

        [Header("Health")]
        [Tooltip("Health lost per minute while hunger is at 0.")]
        [Min(0f)] public float healthDrainWhenStarving = 12f;
        [Tooltip("Health regained per minute while hunger is above the 'well fed' level.")]
        [Min(0f)] public float healthRegen = 3f;
        [Range(0, 100)] public float wellFedLevel = 50f;

        [Header("Thresholds")]
        [Tooltip("Below this hunger the pet is HANGRY.")]
        [Range(0, 100)] public float hangryThreshold = 25f;
        [Tooltip("Below this a stat counts as 'low' (sad when happiness is low, crying when health is low).")]
        [Range(0, 100)] public float lowThreshold = 25f;
        [Tooltip("Seconds hunger may stay at 0 before the pet gets sick (game over).")]
        [Min(0f)] public float starvingSecondsUntilSick = 30f;

        [Header("Offline (time while the game is closed)")]
        [Tooltip("How fast stats change while the game is closed, compared to playing. 0.05 = 1 hour away counts as 3 minutes.")]
        [Range(0, 1)] public float offlineDecayMultiplier = 0.05f;
        [Tooltip("Longer absences are capped at this many hours.")]
        [Min(0f)] public float maxOfflineHours = 72f;

        [Header("Action effects (instant)")]
        public float feedHunger = 25f;
        public float feedHappiness = 3f;
        public float studyIntelligence = 12f;
        public float studyEnergyCost = 10f;
        public float studyHappinessCost = 5f;
        public float playHappiness = 20f;
        public float playEnergyCost = 8f;
        public float playHungerCost = 3f;
        [Tooltip("Happiness gained when the player taps (pets) the hamster.")]
        public float petHappiness = 3f;
        [Tooltip("Happiness lost when the pet is scolded.")]
        public float scoldHappinessCost = 10f;
    }
}
