using System;
using UnityEngine;

namespace Northbound.World
{
    [Serializable]
    public sealed class LocationDefinition
    {
        public string id;
        public GameObject root;
        public Transform spawn;
        public Bounds cameraBounds;
        public Bounds walkableBounds;
        public float cameraOrthographicSize;
        public string displayName;

        public LocationDefinition(string locationId, GameObject locationRoot, Transform locationSpawn, Bounds bounds, string name)
        {
            id = locationId; root = locationRoot; spawn = locationSpawn; cameraBounds = bounds; displayName = name;
            walkableBounds = bounds;
        }

        public LocationDefinition(string locationId, GameObject locationRoot, Transform locationSpawn, Bounds camera, Bounds walkable, float orthographicSize, string name)
        {
            id = locationId;
            root = locationRoot;
            spawn = locationSpawn;
            cameraBounds = camera;
            walkableBounds = walkable;
            cameraOrthographicSize = orthographicSize;
            displayName = name;
        }
    }
}
