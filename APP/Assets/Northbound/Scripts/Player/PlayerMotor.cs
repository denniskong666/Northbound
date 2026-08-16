using Northbound.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Northbound.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField] private InputGate inputGate;

        private Rigidbody2D body;
        private InputAction moveAction;
        private Vector2 explicitMoveInput;
        private bool usesExplicitMoveInput;
        private Bounds? movementBounds;

        public Vector2 AppliedMoveInput { get; private set; }
        public Bounds? CurrentMovementBounds => movementBounds;

        public Vector2 CurrentMoveInput
        {
            get
            {
                var input = usesExplicitMoveInput ? explicitMoveInput : moveAction.ReadValue<Vector2>();
                return Vector2.ClampMagnitude(input, 1f);
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            moveAction = CreateMoveAction();
        }

        private void OnEnable()
        {
            moveAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
        }

        private void FixedUpdate()
        {
            var direction = inputGate != null && inputGate.IsBlocked ? Vector2.zero : AppliedMoveInput;
            var nextPosition = body.position + direction * moveSpeed * Time.fixedDeltaTime;
            if (movementBounds.HasValue)
            {
                var bounds = movementBounds.Value;
                var circle = GetComponent<CircleCollider2D>();
                var padding = circle != null ? circle.radius : 0f;
                nextPosition.x = Mathf.Clamp(nextPosition.x, bounds.min.x + padding, bounds.max.x - padding);
                nextPosition.y = Mathf.Clamp(nextPosition.y, bounds.min.y + padding, bounds.max.y - padding);
            }
            body.MovePosition(nextPosition);
        }

        private void Update()
        {
            AppliedMoveInput = inputGate != null && inputGate.IsBlocked ? Vector2.zero : CurrentMoveInput;
        }

        public void SetInputGate(InputGate gate)
        {
            inputGate = gate;
        }

        public void SetMovementBounds(Bounds bounds)
        {
            movementBounds = bounds;
        }

        public void ClearMovementBounds()
        {
            movementBounds = null;
        }

        public void SetMoveInput(Vector2 value)
        {
            usesExplicitMoveInput = true;
            explicitMoveInput = Vector2.ClampMagnitude(value, 1f);
            AppliedMoveInput = inputGate != null && inputGate.IsBlocked ? Vector2.zero : explicitMoveInput;
        }

        public void ClearMoveInputOverride()
        {
            usesExplicitMoveInput = false;
        }

        private static InputAction CreateMoveAction()
        {
            var action = new InputAction("Move", InputActionType.Value);
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            return action;
        }
    }
}
