using System.Collections;
using Northbound.Art;
using Northbound.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Northbound.Tests
{
    public sealed class CharacterVisualPlayModeTests
    {
        [UnityTest]
        public IEnumerator CharacterVisual_UsesFourDirectionSpritesAndNeverShowsTheKeyBackground()
        {
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");
            var host = new GameObject("Jamie visual test");
            var body = host.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            var visual = host.AddComponent<TopDownCharacterVisual>();

            visual.Configure("jamie", catalog);
            yield return null;
            var southIdle = visual.CurrentSprite;
            Assert.That(southIdle, Is.EqualTo(catalog.Character("jamie", Facing.South, false)));
            Assert.That(visual.CharacterRenderer.material.HasProperty("_KeyColor"), Is.True);
            Assert.That(visual.CharacterRenderer.material.GetColor("_KeyColor"), Is.EqualTo(Color.magenta));
            Assert.That(visual.EstimatedVisibleHeight, Is.EqualTo(TopDownCharacterVisual.StandardVisibleHeight).Within(.01f));

            var configuredScale = visual.CharacterRenderer.transform.localScale;
            visual.Configure("jamie", catalog);
            Assert.That(visual.CharacterRenderer.transform.localScale, Is.EqualTo(configuredScale),
                "Reconfiguring a visual must not multiply its scale.");

            body.linearVelocity = Vector2.right;
            yield return null;
            Assert.That(visual.CurrentFacing, Is.EqualTo(Facing.East));
            Assert.That(visual.CurrentSprite, Is.EqualTo(catalog.Character("jamie", Facing.East, true)));

            Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator EveryCharacterSheet_NormalizesToOneReadableWorldHeight()
        {
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");

            foreach (var id in new[] { "jamie", "elias", "maya", "noah", "leo" })
            {
                var host = new GameObject($"{id} scale test");
                var visual = host.AddComponent<TopDownCharacterVisual>();
                visual.Configure(id, catalog);
                yield return null;

                Assert.That(visual.EstimatedVisibleHeight,
                    Is.EqualTo(TopDownCharacterVisual.StandardVisibleHeight).Within(.01f), id);
                Assert.That(visual.CharacterRenderer.transform.parent, Is.EqualTo(host.transform), id);
                Assert.That(host.transform.localScale, Is.EqualTo(Vector3.one),
                    $"{id} physics and anchor root must remain unscaled.");
                Object.Destroy(host);
            }
        }

        [UnityTest]
        public IEnumerator CharacterVisual_ScriptedNpcMovementUsesEveryDirectionWithoutRigidbody()
        {
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");
            var host = new GameObject("Maya scripted visual test");
            var visual = host.AddComponent<TopDownCharacterVisual>();
            visual.Configure("maya", catalog);

            foreach (var sample in new[]
            {
                (Vector2.up, Facing.North),
                (Vector2.down, Facing.South),
                (Vector2.right, Facing.East),
                (Vector2.left, Facing.West)
            })
            {
                visual.SetScriptedVelocity(sample.Item1);
                yield return null;
                Assert.That(visual.CurrentFacing, Is.EqualTo(sample.Item2));
                Assert.That(visual.CurrentSprite, Is.EqualTo(catalog.Character("maya", sample.Item2, true)));
            }

            visual.SetScriptedVelocity(Vector2.zero);
            yield return null;
            Assert.That(visual.CurrentSprite, Is.EqualTo(catalog.Character("maya", Facing.West, false)));
            Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator JamieActualMotorInput_SelectsEveryWalkingDirectionBeforePhysicsVelocityIsReported()
        {
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");
            var host = new GameObject("Jamie motor visual test");
            host.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var motor = host.AddComponent<PlayerMotor>();
            var visual = host.AddComponent<TopDownCharacterVisual>();
            visual.Configure("jamie", catalog);

            foreach (var sample in new[]
            {
                (Vector2.up, Facing.North),
                (Vector2.left, Facing.West),
                (Vector2.down, Facing.South),
                (Vector2.right, Facing.East)
            })
            {
                motor.SetMoveInput(sample.Item1);
                yield return null;
                Assert.That(visual.CurrentFacing, Is.EqualTo(sample.Item2));
                Assert.That(visual.CurrentSprite, Is.EqualTo(catalog.Character("jamie", sample.Item2, true)));
            }

            motor.SetMoveInput(Vector2.zero);
            yield return null;
            Assert.That(visual.CurrentSprite, Is.EqualTo(catalog.Character("jamie", Facing.East, false)));
            Object.Destroy(host);
        }
    }
}
