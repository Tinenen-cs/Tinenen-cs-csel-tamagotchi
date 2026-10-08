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
            ui.ShowMood(stats.IsHangry ? HangryMessage : $"{PetName} is happy to see you!");
        }

        private string HangryMessage => $"{PetName} is HANGRY! Feed me!";

        // ---------- UI -> stats ----------

        private void OnActionPressed(PetAction action)
        {
            switch (action)
            {
                case PetAction.Feed:
                    stats.Feed();
                    ui.ShowMood($"Yum! {PetName} is eating.");
                    break;
                case PetAction.Drink:
                    stats.Drink();
                    ui.ShowMood($"Gulp gulp! {PetName} is drinking.");
                    break;
                case PetAction.Study:
                    ui.ShowMood(stats.Study()
                        ? $"{PetName} is studying hard."
                        : $"{PetName} is too tired to study...");
                    break;
                case PetAction.Play:
                    ui.ShowMood(stats.Play()
                        ? $"{PetName} is playing! Wheee!"
                        : $"{PetName} is too tired to play...");
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
            ui.ShowMood($"{PetName} is back and feeling great!");
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
            ui.ShowMood(hangry ? HangryMessage : $"{PetName} feels full again.");
        }

        private void OnSleepChanged(bool sleeping)
        {
            ui.ShowMood(sleeping ? $"{PetName} is sleeping... Zzz" : $"{PetName} woke up!");
        }

        private void OnBecameSick()
        {
            ui.SetHangry(false);
            ui.SetActionsInteractable(false);
            ui.ShowMood($"Oh no! {PetName} got sick...");
            ui.ShowGameOver(true);
        }
    }
}
