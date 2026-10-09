using System.Collections;
using NUnit.Framework;
using Tamagotchi.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tamagotchi.Tests
{
    public class UILayoutTests
    {
        private UIManager _ui;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            TestSave.Isolate();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null; // let Start() run
            _ui = Object.FindAnyObjectByType<UIManager>();
            Assert.IsNotNull(_ui, "Main scene should have a UIManager.");
        }

        [Test]
        public void AllStatBarsAreWired()
        {
            Assert.IsNotNull(_ui.HungerBar);
            Assert.IsNotNull(_ui.HappinessBar);
            Assert.IsNotNull(_ui.EnergyBar);
            Assert.IsNotNull(_ui.IntelligenceBar);
            Assert.IsNotNull(_ui.HealthBar);
        }

        [Test]
        public void PortraitFrame_KeepsUIPhoneShaped()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<PortraitFrame>(), "UI should sit in a PortraitFrame.");
            // Wide window: column limited to 9:16 of the height.
            Assert.AreEqual(1080f, PortraitFrame.FrameWidth(3413f, 1920f, 9f / 16f), 0.5f);
            // Phone (portrait or taller): full width.
            Assert.AreEqual(1080f, PortraitFrame.FrameWidth(1080f, 2340f, 9f / 16f), 0.5f);
        }

        [Test]
        public void HungerBar_IsRedWhenLow_GreenWhenFull()
        {
            _ui.HungerBar.SetValue(5);
            Color low = _ui.HungerBar.Fill.color;
            Assert.Greater(low.r, low.g, "Low hunger should be red.");

            _ui.HungerBar.SetValue(100);
            Color full = _ui.HungerBar.Fill.color;
            Assert.Greater(full.g, full.r, "Full hunger should be green.");
        }

        [Test]
        public void SceneButton_CyclesThroughAllBackgrounds()
        {
            var switcher = _ui.Backgrounds;
            Assert.AreEqual(6, switcher.Count);
            Sprite first = switcher.Current;
            Assert.AreEqual("cozy_home", first.name, "Default background should be cozy_home.");

            _ui.SceneButton.onClick.Invoke();
            Assert.AreNotEqual(first, switcher.Current);

            for (int i = 1; i < switcher.Count; i++) _ui.SceneButton.onClick.Invoke();
            Assert.AreEqual(first, switcher.Current, "Should wrap back to the first background.");
        }

        [Test]
        public void ActionButtons_RaiseActionPressed()
        {
            PetAction? received = null;
            _ui.ActionPressed += a => received = a;
            foreach (PetAction action in System.Enum.GetValues(typeof(PetAction)))
            {
                Button b = _ui.GetButton(action);
                Assert.IsNotNull(b, action + " button missing");
                b.onClick.Invoke();
                Assert.AreEqual(action, received);
            }
        }
    }
}
