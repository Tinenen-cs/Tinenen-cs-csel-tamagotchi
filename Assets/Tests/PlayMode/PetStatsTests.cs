using NUnit.Framework;
using Tamagotchi.Pet;
using UnityEngine;

namespace Tamagotchi.Tests
{
    /// <summary>Rules of the stat system, tested on a standalone PetStats with known numbers.</summary>
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

        [Test]
        public void StartsAtConfiguredValues()
        {
            Assert.AreEqual(_cfg.startHunger, _stats.Hunger);
            Assert.AreEqual(_cfg.startHealth, _stats.Health);
            Assert.IsFalse(_stats.IsSick);
        }

        [Test]
        public void StatsDecayOverTime()
        {
            float before = _stats.Hunger;
            _stats.Simulate(60f); // one minute
            Assert.AreEqual(before - _cfg.hungerDecay, _stats.Hunger, 0.01f);
            Assert.AreEqual(_cfg.startThirst - _cfg.thirstDecay, _stats.Thirst, 0.01f);
        }

        [Test]
        public void TimeScaleSpeedsUpDecay()
        {
            _cfg.timeScale = 10f;
            _stats.Simulate(6f); // 6 s x10 = 1 minute
            Assert.AreEqual(_cfg.startHunger - _cfg.hungerDecay, _stats.Hunger, 0.01f);
        }

        [Test]
        public void FeedRaisesHunger_AndClampsAt100()
        {
            _stats.SetValues(50, 50, 50, 50, 50, 100, false, 0);
            _stats.Feed();
            Assert.AreEqual(50 + _cfg.feedHunger, _stats.Hunger, 0.01f);
            for (int i = 0; i < 10; i++) _stats.Feed();
            Assert.AreEqual(100f, _stats.Hunger);
        }

        [Test]
        public void StudyRaisesIntelligence_LowersEnergyAndHappiness()
        {
            _stats.SetValues(50, 50, 50, 50, 10, 100, false, 0);
            Assert.IsTrue(_stats.Study());
            Assert.AreEqual(10 + _cfg.studyIntelligence, _stats.Intelligence, 0.01f);
            Assert.AreEqual(50 - _cfg.studyEnergyCost, _stats.Energy, 0.01f);
            Assert.AreEqual(50 - _cfg.studyHappinessCost, _stats.Happiness, 0.01f);
        }

        [Test]
        public void StudyIsRefusedWhenTooTired()
        {
            _stats.SetValues(50, 50, 50, _cfg.studyEnergyCost - 1, 10, 100, false, 0);
            Assert.IsFalse(_stats.Study());
            Assert.AreEqual(10f, _stats.Intelligence);
        }

        [Test]
        public void HangryTurnsOnBelowThreshold_AndOffWhenFed()
        {
            bool? last = null;
            _stats.HangryChanged += h => last = h;
            _stats.SetValues(_cfg.hangryThreshold + 1, 80, 80, 80, 10, 100, false, 0);
            _stats.Simulate(60f); // drops below the threshold
            Assert.IsTrue(_stats.IsHangry);
            Assert.AreEqual(true, last);

            _stats.Feed();
            Assert.IsFalse(_stats.IsHangry);
            Assert.AreEqual(false, last);
        }

        [Test]
        public void SleepingRestoresEnergy_AndWakesWhenFull()
        {
            _stats.SetValues(80, 80, 80, 10, 10, 100, false, 0);
            _stats.SetSleeping(true);
            _stats.Simulate(5f);
            Assert.AreEqual(10 + 5 * _cfg.sleepEnergyPerSecond, _stats.Energy, 0.5f);
            Assert.IsTrue(_stats.IsSleeping);

            // 10 + 5*4 = 30 so far; 70 more takes 17.5 s. Stop just after it fills up.
            _stats.Simulate(18f);
            Assert.AreEqual(100f, _stats.Energy, 0.1f);
            Assert.IsFalse(_stats.IsSleeping, "Pet should wake up when fully rested.");
        }

        [Test]
        public void StarvingTooLong_MakesPetSick()
        {
            bool sick = false;
            _stats.BecameSick += () => sick = true;
            _stats.SetValues(0, 80, 80, 80, 10, 100, false, 0);
            _stats.Simulate(_cfg.starvingSecondsUntilSick - 1f);
            Assert.IsFalse(_stats.IsSick);
            _stats.Simulate(2f);
            Assert.IsTrue(_stats.IsSick);
            Assert.IsTrue(sick);
        }

        [Test]
        public void HealthDrainsWhenEmpty_AndZeroHealthMeansSick()
        {
            _cfg.starvingSecondsUntilSick = 9999f; // isolate the health rule
            _stats.SetValues(80, 0, 80, 80, 10, 10, false, 0);
            _stats.Simulate(30f);
            Assert.Less(_stats.Health, 10f);
            _stats.Simulate(600f);
            Assert.AreEqual(0f, _stats.Health);
            Assert.IsTrue(_stats.IsSick);
        }

        [Test]
        public void SickPetIgnoresActions_UntilRestart()
        {
            _stats.SetValues(80, 80, 80, 80, 10, 0, false, 0);
            Assert.IsTrue(_stats.IsSick);
            Assert.IsFalse(_stats.Feed());

            _stats.ResetToStart();
            Assert.IsFalse(_stats.IsSick);
            Assert.IsTrue(_stats.Feed());
        }

        [Test]
        public void LongOfflineGap_IsSimulatedAccurately()
        {
            _cfg.starvingSecondsUntilSick = 99999f;
            _cfg.healthDrainPerEmptyStat = 0f;
            _stats.SetValues(100, 100, 100, 100, 100, 100, false, 0);
            _stats.Simulate(3600f); // one hour
            Assert.AreEqual(0f, _stats.Hunger);
            Assert.AreEqual(0f, _stats.Thirst);
        }
    }
}
