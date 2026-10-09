using Tamagotchi.Pet;
using Tamagotchi.UI;
using UnityEngine;

namespace Tamagotchi
{
    /// <summary>
    /// Connects the systems: UI buttons -> pet stats + pet reactions, and pet stats -> UI.
    /// Each system only knows its own job; this is the one place that wires them together.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private PetStats stats;
        [SerializeField] private UIManager ui;
        [SerializeField] private PetController pet;
        [Tooltip("Optional: used for the welcome-back message after time away.")]
        [SerializeField] private SaveSystem save;
        [Tooltip("Name used in the mood messages.")]
        [SerializeField] private string petName = "Hammy";

        [Tooltip("Minimum seconds between pets (taps on the hamster) that count.")]
        [SerializeField] private float petCooldown = 1f;

        private float _nextPet;

        private string PetName => petName;

        private void OnEnable()
        {
            ui.ActionPressed += OnActionPressed;
            ui.RestartPressed += OnRestartPressed;
            ui.PetTapped += OnPetTapped;
            stats.Changed += RefreshBars;
            stats.HangryChanged += OnHangryChanged;
            stats.SleepChanged += OnSleepChanged;
            stats.BecameSick += OnBecameSick;
        }

        private void OnDisable()
        {
            ui.ActionPressed -= OnActionPressed;
            ui.RestartPressed -= OnRestartPressed;
            ui.PetTapped -= OnPetTapped;
            stats.Changed -= RefreshBars;
            stats.HangryChanged -= OnHangryChanged;
            stats.SleepChanged -= OnSleepChanged;
            stats.BecameSick -= OnBecameSick;
        }

        private void Start()
        {
            // Stats may have been loaded from a save (SaveSystem runs first), possibly already sick.
            ui.ShowGameOver(stats.IsSick);
            ui.SetActionsInteractable(!stats.IsSick);
            RefreshBars();
            ui.SetHangry(stats.IsHangry && !stats.IsSick);
            if (stats.IsSick) ui.ShowMood("I feel sick...");
            else if (stats.IsHangry) ui.ShowMood(HangryMessage, sticky: true);
            else if (save != null && save.LastOfflineSeconds >= 60f)
                ui.ShowMood("I missed you! (" + SaveSystem.FormatDuration(save.LastOfflineSeconds) + ")");
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
            bool ok = true;
            switch (action)
            {
                case PetAction.Feed:
                    ok = stats.Feed();
                    Say("Yum yum!");
                    break;
                case PetAction.Scold:
                    ok = stats.Scold();
                    Say("Sniff... I'm sorry!");
                    break;
                case PetAction.Study:
                    ok = stats.Study();
                    Say(ok ? "Studying hard!" : "Too tired to study...");
                    break;
                case PetAction.Play:
                    ok = stats.Play();
                    Say(ok ? "Wheee! Fun!" : "Too tired to play...");
                    break;
                case PetAction.Sleep:
                    stats.SetSleeping(!stats.IsSleeping);
                    break;
            }
            pet.React(action, ok); // animation / emote for this action
        }

        private void OnPetTapped()
        {
            if (Time.time < _nextPet) return;
            _nextPet = Time.time + petCooldown;
            if (!stats.Pet()) return;
            Say("Hehe, that tickles!");
            pet.ReactToPetting();
        }

        private void OnRestartPressed()
        {
            stats.ResetToStart();
            if (save != null) save.Save();
            ui.ShowGameOver(false);
            ui.SetActionsInteractable(true);
            ui.SetHangry(stats.IsHangry);
            ui.ShowMood("I feel great again!");
        }

        // ---------- stats -> UI ----------

        private void RefreshBars()
        {
            ui.HungerBar.SetValue(stats.Hunger);
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
            // The sleeping frames draw their own "Zzz", so the bubble steps aside while asleep.
            if (sleeping) ui.HideMood();
            else ui.ShowMood("I'm awake!");
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
