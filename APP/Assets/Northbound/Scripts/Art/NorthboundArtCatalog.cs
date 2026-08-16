using System;
using System.Collections.Generic;
using UnityEngine;

namespace Northbound.Art
{
    public enum Facing
    {
        North,
        South,
        East,
        West
    }

    [CreateAssetMenu(menuName = "Northbound/Art Catalog")]
    public sealed class NorthboundArtCatalog : ScriptableObject
    {
        // The wagon sheet is an irregular lineup, not five equal-width frames.
        private static readonly Rect ClosedWagonRect = new Rect(584f / 1774f, 333f / 887f, 378f / 1774f, 242f / 887f);
        private static readonly Rect OpenWagonRect = new Rect(1364f / 1774f, 332f / 887f, 368f / 1774f, 242f / 887f);

        [Serializable]
        public sealed class CharacterFrameLayout
        {
            public int columns = 4;
            public int rows = 2;
            public int idleRow = 1;
            public int walkRow = 0;
            public int northColumn = 1;
            public int southColumn = 0;
            public int eastColumn = 2;
            public int westColumn = 3;

            public int Column(Facing facing) => facing switch
            {
                Facing.North => northColumn,
                Facing.South => southColumn,
                Facing.East => eastColumn,
                Facing.West => westColumn,
                _ => southColumn
            };
        }

        [Serializable]
        public sealed class CharacterSheet
        {
            public string id;
            public Texture2D texture;
            public Color keyColor = Color.magenta;
            public CharacterFrameLayout layout = new CharacterFrameLayout();
        }

        [Serializable]
        public sealed class NamedTexture
        {
            public string id;
            public Texture2D texture;
            public Color keyColor = Color.clear;
        }

        [SerializeField] private CharacterSheet[] characters = Array.Empty<CharacterSheet>();
        [SerializeField] private NamedTexture[] props = Array.Empty<NamedTexture>();
        [SerializeField] private NamedTexture[] environments = Array.Empty<NamedTexture>();

        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public Sprite Character(string id, Facing facing, bool walking)
        {
            var sheet = Array.Find(characters, candidate => candidate != null && candidate.id == id);
            if (sheet?.texture == null) return null;
            var layout = sheet.layout ?? new CharacterFrameLayout();
            var column = layout.Column(facing);
            var row = walking ? layout.walkRow : layout.idleRow;
            return CachedSprite($"character:{id}:{facing}:{walking}", sheet.texture, new Rect(
                column * sheet.texture.width / (float)layout.columns,
                row * sheet.texture.height / (float)layout.rows,
                sheet.texture.width / (float)layout.columns,
                sheet.texture.height / (float)layout.rows), new Vector2(.5f, .18f));
        }

        public Sprite Prop(string id)
        {
            var texture = Array.Find(props, candidate => candidate != null && candidate.id == id)?.texture;
            return texture == null ? null : CachedSprite($"prop:{id}", texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .18f));
        }

        public Sprite StationWagon(bool openTrunk = false)
        {
            var texture = Array.Find(props, candidate => candidate != null && candidate.id == "station_wagon")?.texture;
            if (texture == null) return null;
            var normalized = openTrunk ? OpenWagonRect : ClosedWagonRect;
            return CachedSprite($"station_wagon:{openTrunk}", texture, new Rect(
                normalized.x * texture.width,
                normalized.y * texture.height,
                normalized.width * texture.width,
                normalized.height * texture.height), new Vector2(.5f, .18f));
        }

        public Sprite QuestProp(int index)
        {
            var texture = Array.Find(props, candidate => candidate != null && candidate.id == "quest_props")?.texture;
            if (texture == null || index < 0 || index > 15) return null;
            var column = index % 4;
            var row = 3 - index / 4;
            return CachedSprite($"quest_prop:{index}", texture, new Rect(
                column * texture.width / 4f, row * texture.height / 4f, texture.width / 4f, texture.height / 4f), new Vector2(.5f, .18f));
        }

        public Sprite Environment(string id)
        {
            var texture = Array.Find(environments, candidate => candidate != null && candidate.id == id)?.texture;
            // Environment plates are positioned by their visual center. Reusing
            // the character foot pivot shifts the entire painted room upward and
            // leaves the lower third of the camera as unreachable empty space.
            return texture == null ? null : CachedSprite($"environment:{id}", texture,
                new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .5f));
        }

        public Color CharacterKeyColor(string id)
        {
            return Array.Find(characters, candidate => candidate != null && candidate.id == id)?.keyColor ?? Color.clear;
        }

        public Color PropKeyColor(string id)
        {
            return Array.Find(props, candidate => candidate != null && candidate.id == id)?.keyColor ?? Color.clear;
        }

        public void Configure(CharacterSheet[] characterSheets, NamedTexture[] propTextures, NamedTexture[] environmentTextures)
        {
            characters = characterSheets ?? Array.Empty<CharacterSheet>();
            props = propTextures ?? Array.Empty<NamedTexture>();
            environments = environmentTextures ?? Array.Empty<NamedTexture>();
            spriteCache.Clear();
        }

        private Sprite CachedSprite(string key, Texture2D texture, Rect rect, Vector2 pivot)
        {
            if (spriteCache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            sprite = Sprite.Create(texture, rect, pivot, rect.width);
            sprite.name = key;
            spriteCache[key] = sprite;
            return sprite;
        }
    }
}
