using Tamagotchi.Pet;
using Tamagotchi.UI;
using UnityEngine;

namespace Tamagotchi
{
    /// <summary>
    /// Connects the systems: UI buttons -> pet stats, and pet stats -> UI.
    /// Each system only knows its own job; this is the one place that wires them together.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private PetStats stats;
        [SerializeField] private UIManager ui;
        [Tooltip("Name used in the mood messages.")]
        [SerializeField] private string petName = "Hammy";

        private string PetName => petName;

        private void OnEnable()
        {
            ui.ActionPressed += OnActionPressed;
            ui.RestartPressed += OnRestartPressed;
            stats.Changed += RefreshBars;
            stats.HangryChanged += OnHangryChanged;
            stats.SleepChanged += OnSleepChanged;
            stats.BecameSick += OnBecameSick;
        }

        private void OnDisable()
        {
            ui.ActionPressed -= OnActionPressed;
            ui.RestartPressed -= OnRestartPressed;
            stats.Changed -= RefreshBars;
            stats.HangryChanged -= OnHangryChanged;
            stats.SleepChanged -= OnSleepChanged;
            stats.BecameSick -= OnBecameSick;
        }

        private void Start()
        {
            ui.ShowGameOver(false);
            RefreshBars();
            ui.SetHangry(stats.IsHangry);
            if (stats.IsHangry) ui.ShowMood(HangryMessage, sticky: true);
            else ui.ShowMood($"Hi! I'm {PetName}!");
        }

        private string HangryMessage => "I'm HANGRY! Feed me!";

        /// <summary>Speech for an action; while hangry the warning stays attached.</summary>
        private void Say(string message)
        {
            if (stats.IsHangry) ui.ShowMood(message + "\nStill HANGRY!", sticky: true);
            else ui.ShowMood(message);
        }

        // ---------- UI -> stats ----------

        private void OnActionPressed(PetAction action)
        {
            switch (action)
            {
                case PetAction.Feed:
                    stats.Feed();
                    Say("Yum yum!");
                    break;
                case PetAction.Drink:
                    stats.Drink();
                    Say("Gulp gulp!");
                    break;
                case PetAction.Study:
                    Say(stats.Study()
                        ? "Studying hard!"
                        : "Too tired to study...");
                    break;
                case PetAction.Play:
                    Say(stats.Play()
                        ? "Wheee! Fun!"
                        : "Too tired to play...");
                    break;
                case PetAction.Sleep:
                    stats.SetSleeping(!stats.IsSleeping);
                    break;
            }
        }

        private void OnRestartPressed()
        {
            stats.ResetToStart();
            ui.ShowGameOver(false);
            ui.SetActionsInteractable(true);
            ui.SetHangry(stats.IsHangry);
            ui.ShowMood("I feel great again!");
        }

        // ---------- stats -> UI ----------

        private void RefreshBars()
        {
            ui.HungerBar.SetValue(stats.Hunger);
            ui.ThirstBar.SetValue(stats.Thirst);
            ui.HappinessBar.SetValue(stats.Happiness);
            ui.EnergyBar.SetValue(stats.Energy);
            ui.IntelligenceBar.SetValue(stats.Intelligence);
            ui.HealthBar.SetValue(stats.Health);
        }

        private void OnHangryChanged(bool hangry)
        {
            ui.SetHangry(hangry);
            if (hangry) ui.ShowMood(HangryMessage, sticky: true);
            else ui.ShowMood("Full again, thanks!");
        }

        private void OnSleepChanged(bool sleeping)
        {
            ui.ShowMood(sleeping ? "Zzz..." : "I'm awake!", sticky: sleeping);
        }

        private void OnBecameSick()
        {
            ui.SetHangry(false);
            ui.SetActionsInteractable(false);
            ui.ShowMood("I feel sick...");
            ui.ShowGameOver(true);
        }
    }
}
