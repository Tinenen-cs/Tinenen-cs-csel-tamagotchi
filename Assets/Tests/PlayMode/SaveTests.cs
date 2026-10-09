using System;
using System.Collections;
using NUnit.Framework;
using Tamagotchi.Pet;
using Tamagotchi.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tamagotchi.Tests
{
    /// <summary>Save/load through PlayerPrefs, including time spent away (fake clock).</summary>
    public class SaveTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);

        private PetStats _stats;
        private UIManager _ui;
        private SaveSystem _save;

        [SetUp]
        public void Fresh()
        {
            TestSave.Isolate();
            SaveSystem.UtcNow = () => T0;
        }

        [TearDown]
        public void Cleanup() => TestSave.Isolate();

        private IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _stats = Object.FindAnyObjectByType<PetStats>();
            _ui = Object.FindAnyObjectByType<UIManager>();
            _save = Object.FindAnyObjectByType<SaveSystem>();
            Assert.IsNotNull(_save, "Main scene should have a SaveSystem.");
        }

        [UnityTest]
        public IEnumerator NewGame_StartsFromConfig()
        {
            yield return LoadScene();
            Assert.IsFalse(_save.LoadedFromSave);
            Assert.AreEqual(_stats.Config.startHunger, _stats.Hunger, 0.5f);
        }

        [UnityTest]
        public IEnumerator SaveAndReload_RestoresStatsAndScene()
        {
            yield return LoadScene();
            _stats.SetValues(40, 60, 50, 30, 90, false, 0);
            _ui.SceneButton.onClick.Invoke(); // background index 1
            _save.Save();

            yield return LoadScene();
            Assert.IsTrue(_save.LoadedFromSave);
            Assert.AreEqual(40f, _stats.Hunger, 0.5f);
            Assert.AreEqual(60f, _stats.Happiness, 0.5f);
            Assert.AreEqual(50f, _stats.Stamina, 0.5f);
            Assert.AreEqual(30f, _stats.Intelligence, 0.5f);
            Assert.AreEqual(90f, _stats.Health, 0.5f);
            Assert.AreEqual(1, _ui.Backgrounds.Index, "Chosen scene should be remembered.");
        }

        [UnityTest]
        public IEnumerator TimeAway_AppliesOfflineDecay_AndWelcomesBack()
        {
            yield return LoadScene();
            _stats.SetValues(80, 80, 80, 10, 100, false, 0);
            _save.Save();

            SaveSystem.UtcNow = () => T0.AddHours(1);
            yield return LoadScene();
            var cfg = _stats.Config;
            float expectedHunger = 80f - 60f * cfg.offlineDecayMultiplier * cfg.hungerDecay; // 1 h at offline speed
            Assert.AreEqual(3600f, _save.LastOfflineSeconds, 1f);
            Assert.AreEqual(expectedHunger, _stats.Hunger, 1f);
            StringAssert.Contains("missed you", _ui.SpeechBubble.Message);
        }

        [UnityTest]
        public IEnumerator ClockSetBackwards_CountsAsNoTimeAway()
        {
            yield return LoadScene();
            _stats.SetValues(80, 80, 80, 10, 100, false, 0);
            _save.Save();

            SaveSystem.UtcNow = () => T0.AddHours(-5);
            yield return LoadScene();
            Assert.AreEqual(0f, _save.LastOfflineSeconds);
            Assert.AreEqual(80f, _stats.Hunger, 0.5f);
        }

        [UnityTest]
        public IEnumerator SickPet_IsStillSickAfterReload()
        {
            yield return LoadScene();
            _stats.SetValues(0, 50, 50, 10, 0, false, 0);
            Assert.IsTrue(_stats.IsSick);
            _save.Save();

            yield return LoadScene();
            Assert.IsTrue(_stats.IsSick);
            Assert.IsTrue(_ui.IsGameOverShown, "Game-over card should show for a saved sick pet.");
        }

        [UnityTest]
        public IEnumerator Restart_SavesAFreshPet()
        {
            yield return LoadScene();
            _stats.SetValues(0, 50, 50, 10, 0, false, 0); // sick
            _ui.RestartButton.onClick.Invoke();

            yield return LoadScene();
            Assert.IsFalse(_stats.IsSick);
            Assert.AreEqual(_stats.Config.startHunger, _stats.Hunger, 0.5f);
        }

        [Test]
        public void FormatDuration_IsReadable()
        {
            Assert.AreEqual("45s", SaveSystem.FormatDuration(45));
            Assert.AreEqual("12m", SaveSystem.FormatDuration(12 * 60));
            Assert.AreEqual("2h 5m", SaveSystem.FormatDuration(2 * 3600 + 5 * 60));
            Assert.AreEqual("3d 1h", SaveSystem.FormatDuration(3 * 86400 + 3600));
        }
    }
}
