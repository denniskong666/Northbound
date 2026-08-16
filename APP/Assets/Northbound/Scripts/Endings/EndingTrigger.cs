using System;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.Player;
using Northbound.Content;
using UnityEngine;

namespace Northbound.Endings
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class EndingTrigger : MonoBehaviour
    {
        public const float IndicatorDelaySeconds = 0.4f;
        public const float CommitmentSeconds = 1.25f;
        private const float DirectionThreshold = 0.3f;

        [SerializeField] private EndingDirection direction;
        [SerializeField] private string friendId;
        [SerializeField] private Vector2 commitmentDirection;

        private EndingResolver resolver;
        private NarrativeStateStore narrativeState;
        private PlayerMotor player;
        private GameObject holdIndicator;
        private bool confirmed;
        private Func<bool> availability;
        private Func<float> interactionTimeMultiplier;

        public EndingDirection Direction => direction;
        public string FriendId => friendId;
        public Vector2 CommitmentDirection => commitmentDirection;
        public float HoldSeconds { get; private set; }
        public bool IsAvailable => IsFinaleActive();
        public bool IsIndicatorVisible => holdIndicator != null && holdIndicator.activeSelf;
        public EndingContext LastContext { get; private set; }
        public string LastEndCard => LastContext != null ? LastContext.EndCard : string.Empty;
        public event Action<EndingContext> Confirmed;

        private void Awake()
        {
            CreateHoldIndicator();
        }

        private void OnDestroy()
        {
            if (narrativeState != null)
            {
                narrativeState.Changed -= RefreshAvailability;
            }
        }

        private void OnEnable()
        {
            RefreshAvailability();
        }

        private void Update()
        {
            if (player == null || confirmed)
            {
                return;
            }

            Tick(Time.deltaTime, true, player.CurrentMoveInput);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsAvailable)
            {
                return;
            }

            var motor = other.GetComponent<PlayerMotor>();
            if (motor != null)
            {
                player = motor;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (player != null && other.gameObject == player.gameObject)
            {
                player = null;
                Cancel();
            }
        }

        public void Configure(
            EndingDirection endingDirection,
            string selectedFriendId,
            EndingResolver endingResolver,
            NarrativeStateStore state,
            Vector2 requiredDirection = default(Vector2),
            Func<bool> availabilityCheck = null,
            Func<float> interactionTimeMultiplierProvider = null)
        {
            if (narrativeState != null)
            {
                narrativeState.Changed -= RefreshAvailability;
            }

            direction = endingDirection;
            friendId = selectedFriendId ?? string.Empty;
            resolver = endingResolver ?? throw new ArgumentNullException(nameof(endingResolver));
            narrativeState = state ?? throw new ArgumentNullException(nameof(state));
            commitmentDirection = requiredDirection.sqrMagnitude > 0f ? requiredDirection.normalized : DefaultDirection(endingDirection);
            availability = availabilityCheck;
            interactionTimeMultiplier = interactionTimeMultiplierProvider;
            narrativeState.Changed += RefreshAvailability;
            RefreshAvailability();
        }

        public void Tick(float deltaSeconds, bool playerInside, Vector2 playerDirection)
        {
            if (confirmed || !playerInside || !IsAvailable || !IsContinuingInDirection(playerDirection))
            {
                Cancel();
                return;
            }

            var multiplier = Mathf.Clamp(interactionTimeMultiplier?.Invoke() ?? 1f, .5f, 1.5f);
            var commitmentSeconds = CommitmentSeconds * multiplier;
            HoldSeconds = Mathf.Min(commitmentSeconds, HoldSeconds + Mathf.Max(0f, deltaSeconds));
            SetIndicatorVisible(HoldSeconds >= IndicatorDelaySeconds * multiplier);
            if (HoldSeconds < commitmentSeconds)
            {
                return;
            }

            Confirm();
        }

        public void Cancel()
        {
            if (confirmed)
            {
                return;
            }

            HoldSeconds = 0f;
            SetIndicatorVisible(false);
        }

        private void Confirm()
        {
            var context = (resolver ?? new EndingResolver()).Resolve(direction, friendId, narrativeState != null ? narrativeState.State : null);
            if (!PersistSelection(context))
            {
                Cancel();
                return;
            }

            confirmed = true;
            SetIndicatorVisible(false);
            LastContext = context;
            Confirmed?.Invoke(LastContext);
            var director = FindFirstObjectByType<NarrativeContentDirector>();
            if (director == null || !director.PlayEndingDialogue(LastContext, () => GameBootstrap.Instance?.Endings?.Show(LastContext)))
                GameBootstrap.Instance?.Endings?.Show(LastContext);
        }

        private bool IsFinaleActive()
        {
            return availability == null || availability();
        }

        private bool IsContinuingInDirection(Vector2 playerDirection)
        {
            return playerDirection.sqrMagnitude > 0f && Vector2.Dot(playerDirection.normalized, commitmentDirection) >= DirectionThreshold;
        }

        private bool PersistSelection(EndingContext context)
        {
            var bootstrap = GameBootstrap.Instance;
            if (bootstrap != null && ReferenceEquals(narrativeState, bootstrap.NarrativeState))
            {
                var prospective = NarrativeState.FromJson(narrativeState.State.ToJson());
                prospective.Set("ending_selected", true);
                prospective.Set($"ending_{context.AssetId}", true);
                if (!bootstrap.SaveGame.Save(prospective))
                {
                    return false;
                }
            }

            narrativeState?.Set("ending_selected", true);
            narrativeState?.Set($"ending_{context.AssetId}", true);
            return true;
        }

        public void RefreshAvailability()
        {
            var triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = IsAvailable;
            }

            if (!IsAvailable)
            {
                Cancel();
            }
        }

        private void CreateHoldIndicator()
        {
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = "Ending Hold Indicator";
            indicator.transform.SetParent(transform, false);
            indicator.transform.localPosition = new Vector3(0f, 0.9f, -0.1f);
            indicator.transform.localScale = new Vector3(0.5f, 0.12f, 1f);
            var renderer = indicator.GetComponent<MeshRenderer>();
            renderer.material.color = new Color(0.95f, 0.9f, 0.66f, 0.9f);
            var collider = indicator.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            holdIndicator = indicator;
            holdIndicator.SetActive(false);
        }

        private void SetIndicatorVisible(bool visible)
        {
            if (holdIndicator != null)
            {
                holdIndicator.SetActive(visible);
            }
        }

        private static Vector2 DefaultDirection(EndingDirection endingDirection)
        {
            switch (endingDirection)
            {
                case EndingDirection.Northbound: return Vector2.down;
                case EndingDirection.HomeChosen: return Vector2.left;
                case EndingDirection.NoMap: return Vector2.right;
                default: return Vector2.up;
            }
        }
    }
}
