using System.Collections.Generic;
using UnityEngine;

namespace Northbound.Quests
{
    /// <summary>
    /// Makes an available mission easy to start from the room floor while reserving
    /// the authored doorway for entering and leaving the location.
    /// </summary>
    public sealed class RoomMissionStartZone : MonoBehaviour
    {
        public const float DoorKeepoutHalfWidth = 2.1f;
        public const float DoorKeepoutHalfHeight = 2.1f;

        private readonly List<BoxCollider2D> segments = new List<BoxCollider2D>();

        public Bounds RoomBounds { get; private set; }
        public Bounds DoorKeepoutBounds { get; private set; }
        public int SegmentCount => segments.Count;
        public float InteractionArea { get; private set; }

        public void Configure(Bounds roomBounds, Vector2 doorPosition)
        {
            RoomBounds = roomBounds;
            DoorKeepoutBounds = new Bounds(
                new Vector3(doorPosition.x, doorPosition.y, roomBounds.center.z),
                new Vector3(DoorKeepoutHalfWidth * 2f, DoorKeepoutHalfHeight * 2f, 1f));

            foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
            segments.Clear();
            InteractionArea = 0f;

            var roomMin = (Vector2)roomBounds.min;
            var roomMax = (Vector2)roomBounds.max;
            var keepoutMin = Vector2.Max(roomMin, (Vector2)DoorKeepoutBounds.min);
            var keepoutMax = Vector2.Min(roomMax, (Vector2)DoorKeepoutBounds.max);
            if (keepoutMin.x >= keepoutMax.x || keepoutMin.y >= keepoutMax.y)
            {
                AddSegment(roomMin.x, roomMax.x, roomMin.y, roomMax.y);
                return;
            }

            AddSegment(roomMin.x, keepoutMin.x, roomMin.y, roomMax.y);
            AddSegment(keepoutMax.x, roomMax.x, roomMin.y, roomMax.y);
            AddSegment(keepoutMin.x, keepoutMax.x, roomMin.y, keepoutMin.y);
            AddSegment(keepoutMin.x, keepoutMax.x, keepoutMax.y, roomMax.y);
        }

        public bool Contains(Vector2 worldPoint)
        {
            return RoomBounds.Contains(new Vector3(worldPoint.x, worldPoint.y, RoomBounds.center.z)) &&
                !DoorKeepoutBounds.Contains(new Vector3(worldPoint.x, worldPoint.y, DoorKeepoutBounds.center.z));
        }

        private void AddSegment(float minX, float maxX, float minY, float maxY)
        {
            var width = maxX - minX;
            var height = maxY - minY;
            if (width <= .05f || height <= .05f) return;

            var worldCenter = new Vector3((minX + maxX) * .5f, (minY + maxY) * .5f, transform.position.z);
            var localCenter = transform.InverseTransformPoint(worldCenter);
            var segment = gameObject.AddComponent<BoxCollider2D>();
            segment.isTrigger = true;
            segment.offset = new Vector2(localCenter.x, localCenter.y);
            segment.size = new Vector2(width, height);
            segments.Add(segment);
            InteractionArea += width * height;
        }
    }
}
