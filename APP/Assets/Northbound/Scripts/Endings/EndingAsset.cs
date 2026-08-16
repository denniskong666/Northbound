using UnityEngine;

namespace Northbound.Endings
{
    [CreateAssetMenu(menuName = "Northbound/Ending", fileName = "Ending")]
    public sealed class EndingAsset : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField, TextArea] private string endCard;

        public string Id => id;
        public string EndCard => endCard;
    }
}
