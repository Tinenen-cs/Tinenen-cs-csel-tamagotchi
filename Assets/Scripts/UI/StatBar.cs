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

        [Header("Alarm (e.g. hangry)")]
        [SerializeField] private Color alarmColor = new Color(1f, 0.15f, 0.1f);
        [Tooltip("Flashes per second while the alarm is on.")]
        [SerializeField] private float alarmSpeed = 2.5f;
        [SerializeField] private float alarmPulseScale = 0.04f;

        [Header("Bump (a quick pop when an action changes the value)")]
        [SerializeField] private float bumpScale = 0.08f;
        [Tooltip("Changes at least this big in one update count as an action (decay is much smaller).")]
        [SerializeField] private float bumpThreshold = 2f;

        private bool _alarm;
        private float _bump;     // 1 right after a bump, fades to 0

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
            if (Mathf.Abs(value - slider.value) >= bumpThreshold) _bump = 1f;
            slider.value = value;
            fill.color = CurrentColor;
            if (valueText != null) valueText.text = Mathf.RoundToInt(value) + "%";
        }

        /// <summary>Whether the bar is currently flashing.</summary>
        public bool IsAlarmOn => _alarm;

        /// <summary>Turns the red flashing + pulsing warning on or off.</summary>
        public void SetAlarm(bool on)
        {
            _alarm = on;
            if (!on)
            {
                fill.color = CurrentColor;
                transform.localScale = Vector3.one;
            }
        }

        /// <summary>True while the bar is popping after an action changed it.</summary>
        public bool IsBumping => _bump > 0f;

        private void Update()
        {
            float s = 1f;
            if (_bump > 0f)
            {
                s += bumpScale * _bump;
                _bump = Mathf.Max(0f, _bump - Time.unscaledDeltaTime * 4f); // ~0.25 s
            }
            if (_alarm)
            {
                // 0..1..0 wave: blend the fill toward red and pulse the whole bar.
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * alarmSpeed * Mathf.PI * 2f);
                fill.color = Color.Lerp(CurrentColor, alarmColor, wave);
                s += alarmPulseScale * wave;
            }
            transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>The color the fill should have at the current value.</summary>
        public Color CurrentColor => useGradient ? colorByValue.Evaluate(slider.value / 100f) : fixedColor;
    }
}
