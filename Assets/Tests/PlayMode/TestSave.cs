using System;

namespace Tamagotchi.Tests
{
    /// <summary>Gives tests their own empty save slot and the real clock, so they never touch a player's save.</summary>
    public static class TestSave
    {
        public static void Isolate()
        {
            SaveSystem.KeyPrefix = "TamagotchiTest.";
            SaveSystem.UtcNow = () => DateTime.UtcNow;
            SaveSystem.ClearSave();
        }
    }
}
