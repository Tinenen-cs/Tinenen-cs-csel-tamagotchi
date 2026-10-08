using UnityEngine;

namespace Tamagotchi.UI
{
    /// <summary>
    /// Keeps the game UI in a phone-shaped portrait column. On a phone it fills the
    /// screen; in a wide window (desktop, browser, Unity's "Free Aspect" Game view) it
    /// stays centered at 9:16 instead of being squashed, while the background behind it
    /// still fills the whole window. Runs in the editor too, so the Scene view matches.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class PortraitFrame : MonoBehaviour
    {
        [Tooltip("Widest allowed width / height ratio (9:16 = 0.5625).")]
        [SerializeField] private float maxAspect = 9f / 16f;

        private RectTransform _rect;
        private Vector2 _lastParentSize;

        /// <summary>Width the frame gets inside a parent of the given size.</summary>
        public static float FrameWidth(float parentWidth, float parentHeight, float maxAspect) =>
            Mathf.Min(parentWidth, parentHeight * maxAspect);

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            _lastParentSize = Vector2.zero;
            Apply();
        }

        private void Update() => Apply();

        /// <summary>Re-fits immediately (e.g. after the screen size changed in the same frame).</summary>
        public void Refresh()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            _lastParentSize = Vector2.zero;
            Apply();
        }

        private void Apply()
        {
            var parent = _rect.parent as RectTransform;
            if (parent == null) return;
            Vector2 size = parent.rect.size;
            if (size == _lastParentSize) return;
            _lastParentSize = size;

            // Full height, width limited to the portrait ratio, centered.
            float width = FrameWidth(size.x, size.y, maxAspect);
            _rect.anchorMin = new Vector2(0.5f, 0f);
            _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(width, 0f);
            _rect.anchoredPosition = Vector2.zero;
        }
    }
}
