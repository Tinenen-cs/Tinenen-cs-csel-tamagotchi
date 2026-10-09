using System.Collections;
using NUnit.Framework;
using Tamagotchi.Pet;
using Tamagotchi.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tamagotchi.Tests
{
    /// <summary>End-to-end checks in the real Main scene: buttons -> stats -> UI.</summary>
    public class GameFlowTests
    {
        private UIManager _ui;
        private PetStats _stats;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            TestSave.Isolate();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _ui = Object.FindAnyObjectByType<UIManager>();
            _stats = Object.FindAnyObjectByType<PetStats>();
            Assert.IsNotNull(_stats, "Main scene should have PetStats.");
        }

        [UnityTest]
        public IEnumerator FeedButton_FillsHungerBar()
        {
            _stats.SetValues(40, 80, 80, 10, 100, false, 0);
            _ui.GetButton(PetAction.Feed).onClick.Invoke();
            yield return null;
            Assert.Greater(_stats.Hunger, 60f);
            Assert.AreEqual(_stats.Hunger, _ui.HungerBar.Value, 0.5f);
        }

        [UnityTest]
        public IEnumerator LowHunger_FlashesHungerBar()
        {
            _stats.SetValues(10, 80, 80, 10, 100, false, 0);
            _stats.Simulate(0.1f);
            yield return null;
            Assert.IsTrue(_ui.HungerBar.IsAlarmOn, "Hunger bar should flash when hangry.");

            _ui.GetButton(PetAction.Feed).onClick.Invoke();
            _ui.GetButton(PetAction.Feed).onClick.Invoke();
            yield return null;
            Assert.IsFalse(_ui.HungerBar.IsAlarmOn);
        }

        [UnityTest]
        public IEnumerator Feed_ShowsSpeechBubbleAbovePet()
        {
            _stats.SetValues(60, 80, 80, 10, 100, false, 0);
            _ui.GetButton(PetAction.Feed).onClick.Invoke();
            yield return null;
            Assert.IsTrue(_ui.SpeechBubble.IsVisible);
            Assert.AreEqual("Yum yum!", _ui.SpeechBubble.Message);
        }

        [UnityTest]
        public IEnumerator Hangry_WarningStaysInBubble_EvenAfterOtherActions()
        {
            _stats.SetValues(10, 80, 80, 10, 100, false, 0);
            _stats.Simulate(0.1f);
            _ui.GetButton(PetAction.Study).onClick.Invoke();
            yield return null;
            StringAssert.Contains("HANGRY", _ui.SpeechBubble.Message);
        }

        [UnityTest]
        public IEnumerator Sick_ShowsGameOver_RestartRecovers()
        {
            Assert.IsFalse(_ui.IsGameOverShown);
            _stats.SetValues(0, 50, 50, 10, 0, false, 0); // health 0 -> sick
            yield return null;
            Assert.IsTrue(_stats.IsSick);
            Assert.IsTrue(_ui.IsGameOverShown);
            Assert.IsFalse(_ui.GetButton(PetAction.Feed).interactable);

            _ui.RestartButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(_stats.IsSick);
            Assert.IsFalse(_ui.IsGameOverShown);
            Assert.IsTrue(_ui.GetButton(PetAction.Feed).interactable);
            Assert.AreEqual(_stats.Config.startHunger, _ui.HungerBar.Value, 0.5f);
        }
    }
}
