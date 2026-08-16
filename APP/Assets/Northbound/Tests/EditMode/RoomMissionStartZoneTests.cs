using System.Linq;
using Northbound.Quests;
using NUnit.Framework;
using UnityEngine;

namespace Northbound.Tests
{
    public sealed class RoomMissionStartZoneTests
    {
        [Test]
        public void Configure_CoversTheRoomFloorButLeavesTheDoorForExit()
        {
            var route = new GameObject("Mission Route");
            try
            {
                var oldPoint = route.AddComponent<CircleCollider2D>();
                oldPoint.isTrigger = true;
                var zone = route.AddComponent<RoomMissionStartZone>();
                var room = new Bounds(Vector3.zero, new Vector3(18f, 7f, 1f));
                var door = new Vector2(7.8f, -2.6f);

                zone.Configure(room, door);

                Assert.That(oldPoint.enabled, Is.False, "The tiny legacy activation point must not remain active.");
                Assert.That(zone.SegmentCount, Is.GreaterThanOrEqualTo(2));
                Assert.That(zone.InteractionArea, Is.GreaterThan(room.size.x * room.size.y * .7f));
                Assert.That(zone.Contains(room.center), Is.True);
                Assert.That(zone.Contains(door), Is.False);
                Assert.That(route.GetComponents<BoxCollider2D>().Where(item => item.enabled),
                    Has.All.Matches<BoxCollider2D>(item => !item.OverlapPoint(door)));
            }
            finally
            {
                Object.DestroyImmediate(route);
            }
        }
    }
}
