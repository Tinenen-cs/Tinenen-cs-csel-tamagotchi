using System;
using Tamagotchi.World;
using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi.UI
{
    /// <summary>Actions the player can trigger from the bottom button bar.</summary>
    public enum PetAction { Feed, Scold, Study, Sleep, Play }

    /// <summary>
    /// Owns all on-screen UI: stat bars, action buttons, mute button and the speech bubble.
    /// Gameplay code listens to <see cref="ActionPressed"/> / <see cref="MutePressed"/>
    /// and pushes values back with the Show* methods; it never touches UI objects directly.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Stat bars")]
        [SerializeField] private StatBar hungerBar;
        [SerializeField] private StatBar happinessBar;
        [SerializeField] private StatBar staminaBar;
        [SerializeField] private StatBar intelligenceBar;
        [SerializeField] private StatBar healthBar;

        [Header("Action buttons")]
        [SerializeField] private Button feedButton;
        [SerializeField] private Button scoldButton;
        [SerializeField] private Button studyButton;
        [SerializeField] private Button sleepButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button sceneButton;

        [Header("Other")]
        [SerializeField] private Button muteButton;
        [Tooltip("Invisible button on the hamster: tapping it pets the pet.")]
        [SerializeField] private Button petButton;
        [SerializeField] private Image muteIcon;
        [Tooltip("Speech bubble above the pet's head.")]
        [SerializeField] private SpeechBubble speechBubble;
        [SerializeField] private BackgroundSwitcher backgrounds;

        [Header("Game over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;

        /// <summary>Raised when one of the pet action buttons is tapped.</summary>
        public event Action<PetAction> ActionPressed;

        /// <summary>Raised when the player taps the hamster.</summary>
        public event Action PetTapped;

        /// <summary>Raised when the mute button is tapped.</summary>
        public event Action MutePressed;

        /// <summary>Raised when Restart is tapped on the game-over screen.</summary>
        public event Action RestartPressed;

        /// <summary>Raised for every button tap (used for the generic click sound).</summary>
        public event Action AnyButtonPressed;

        public StatBar HungerBar => hungerBar;
        public StatBar HappinessBar => happinessBar;
        public StatBar StaminaBar => staminaBar;
        public StatBar IntelligenceBar => intelligenceBar;
        public StatBar HealthBar => healthBar;
        public BackgroundSwitcher Backgrounds => backgrounds;

        private void Awake()
        {
            Hook(feedButton, () => ActionPressed?.Invoke(PetAction.Feed));
            Hook(scoldButton, () => ActionPressed?.Invoke(PetAction.Scold));
            Hook(studyButton, () => ActionPressed?.Invoke(PetAction.Study));
            Hook(sleepButton, () => ActionPressed?.Invoke(PetAction.Sleep));
            Hook(playButton, () => ActionPressed?.Invoke(PetAction.Play));
            Hook(sceneButton, () => backgrounds.Next());
            Hook(muteButton, () => MutePressed?.Invoke());
            Hook(restartButton, () => RestartPressed?.Invoke());
            // Petting has its own sound/reaction, so it does not play the button click.
            if (petButton != null) petButton.onClick.AddListener(() => PetTapped?.Invoke());
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
            PetAction.Scold => scoldButton,
            PetAction.Study => studyButton,
            PetAction.Sleep => sleepButton,
            _ => playButton,
        };

        public Button SceneButton => sceneButton;
        public Button MuteButton => muteButton;
        public Button PetButton => petButton;
        public Button RestartButton => restartButton;
        public bool IsGameOverShown => gameOverPanel != null && gameOverPanel.activeSelf;

        /// <summary>Enables or disables all pet action buttons (e.g. while sick).</summary>
        public void SetActionsInteractable(bool interactable)
        {
            foreach (PetAction a in Enum.GetValues(typeof(PetAction)))
                SetUnlocked(a, interactable);
        }

        /// <summary>
        /// Locks or unlocks one action button. A locked button can't be pressed and is faded out,
        /// so the lock is clearly visible (e.g. Play/Study when stamina is below 20).
        /// </summary>
        public void SetUnlocked(PetAction action, bool unlocked)
        {
            Button b = GetButton(action);
            if (b == null) return;
            b.interactable = unlocked;
            var group = b.GetComponent<CanvasGroup>();
            if (group == null) group = b.gameObject.AddComponent<CanvasGroup>();
            group.alpha = unlocked ? 1f : 0.4f;
        }

        /// <summary>True if the action's button can be pressed right now.</summary>
        public bool IsUnlocked(PetAction action) => GetButton(action).interactable;

        /// <summary>
        /// Shows a message in the speech bubble above the pet. Normal messages fade after a few
        /// seconds; sticky ones (warnings like HANGRY) stay until replaced.
        /// </summary>
        public void ShowMood(string message, bool sticky = false)
        {
            if (speechBubble != null) speechBubble.Show(message, sticky);
        }

        public SpeechBubble SpeechBubble => speechBubble;

        /// <summary>Hides the speech bubble.</summary>
        public void HideMood()
        {
            if (speechBubble != null) speechBubble.Hide();
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
