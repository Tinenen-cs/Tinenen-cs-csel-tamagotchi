using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi.World
{
    /// <summary>Props shown on the left and right of the pet for one background.</summary>
    [Serializable]
    public class DecorSet
    {
        public Sprite background;
        public Sprite left;
        public Sprite right;
    }

    /// <summary>
    /// Places the asset pack's props beside the pet to match the background that is showing
    /// (e.g. hamster house + water bottle at home, palm tree + umbrella at the beach).
    /// Props are drawn at the same pixel scale as the hamster, behind it.
    /// </summary>
    public class SceneDecor : MonoBehaviour
    {
        [SerializeField] private BackgroundSwitcher backgrounds;
        [SerializeField] private Image leftSlot;
        [SerializeField] private Image rightSlot;
        [Tooltip("The pet's frame; props are scaled to the same pixel size as the hamster.")]
        [SerializeField] private RectTransform petSpot;
        [Tooltip("Height of one hamster frame in source pixels.")]
        [SerializeField] private float petFramePixels = 187f;
        [Tooltip("Prop size relative to the hamster's pixel scale.")]
        [SerializeField] private float propScale = 0.8f;
        [SerializeField] private DecorSet[] sets = new DecorSet[0];

        private void OnEnable()
        {
            if (backgrounds != null) backgrounds.DisplayChanged += Show;
        }

        private void OnDisable()
        {
            if (backgrounds != null) backgrounds.DisplayChanged -= Show;
        }

        private void Start()
        {
            if (backgrounds != null) Show(backgrounds.Current);
        }

        /// <summary>Shows the props that belong to <paramref name="background"/>.</summary>
        public void Show(Sprite background)
        {
            DecorSet set = Array.Find(sets, s => s.background == background);
            Fill(leftSlot, set?.left);
            Fill(rightSlot, set?.right);
        }

        private void Fill(Image slot, Sprite sprite)
        {
            if (slot == null) return;
            slot.enabled = sprite != null;
            if (sprite == null) return;
            slot.sprite = sprite;
            float scale = PixelScale() * propScale;
            slot.rectTransform.sizeDelta = sprite.rect.size * scale;
        }

        /// <summary>Canvas units per source pixel, matching the hamster's size on screen.</summary>
        private float PixelScale()
        {
            float h = petSpot != null ? petSpot.rect.height : 0f;
            return h > 1f ? h / petFramePixels : 2.87f;
        }
    }
}
