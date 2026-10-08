using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi.World
{
    /// <summary>
    /// Swaps the scene behind the pet. The Scene button calls <see cref="Next"/>.
    /// Other systems (e.g. sleeping) can show a temporary background with
    /// <see cref="ShowOverride"/> and go back with <see cref="ClearOverride"/>.
    /// </summary>
    public class BackgroundSwitcher : MonoBehaviour
    {
        [SerializeField] private Image target;
        [Tooltip("Optional: keeps the background's aspect ratio (scenes have different shapes).")]
        [SerializeField] private AspectRatioFitter fitter;
        [Tooltip("Backgrounds the Scene button cycles through, in order. Element 0 is the default.")]
        [SerializeField] private Sprite[] backgrounds = Array.Empty<Sprite>();

        private int _index;
        private Sprite _override;
        private float _alignX = 0.5f; // 0 = show the left edge, 0.5 = middle, 1 = right edge

        /// <summary>Raised whenever the chosen background index changes (for saving).</summary>
        public event Action<int> IndexChanged;

        public int Index => _index;
        public int Count => backgrounds.Length;
        public Sprite Current => target != null ? target.sprite : null;

        private void Start() => Refresh();

        /// <summary>Cycles to the next background.</summary>
        public void Next() => SetIndex(_index + 1);

        /// <summary>Selects a background by index (wraps around).</summary>
        public void SetIndex(int index)
        {
            if (backgrounds.Length == 0) return;
            _index = ((index % backgrounds.Length) + backgrounds.Length) % backgrounds.Length;
            Refresh();
            IndexChanged?.Invoke(_index);
        }

        /// <summary>
        /// Temporarily shows another background (the player's choice is kept).
        /// <paramref name="alignX"/> picks which part of a wider-than-screen image is visible
        /// (0 = left edge, 0.5 = middle, 1 = right edge), e.g. to keep a painted bed out of view.
        /// </summary>
        public void ShowOverride(Sprite sprite, float alignX = 0.5f)
        {
            _override = sprite;
            _alignX = Mathf.Clamp01(alignX);
            Refresh();
        }

        /// <summary>Returns to the player's chosen background.</summary>
        public void ClearOverride()
        {
            _override = null;
            _alignX = 0.5f;
            Refresh();
        }

        private void Refresh()
        {
            if (target == null) return;
            if (_override != null) target.sprite = _override;
            else if (backgrounds.Length > 0) target.sprite = backgrounds[_index];

            if (fitter != null && target.sprite != null)
                fitter.aspectRatio = target.sprite.rect.width / target.sprite.rect.height;

            // The envelope fitter stretches the image over the screen; the pivot slides the extra
            // width, so 0..1 always keeps the screen covered.
            var rt = target.rectTransform;
            rt.pivot = new Vector2(_override != null ? _alignX : 0.5f, rt.pivot.y);
        }
    }
}
