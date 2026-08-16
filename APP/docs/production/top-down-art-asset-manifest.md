# Top-down playable art manifest

This playable pass uses an original, hand-painted 2D/2.5D top-down direction: readable silhouettes, warm evening interiors, cool Greybridge streets, and restrained texture. It does not copy any third-party game's assets or interface.

## Runtime assets

- Five current adult character sprite sheets: `Jamie`, `Elias`, `Maya`, `Noah`, and `Leo` in `Assets/Northbound/Art/Characters/`.
- Location plates: garage, diner, rooftop, and street in `Assets/Northbound/Art/Environment/`.
- Blue station wagon and the sixteen quest/carry-item sprites in `Assets/Northbound/Art/Props/`.

The sheets were generated with the built-in image generator from the approved adult model references and location references, then imported through `NorthboundArtAssetSeeder`. Character and wagon source sheets use a chroma-key material at runtime; authored quest props retain their source alpha. `NorthboundArtCatalog` provides every runtime slice, while `GreybridgeArtBuilder` places the locations, car, and visible objective props without changing the existing colliders or mission logic.

## Replacement boundary

These are runtime-ready proxy art assets, not a claim of final character animation or hand-painted production art. A later production-art pass can replace a PNG while preserving the catalog ID and the existing gameplay trigger/object name.
