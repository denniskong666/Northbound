using System;
using System.Linq;
using Northbound.Art;
using NUnit.Framework;
using UnityEngine;

namespace Northbound.Tests
{
    public sealed class ArtCatalogTests
    {
        [Test]
        public void Catalog_ContainsEveryAdultCharacterDirectionAndPrimaryLocation()
        {
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");

            Assert.That(catalog, Is.Not.Null, "Northbound requires a serialized top-down art catalog.");
            foreach (var characterId in new[] { "jamie", "elias", "maya", "noah", "leo" })
            {
                foreach (var facing in Enum.GetValues(typeof(Facing)).Cast<Facing>())
                {
                    Assert.That(catalog.Character(characterId, facing, false), Is.Not.Null, $"{characterId} requires a {facing} idle sprite.");
                    Assert.That(catalog.Character(characterId, facing, true), Is.Not.Null, $"{characterId} requires a {facing} walk sprite.");
                }
            }

            var locations = new[] { "exterior", "jamie_home", "vale_garage", "ruths_diner", "maya_studio", "noah_electronics", "rooftop_overlook" };
            foreach (var id in locations)
            {
                Assert.That(catalog.Environment(id), Is.Not.Null, $"{id} requires authored environment art.");
            }
            Assert.That(locations.Select(id => catalog.Environment(id).texture).Distinct().Count(), Is.EqualTo(locations.Length),
                "Every story location needs its own 3/4 top-down environment instead of aliasing the street image.");

            Assert.That(catalog.Prop("station_wagon"), Is.Not.Null, "The blue station wagon requires authored prop art.");
        }

        [Test]
        public void StationWagon_UsesTightAuthoredFramesInsteadOfEqualSheetSlices()
        {
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");
            var closed = catalog.StationWagon();
            var open = catalog.StationWagon(true);

            Assert.That(closed, Is.Not.Null);
            Assert.That(open, Is.Not.Null);
            Assert.That(closed.rect.width / closed.rect.height, Is.GreaterThan(1.4f),
                "A fixed one-fifth crop cuts two neighboring vehicles into detached pieces.");
            Assert.That(open.rect.width / open.rect.height, Is.GreaterThan(1.4f));
            Assert.That(closed.rect.Overlaps(open.rect), Is.False);
        }

        [Test]
        public void Catalog_MapsAuthoredFrontBackAndSideColumnsForAllFiveCharacters()
        {
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");

            foreach (var characterId in new[] { "jamie", "elias", "maya", "noah", "leo" })
            {
                var south = catalog.Character(characterId, Facing.South, false);
                var north = catalog.Character(characterId, Facing.North, false);
                var east = catalog.Character(characterId, Facing.East, false);
                var west = catalog.Character(characterId, Facing.West, false);

                Assert.That(south.rect.x, Is.EqualTo(0f).Within(.01f), $"{characterId} front/South frame is column 0.");
                Assert.That(north.rect.x, Is.EqualTo(south.texture.width / 4f).Within(.01f), $"{characterId} back/North frame is column 1.");
                Assert.That(east.rect.x, Is.EqualTo(south.texture.width / 2f).Within(.01f), $"{characterId} East frame is column 2.");
                Assert.That(west.rect.x, Is.EqualTo(south.texture.width * .75f).Within(.01f), $"{characterId} West frame is column 3.");
                Assert.That(new[] { south, north, east, west }.Distinct().Count(), Is.EqualTo(4));
            }
        }

        [Test]
        public void CharacterVisuals_NormalizeIrregularSheetsWithoutScalingInteractionRoots()
        {
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");

            foreach (var characterId in new[] { "jamie", "elias", "maya", "noah", "leo" })
            {
                var host = new GameObject($"{characterId} edit-mode visual");
                try
                {
                    var visual = host.AddComponent<TopDownCharacterVisual>();
                    visual.Configure(characterId, catalog);
                    Assert.That(visual.EstimatedVisibleHeight,
                        Is.EqualTo(TopDownCharacterVisual.StandardVisibleHeight).Within(.01f), characterId);
                    Assert.That(host.transform.localScale, Is.EqualTo(Vector3.one), characterId);
                    Assert.That(visual.CharacterRenderer.transform.localScale.y, Is.GreaterThan(1f), characterId);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }
            }

            Assert.That(TopDownCharacterVisual.StandardVisibleHeight, Is.GreaterThanOrEqualTo(2.3f));
        }
    }
}
