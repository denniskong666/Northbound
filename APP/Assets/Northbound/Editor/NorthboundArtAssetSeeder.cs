using Northbound.Art;
using UnityEditor;
using UnityEngine;

namespace Northbound.Editor
{
    public static class NorthboundArtAssetSeeder
    {
        private const string CatalogPath = "Assets/Northbound/Resources/Northbound/NorthboundArtCatalog.asset";

        public static void Rebuild()
        {
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<NorthboundArtCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<NorthboundArtCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Configure(
                new[]
                {
                    Character("jamie", "Assets/Northbound/Art/Characters/jamie-sprite-sheet.png", Color.magenta),
                    Character("elias", "Assets/Northbound/Art/Characters/elias-sprite-sheet.png", Color.green),
                    Character("maya", "Assets/Northbound/Art/Characters/maya-sprite-sheet.png", Color.magenta),
                    Character("noah", "Assets/Northbound/Art/Characters/noah-sprite-sheet.png", Color.green),
                    Character("leo", "Assets/Northbound/Art/Characters/leo-sprite-sheet.png", Color.green)
                },
                new[]
                {
                    Named("station_wagon", "Assets/Northbound/Art/Props/station-wagon-sprite-sheet.png", Color.magenta),
                    Named("quest_props", "Assets/Northbound/Art/Props/quest-props-sprite-sheet.png", Color.clear)
                },
                new[]
                {
                    Named("exterior", "Assets/Northbound/Art/Environment/street-plate.png", Color.clear),
                    Named("jamie_home", "Assets/Northbound/Art/Environment/jamie-home.png", Color.clear),
                    Named("vale_garage", "Assets/Northbound/Art/Environment/garage-plate.png", Color.clear),
                    Named("ruths_diner", "Assets/Northbound/Art/Environment/diner-plate.png", Color.clear),
                    Named("maya_studio", "Assets/Northbound/Art/Environment/maya-studio.png", Color.clear),
                    Named("noah_electronics", "Assets/Northbound/Art/Environment/noah-electronics.png", Color.clear),
                    Named("rooftop_overlook", "Assets/Northbound/Art/Environment/rooftop-plate.png", Color.clear)
                });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static NorthboundArtCatalog.CharacterSheet Character(string id, string path, Color keyColor) => new NorthboundArtCatalog.CharacterSheet
        {
            id = id,
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            keyColor = keyColor
        };

        private static NorthboundArtCatalog.NamedTexture Named(string id, string path, Color keyColor) => new NorthboundArtCatalog.NamedTexture
        {
            id = id,
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            keyColor = keyColor
        };
    }
}
