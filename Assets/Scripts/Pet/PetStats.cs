using System;
using UnityEngine;

namespace Tamagotchi.Pet
{
    /// <summary>
    /// The pet's five stats (0-100) and the rules that change them:
    /// decay over time, action effects, sleeping, health, hangry and sick.
    /// Contains no UI, animation or audio code; other systems listen to its events.
    /// </summary>
    public class PetStats : MonoBehaviour
    {
        [SerializeField] private PetStatsConfig config;

        // Largest step used when simulating, so long gaps (offline time) stay accurate.
        private const float MaxStepSeconds = 1f;

        public float Hunger { get; private set; }
        public float Happiness { get; private set; }
        public float Energy { get; private set; }
        public float Intelligence { get; private set; }
        public float Health { get; private set; }

        public bool IsSleeping { get; private set; }
        public bool IsHangry { get; private set; }
        public bool IsSick { get; private set; }

        /// <summary>Seconds hunger has been at 0 (resets when fed).</summary>
        public float StarvingSeconds { get; private set; }

        public PetStatsConfig Config => config;

        public bool IsUnhappy => Happiness < config.lowThreshold;
        public bool IsUnwell => Health < config.lowThreshold;

        /// <summary>Any stat value changed.</summary>
        public event Action Changed;
        /// <summary>Hunger crossed the hangry threshold (true = now hangry).</summary>
        public event Action<bool> HangryChanged;
        /// <summary>The pet fell asleep (true) or woke up (false).</summary>
        public event Action<bool> SleepChanged;
        /// <summary>The pet got sick (game over).</summary>
        public event Action BecameSick;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning("[PetStats] No config assigned; using defaults.");
                config = ScriptableObject.CreateInstance<PetStatsConfig>();
            }
            ResetToStart();
        }

        private void Update()
        {
            if (!IsSick) Simulate(Time.deltaTime);
        }

        /// <summary>Use a specific config (tests, or swapping difficulty at runtime).</summary>
        public void SetConfig(PetStatsConfig newConfig)
        {
            config = newConfig;
        }

        /// <summary>Puts every stat back to its starting value (new game / restart).</summary>
        public void ResetToStart()
        {
            Hunger = config.startHunger;
            Happiness = config.startHappiness;
            Energy = config.startEnergy;
            Intelligence = config.startIntelligence;
            Health = config.startHealth;
            StarvingSeconds = 0f;
            IsSick = false;
            IsSleeping = false;
            UpdateHangry();
            Changed?.Invoke();
        }

        /// <summary>Restores saved values (used by the save system).</summary>
        public void SetValues(float hunger, float happiness, float energy, float intelligence,
            float health, bool sleeping, float starvingSeconds)
        {
            Hunger = Clamp(hunger);
            Happiness = Clamp(happiness);
            Energy = Clamp(energy);
            Intelligence = Clamp(intelligence);
            Health = Clamp(health);
            IsSleeping = sleeping;
            StarvingSeconds = Mathf.Max(0f, starvingSeconds);
            IsSick = false;
            UpdateHangry();
            Changed?.Invoke();
            CheckSick();
        }

        // ------------------------------------------------------------------
        // Time
        // ------------------------------------------------------------------

        /// <summary>
        /// Advances the stats by <paramref name="seconds"/> of game time.
        /// Called every frame, and with a large value to apply offline time.
        /// </summary>
        public void Simulate(float seconds) => Simulate(seconds, applyTimeScale: true);

        /// <summary>
        /// Advances the stats; <paramref name="applyTimeScale"/> = false ignores the demo Time Scale
        /// (used for offline time, which has its own multiplier).
        /// </summary>
        public void Simulate(float seconds, bool applyTimeScale)
        {
            float remaining = seconds * (applyTimeScale ? config.timeScale : 1f);
            while (remaining > 0f && !IsSick)
            {
                float step = Mathf.Min(remaining, MaxStepSeconds);
                Step(step);
                remaining -= step;
            }
            Changed?.Invoke();
        }

        private void Step(float dt)
        {
            float minutes = dt / 60f;
            float slow = IsSleeping ? config.sleepDecayMultiplier : 1f;

            Hunger = Clamp(Hunger - config.hungerDecay * slow * minutes);
            Happiness = Clamp(Happiness - config.happinessDecay * slow * minutes);
            Intelligence = Clamp(Intelligence - config.intelligenceDecay * slow * minutes);

            if (IsSleeping)
            {
                Energy = Clamp(Energy + config.sleepEnergyPerSecond * dt);
                if (Energy >= 100f) SetSleeping(false); // wakes up fully rested
            }
            else
            {
                Energy = Clamp(Energy - config.energyDecay * minutes);
            }

            // Health: drains while starving, slowly heals while well fed.
            if (Hunger <= 0f)
                Health = Clamp(Health - config.healthDrainWhenStarving * minutes);
            else if (Hunger >= config.wellFedLevel)
                Health = Clamp(Health + config.healthRegen * minutes);

            StarvingSeconds = Hunger <= 0f ? StarvingSeconds + dt : 0f;

            UpdateHangry();
            CheckSick();
        }

        // ------------------------------------------------------------------
        // Actions (return false if the pet refuses / can't do it right now)
        // ------------------------------------------------------------------

        public bool Feed()
        {
            if (IsSick) return false;
            Hunger = Clamp(Hunger + config.feedHunger);
            Happiness = Clamp(Happiness + config.feedHappiness);
            StarvingSeconds = 0f;
            return AfterAction();
        }

        /// <summary>Scolding: the pet gets sad (Happiness goes down).</summary>
        public bool Scold()
        {
            if (IsSick) return false;
            Happiness = Clamp(Happiness - config.scoldHappinessCost);
            return AfterAction();
        }

        /// <summary>Raises Intelligence, costs Energy and a bit of Happiness. Refused when too tired.</summary>
        public bool Study()
        {
            if (IsSick || Energy < config.studyEnergyCost) return false;
            Intelligence = Clamp(Intelligence + config.studyIntelligence);
            Energy = Clamp(Energy - config.studyEnergyCost);
            Happiness = Clamp(Happiness - config.studyHappinessCost);
            return AfterAction();
        }

        /// <summary>Raises Happiness, costs a little Energy and Hunger. Refused when too tired.</summary>
        public bool Play()
        {
            if (IsSick || Energy < config.playEnergyCost) return false;
            Happiness = Clamp(Happiness + config.playHappiness);
            Energy = Clamp(Energy - config.playEnergyCost);
            Hunger = Clamp(Hunger - config.playHungerCost);
            return AfterAction();
        }

        /// <summary>Falls asleep (energy then refills over time) or wakes up.</summary>
        public void SetSleeping(bool sleeping)
        {
            if (IsSick || sleeping == IsSleeping) return;
            IsSleeping = sleeping;
            SleepChanged?.Invoke(sleeping);
            Changed?.Invoke();
        }

        private bool AfterAction()
        {
            if (IsSleeping) SetSleeping(false); // any interaction wakes the pet
            UpdateHangry();
            Changed?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------

        private void UpdateHangry()
        {
            bool hangry = Hunger < config.hangryThreshold;
            if (hangry == IsHangry) return;
            IsHangry = hangry;
            HangryChanged?.Invoke(hangry);
        }

        private void CheckSick()
        {
            if (IsSick) return;
            bool starvedTooLong = StarvingSeconds >= config.starvingSecondsUntilSick;
            if (Health <= 0f || starvedTooLong)
            {
                IsSick = true;
                IsSleeping = false;
                BecameSick?.Invoke();
            }
        }

        private static float Clamp(float v) => Mathf.Clamp(v, 0f, 100f);
    }
}
