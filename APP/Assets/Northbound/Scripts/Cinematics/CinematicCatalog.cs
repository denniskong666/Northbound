using System;
using UnityEngine;

namespace Northbound.Cinematics
{
    [CreateAssetMenu(menuName = "Northbound/Cinematic Catalog", fileName = "CinematicCatalog")]
    public sealed class CinematicCatalog : ScriptableObject
    {
        [SerializeField] private CinematicAsset[] all = Array.Empty<CinematicAsset>();

        public CinematicAsset[] All => all;

        public CinematicAsset Find(string id)
        {
            foreach (var cinematic in all)
            {
                if (cinematic != null && cinematic.id == id)
                {
                    return cinematic;
                }
            }

            return null;
        }
    }
}
