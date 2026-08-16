using UnityEngine;

namespace Northbound.World
{
    [RequireComponent(typeof(Camera))]
    public sealed class GreybridgeCameraBounds : MonoBehaviour
    {
        private Rect bounds;
        private bool isConfigured;

        public void Configure(Rect worldBounds)
        {
            bounds = worldBounds;
            isConfigured = true;
        }

        private void LateUpdate()
        {
            if (!isConfigured)
            {
                return;
            }

            var cameraComponent = GetComponent<Camera>();
            var halfHeight = cameraComponent.orthographicSize;
            var halfWidth = halfHeight * cameraComponent.aspect;
            var minimumX = bounds.xMin + halfWidth;
            var maximumX = bounds.xMax - halfWidth;
            var minimumY = bounds.yMin + halfHeight;
            var maximumY = bounds.yMax - halfHeight;
            var position = transform.position;
            position.x = Mathf.Clamp(position.x, minimumX, maximumX);
            position.y = Mathf.Clamp(position.y, minimumY, maximumY);
            transform.position = position;
        }
    }
}
