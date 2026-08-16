using System.Collections;
using System.Collections.Generic;
using Northbound.Core;
using Northbound.Player;
using UnityEngine;
using System.Linq;
using System;

namespace Northbound.World
{
    public sealed class LocationTransitionController : MonoBehaviour
    {
        private readonly Dictionary<string, LocationDefinition> locations = new Dictionary<string, LocationDefinition>();
        private Transform player;
        private PlayerMotor playerMotor;
        private InputGate gate;
        private FollowCamera followCamera;
        private LocationFadeView fade;
        private bool travelling;
        private float transitionDuration = .12f;

        public string CurrentLocationId { get; private set; }
        public bool IsTravelling => travelling;
        public IReadOnlyCollection<string> RegisteredLocationIds => locations.Keys.ToArray();
        public event Action<string> LocationChanged;

        public void Configure(Transform playerTransform, InputGate inputGate, FollowCamera camera, LocationFadeView fadeView = null)
        {
            player = playerTransform;
            playerMotor = playerTransform != null ? playerTransform.GetComponent<PlayerMotor>() : null;
            gate = inputGate;
            followCamera = camera;
            fade = fadeView;
        }

        public void Register(LocationDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id) || definition.root == null) return;
            locations[definition.id] = definition;
            CreateRoomBoundaries(definition);
            definition.root.SetActive(definition.id == CurrentLocationId);
        }

        public void SetInitial(string locationId)
        {
            if (!locations.TryGetValue(locationId, out var location)) return;
            Apply(locationId);
            ConfigureActiveRoom(location);
        }

        public bool CanTravel(string destinationId) => !travelling && locations.ContainsKey(destinationId);

        public void SetTransitionDuration(float seconds) => transitionDuration = Mathf.Max(0f, seconds);

        public bool StartTravel(string destinationId)
        {
            if (!CanTravel(destinationId)) return false;
            StartCoroutine(Travel(destinationId));
            return true;
        }

        public IEnumerator Travel(string destinationId)
        {
            if (!locations.TryGetValue(destinationId, out var destination) || travelling) yield break;
            travelling = true;
            var lease = gate?.Acquire(this);
            try
            {
                if (fade != null) yield return fade.Fade(0f, 1f, transitionDuration);
                Apply(destinationId);
                if (player != null && destination.spawn != null) player.position = destination.spawn.position;
                Physics2D.SyncTransforms();
                ConfigureActiveRoom(destination);
                if (fade != null) yield return fade.Fade(1f, 0f, transitionDuration);
            }
            finally
            {
                lease?.Dispose();
                travelling = false;
            }
        }

        private void Apply(string locationId)
        {
            foreach (var pair in locations) pair.Value.root.SetActive(pair.Key == locationId);
            CurrentLocationId = locationId;
            LocationChanged?.Invoke(locationId);
        }

        private void ConfigureActiveRoom(LocationDefinition destination)
        {
            playerMotor?.SetMovementBounds(destination.walkableBounds);
            followCamera?.SetOrthographicSize(destination.cameraOrthographicSize);
            if (followCamera != null)
            {
                var exteriorClamp = followCamera.GetComponent<GreybridgeCameraBounds>();
                if (exteriorClamp != null) exteriorClamp.enabled = false;
                followCamera.SetBounds(CameraSafeBounds(destination));
                followCamera.SnapTo(destination.cameraBounds.center);
            }
            CoverRoomBackground(destination);
        }

        private static Bounds CameraSafeBounds(LocationDefinition destination)
        {
            var camera = Camera.main;
            if (camera == null) return destination.cameraBounds;
            var halfHeight = destination.cameraOrthographicSize;
            var halfWidth = halfHeight * camera.aspect;
            var bounds = destination.cameraBounds;
            var halfSafeWidth = Mathf.Max(0f, bounds.extents.x - halfWidth);
            var halfSafeHeight = Mathf.Max(0f, bounds.extents.y - halfHeight);
            return new Bounds(bounds.center, new Vector3(halfSafeWidth * 2f, halfSafeHeight * 2f, bounds.size.z));
        }

        private static void CreateRoomBoundaries(LocationDefinition definition)
        {
            if (definition.id == "exterior") return;
            var bounds = definition.walkableBounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f) return;
            const float thickness = .8f;
            CreateBoundary("Room Boundary North", new Vector2(bounds.center.x, bounds.max.y + thickness * .5f), new Vector2(bounds.size.x + thickness * 2f, thickness));
            CreateBoundary("Room Boundary South", new Vector2(bounds.center.x, bounds.min.y - thickness * .5f), new Vector2(bounds.size.x + thickness * 2f, thickness));
            CreateBoundary("Room Boundary West", new Vector2(bounds.min.x - thickness * .5f, bounds.center.y), new Vector2(thickness, bounds.size.y));
            CreateBoundary("Room Boundary East", new Vector2(bounds.max.x + thickness * .5f, bounds.center.y), new Vector2(thickness, bounds.size.y));

            void CreateBoundary(string name, Vector2 position, Vector2 size)
            {
                var boundary = definition.root.transform.Find(name);
                if (boundary == null)
                {
                    var boundaryObject = new GameObject(name);
                    boundaryObject.transform.SetParent(definition.root.transform, true);
                    boundary = boundaryObject.transform;
                }
                boundary.position = new Vector3(position.x, position.y, 0f);
                var collider = boundary.GetComponent<BoxCollider2D>();
                if (collider == null) collider = boundary.gameObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = false;
                collider.size = size;
            }
        }

        private static void CoverRoomBackground(LocationDefinition destination)
        {
            if (destination.id == "exterior" || destination.cameraOrthographicSize <= 0f) return;
            var renderer = destination.root.GetComponentsInChildren<SpriteRenderer>(true)
                .FirstOrDefault(item => item.name.StartsWith("Art ") && item.sprite != null);
            var camera = Camera.main;
            if (renderer == null || camera == null) return;
            var requiredHeight = camera.orthographicSize * 2f;
            var requiredWidth = requiredHeight * camera.aspect;
            var current = renderer.bounds.size;
            if (current.x <= 0f || current.y <= 0f) return;
            var scale = Mathf.Max(requiredWidth / current.x, requiredHeight / current.y);
            if (scale > 1f) renderer.transform.localScale *= scale;
        }
    }
}
