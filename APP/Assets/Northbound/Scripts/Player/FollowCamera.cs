using System;
using UnityEngine;

namespace Northbound.Player
{
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;
        [SerializeField, Min(0.01f)] private float orthographicSize = 5f;

        private Vector3 velocity;
        private Func<bool> reducedMotionProvider;
        private Bounds? movementBounds;

        private void Awake()
        {
            var cameraComponent = GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = orthographicSize;
            transform.rotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = target.position + offset;
            var smoothedPosition = reducedMotionProvider != null && reducedMotionProvider()
                ? desiredPosition
                : Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
            smoothedPosition.z = desiredPosition.z;
            if (movementBounds.HasValue)
            {
                var bounds = movementBounds.Value;
                smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, bounds.min.x, bounds.max.x);
                smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, bounds.min.y, bounds.max.y);
            }
            transform.position = smoothedPosition;
            transform.rotation = Quaternion.identity;
        }

        public void SetTarget(Transform value)
        {
            target = value;
        }

        public void SetReducedMotionProvider(Func<bool> provider)
        {
            reducedMotionProvider = provider;
        }

        public void SetBounds(Bounds bounds) => movementBounds = bounds;

        public void SnapTo(Vector2 focus)
        {
            velocity = Vector3.zero;
            var position = new Vector3(focus.x, focus.y, 0f) + offset;
            if (movementBounds.HasValue)
            {
                var bounds = movementBounds.Value;
                position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
                position.y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);
            }
            transform.position = position;
            transform.rotation = Quaternion.identity;
        }

        public void SetOrthographicSize(float size)
        {
            if (size <= 0f) return;
            orthographicSize = size;
            GetComponent<Camera>().orthographicSize = size;
        }
    }
}
