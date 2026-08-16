using UnityEngine;
using UnityEngine.UI;

namespace Northbound.UI
{
    /// <summary>Shared accessibility presentation for dialogue and timed cinematic subtitles.</summary>
    public static class SubtitleView
    {
        public static void Apply(Text label, Image background, SettingsModel settings)
        {
            if (label == null) return;
            var model = settings ?? new SettingsModel();
            label.fontSize = Mathf.RoundToInt(32f * model.SubtitleScale);
            if (background != null)
            {
                var color = background.color;
                color.a = model.SubtitleBackgroundOpacity;
                background.color = color;
            }
        }
    }
}
