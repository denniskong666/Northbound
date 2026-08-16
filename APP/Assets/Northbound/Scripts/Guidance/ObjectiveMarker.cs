using UnityEngine;
using Northbound.Content;
using Northbound.Core;

namespace Northbound.Guidance
{
    public enum MarkerKind { Required, Optional }

    public sealed class ObjectiveMarker : MonoBehaviour
    {
        private GameObject visual;
        private GameObject objectiveOutline;
        private Vector3 markerBaseScale;
        private float phase;

        public bool IsHighlighted => visual != null && visual.activeSelf;
        public bool HasObjectiveOutline => objectiveOutline != null;
        public bool ObjectiveOutlineVisible => objectiveOutline != null && objectiveOutline.activeSelf;

        public void Configure(MarkerKind kind)
        {
            if (visual != null) return;
            var isPhysicalObjective = GetComponent<NarrativeObjectiveTrigger>() != null;
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = kind == MarkerKind.Required ? new Color(1f, .72f, .12f) : new Color(.82f, .9f, 1f);
            visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = kind == MarkerKind.Required ? "Required Objective Star" : "Optional Conversation Marker";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(0f, isPhysicalObjective ? 1.55f : 1.25f, -0.2f);
            markerBaseScale = kind == MarkerKind.Required ? new Vector3(.56f, .56f, 1f) : new Vector3(.25f, .25f, 1f);
            visual.transform.localScale = markerBaseScale;
            if (kind == MarkerKind.Required) visual.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Destroy(visual.GetComponent<Collider>());
            var markerRenderer = visual.GetComponent<MeshRenderer>();
            markerRenderer.sharedMaterial = material;
            markerRenderer.sortingOrder = 90;
            if (kind == MarkerKind.Required && isPhysicalObjective) CreateObjectiveOutline(material);
            visual.SetActive(false);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (visual != null) visual.SetActive(highlighted);
            if (objectiveOutline != null) objectiveOutline.SetActive(highlighted);
        }

        private void Update()
        {
            if (visual == null || !visual.activeSelf) return;
            phase += Time.unscaledDeltaTime * 3.5f;
            var reducedMotion = GameBootstrap.Instance?.Settings?.ReducedMotion == true;
            var motion = reducedMotion ? 0f : Mathf.Sin(phase);
            var baseHeight = objectiveOutline != null ? 1.55f : 1.25f;
            visual.transform.localPosition = new Vector3(0f, baseHeight + motion * .12f, -.2f);
            visual.transform.localScale = markerBaseScale * (1f + motion * .08f);
            if (objectiveOutline != null) objectiveOutline.transform.localScale = Vector3.one * (1f + motion * .035f);
        }

        private void CreateObjectiveOutline(Material material)
        {
            var bounds = ResolveVisualBounds();
            var width = Mathf.Clamp(bounds.size.x + .38f, 1.15f, 2.8f);
            var height = Mathf.Clamp(bounds.size.y + .38f, 1.15f, 2.8f);
            const float thickness = .075f;

            objectiveOutline = new GameObject("Gold Objective Outline");
            objectiveOutline.transform.SetParent(transform, false);
            objectiveOutline.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, -.16f);
            CreateBar("Outline Top", new Vector2(0f, height * .5f), new Vector2(width, thickness));
            CreateBar("Outline Bottom", new Vector2(0f, -height * .5f), new Vector2(width, thickness));
            CreateBar("Outline Left", new Vector2(-width * .5f, 0f), new Vector2(thickness, height));
            CreateBar("Outline Right", new Vector2(width * .5f, 0f), new Vector2(thickness, height));
            objectiveOutline.SetActive(false);

            void CreateBar(string barName, Vector2 position, Vector2 size)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bar.name = barName;
                bar.transform.SetParent(objectiveOutline.transform, false);
                bar.transform.localPosition = position;
                bar.transform.localScale = new Vector3(size.x, size.y, 1f);
                Destroy(bar.GetComponent<Collider>());
                var renderer = bar.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 88;
            }
        }

        private Bounds ResolveVisualBounds()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0) return new Bounds(new Vector3(0f, .25f, 0f), Vector3.one);
            var hasBounds = false;
            var result = new Bounds();
            foreach (var renderer in renderers)
            {
                if (renderer.sprite == null) continue;
                var worldBounds = renderer.bounds;
                var localMin = transform.InverseTransformPoint(worldBounds.min);
                var localMax = transform.InverseTransformPoint(worldBounds.max);
                var localBounds = new Bounds((localMin + localMax) * .5f, localMax - localMin);
                if (!hasBounds)
                {
                    result = localBounds;
                    hasBounds = true;
                }
                else
                {
                    result.Encapsulate(localBounds.min);
                    result.Encapsulate(localBounds.max);
                }
            }
            return hasBounds ? result : new Bounds(new Vector3(0f, .25f, 0f), Vector3.one);
        }
    }
}
