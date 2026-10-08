using System;
using System.Collections;
using NUnit.Framework;
using Tamagotchi.Pet;
using Tamagotchi.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tamagotchi.Tests
{
    /// <summary>The pet's state machine and emotes, in the real Main scene.</summary>
    public class PetControllerTests
    {
        private UIManager _ui;
        private PetStats _stats;
        private PetController _pet;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _ui = Object.FindAnyObjectByType<UIManager>();
            _stats = Object.FindAnyObjectByType<PetStats>();
            _pet = Object.FindAnyObjectByType<PetController>();
            Assert.IsNotNull(_pet, "Main scene should have a PetController.");
            _stats.SetValues(80, 80, 80, 10, 100, false, 0); // healthy, idle
            yield return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        private void Press(PetAction action) => _ui.GetButton(action).onClick.Invoke();

        [Test]
        public void EveryStateHasAnimationFrames()
        {
            foreach (PetState state in Enum.GetValues(typeof(PetState)))
                Assert.IsNotEmpty(_pet.FramesFor(state), state + " has no frames");
        }

        [Test]
        public void StartsIdle_WhenHealthy()
        {
            Assert.AreEqual(PetState.Idle, _pet.State);
        }

        [UnityTest]
        public IEnumerator Feed_ShowsEating_ThenReturnsToIdle()
        {
            Press(PetAction.Feed);
            yield return null;
            Assert.AreEqual(PetState.Eating, _pet.State);
            yield return Wait(2.3f);
            Assert.AreEqual(PetState.Idle, _pet.State);
        }

        [UnityTest]
        public IEnumerator Play_ShowsPlaying_ThenHappy_ThenIdle()
        {
            Press(PetAction.Play);
            yield return null;
            Assert.AreEqual(PetState.Playing, _pet.State);
            yield return Wait(2.2f);
            Assert.AreEqual(PetState.Happy, _pet.State);
            yield return Wait(1.4f);
            Assert.AreEqual(PetState.Idle, _pet.State);
        }

        [UnityTest]
        public IEnumerator Study_ShowsStudying_OrSadWhenTooTired()
        {
            Press(PetAction.Study);
            yield return null;
            Assert.AreEqual(PetState.Studying, _pet.State);

            _stats.SetValues(80, 80, 1, 10, 100, false, 0);
            Press(PetAction.Study);
            yield return null;
            Assert.AreEqual(PetState.Sad, _pet.State, "Refusing (too tired) should look sad.");
        }

        [UnityTest]
        public IEnumerator Scold_ShowsSadReaction()
        {
            Press(PetAction.Scold);
            yield return null;
            Assert.AreEqual(PetState.Sad, _pet.State);
            Assert.AreEqual("Sniff... I'm sorry!", _ui.SpeechBubble.Message);
        }

        [UnityTest]
        public IEnumerator Sleep_DimsScene_AndShowsNightBackground_UntilWake()
        {
            Sprite day = _ui.Backgrounds.Current;
            Press(PetAction.Sleep);
            yield return Wait(0.5f);
            Assert.AreEqual(PetState.Sleeping, _pet.State);
            Assert.AreEqual("moonlit_bedroom", _ui.Backgrounds.Current.name);
            var dim = GameObject.Find("DimOverlay").GetComponent<UnityEngine.UI.Image>();
            Assert.Greater(dim.color.a, 0.1f, "Scene should be dimmed while sleeping.");

            Press(PetAction.Sleep); // wake up
            yield return null;
            Assert.AreNotEqual(PetState.Sleeping, _pet.State);
            Assert.AreEqual(day, _ui.Backgrounds.Current, "Day background should come back.");
        }

        [UnityTest]
        public IEnumerator Bed_OnlyWhileSleeping_Bowl_OnlyWhileEating()
        {
            var bed = GameObject.Find("PetSpot").transform.Find("Bed").gameObject;
            var bowl = GameObject.Find("PetSpot").transform.Find("FoodBowl").gameObject;
            Assert.IsFalse(bed.activeSelf);
            Assert.IsFalse(bowl.activeSelf);

            Press(PetAction.Feed);
            yield return null;
            Assert.IsTrue(bowl.activeSelf, "Food bowl should appear while eating.");
            Assert.IsFalse(bed.activeSelf);

            Press(PetAction.Sleep);
            yield return null;
            Assert.IsTrue(bed.activeSelf, "Bed should appear while sleeping.");
            Assert.IsFalse(bowl.activeSelf);

            Press(PetAction.Sleep); // wake
            yield return null;
            Assert.IsFalse(bed.activeSelf);
        }

        [UnityTest]
        public IEnumerator BaseStates_FollowTheStats()
        {
            _stats.SetValues(10, 80, 80, 10, 100, false, 0);
            yield return null;
            Assert.AreEqual(PetState.Hangry, _pet.State);

            _stats.SetValues(80, 80, 80, 10, 10, false, 0);
            yield return null;
            Assert.AreEqual(PetState.Crying, _pet.State, "Low health should make the pet cry.");

            _stats.SetValues(80, 10, 80, 10, 100, false, 0);
            yield return null;
            Assert.AreEqual(PetState.Sad, _pet.State, "Low happiness should make the pet sad.");

            _stats.SetValues(0, 50, 50, 10, 0, false, 0);
            yield return null;
            Assert.AreEqual(PetState.Sick, _pet.State);
        }

        [UnityTest]
        public IEnumerator StateChanged_IsRaised_ForAudio()
        {
            PetState? last = null;
            _pet.StateChanged += s => last = s;
            Press(PetAction.Feed);
            yield return null;
            Assert.AreEqual(PetState.Eating, last);
        }
    }
}
