using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi.Pet
{
    /// <summary>
    /// Flip-book animation for a UI Image: cycles through sprite frames at a fixed rate.
    /// Used for the pet's emotes and the crying pet on the game-over card.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [Tooltip("Frames per second.")]
        [SerializeField] private float fps = 6f;

        private Image _image;
        private float _time;

        public Sprite[] Frames => frames;
        public Sprite CurrentFrame => _image != null ? _image.sprite : null;

        private void Awake()
        {
            _image = GetComponent<Image>();
            ShowFrame(0);
        }

        /// <summary>Starts looping a new set of frames from the first one.</summary>
        public void Play(Sprite[] newFrames, float framesPerSecond)
        {
            frames = newFrames ?? new Sprite[0];
            fps = framesPerSecond;
            _time = 0f;
            ShowFrame(0);
        }

        private void Update()
        {
            if (frames.Length <= 1 || fps <= 0f) return;
            _time += Time.deltaTime;
            ShowFrame((int)(_time * fps) % frames.Length);
        }

        private void ShowFrame(int index)
        {
            if (_image == null) _image = GetComponent<Image>();
            if (frames.Length > 0 && frames[index] != null) _image.sprite = frames[index];
        }
    }
}
