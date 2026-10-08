using TMPro;
using UnityEngine;

namespace Tamagotchi.UI
{
    /// <summary>
    /// The speech bubble above the pet's head. Pops in when a message is shown and
    /// fades out after a few seconds, unless the message is "sticky" (e.g. HANGRY!),
    /// which stays until something else is said.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [Tooltip("Seconds a normal message stays visible.")]
        [SerializeField] private float showSeconds = 3.5f;
        [SerializeField] private float fadeSpeed = 6f;

        private CanvasGroup _group;
        private float _hideAt;
        private bool _visible;

        /// <summary>The message currently shown (empty when hidden).</summary>
        public string Message => _visible ? text.text : string.Empty;

        public bool IsVisible => _visible;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            transform.localScale = Vector3.one * 0.6f;
        }

        /// <summary>Shows a message. Sticky messages stay until replaced or hidden.</summary>
        public void Show(string message, bool sticky = false)
        {
            if (string.IsNullOrEmpty(message)) { Hide(); return; }
            bool changed = !_visible || text.text != message;
            text.text = message;
            _visible = true;
            _hideAt = sticky ? float.PositiveInfinity : Time.unscaledTime + showSeconds;
            if (changed) transform.localScale = Vector3.one * 0.6f; // pop again for new text
        }

        public void Hide() => _visible = false;

        private void Update()
        {
            if (_visible && Time.unscaledTime >= _hideAt) _visible = false;

            float k = 1f - Mathf.Exp(-fadeSpeed * Time.unscaledDeltaTime);
            _group.alpha = Mathf.Lerp(_group.alpha, _visible ? 1f : 0f, k);
            float scale = Mathf.Lerp(transform.localScale.x, _visible ? 1f : 0.8f, k * 1.5f);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
