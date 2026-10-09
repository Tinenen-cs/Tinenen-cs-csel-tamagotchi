using System;
using UnityEngine;

namespace Tamagotchi.Pet
{
    /// <summary>
    /// The pet's stats (0-100) and the rules that change them. Implements the Monster Simulator rules:
    ///
    ///  - Happiness (max 100) passively decays by 0.01 every game tick.
    ///  - Play: happiness +0.5.  Study: happiness -0.5.  Eat: adds stamina and happiness.
    ///  - Sleep: restores stamina to full.
    ///  - Stamina below 20: Play and Study are locked. Sleep is only usable while stamina is below 20.
    ///  - Happiness exactly 100: <see cref="ReachedFullHappiness"/> (happy animation).
    ///    Happiness 50 or below: <see cref="IsSad"/> (sad animation).
    ///
    /// Hunger, intelligence and health are extra stats of this game. No UI, animation or audio code
    /// lives here; other systems listen to the events.
    /// </summary>
    public class PetStats : MonoBehaviour
    {
        [SerializeField] private PetStatsConfig config;

        // Largest step used when simulating, so long gaps (offline time) stay accurate.
        private const float MaxStepSeconds = 1f;

        public float Hunger { get; private set; }
        public float Happiness { get; private set; }
        public float Stamina { get; private set; }
        public float Intelligence { get; private set; }
        public float Health { get; private set; }

        public bool IsSleeping { get; private set; }
        public bool IsHangry { get; private set; }
        public bool IsSick { get; private set; }

        /// <summary>Seconds hunger has been at 0 (resets when fed).</summary>
        public float StarvingSeconds { get; private set; }

        public PetStatsConfig Config => config;

        /// <summary>[Rule] Happiness 50 or below.</summary>
        public bool IsSad => Happiness <= config.sadThreshold;
        public bool IsUnwell => Health < config.lowThreshold;

        /// <summary>[Rule] Stamina below 20: too tired to Play or Study.</summary>
        public bool IsExhausted => Stamina < config.staminaLock;
        public bool CanPlay => !IsSick && !IsExhausted;
        public bool CanStudy => !IsSick && !IsExhausted;
        /// <summary>[Rule] The pet can only fall asleep while stamina is below 20.</summary>
        public bool CanSleep => !IsSick && !IsSleeping && IsExhausted;

        /// <summary>Any stat value changed.</summary>
        public event Action Changed;
        /// <summary>Hunger crossed the hangry threshold (true = now hangry).</summary>
        public event Action<bool> HangryChanged;
        /// <summary>The pet fell asleep (true) or woke up (false).</summary>
        public event Action<bool> SleepChanged;
        /// <summary>[Rule] Happiness just reached exactly 100.</summary>
        public event Action ReachedFullHappiness;
        /// <summary>The pet got sick (game over).</summary>
        public event Action BecameSick;

        private float _napLeft;

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
            Stamina = config.startStamina;
            Intelligence = config.startIntelligence;
            Health = config.startHealth;
            StarvingSeconds = 0f;
            IsSick = false;
            IsSleeping = false;
            UpdateHangry();
            Changed?.Invoke();
        }

        /// <summary>Restores saved values (used by the save system and tests).</summary>
        public void SetValues(float hunger, float happiness, float stamina, float intelligence,
            float health, bool sleeping, float starvingSeconds)
        {
            Hunger = Clamp(hunger);
            Happiness = Clamp(happiness);
            Stamina = Clamp(stamina);
            Intelligence = Clamp(intelligence);
            Health = Clamp(health);
            IsSleeping = sleeping;
            _napLeft = sleeping ? config.napSeconds : 0f;
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

            // [Rule] Passive decay: 0.01 happiness per game tick (dt / tickSeconds ticks have passed).
            float ticks = dt / config.tickSeconds;
            Happiness = Clamp(Happiness - config.happinessDecayPerTick * ticks);

            Hunger = Clamp(Hunger - config.hungerDecay * slow * minutes);
            Intelligence = Clamp(Intelligence - config.intelligenceDecay * slow * minutes);
            if (!IsSleeping) Stamina = Clamp(Stamina - config.staminaDecay * minutes);

            if (IsSleeping)
            {
                _napLeft -= dt;
                if (_napLeft <= 0f) SetSleeping(false); // nap over: wakes up by itself
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
        // Actions (return false if the pet can't do it right now)
        // ------------------------------------------------------------------

        /// <summary>[Rule] Eat: adds stamina and happiness (and fills hunger).</summary>
        public bool Feed()
        {
            if (IsSick) return false;
            Hunger = Clamp(Hunger + config.feedHunger);
            Stamina = Clamp(Stamina + config.feedStamina);
            AddHappiness(config.feedHappiness);
            StarvingSeconds = 0f;
            return AfterAction();
        }

        /// <summary>[Rule] Play: happiness +0.5. Locked while stamina is below 20.</summary>
        public bool Play()
        {
            if (!CanPlay) return false;
            AddHappiness(config.playHappiness);
            Stamina = Clamp(Stamina - config.playStaminaCost);
            Hunger = Clamp(Hunger - config.playHungerCost);
            return AfterAction();
        }

        /// <summary>[Rule] Study: happiness -0.5. Locked while stamina is below 20.</summary>
        public bool Study()
        {
            if (!CanStudy) return false;
            AddHappiness(-config.studyHappinessCost);
            Intelligence = Clamp(Intelligence + config.studyIntelligence);
            Stamina = Clamp(Stamina - config.studyStaminaCost);
            return AfterAction();
        }

        /// <summary>
        /// [Rule] Sleep: restores stamina to full. Only usable while stamina is below 20.
        /// The pet then naps for a few seconds and wakes up by itself.
        /// </summary>
        public bool Sleep()
        {
            if (!CanSleep) return false;
            Stamina = 100f;
            _napLeft = config.napSeconds;
            SetSleeping(true);
            return true;
        }

        /// <summary>Petting (tapping the hamster): a little Happiness. Ignored while asleep or sick.</summary>
        public bool Pet()
        {
            if (IsSick || IsSleeping) return false;
            AddHappiness(config.petHappiness);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Scolding: the pet gets sad (Happiness goes down).</summary>
        public bool Scold()
        {
            if (IsSick) return false;
            AddHappiness(-config.scoldHappinessCost);
            return AfterAction();
        }

        /// <summary>Puts the pet to sleep or wakes it (no stamina rule; used by waking and tests).</summary>
        public void SetSleeping(bool sleeping)
        {
            if (IsSick || sleeping == IsSleeping) return;
            IsSleeping = sleeping;
            if (sleeping && _napLeft <= 0f) _napLeft = config.napSeconds;
            SleepChanged?.Invoke(sleeping);
            Changed?.Invoke();
        }

        private void AddHappiness(float delta)
        {
            float before = Happiness;
            Happiness = Clamp(Happiness + delta); // max 100
            if (before < 100f && Happiness >= 100f) ReachedFullHappiness?.Invoke();
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
