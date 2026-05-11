# ADR-001: Quality of Life and Polish Features

| Field       | Value                    |
|-------------|--------------------------|
| Status      | **Proposed**             |
| Date        | 2026-05-12               |
| Author      | AI Agent                 |
| GDD Section | TBD                      |

## Context

We need to implement several features to improve the Quality of Life and visual polish of the game. These include:
1. **ToxicWave:** The instantiation logic for the prefab currently has errors and needs visual improvement.
2. **Inventory Quick Transfer:** Lack of quick-transfer for items between the main inventory and quick-slots (hotslots).
3. **Interaction Visuals:** Lack of visual feedback (HCI Aura) for interactables, such as the Left 4 Dead-style polygon aura.
4. **Item Dropping Size:** Dropped items do not reflect their rarity size visually; the dropping logic uses the default prefab scale instead of sizing it to match the rarity.

## Decision

We decided to implement the following architectural solutions:

1. **ToxicWave Instantiation:** Refactor `ToxicWaveManager` to ensure the wave prefab is handled robustly via the NetworkManager's `NetworkPrefabs` list, mitigating instantiation errors. Add visual polish hooks (e.g., PostProcessing or Particle Systems) for enhanced game feel.
2. **RMB Quick Transfer:** Extend `InventoryItem.OnPointerClick` to intercept Right Mouse Button (RMB) events. When triggered, it will invoke `InventoryManager.QuickTransfer()`. Items in Quick Slots will move to the first free Main Slot, and items in Main Slots will move to the first free Quick Slot, bypassing manual drag-and-drop.
3. **HCI Aura (L4D-Style Outline):** Implement an `InteractionHighlight` component (or leverage an Outline Shader/Renderer Feature) that attaches to `ModularInteractable`. The highlight will activate during `OnFocusEnter` and deactivate during `OnFocusExit` events from the interaction raycaster.
4. **Dropped Item Sizing:** Since `ItemInstanceData` does not store `ItemSizeRarity` to save network bandwidth, we will infer the `ItemSizeRarity` from the item's `weightKg` property within `PlayerDropItem.RequestSpawnItemServerRpc`. Using a reverse-lookup in `WeightedRandomUtility`, we will determine the size and apply `WeightedRandomUtility.GetScaleMultiplier` to the dropped prefab's local scale.

## Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| **Store `ItemSizeRarity` in `ItemInstanceData`** | Simpler logic during the drop sequence. | Increases network payload size; redundant since size correlates strictly with weight. |
| **In-scene ToxicWave vs Dynamic Spawning** | Eliminates runtime instantiation errors completely. | Doesn't scale well for dynamic map events; harder to control timing. |

## Consequences

### Positive
- Consistent item scales in the world depending on rarity/weight.
- Faster and more intuitive inventory management for the player.
- Better visual feedback for interactables, improving UX.
- Stable ToxicWave event execution.

### Negative / Trade-offs
- Outline shaders and Renderer Features can incur a minor performance cost depending on their implementation.
- Inferring size from weight restricts design freedom (e.g., you cannot have a very light but `SuperHuge` item).

### Migration
- Existing item weights will immediately dictate their dropped sizes, requiring no data migration.
- Existing `ModularInteractable` prefabs will need to be updated to include the new `InteractionHighlight` visual logic.

## Related
- Scripts affected: `InventoryManager.cs`, `InventoryItem.cs`, `PlayerDropItem.cs`, `WeightedRandomUtility.cs`, `ToxicWaveManager.cs`, `ModularInteractable.cs`
