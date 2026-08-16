using UnityEngine;

namespace Northbound.World
{
    public sealed class GreybridgeNpcAnchor : MonoBehaviour
    {
        [SerializeField] private string characterId;
        [SerializeField] private string locationId;

        public string CharacterId => characterId;
        public string LocationId => locationId;

        public void Configure(string character, string location)
        {
            characterId = character;
            locationId = location;
        }
    }
}
