using UnityEngine;
using Northbound.Player;

namespace Northbound.Art
{
    public sealed class TopDownCharacterVisual : MonoBehaviour
    {
        public const float StandardVisibleHeight = 2.4f;
        private const int ShadowSortingOrder = 20;
        private const int CharacterSortingOrder = 30;

        private Rigidbody2D body;
        private PlayerMotor motor;
        private NorthboundArtCatalog catalog;
        private string characterId;
        private SpriteRenderer shadowRenderer;
        private Material chromaKeyInstance;
        private Vector2 scriptedVelocity;
        private bool usesScriptedVelocity;

        public Facing CurrentFacing { get; private set; } = Facing.South;
        public SpriteRenderer CharacterRenderer { get; private set; }
        public Sprite CurrentSprite => CharacterRenderer == null ? null : CharacterRenderer.sprite;
        public float EstimatedVisibleHeight => CharacterRenderer == null
            ? 0f
            : NativeVisibleHeight(characterId) * CharacterRenderer.transform.localScale.y;

        public void Configure(string id, NorthboundArtCatalog value)
        {
            characterId = id;
            catalog = value;
            body = GetComponent<Rigidbody2D>();
            motor = GetComponent<PlayerMotor>();
            CharacterRenderer = EnsureRenderer("Character Sprite", CharacterSortingOrder);
            shadowRenderer = EnsureRenderer("Character Shadow", ShadowSortingOrder);
            shadowRenderer.sprite = ShadowSprite();
            shadowRenderer.color = new Color(0f, 0f, 0f, .3f);
            shadowRenderer.transform.localPosition = new Vector3(0f, -.12f, .05f);
            shadowRenderer.transform.localScale = new Vector3(1.15f, .36f, 1f);

            var chromaKeyMaterial = Resources.Load<Material>("Northbound/ChromaKeySprite");
            if (chromaKeyMaterial != null)
            {
                DisposeChromaKeyInstance();
                chromaKeyInstance = new Material(chromaKeyMaterial);
                chromaKeyInstance.SetColor("_KeyColor", catalog.CharacterKeyColor(characterId));
                CharacterRenderer.sharedMaterial = chromaKeyInstance;
            }
            Refresh(Vector2.zero);
            ApplyStandardScale();
        }

        private void LateUpdate()
        {
            if (catalog != null)
            {
                Refresh(usesScriptedVelocity ? scriptedVelocity : motor != null ? motor.AppliedMoveInput : body != null ? body.linearVelocity : Vector2.zero);
            }
        }

        public void SetScriptedVelocity(Vector2 velocity)
        {
            usesScriptedVelocity = true;
            scriptedVelocity = velocity;
        }

        public void ClearScriptedVelocity()
        {
            usesScriptedVelocity = false;
            scriptedVelocity = Vector2.zero;
        }

        private void OnDestroy()
        {
            DisposeChromaKeyInstance();
        }

        private void Refresh(Vector2 velocity)
        {
            var walking = velocity.sqrMagnitude > .0025f;
            if (walking)
            {
                CurrentFacing = Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y)
                    ? velocity.x >= 0f ? Facing.East : Facing.West
                    : velocity.y >= 0f ? Facing.North : Facing.South;
            }
            CharacterRenderer.sprite = catalog.Character(characterId, CurrentFacing, walking);
        }

        private void ApplyStandardScale()
        {
            // The five generated sheets contain different amounts of keyed padding.
            // Scale only the visual child so physics, interaction and authored anchors stay stable.
            var scale = StandardVisibleHeight / NativeVisibleHeight(characterId);
            CharacterRenderer.transform.localPosition = Vector3.zero;
            CharacterRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static float NativeVisibleHeight(string id) => id switch
        {
            "jamie" => 1.05f,
            "elias" => 1.52f,
            "maya" => 1.47f,
            "noah" => 1.08f,
            "leo" => .98f,
            _ => 1f
        };

        private void DisposeChromaKeyInstance()
        {
            if (chromaKeyInstance == null) return;
            if (Application.isPlaying) Destroy(chromaKeyInstance);
            else DestroyImmediate(chromaKeyInstance);
            chromaKeyInstance = null;
        }

        private SpriteRenderer EnsureRenderer(string name, int sortingOrder)
        {
            var child = transform.Find(name);
            if (child == null)
            {
                var childObject = new GameObject(name);
                childObject.transform.SetParent(transform, false);
                child = childObject.transform;
            }
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Sprite ShadowSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(.5f, .5f), 1f);
        }
    }
}
