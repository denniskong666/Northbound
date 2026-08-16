using UnityEngine;

namespace Northbound.Art
{
    public sealed class GreybridgeArtBuilder : MonoBehaviour
    {
        private NorthboundArtCatalog catalog;

        public void Build(Transform mapRoot)
        {
            catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");
            if (catalog == null) return;
            CreatePlate(mapRoot, "Art Street", "exterior", new Vector2(-2f, 0f), new Vector2(56f, 31.5f), 0);
            CreatePlate(mapRoot, "Art Jamie Home", "jamie_home", new Vector2(-2f, 0f), new Vector2(24f, 13.5f), 3);
            CreatePlate(mapRoot, "Art Garage", "vale_garage", new Vector2(-20f, -4f), new Vector2(24f, 13.5f), 3);
            CreatePlate(mapRoot, "Art Diner", "ruths_diner", new Vector2(-7f, 3f), new Vector2(24f, 13.5f), 3);
            CreatePlate(mapRoot, "Art Rooftop", "rooftop_overlook", new Vector2(23f, 9f), new Vector2(24f, 13.5f), 3);
            CreatePlate(mapRoot, "Art Gallery", "maya_studio", new Vector2(13f, 5f), new Vector2(24f, 13.5f), 3);
            CreatePlate(mapRoot, "Art Electronics", "noah_electronics", new Vector2(10f, -2f), new Vector2(24f, 13.5f), 3);
        }

        public SpriteRenderer AttachQuestProp(Transform parent, int index)
        {
            var child = new GameObject("Quest Object Visual");
            child.transform.SetParent(parent, false);
            child.transform.localPosition = new Vector3(0f, .25f, -.03f);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = catalog == null ? null : catalog.QuestProp(index);
            renderer.sortingOrder = 35;
            return renderer;
        }

        private void CreatePlate(Transform parent, string name, string id, Vector2 position, Vector2 size, int order)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(position.x, position.y, .2f);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = catalog.Environment(id);
            renderer.sortingOrder = order;
            ScaleTo(renderer, size);
        }

        private static void ScaleTo(SpriteRenderer renderer, Vector2 size)
        {
            var bounds = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(size.x / bounds.x, size.y / bounds.y, 1f);
        }

        public static void ApplyKeyMaterial(SpriteRenderer renderer, Color keyColor)
        {
            if (keyColor.a <= 0f) return;
            var material = Resources.Load<Material>("Northbound/ChromaKeySprite");
            if (material == null) return;
            renderer.material = new Material(material);
            renderer.material.SetColor("_KeyColor", keyColor);
        }
    }
}
