using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tamagotchi.Tests
{
    /// <summary>
    /// Loads Main.unity and lets it run. Any Debug.LogError or exception
    /// during that time fails the test (Unity Test Framework default).
    /// </summary>
    public class MainSceneSmokeTest
    {
        [UnityTest]
        public IEnumerator MainScene_RunsWithoutErrors()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            Assert.AreEqual("Main", SceneManager.GetActiveScene().name);

            // Run for ~2 seconds of game time.
            float end = Time.time + 2f;
            while (Time.time < end) yield return null;

            Assert.IsNotNull(Object.FindAnyObjectByType<Canvas>(), "Main scene should contain a Canvas.");
        }
    }
}
