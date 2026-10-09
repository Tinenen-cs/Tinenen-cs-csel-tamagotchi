using UnityEngine;

namespace Tamagotchi.UI
{
    /// <summary>
    /// Fades a panel in and pops its content up from a smaller size every time it is shown
    /// (used for the game-over card).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PopIn : MonoBehaviour
    {
        [Tooltip("The part that scales up (e.g. the card). Leave empty to scale this object.")]
        [SerializeField] private Transform content;
        [SerializeField] private float seconds = 0.3f;
        [SerializeField] private float startScale = 0.85f;

        private CanvasGroup _group;
        private float _t = 1f;

        /// <summary>True while the pop-in animation is running.</summary>
        public bool IsAnimating => _t < 1f;

        private void Awake() => _group = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            _t = 0f;
            Apply();
        }

        private void Update()
        {
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds));
            Apply();
        }

        private void Apply()
        {
            // Ease-out with a tiny overshoot so it feels springy.
            float e = 1f - Mathf.Pow(1f - _t, 3f);
            float scale = Mathf.LerpUnclamped(startScale, 1f, e) + Mathf.Sin(_t * Mathf.PI) * 0.03f;
            _group.alpha = e;
            (content != null ? content : transform).localScale = new Vector3(scale, scale, 1f);
        }
    }
}
