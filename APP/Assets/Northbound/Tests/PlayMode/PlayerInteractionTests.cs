using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Northbound.Core;
using Northbound.Interaction;
using Northbound.Player;
using Northbound.Quests;
using Northbound.UI;
using Northbound.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Northbound.Tests
{
    public sealed class PlayerInteractionTests
    {
        private readonly ArrayList spawnedObjects = new ArrayList();

        [UnityTest]
        public IEnumerator PlayerMovesOnUnobstructedGround()
        {
            var motor = CreatePlayer();

            motor.SetMoveInput(Vector2.right);
            yield return WaitForFixedUpdates(3);

            Assert.That(motor.transform.position.x, Is.GreaterThan(0.1f));
        }

        [UnityTest]
        public IEnumerator PlayerStopsAtCollider2D()
        {
            var motor = CreatePlayer();
            CreateWall(new Vector2(1f, 0f), new Vector2(0.2f, 2f));

            motor.SetMoveInput(Vector2.right);
            yield return WaitForFixedUpdates(30);

            Assert.That(motor.transform.position.x, Is.LessThan(0.6f));
        }

        [UnityTest]
        public IEnumerator PlayerCannotLeaveConfiguredRoomBoundsEvenWhenPhysicsMissesAFrame()
        {
            var motor = CreatePlayer();
            motor.SetMovementBounds(new Bounds(Vector3.zero, new Vector3(2f, 2f, 1f)));

            motor.SetMoveInput(new Vector2(1f, -1f));
            yield return WaitForFixedUpdates(90);

            Assert.That(motor.transform.position.x, Is.LessThanOrEqualTo(.71f));
            Assert.That(motor.transform.position.y, Is.GreaterThanOrEqualTo(-.71f));
        }

        [UnityTest]
        public IEnumerator InputLeaseDisablesMovementUntilDisposed()
        {
            var gate = CreateObject("Input Gate").AddComponent<InputGate>();
            var motor = CreatePlayer(gate);

            using (gate.Acquire(this))
            {
                motor.SetMoveInput(Vector2.right);
                yield return new WaitForFixedUpdate();
                Assert.That(motor.transform.position.x, Is.EqualTo(0f).Within(0.01f));
            }

            yield return new WaitForFixedUpdate();
            Assert.That(motor.transform.position.x, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator ClosestEnabledInteractableDrivesPrompt()
        {
            GameText.Use(GameLanguage.English);
            var player = CreatePlayer();
            var prompt = CreateObject("Interaction Prompt").AddComponent<InteractionPromptView>();
            var interactor = player.gameObject.AddComponent<PlayerInteractor>();
            interactor.SetPromptView(prompt);
            interactor.SetInteractionRange(2f);
            CreateInteractable("Disabled", new Vector2(0.4f, 0f), "Disabled prompt", false);
            CreateInteractable("Far", new Vector2(1.4f, 0f), "Far prompt", true);
            CreateInteractable("Near", new Vector2(0.8f, 0f), "Near prompt", true);

            yield return null;

            Assert.That(interactor.CurrentInteractable.Prompt, Is.EqualTo("Near prompt"));
            Assert.That(prompt.IsVisible, Is.True);
            Assert.That(prompt.CurrentPrompt, Is.EqualTo("[E / ENTER] Near prompt"));
        }

        [UnityTest]
        public IEnumerator InteractInvokesClosestTargetExactlyOnce()
        {
            var player = CreatePlayer();
            var interactor = player.gameObject.AddComponent<PlayerInteractor>();
            interactor.SetInteractionRange(2f);
            var target = CreateInteractable("Target", new Vector2(0.8f, 0f), "Inspect", true);

            yield return null;
            interactor.TryInteract();
            yield return null;

            Assert.That(target.InteractionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DoorWinsWhenItsInteractionRangeOverlapsTheRoomMissionZone()
        {
            var player = CreatePlayer();
            player.transform.position = new Vector3(10.35f, -2.4f, 0f);
            var interactor = player.gameObject.AddComponent<PlayerInteractor>();
            interactor.SetInteractionRange(1.25f);

            var host = CreateObject("Door Priority Location Host");
            var controller = host.AddComponent<LocationTransitionController>();
            controller.Configure(player.transform, host.AddComponent<InputGate>(), null);
            var room = CreateObject("Door Priority Electronics Room");
            var exterior = CreateObject("Door Priority Exterior");
            controller.Register(new LocationDefinition("noah_electronics", room, room.transform,
                new Bounds(new Vector3(10.25f, -4f, 0f), new Vector3(20.5f, 7f, 1f)), "Noah's Electronics"));
            controller.Register(new LocationDefinition("exterior", exterior, exterior.transform,
                new Bounds(Vector3.zero, Vector3.one * 30f), "Greybridge"));
            controller.SetInitial("noah_electronics");

            var missionObject = new GameObject("Static Room Mission");
            missionObject.transform.SetParent(room.transform, true);
            missionObject.transform.position = new Vector3(11f, -2f, 0f);
            missionObject.AddComponent<CircleCollider2D>().isTrigger = true;
            var mission = missionObject.AddComponent<ProbeInteractable>();
            mission.Configure("Begin mission", true);
            missionObject.AddComponent<RoomMissionStartZone>().Configure(
                new Bounds(new Vector3(10.25f, -4f, 0f), new Vector3(20.5f, 7f, 1f)),
                new Vector2(12f, -1.1f));

            var doorObject = new GameObject("Electronics Exit Door");
            doorObject.transform.SetParent(room.transform, true);
            doorObject.transform.position = new Vector3(12f, -1.1f, 0f);
            var doorCollider = doorObject.AddComponent<BoxCollider2D>();
            doorCollider.isTrigger = true;
            doorCollider.size = new Vector2(1.2f, 1.8f);
            var door = doorObject.AddComponent<DoorInteractor>();
            door.Configure("[E] Return to Greybridge", "exterior", controller);
            Physics2D.SyncTransforms();

            interactor.RefreshTarget();

            Assert.That(interactor.CurrentInteractable, Is.SameAs(door),
                "The door must win even when a nearby room-wide mission collider also overlaps the player scan.");
            interactor.TryInteract();
            yield return null;
            Assert.That(controller.CurrentLocationId, Is.EqualTo("exterior"));
            Assert.That(mission.InteractionCount, Is.Zero);
        }

        [Test]
        public void InteractBinding_IncludesEEnterAndSpace()
        {
            var player = CreateObject("Keyboard Binding Jamie");
            var interactor = player.AddComponent<PlayerInteractor>();
            var action = (InputAction)typeof(PlayerInteractor)
                .GetField("interactAction", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(interactor);
            var paths = action.bindings.Select(binding => binding.path).ToArray();

            Assert.That(paths, Does.Contain("<Keyboard>/e"));
            Assert.That(paths, Does.Contain("<Keyboard>/enter"));
            Assert.That(paths, Does.Contain("<Keyboard>/space"));
        }

        [UnityTest]
        public IEnumerator ClosestInteractableIsSelectedWhenMoreThanInitialBufferCapacityOverlap()
        {
            var player = CreatePlayer();
            var interactor = player.gameObject.AddComponent<PlayerInteractor>();
            interactor.SetInteractionRange(2f);

            for (var index = 0; index < 24; index++)
            {
                CreateInteractable($"Far {index}", new Vector2(1.5f, 0f), $"Far {index}", true);
            }

            var closest = CreateInteractable("Closest", new Vector2(0.25f, 0f), "Closest", true);
            yield return null;

            Assert.That(interactor.CurrentInteractable, Is.SameAs(closest));
        }

        [UnityTest]
        public IEnumerator FollowCameraTracksTargetWithoutRotating()
        {
            var target = CreateObject("Target");
            target.transform.position = new Vector3(4f, 3f, 0f);
            var cameraObject = CreateObject("Camera");
            cameraObject.AddComponent<Camera>();
            var followCamera = cameraObject.AddComponent<FollowCamera>();
            followCamera.SetTarget(target.transform);

            yield return null;

            Assert.That(cameraObject.transform.position.x, Is.GreaterThan(0f));
            Assert.That(cameraObject.transform.position.y, Is.GreaterThan(0f));
            Assert.That(cameraObject.transform.position.z, Is.EqualTo(-10f).Within(0.01f));
            Assert.That(cameraObject.transform.rotation, Is.EqualTo(Quaternion.identity));
        }

        [UnityTest]
        public IEnumerator ReducedMotion_SnapsFollowCameraInsteadOfApplyingSmoothing()
        {
            var target = CreateObject("Reduced Motion Target");
            target.transform.position = new Vector3(4f, 3f, 0f);
            var cameraObject = CreateObject("Reduced Motion Camera");
            cameraObject.AddComponent<Camera>();
            var followCamera = cameraObject.AddComponent<FollowCamera>();
            followCamera.SetTarget(target.transform);
            var setProvider = typeof(FollowCamera).GetMethod("SetReducedMotionProvider");
            Assert.That(setProvider, Is.Not.Null);
            setProvider.Invoke(followCamera, new object[] { new System.Func<bool>(() => true) });

            yield return null;

            Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(4f, 3f, -10f)));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject spawnedObject in spawnedObjects)
            {
                Object.DestroyImmediate(spawnedObject);
            }

            spawnedObjects.Clear();
        }

        private PlayerMotor CreatePlayer(InputGate gate = null)
        {
            var player = CreateObject("Jamie");
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            player.AddComponent<CircleCollider2D>().radius = 0.3f;
            var motor = player.AddComponent<PlayerMotor>();
            motor.SetInputGate(gate);
            return motor;
        }

        private ProbeInteractable CreateInteractable(string name, Vector2 position, string prompt, bool enabled)
        {
            var interactable = CreateObject(name);
            interactable.transform.position = position;
            interactable.AddComponent<CircleCollider2D>().isTrigger = true;
            var probe = interactable.AddComponent<ProbeInteractable>();
            probe.Configure(prompt, enabled);
            return probe;
        }

        private void CreateWall(Vector2 position, Vector2 size)
        {
            var wall = CreateObject("Wall");
            wall.transform.position = position;
            wall.AddComponent<BoxCollider2D>().size = size;
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            spawnedObjects.Add(gameObject);
            return gameObject;
        }

        private static IEnumerator WaitForFixedUpdates(int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private sealed class ProbeInteractable : MonoBehaviour, IInteractable
        {
            private string prompt;
            private bool canInteract;

            public string Prompt => prompt;

            public bool CanInteract => canInteract;

            public int InteractionCount { get; private set; }

            public void Configure(string value, bool enabledValue)
            {
                prompt = value;
                canInteract = enabledValue;
                enabled = enabledValue;
            }

            public void Interact(GameObject actor)
            {
                InteractionCount++;
            }
        }
    }
}
