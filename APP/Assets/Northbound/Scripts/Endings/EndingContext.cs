using System;

namespace Northbound.Endings
{
    [Serializable]
    public sealed class EndingContext
    {
        public EndingDirection Direction { get; }
        public string AssetId { get; }
        public string DialogueVariantId { get; }
        public string CarriedPropId { get; }
        public string LightingVariantId { get; }
        public string EndCard { get; }
        public string FriendId { get; }
        public string HistoryEchoId { get; }
        public string HistoryEchoText { get; }
        public string HistoryEchoTextChinese { get; }

        public EndingContext(
            EndingDirection direction,
            string assetId,
            string dialogueVariantId,
            string carriedPropId,
            string lightingVariantId,
            string endCard,
            string friendId = "",
            string historyEchoId = "",
            string historyEchoText = "",
            string historyEchoTextChinese = "")
        {
            Direction = direction;
            AssetId = assetId;
            DialogueVariantId = dialogueVariantId;
            CarriedPropId = carriedPropId;
            LightingVariantId = lightingVariantId;
            EndCard = endCard;
            FriendId = friendId ?? string.Empty;
            HistoryEchoId = historyEchoId ?? string.Empty;
            HistoryEchoText = historyEchoText ?? string.Empty;
            HistoryEchoTextChinese = historyEchoTextChinese ?? string.Empty;
        }
    }
}
