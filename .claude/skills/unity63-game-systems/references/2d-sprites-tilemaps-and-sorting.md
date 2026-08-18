# 2D Sprites, Tilemaps, Sorting, Animation, and Physics

## Scope

Covers Sprite assets/renderers, atlases, sorting layers/groups, Tilemaps, 2D cameras and lighting boundaries, 2D animation, and Physics2D integration.

Read this reference when the request involves:

- building a 2D game or mixed 2D/3D scene
- using sprites, sprite atlases, tilemaps, sorting, or 2D animation
- debugging 2D rendering order, collider generation, or batching

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **2D development**
- **Sprites**
- **Tilemaps**
- **sorting**
- **2D animation and physics**
- **SpriteRenderer**
- **SortingGroup**
- **Tilemap**
- **SpriteAtlas**
- **Physics2D**

## Verified Unity 6.3 facts

- Unity’s 2D development documentation covers sprites, sprite import/editing, SpriteRenderer, atlases, Tilemaps, 2D animation, 2D physics, and 2D rendering workflows. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Unity2D.html))
- SpriteRenderer displays a Sprite and participates in sorting by layer/order plus additional renderer/pipeline rules. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SpriteRenderer.html))
- SortingLayer and order in layer organize renderer order; SortingGroup makes a renderer hierarchy sort as one group. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.SortingGroup.html))
- Tilemap stores tiles on a Grid and exposes cell/world conversion, tile data, refresh, bounds, and bulk operations. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.Tilemap.html))
- TilemapCollider2D generates collider shapes from tiles and CompositeCollider2D can combine eligible collider geometry. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.TilemapCollider2D.html))
- SpriteAtlas packs sprites for runtime texture/batching efficiency and can be loaded/queried through documented APIs. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/U2D.SpriteAtlas.html))
- 2D physics is a separate simulation/API family from 3D physics. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/2d-physics/2d-physics.html))
- Unity 2D animation workflows can deform sprites with bones/skin and integrate clips/Animator, subject to the installed 2D packages. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Unity2D.html))
- 2D Animation 13.0 is the Unity 6000.3-compatible package line for rigging and animating 2D characters, and its documentation describes integration with PSD Importer. ([official source](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/index.html))
- 2D PSD Importer 12.0 imports layered Photoshop PSB content and can generate Sprite-based Prefabs; its documentation lists 12.x as the Unity 6000.3-compatible package line. ([official source](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@12.0/manual/index.html))
- 2D Tilemap Extras 6.0 supplies optional reusable editor brushes and scriptable tile types such as Rule Tile; these are package features rather than built-in Tilemap guarantees. ([official source](https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@6.0/manual/index.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Choose a consistent units-per-pixel and camera scaling policy early; art import, physics scale, movement speed, and pixel-perfect behavior depend on it.
- **Interpretation:** Use sorting layers for semantic render groups and order-in-layer for local ordering. Add SortingGroup when a multi-renderer character/effect must remain internally coherent.
- **Interpretation:** Batch Tilemap changes with block/bulk APIs and refresh only affected regions; frequent per-cell writes can drive collider/mesh rebuilds.
- **Interpretation:** Separate visual Tilemaps from collision/navigation metadata when different update, baking, or runtime policies apply.
- **Interpretation:** Use SpriteAtlas boundaries based on co-usage and load lifetime, not merely folder location; an atlas can increase memory residency if unrelated sprites are packed together.
- **Interpretation:** Keep 2D and 3D physics interactions explicitly separate, even in a scene that uses a 3D camera or effects.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Single sprite object | SpriteRenderer | Direct sprite/material/sorting control. |
| Grid-authored level | Grid + Tilemap | Cell/world mapping and tile asset reuse. |
| Multi-renderer character that must sort together | SortingGroup | Preserves internal hierarchy order as one external unit. |
| Many sprites loaded together | Purposeful SpriteAtlas | Potential batching/texture efficiency with shared lifetime. |

## Risks and review checks

- Do not mix inconsistent Pixels Per Unit across content without an intentional scale policy.
- Do not use Z position, sorting layer, sorting order, and transparent sort settings simultaneously without documenting which owns order.
- Do not rebuild large Tilemaps one cell at a time in a hot frame when a block operation is available.
- Do not expect Collider2D to interact with a 3D Rigidbody/Collider.
- Do not pack unrelated lifetime groups into one large atlas without measuring memory residency.
- Do not mutate shared Sprite/Tile assets when only one runtime instance should change.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.SpriteRenderer.sprite`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SpriteRenderer-sprite.html) | `public Sprite sprite { get; set; }` | Runtime | Sprite displayed by a SpriteRenderer. |
| [`UnityEngine.Renderer.sortingLayerID`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer-sortingLayerID.html) | `public int sortingLayerID { get; set; }` | Runtime | Sorting layer identifier for eligible renderers. |
| [`UnityEngine.Renderer.sortingOrder`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer-sortingOrder.html) | `public int sortingOrder { get; set; }` | Runtime | Order within the sorting layer. |
| [`UnityEngine.Tilemaps.Tilemap.SetTile`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.Tilemap.SetTile.html) | `public void SetTile(Vector3Int position, TileBase tile)` | Runtime | Sets one cell tile. |
| [`UnityEngine.Tilemaps.Tilemap.SetTilesBlock`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.Tilemap.SetTilesBlock.html) | `public void SetTilesBlock(BoundsInt position, TileBase[] tileArray)` | Runtime | Sets a contiguous block from caller tile data. |
| [`UnityEngine.Tilemaps.Tilemap.WorldToCell`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.Tilemap.WorldToCell.html) | `public Vector3Int WorldToCell(Vector3 worldPosition)` | Runtime | Converts world position to grid cell coordinates. |
| [`UnityEngine.U2D.SpriteAtlas.GetSprite`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/U2D.SpriteAtlas.GetSprite.html) | `public Sprite GetSprite(string name)` | Runtime | Returns a copy of a named packed sprite when present. |

### Package prerequisites

- `com.unity.2d.animation@13.0` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.2d.psdimporter@12.0` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@12.0/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.2d.tilemap.extras@6.0` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@6.0/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Unity2D — https://docs.unity3d.com/6000.3/Documentation/Manual/Unity2D.html
- 2D Physics — https://docs.unity3d.com/6000.3/Documentation/Manual/2d-physics/2d-physics.html
- Sprite Renderer — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SpriteRenderer.html
- Sprite Renderer Sprite — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SpriteRenderer-sprite.html
- Rendering.Sorting Group — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.SortingGroup.html
- Renderer Sorting Layer Id — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer-sortingLayerID.html
- Renderer Sorting Order — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer-sortingOrder.html
- Tilemaps.Tilemap — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.Tilemap.html
- Tilemaps.Tilemap.Set Tile — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.Tilemap.SetTile.html
- Tilemaps.Tilemap.Set Tiles Block — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.Tilemap.SetTilesBlock.html
- Tilemaps.Tilemap.World To Cell — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.Tilemap.WorldToCell.html
- Tilemaps.Tilemap Collider2D — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.TilemapCollider2D.html
- U2D.Sprite Atlas — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/U2D.SpriteAtlas.html
- U2D.Sprite Atlas.Get Sprite — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/U2D.SpriteAtlas.GetSprite.html
- Com.Unity.2D.Animation@13.0 — https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/index.html
- Com.Unity.2D.Psdimporter@12.0 — https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@12.0/manual/index.html
- Com.Unity.2D.Tilemap.Extras@6.0 — https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@6.0/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
