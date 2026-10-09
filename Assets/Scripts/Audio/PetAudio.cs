using Tamagotchi.Pet;
using Tamagotchi.UI;
using UnityEngine;

namespace Tamagotchi.Audio
{
    /// <summary>
    /// Decides which sound plays when: a click for every button, a unique sound for each
    /// button's action, and a unique sound whenever the pet's emote/state changes.
    /// While the pet is hangry its sound repeats every few seconds.
    /// </summary>
    public class PetAudio : MonoBehaviour
    {
        [SerializeField] private UIManager ui;
        [SerializeField] private PetController pet;
        [SerializeField] private PetStats stats;

        [Tooltip("Seconds between hangry sounds while the pet stays hangry.")]
        [SerializeField] private float hangryRepeatSeconds = 4f;
        [Tooltip("Delay before the game-over jingle, so it follows the sick sound.")]
        [SerializeField] private float gameOverDelay = 1.2f;

        private float _nextHangry;

        private static AudioManager Audio => AudioManager.Instance;

        private void OnEnable()
        {
            ui.AnyButtonPressed += OnAnyButton;
            ui.ActionPressed += OnAction;
            ui.SceneButton.onClick.AddListener(OnSceneButton);
            ui.MutePressed += OnMutePressed;
            pet.StateChanged += OnStateChanged;
            stats.BecameSick += OnBecameSick;
        }

        private void OnDisable()
        {
            ui.AnyButtonPressed -= OnAnyButton;
            ui.ActionPressed -= OnAction;
            ui.SceneButton.onClick.RemoveListener(OnSceneButton);
            ui.MutePressed -= OnMutePressed;
            pet.StateChanged -= OnStateChanged;
            stats.BecameSick -= OnBecameSick;
        }

        private void Start()
        {
            if (Audio != null)
            {
                ui.ShowMuted(Audio.IsMuted);
                Audio.MuteChanged += ui.ShowMuted;
            }
        }

        private void OnDestroy()
        {
            if (Audio != null) Audio.MuteChanged -= ui.ShowMuted;
        }

        private void Update()
        {
            // Hangry: repeat the warning sound while it lasts.
            if (pet.State == PetState.Hangry && Time.time >= _nextHangry)
                PlayHangry();
        }

        private void OnAnyButton() => Play("click");

        private void OnSceneButton() => Play("scene");

        private void OnAction(PetAction action)
        {
            // Button-specific sounds that are not covered by a state change.
            if (action == PetAction.Scold) Play("scold");
        }

        private void OnMutePressed()
        {
            if (Audio == null) return;
            bool unmuting = Audio.IsMuted;
            Audio.ToggleMute();
            if (unmuting) Play("mute"); // confirm that sound is back on
        }

        private void OnStateChanged(PetState state)
        {
            switch (state)
            {
                case PetState.Eating: Play("eat"); break;
                case PetState.Studying: Play("study"); break;
                case PetState.Sleeping: Play("sleep"); break;
                case PetState.Playing: Play("play"); break;
                case PetState.Happy: Play("happy"); break;
                case PetState.Sad: Play("sad"); break;
                case PetState.Crying: Play("cry"); break;
                case PetState.Hangry: PlayHangry(); break;
                case PetState.Sick: Play("sick"); break;
            }
        }

        private void PlayHangry()
        {
            Play("hangry");
            _nextHangry = Time.time + hangryRepeatSeconds;
        }

        private void OnBecameSick() => Invoke(nameof(PlayGameOver), gameOverDelay);

        private void PlayGameOver()
        {
            if (stats.IsSick) Play("gameover");
        }

        private static void Play(string soundName)
        {
            if (Audio != null) Audio.Play(soundName);
        }
    }
}
