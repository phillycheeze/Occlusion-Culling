### Occlusion Culling – Terrain-Only Issues and Optimization Notes

Date: 2025-10-03

Scope: Terrain-based occlusion only (object occlusion intentionally disabled for now).

#### Potential issues / correctness risks
- Burst-incompatible access in job: `indices.ElementAt(index)` inside `CullBySectorJob`. Use indexed access `indices[index]`. LINQ-based `ElementAt` is not burstable and may cause runtime Burst compile errors or non-burst fallback.
- TempJob array not disposed: `indices = candidates.GetKeyArray(Allocator.TempJob)` is never disposed. Dispose with `indices.Dispose(handle)` (after scheduling) or mark with `[DeallocateOnJobCompletion]` in the job.
- ComponentLookup refresh: `m_DirtyComponentLookup` and `m_CullingInfoLookup` are created in `OnCreate` but not updated each frame. Call `m_DirtyComponentLookup.Update(this);` and `m_CullingInfoLookup.Update(this);` in `OnUpdate` before use to avoid safety exceptions/stale handles.
- Missing disposals (leaks): `m_visibleCandidates` and `m_Queue` are `Allocator.Persistent` but not disposed in `OnDestroy`. Dispose both.
- Incremental candidate update uses stale flags: In the `GetUpdatedData` path, the code checks `item.m_Data.m_Flags` (old) to decide removal instead of the new `c.m_Flags`. This can leave entries in an inconsistent state. Use the latest `c` and update/replace stored `m_Data`.
- Bounds staleness for existing entries: Existing `m_visibleCandidates` do not refresh bounds when objects move or LOD changes. Consider updating bounds for existing entries (e.g., re-`TryGet` from the search tree) at some cadence. If relying on a static tree, dynamic objects will be missing or stale.
- Potential `CullingInfo` missing: `GetRefRW<CullingInfo>` assumes the component exists on all entities being dirtied. If any do not, this will throw. Guard with `HasComponent<CullingInfo>(e)` or ensure population via an authoring/system.
- Early-outs and camera deltas: When early-returning, `m_LastCameraPos/Dir` are not updated, which can skew the next-frame movement check. This may be intentional, but be aware for edge cases (e.g., rapid toggling).
- Misleading debug metric: `CullByRadialMap` logs `queueCount` before the culling job enqueues results. It will often show 0. Log after completion if you rely on it.

#### Performance opportunities
- Skip object path resources: Since object occlusion is disabled, skip allocating `objectHeights` and skip scheduling `RadialMapForObjects` entirely. In `MergeRadialMaps`, if objects are disabled, just copy/alias `terrainHeights` to `heights` (or run a simple memcpy job) to avoid extra work.
- XX Remove unused per-frame allocations: `NativeQuadTree<Entity, QuadTreeBoundsXZ> candidates` in `OnUpdate` is created and disposed but unused. Remove it.
- Avoid config indirection in job: Replace the `NativeHashMap<int, CullingConfig> hm_Config` with a direct `CullingConfig` field on `CullBySectorJob`. This eliminates an allocation and per-iteration hashmap lookup.
- Reuse large buffers: The `heights`/`terrainHeights` arrays are sized `sectorCount * binCount` per frame. If config is stable, consider persistent allocations reused across frames, cleared by jobs.
- Gate logs: Hot-path logs (e.g., summary per frame) can be gated (sample every N frames) or compiled out for release to reduce string formatting and logging overhead.
- Bin mapping off-by-one: `bin = (int)(dist / distanceStep) - 1` risks underflow and misalignment with the center-sampled bins `(j + 0.5f)`. Prefer `int bin = math.clamp((int)math.floor((dist - 0.5f * distanceStep) / distanceStep), 0, config.binCount - 1);` for center-consistent mapping.
- Minimize trig recomputation: Within `CullBySectorJob`, `sectorAngleStep` is recomputed per entity. Precompute once and store in the job.

#### Suggested concrete fixes (terrain-only, high value)
1. In `CullBySectorJob`, replace `indices.ElementAt(index)` with `indices[index]`. Add `indices.Dispose(handle)` after scheduling (or use `[DeallocateOnJobCompletion]`).
2. In `OnUpdate`, call `m_DirtyComponentLookup.Update(this);` and `m_CullingInfoLookup.Update(this);` before using the lookups.
3. Dispose `m_visibleCandidates` and `m_Queue` in `OnDestroy`.
4. Fix incremental update logic: use `c.m_Flags` to decide add/remove, and update stored `m_Data` (and `m_Bounds` if needed) for existing entries.
5. If object occlusion remains disabled, remove `objectHeights` allocation and `RadialMapForObjects` scheduling; in merge, take `heights[i] = terrainHeights[i]` directly.
6. Remove the unused per-frame `NativeQuadTree` allocation in `OnUpdate`.
7. Pass `CullingConfig` directly into `CullBySectorJob` and drop the `hm_Config` hashmap.
8. Consider refreshing bounds for existing visible candidates periodically or only for those flagged as dirty/moved by engine systems, to prevent staleness without scanning all.


