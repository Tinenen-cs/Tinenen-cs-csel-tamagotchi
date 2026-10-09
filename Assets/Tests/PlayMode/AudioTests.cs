using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Tamagotchi.Audio;
using Tamagotchi.Pet;
using Tamagotchi.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tamagotchi.Tests
{
    /// <summary>Every button and every emote makes a sound; mute works and is remembered.</summary>
    public class AudioTests
    {
        private UIManager _ui;
        private PetStats _stats;
        private AudioManager _audio;
        private readonly List<string> _played = new List<string>();
        private int _savedMute;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            _savedMute = PlayerPrefs.GetInt("Tamagotchi.Muted", 0);
            PlayerPrefs.SetInt("Tamagotchi.Muted", 0);
            TestSave.Isolate();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _ui = Object.FindAnyObjectByType<UIManager>();
            _stats = Object.FindAnyObjectByType<PetStats>();
            _audio = AudioManager.Instance;
            Assert.IsNotNull(_audio, "Main scene should have an AudioManager.");
            _stats.SetValues(80, 80, 80, 10, 100, false, 0);
            yield return null;
            _played.Clear();
            _audio.SoundPlayed += _played.Add;
        }

        [TearDown]
        public void RestoreMute()
        {
            if (_audio != null) _audio.SoundPlayed -= _played.Add;
            PlayerPrefs.SetInt("Tamagotchi.Muted", _savedMute);
        }

        [Test]
        public void SceneHasExactlyOneAudioListener()
        {
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length,
                "Without an AudioListener nothing is audible.");
        }

        [Test]
        public void EverySoundHasAClip()
        {
            foreach (var name in new[] { "click", "mute", "scene", "scold", "eat", "study", "sleep", "play",
                                         "happy", "sad", "cry", "hangry", "sick", "gameover", "music" })
                Assert.IsTrue(_audio.Has(name), name + " has no clip");
        }

        [UnityTest]
        public IEnumerator EachButton_PlaysClick_AndItsOwnSound()
        {
            var expected = new Dictionary<PetAction, string>
            {
                { PetAction.Feed, "eat" }, { PetAction.Study, "study" }, { PetAction.Play, "play" },
                { PetAction.Scold, "scold" }, { PetAction.Sleep, "sleep" },
            };
            foreach (var pair in expected)
            {
                _stats.SetValues(80, 80, 80, 10, 100, false, 0); // awake, rested, idle
                yield return null;
                _played.Clear();
                _ui.GetButton(pair.Key).onClick.Invoke();
                yield return null;
                CollectionAssert.Contains(_played, "click", pair.Key + " should click");
                CollectionAssert.Contains(_played, pair.Value, pair.Key + " should play " + pair.Value);
            }
        }

        [UnityTest]
        public IEnumerator SceneButton_PlaysSceneSound()
        {
            _ui.SceneButton.onClick.Invoke();
            yield return null;
            CollectionAssert.Contains(_played, "scene");
        }

        [UnityTest]
        public IEnumerator Scold_AlsoPlaysSadEmote()
        {
            _ui.GetButton(PetAction.Scold).onClick.Invoke();
            yield return null;
            CollectionAssert.Contains(_played, "sad");
        }

        [UnityTest]
        public IEnumerator Hangry_PlaysAndRepeats()
        {
            _stats.SetValues(10, 80, 80, 10, 100, false, 0);
            yield return null;
            CollectionAssert.Contains(_played, "hangry");
            int first = _played.FindAll(s => s == "hangry").Count;
            float end = Time.time + 4.5f;
            while (Time.time < end) yield return null;
            Assert.Greater(_played.FindAll(s => s == "hangry").Count, first, "Hangry sound should repeat.");
        }

        [UnityTest]
        public IEnumerator Sick_PlaysSick_ThenGameOver()
        {
            _stats.SetValues(0, 50, 50, 10, 0, false, 0);
            yield return null;
            CollectionAssert.Contains(_played, "sick");
            float end = Time.time + 1.6f;
            while (Time.time < end) yield return null;
            CollectionAssert.Contains(_played, "gameover");
        }

        [UnityTest]
        public IEnumerator MuteButton_TogglesMute_AndRemembersIt()
        {
            Assert.IsFalse(_audio.IsMuted);
            _ui.MuteButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(_audio.IsMuted);
            Assert.AreEqual(1, PlayerPrefs.GetInt("Tamagotchi.Muted"));

            _ui.MuteButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(_audio.IsMuted);
            Assert.AreEqual(0, PlayerPrefs.GetInt("Tamagotchi.Muted"));
        }
    }
}
