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

        /// <summary>Raised whenever the chosen background index changes (for saving).</summary>
        public event Action<int> IndexChanged;

        /// <summary>Raised whenever the background on screen changes (incl. the sleep override).</summary>
        public event Action<Sprite> DisplayChanged;

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

        /// <summary>Temporarily shows another background (the player's choice is kept).</summary>
        public void ShowOverride(Sprite sprite)
        {
            _override = sprite;
            Refresh();
        }

        /// <summary>Returns to the player's chosen background.</summary>
        public void ClearOverride()
        {
            _override = null;
            Refresh();
        }

        private void Refresh()
        {
            if (target == null) return;
            if (_override != null) target.sprite = _override;
            else if (backgrounds.Length > 0) target.sprite = backgrounds[_index];

            if (fitter != null && target.sprite != null)
                fitter.aspectRatio = target.sprite.rect.width / target.sprite.rect.height;
            DisplayChanged?.Invoke(target.sprite);
        }
    }
}
