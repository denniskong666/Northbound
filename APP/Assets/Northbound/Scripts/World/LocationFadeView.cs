using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Northbound.World
{
    public sealed class LocationFadeView : MonoBehaviour
    {
        private CanvasGroup group;

        public static LocationFadeView Create()
        {
            var root = new GameObject("Location Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
            var image = root.GetComponent<Image>(); image.color = new Color(.025f, .03f, .04f, 1f); image.raycastTarget = false;
            var group = root.GetComponent<CanvasGroup>(); group.alpha = 0f; group.blocksRaycasts = false;
            var view = root.AddComponent<LocationFadeView>(); view.group = group;
            return view;
        }

        public IEnumerator Fade(float from, float to, float duration)
        {
            if (group == null) yield break;
            if (duration <= 0f) { group.alpha = to; yield break; }
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            group.alpha = to;
        }
    }
}
