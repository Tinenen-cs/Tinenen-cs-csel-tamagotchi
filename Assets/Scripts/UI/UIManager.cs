using System;
using Tamagotchi.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi.UI
{
    /// <summary>Actions the player can trigger from the bottom button bar.</summary>
    public enum PetAction { Feed, Drink, Study, Sleep, Play }

    /// <summary>
    /// Owns all on-screen UI: stat bars, action buttons, mute button and mood text.
    /// Gameplay code listens to <see cref="ActionPressed"/> / <see cref="MutePressed"/>
    /// and pushes values back with the Show* methods; it never touches UI objects directly.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Stat bars")]
        [SerializeField] private StatBar hungerBar;
        [SerializeField] private StatBar thirstBar;
        [SerializeField] private StatBar happinessBar;
        [SerializeField] private StatBar energyBar;
        [SerializeField] private StatBar intelligenceBar;
        [SerializeField] private StatBar healthBar;

        [Header("Action buttons")]
        [SerializeField] private Button feedButton;
        [SerializeField] private Button drinkButton;
        [SerializeField] private Button studyButton;
        [SerializeField] private Button sleepButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button sceneButton;

        [Header("Other")]
        [SerializeField] private Button muteButton;
        [SerializeField] private Image muteIcon;
        [SerializeField] private TMP_Text moodText;
        [SerializeField] private BackgroundSwitcher backgrounds;

        [Header("Game over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;

        /// <summary>Raised when one of the pet action buttons is tapped.</summary>
        public event Action<PetAction> ActionPressed;

        /// <summary>Raised when the mute button is tapped.</summary>
        public event Action MutePressed;

        /// <summary>Raised when Restart is tapped on the game-over screen.</summary>
        public event Action RestartPressed;

        /// <summary>Raised for every button tap (used for the generic click sound).</summary>
        public event Action AnyButtonPressed;

        public StatBar HungerBar => hungerBar;
        public StatBar ThirstBar => thirstBar;
        public StatBar HappinessBar => happinessBar;
        public StatBar EnergyBar => energyBar;
        public StatBar IntelligenceBar => intelligenceBar;
        public StatBar HealthBar => healthBar;
        public BackgroundSwitcher Backgrounds => backgrounds;

        private void Awake()
        {
            Hook(feedButton, () => ActionPressed?.Invoke(PetAction.Feed));
            Hook(drinkButton, () => ActionPressed?.Invoke(PetAction.Drink));
            Hook(studyButton, () => ActionPressed?.Invoke(PetAction.Study));
            Hook(sleepButton, () => ActionPressed?.Invoke(PetAction.Sleep));
            Hook(playButton, () => ActionPressed?.Invoke(PetAction.Play));
            Hook(sceneButton, () => backgrounds.Next());
            Hook(muteButton, () => MutePressed?.Invoke());
            Hook(restartButton, () => RestartPressed?.Invoke());
        }

        private void Hook(Button button, Action onClick)
        {
            if (button == null) return;
            button.onClick.AddListener(() =>
            {
                AnyButtonPressed?.Invoke();
                onClick();
            });
        }

        /// <summary>Returns the button for an action (e.g. to simulate taps in tests).</summary>
        public Button GetButton(PetAction action) => action switch
        {
            PetAction.Feed => feedButton,
            PetAction.Drink => drinkButton,
            PetAction.Study => studyButton,
            PetAction.Sleep => sleepButton,
            _ => playButton,
        };

        public Button SceneButton => sceneButton;
        public Button MuteButton => muteButton;
        public Button RestartButton => restartButton;
        public bool IsGameOverShown => gameOverPanel != null && gameOverPanel.activeSelf;

        /// <summary>Enables or disables all pet action buttons (e.g. while sick).</summary>
        public void SetActionsInteractable(bool interactable)
        {
            foreach (PetAction a in Enum.GetValues(typeof(PetAction)))
                GetButton(a).interactable = interactable;
        }

        /// <summary>Shows the one-line mood / status message under the scene.</summary>
        public void ShowMood(string message)
        {
            if (moodText != null) moodText.text = message;
        }

        /// <summary>Hangry warning: the hunger bar flashes red and pulses.</summary>
        public void SetHangry(bool hangry) => hungerBar.SetAlarm(hangry);

        /// <summary>Shows or hides the "your pet got sick" screen with the Restart button.</summary>
        public void ShowGameOver(bool show)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(show);
        }

        /// <summary>Dims the mute icon when audio is muted.</summary>
        public void ShowMuted(bool muted)
        {
            if (muteIcon != null) muteIcon.color = muted ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
        }
    }
}
