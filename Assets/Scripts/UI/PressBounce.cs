using UnityEngine;
using UnityEngine.EventSystems;

namespace Tamagotchi.UI
{
    /// <summary>
    /// Makes a button squash a little while pressed and spring back on release,
    /// so every tap gets instant visual feedback.
    /// </summary>
    public class PressBounce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressedScale = 0.9f;
        [SerializeField] private float speed = 18f;

        private float _target = 1f;

        public void OnPointerDown(PointerEventData eventData) => _target = pressedScale;
        public void OnPointerUp(PointerEventData eventData) => _target = 1f;

        private void OnDisable()
        {
            _target = 1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, _target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
