using NUnit.Framework;
using Tamagotchi.Pet;
using UnityEngine;

namespace Tamagotchi.Tests
{
    /// <summary>
    /// The stat rules, on a standalone PetStats with the default config. The first group checks the
    /// Monster Simulator activity rules one by one.
    /// </summary>
    public class PetStatsTests
    {
        private GameObject _go;
        private PetStats _stats;
        private PetStatsConfig _cfg;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<PetStatsConfig>();
            _go = new GameObject("PetStatsTest");
            _go.SetActive(false); // so Awake runs after the config is set
            _stats = _go.AddComponent<PetStats>();
            _stats.SetConfig(_cfg);
            _stats.enabled = false; // no Update ticking; tests drive time with Simulate
            _go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_cfg);
        }

        // ---------------- 1. State variables ----------------

        [Test]
        public void Rule_HappinessDecays_0_01_PerTick()
        {
            Assert.AreEqual(0.01f, _cfg.happinessDecayPerTick);
            _stats.SetValues(80, 80, 60, 10, 100, false, 0);
            _stats.Simulate(_cfg.tickSeconds * 100); // 100 ticks
            Assert.AreEqual(80f - 100 * 0.01f, _stats.Happiness, 0.001f);
        }

        [Test]
        public void Rule_HappinessMaxIs100()
        {
            _stats.SetValues(80, 99.8f, 60, 10, 100, false, 0);
            _stats.Play();
            Assert.AreEqual(100f, _stats.Happiness, "Play at 99.8 should stop at exactly 100.");
            _stats.Feed();
            Assert.AreEqual(100f, _stats.Happiness);
        }

        // ---------------- 2. Action mechanics ----------------

        [Test]
        public void Rule_Play_AddsHalfHappiness()
        {
            _stats.SetValues(80, 70, 60, 10, 100, false, 0);
            Assert.IsTrue(_stats.Play());
            Assert.AreEqual(70.5f, _stats.Happiness, 0.0001f);
        }

        [Test]
        public void Rule_Study_RemovesHalfHappiness()
        {
            _stats.SetValues(80, 70, 60, 10, 100, false, 0);
            Assert.IsTrue(_stats.Study());
            Assert.AreEqual(69.5f, _stats.Happiness, 0.0001f);
        }

        [Test]
        public void Rule_Eat_AddsStaminaAndHappiness()
        {
            _stats.SetValues(50, 70, 40, 10, 100, false, 0);
            Assert.IsTrue(_stats.Feed());
            Assert.Greater(_stats.Stamina, 40f);
            Assert.Greater(_stats.Happiness, 70f);
            Assert.AreEqual(40f + _cfg.feedStamina, _stats.Stamina, 0.0001f);
            Assert.AreEqual(70f + _cfg.feedHappiness, _stats.Happiness, 0.0001f);
        }

        [Test]
        public void Rule_Sleep_RestoresStaminaToFull()
        {
            _stats.SetValues(80, 70, 10, 10, 100, false, 0);
            Assert.IsTrue(_stats.Sleep());
            Assert.AreEqual(100f, _stats.Stamina);
            Assert.IsTrue(_stats.IsSleeping);
            _stats.Simulate(_cfg.napSeconds + 0.1f);
            Assert.IsFalse(_stats.IsSleeping, "The pet wakes up after its nap.");
        }

        // ---------------- 3. Conditional restrictions ----------------

        [Test]
        public void Rule_StaminaBelow20_LocksPlayAndStudy()
        {
            _stats.SetValues(80, 70, 19.9f, 10, 100, false, 0);
            Assert.IsFalse(_stats.CanPlay);
            Assert.IsFalse(_stats.CanStudy);
            Assert.IsFalse(_stats.Play(), "Play must be refused below 20 stamina.");
            Assert.IsFalse(_stats.Study(), "Study must be refused below 20 stamina.");
            Assert.AreEqual(70f, _stats.Happiness, 0.0001f, "Refused actions change nothing.");

            _stats.SetValues(80, 70, 20f, 10, 100, false, 0);
            Assert.IsTrue(_stats.CanPlay, "At exactly 20 stamina Play is allowed again.");
            Assert.IsTrue(_stats.CanStudy);
        }

        [Test]
        public void Rule_SleepOnlyUnlockedBelow20Stamina()
        {
            _stats.SetValues(80, 70, 20f, 10, 100, false, 0);
            Assert.IsFalse(_stats.CanSleep);
            Assert.IsFalse(_stats.Sleep(), "Sleep must be refused at 20 stamina or more.");
            Assert.AreEqual(20f, _stats.Stamina);

            _stats.SetValues(80, 70, 19f, 10, 100, false, 0);
            Assert.IsTrue(_stats.CanSleep);
            Assert.IsTrue(_stats.Sleep());
        }

        // ---------------- 4. Visual / animation states ----------------

        [Test]
        public void Rule_ReachingExactly100_TriggersHappy()
        {
            int fired = 0;
            _stats.ReachedFullHappiness += () => fired++;
            _stats.SetValues(80, 99.6f, 60, 10, 100, false, 0);
            _stats.Play();
            Assert.AreEqual(100f, _stats.Happiness);
            Assert.AreEqual(1, fired);
            _stats.Play(); // already 100: no second trigger
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Rule_Happiness50OrBelow_IsSad()
        {
            _stats.SetValues(80, 50.01f, 60, 10, 100, false, 0);
            Assert.IsFalse(_stats.IsSad);
            _stats.SetValues(80, 50f, 60, 10, 100, false, 0);
            Assert.IsTrue(_stats.IsSad, "Exactly 50 counts as sad.");
            _stats.SetValues(80, 49f, 60, 10, 100, false, 0);
            Assert.IsTrue(_stats.IsSad);
        }

        // ---------------- Other rules of this game ----------------

        [Test]
        public void StartsAtConfiguredValues()
        {
            Assert.AreEqual(_cfg.startHappiness, _stats.Happiness);
            Assert.AreEqual(_cfg.startStamina, _stats.Stamina);
            Assert.IsFalse(_stats.IsSick);
        }

        [Test]
        public void HungerDecaysOverTime()
        {
            _stats.SetValues(80, 80, 60, 10, 100, false, 0);
            _stats.Simulate(60f);
            Assert.AreEqual(80f - _cfg.hungerDecay, _stats.Hunger, 0.01f);
        }

        [Test]
        public void TimeScaleSpeedsUpDecay()
        {
            _cfg.timeScale = 10f;
            _stats.SetValues(80, 80, 60, 10, 100, false, 0);
            _stats.Simulate(6f); // 6 s x10 = 1 minute
            Assert.AreEqual(80f - _cfg.hungerDecay, _stats.Hunger, 0.01f);
        }

        [Test]
        public void HangryTurnsOnBelowThreshold_AndOffWhenFed()
        {
            bool? last = null;
            _stats.HangryChanged += h => last = h;
            _stats.SetValues(_cfg.hangryThreshold + 1, 80, 60, 10, 100, false, 0);
            _stats.Simulate(60f);
            Assert.IsTrue(_stats.IsHangry);
            Assert.AreEqual(true, last);
            _stats.Feed();
            Assert.IsFalse(_stats.IsHangry);
            Assert.AreEqual(false, last);
        }

        [Test]
        public void ScoldLowersHappiness()
        {
            _stats.SetValues(50, 50, 60, 10, 100, false, 0);
            Assert.IsTrue(_stats.Scold());
            Assert.AreEqual(50 - _cfg.scoldHappinessCost, _stats.Happiness, 0.01f);
        }

        [Test]
        public void StarvingTooLong_MakesPetSick()
        {
            bool sick = false;
            _stats.BecameSick += () => sick = true;
            _stats.SetValues(0, 80, 60, 10, 100, false, 0);
            _stats.Simulate(_cfg.starvingSecondsUntilSick - 1f);
            Assert.IsFalse(_stats.IsSick);
            _stats.Simulate(2f);
            Assert.IsTrue(_stats.IsSick);
            Assert.IsTrue(sick);
        }

        [Test]
        public void HealthDrainsWhileStarving_AndZeroHealthMeansSick()
        {
            _cfg.starvingSecondsUntilSick = 9999f; // isolate the health rule
            _stats.SetValues(0, 80, 60, 10, 10, false, 0);
            _stats.Simulate(30f);
            Assert.Less(_stats.Health, 10f);
            _stats.Simulate(600f);
            Assert.AreEqual(0f, _stats.Health);
            Assert.IsTrue(_stats.IsSick);
        }

        [Test]
        public void SickPetIgnoresActions_UntilRestart()
        {
            _stats.SetValues(80, 80, 60, 10, 0, false, 0);
            Assert.IsTrue(_stats.IsSick);
            Assert.IsFalse(_stats.Feed());
            Assert.IsFalse(_stats.Play());
            _stats.ResetToStart();
            Assert.IsFalse(_stats.IsSick);
            Assert.IsTrue(_stats.Feed());
        }

        [Test]
        public void LongOfflineGap_IsSimulatedAccurately()
        {
            _cfg.starvingSecondsUntilSick = 99999f;
            _cfg.healthDrainWhenStarving = 0f;
            _stats.SetValues(100, 100, 100, 100, 100, false, 0);
            _stats.Simulate(3600f); // one hour
            Assert.AreEqual(0f, _stats.Hunger);
            Assert.AreEqual(100f - 60f * _cfg.intelligenceDecay, _stats.Intelligence, 0.5f);
            Assert.AreEqual(0f, _stats.Happiness, "0.01 per tick for an hour empties happiness.");
        }
    }
}
