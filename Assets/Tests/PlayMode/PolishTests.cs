using System.Collections;
using NUnit.Framework;
using Tamagotchi.Pet;
using Tamagotchi.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tamagotchi.Tests
{
    /// <summary>Feel and feedback: bar pops, scene crossfade, petting, game-over pop-in.</summary>
    public class PolishTests
    {
        private UIManager _ui;
        private PetStats _stats;
        private PetController _pet;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            TestSave.Isolate();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _ui = Object.FindAnyObjectByType<UIManager>();
            _stats = Object.FindAnyObjectByType<PetStats>();
            _pet = Object.FindAnyObjectByType<PetController>();
            _stats.SetValues(50, 50, 80, 10, 100, false, 0);
            yield return new WaitForSeconds(0.4f); // let the bumps from SetValues settle
        }

        private static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator Feed_MakesHungerBarPop()
        {
            Assert.IsFalse(_ui.HungerBar.IsBumping);
            _ui.GetButton(PetAction.Feed).onClick.Invoke();
            yield return null;
            Assert.IsTrue(_ui.HungerBar.IsBumping, "The bar an action changes should pop.");
            Assert.IsFalse(_ui.IntelligenceBar.IsBumping, "Unchanged bars should stay still.");
        }

        [UnityTest]
        public IEnumerator SceneChange_Crossfades()
        {
            _ui.SceneButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(_ui.Backgrounds.IsFading);
            yield return Wait(0.6f);
            Assert.IsFalse(_ui.Backgrounds.IsFading);
        }

        [UnityTest]
        public IEnumerator TappingThePet_PetsIt_WithCooldown()
        {
            float before = _stats.Happiness;
            _ui.PetButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(before + _stats.Config.petHappiness, _stats.Happiness, 0.2f);
            Assert.AreEqual(PetState.Happy, _pet.State);
            Assert.AreEqual("Hehe, that tickles!", _ui.SpeechBubble.Message);

            float afterFirst = _stats.Happiness;
            _ui.PetButton.onClick.Invoke(); // within the cooldown: ignored
            yield return null;
            Assert.AreEqual(afterFirst, _stats.Happiness, 0.2f);
        }

        [UnityTest]
        public IEnumerator Petting_IsIgnoredWhileAsleep()
        {
            _stats.SetSleeping(true);
            yield return null;
            float before = _stats.Happiness;
            _ui.PetButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(_stats.IsSleeping, "Petting should not wake the pet.");
            Assert.AreEqual(before, _stats.Happiness, 0.2f);
        }

        [UnityTest]
        public IEnumerator GameOverCard_PopsIn()
        {
            _stats.SetValues(0, 50, 50, 10, 0, false, 0); // sick
            yield return null;
            var pop = Object.FindAnyObjectByType<PopIn>();
            Assert.IsNotNull(pop);
            Assert.IsTrue(pop.IsAnimating);
            yield return Wait(0.5f);
            Assert.IsFalse(pop.IsAnimating);
            Assert.AreEqual(1f, pop.GetComponent<CanvasGroup>().alpha, 0.01f);
        }
    }
}
