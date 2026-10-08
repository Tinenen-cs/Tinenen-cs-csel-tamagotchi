using System;
using Tamagotchi.UI;
using Tamagotchi.World;
using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi.Pet
{
    /// <summary>Everything the pet can visibly be doing.</summary>
    public enum PetState
    {
        Idle, Eating, Drinking, Studying, Sleeping, Playing, Happy, Sad, Crying, Hangry, Sick
    }

    /// <summary>Frames used for one state.</summary>
    [Serializable]
    public class StateAnimation
    {
        public PetState state;
        public Sprite[] frames;
        [Min(0f)] public float fps = 6f;
    }

    /// <summary>
    /// The pet's state machine. Two layers decide what is shown:
    ///
    ///  1. Base state, from the stats (highest priority first):
    ///     Sick > Sleeping > Hangry > Crying (low health) > Sad (low happiness) > Idle
    ///  2. Short reactions to buttons, shown on top of the base state for a moment:
    ///     Eating, Drinking, Studying, Playing -> Happy, or Sad when the pet refuses.
    ///
    /// Each state plays its flip-book animation plus a small effect (bounce, shake, tint).
    /// Sleeping also dims the scene and switches to the night background.
    /// </summary>
    public class PetController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PetStats stats;
        [SerializeField] private SpriteAnimator animator;
        [SerializeField] private Image petImage;

        [Header("Animations (one entry per state)")]
        [SerializeField] private StateAnimation[] animations = new StateAnimation[0];

        [Header("Sleeping")]
        [SerializeField] private BackgroundSwitcher backgrounds;
        [SerializeField] private Sprite sleepBackground;
        [SerializeField] private Image dimOverlay;
        [Range(0, 1)] [SerializeField] private float sleepDim = 0.45f;

        [Header("Reaction timing (seconds)")]
        [SerializeField] private float reactionSeconds = 2f;
        [SerializeField] private float happyAfterPlaySeconds = 1.2f;
        [SerializeField] private float refuseSeconds = 1.5f;

        [Header("Effects")]
        [SerializeField] private Color hangryTint = new Color(1f, 0.55f, 0.5f);
        [SerializeField] private Color sickTint = new Color(0.75f, 0.95f, 0.7f);
        [SerializeField] private float bounceHeight = 30f;
        [SerializeField] private float shakeAmount = 7f;

        /// <summary>What the pet is showing right now.</summary>
        public PetState State { get; private set; } = PetState.Idle;

        /// <summary>Raised whenever the shown state changes (audio listens to this).</summary>
        public event Action<PetState> StateChanged;

        private bool _hasReaction;
        private PetState _reaction;
        private float _reactionEnds;
        private bool _hasFollowUp;      // e.g. Happy after Playing
        private PetState _followUp;
        private float _followUpSeconds;
        private RectTransform _petRect;
        private Vector2 _restPosition;
        private bool _started;

        private void Awake()
        {
            _petRect = petImage.rectTransform;
            _restPosition = _petRect.anchoredPosition;
        }

        private void OnEnable()
        {
            stats.Changed += Evaluate;
            stats.BecameSick += OnBecameSick;
        }

        private void OnDisable()
        {
            stats.Changed -= Evaluate;
            stats.BecameSick -= OnBecameSick;
        }

        private void Start()
        {
            _started = true;
            SetState(BaseState(), force: true);
        }

        /// <summary>
        /// Shows the pet's reaction to a button. <paramref name="success"/> is false when the
        /// pet refused (e.g. too tired to study), which shows a short sad reaction instead.
        /// </summary>
        public void React(PetAction action, bool success)
        {
            if (stats.IsSick) return;
            _hasFollowUp = false;

            if (!success)
            {
                StartReaction(PetState.Sad, refuseSeconds);
                return;
            }

            switch (action)
            {
                case PetAction.Feed: StartReaction(PetState.Eating, reactionSeconds); break;
                case PetAction.Drink: StartReaction(PetState.Drinking, reactionSeconds); break;
                case PetAction.Study: StartReaction(PetState.Studying, reactionSeconds); break;
                case PetAction.Play:
                    StartReaction(PetState.Playing, reactionSeconds);
                    _hasFollowUp = true;
                    _followUp = PetState.Happy;
                    _followUpSeconds = happyAfterPlaySeconds;
                    break;
                case PetAction.Sleep:
                    // Sleeping is a stat state, not a timed reaction.
                    _hasReaction = false;
                    Evaluate();
                    break;
            }
        }

        /// <summary>The state the stats call for, ignoring reactions.</summary>
        public PetState BaseState()
        {
            if (stats.IsSick) return PetState.Sick;
            if (stats.IsSleeping) return PetState.Sleeping;
            if (stats.IsHangry) return PetState.Hangry;
            if (stats.IsUnwell) return PetState.Crying;
            if (stats.IsUnhappy) return PetState.Sad;
            return PetState.Idle;
        }

        /// <summary>The frames configured for a state (empty if none).</summary>
        public Sprite[] FramesFor(PetState state)
        {
            foreach (var a in animations)
                if (a.state == state) return a.frames;
            return new Sprite[0];
        }

        private void StartReaction(PetState state, float seconds)
        {
            _hasReaction = true;
            _reaction = state;
            _reactionEnds = Time.time + seconds;
            Evaluate();
        }

        private void OnBecameSick()
        {
            _hasReaction = false;
            _hasFollowUp = false;
            Evaluate();
        }

        private void Evaluate()
        {
            if (!_started) return;
            // Sick and sleeping always win over a reaction.
            PetState baseState = BaseState();
            bool reactionAllowed = baseState != PetState.Sick && baseState != PetState.Sleeping;
            SetState(_hasReaction && reactionAllowed ? _reaction : baseState);
        }

        private void SetState(PetState state, bool force = false)
        {
            if (state == State && !force) return;
            PetState previous = State;
            State = state;

            float fps = 6f;
            foreach (var a in animations)
                if (a.state == state) fps = a.fps;
            animator.Play(FramesFor(state), fps);

            // Night scene while asleep.
            if (backgrounds != null)
            {
                if (state == PetState.Sleeping && sleepBackground != null) backgrounds.ShowOverride(sleepBackground);
                else if (previous == PetState.Sleeping) backgrounds.ClearOverride();
            }

            StateChanged?.Invoke(state);
        }

        private void Update()
        {
            // End timed reactions (and chain Playing -> Happy).
            if (_hasReaction && Time.time >= _reactionEnds)
            {
                if (_hasFollowUp)
                {
                    _hasFollowUp = false;
                    StartReaction(_followUp, _followUpSeconds);
                }
                else
                {
                    _hasReaction = false;
                    Evaluate();
                }
            }

            AnimateEffects();
        }

        /// <summary>Per-state motion and tint on top of the flip-book frames.</summary>
        private void AnimateEffects()
        {
            float t = Time.time;
            Vector2 offset = Vector2.zero;
            Color tint = Color.white;

            switch (State)
            {
                case PetState.Happy:
                case PetState.Playing:
                    offset.y = Mathf.Abs(Mathf.Sin(t * 8f)) * bounceHeight;
                    break;
                case PetState.Hangry:
                    offset.x = Mathf.Sin(t * 45f) * shakeAmount;
                    tint = Color.Lerp(Color.white, hangryTint, 0.5f + 0.5f * Mathf.Sin(t * 6f));
                    break;
                case PetState.Sick:
                    tint = sickTint;
                    break;
                case PetState.Eating:
                case PetState.Drinking:
                    offset.y = Mathf.Abs(Mathf.Sin(t * 12f)) * bounceHeight * 0.25f;
                    break;
            }

            _petRect.anchoredPosition = _restPosition + offset;
            petImage.color = tint;

            if (dimOverlay != null)
            {
                Color c = dimOverlay.color;
                float target = State == PetState.Sleeping ? sleepDim : 0f;
                c.a = Mathf.MoveTowards(c.a, target, Time.deltaTime * 1.5f);
                dimOverlay.color = c;
            }
        }
    }
}
