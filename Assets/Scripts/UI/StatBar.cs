using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi.UI
{
    /// <summary>
    /// One stat meter: a non-interactive Slider plus an optional "75%" label.
    /// The fill is either a fixed color or follows a gradient by value
    /// (the hunger bar uses red -> yellow -> green).
    /// </summary>
    public class StatBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text valueText;

        [Tooltip("If on, the fill color is taken from the gradient (left = empty, right = full).")]
        [SerializeField] private bool useGradient;
        [SerializeField] private Gradient colorByValue = new Gradient();
        [SerializeField] private Color fixedColor = Color.white;

        /// <summary>Current value, 0-100.</summary>
        public float Value => slider != null ? slider.value : 0f;

        /// <summary>The fill Image, so effects (e.g. hangry flashing) can tint it.</summary>
        public Image Fill => fill;

        private void Awake()
        {
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.interactable = false;
            SetValue(slider.value);
        }

        /// <summary>Shows a value between 0 and 100.</summary>
        public void SetValue(float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);
            slider.value = value;
            fill.color = CurrentColor;
            if (valueText != null) valueText.text = Mathf.RoundToInt(value) + "%";
        }

        /// <summary>The color the fill should have at the current value.</summary>
        public Color CurrentColor => useGradient ? colorByValue.Evaluate(slider.value / 100f) : fixedColor;
    }
}
