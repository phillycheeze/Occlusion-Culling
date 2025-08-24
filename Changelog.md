# 0.9.0 
* Refactor RadialHeightMap calculations into three burst jobs, parallelized
* Perform candidate collection before heightmap, then re-use much smaller candidates for Iterate()
* Fix system order phase for same-frame culling
* Refactor temp memory allocations when changing component data
* Refactor interestion utility return types for better performance

# 0.8.0
* Use NormalLayers boundmasks filter to reduce number of intersect() tests against the search tree (small performance increase)
* Fix testing true geometry bounds against each occluder before first checking the occluder's size (large performance increase)
* Reduce max occluders max to 1, since the boundary point box is now 0.5m x 0.5m (small performance increase)
* Switch expensive EntityManager api calls to ComponentLookup refs instead (small performance increase)
* Only run culling system every other frame (moderate performance increase, but needs testing)
* Increase persistant cache to reduce auto-adjust rewrite chance (small performance increase)
* Fix TempJob allocators
* Remove log lines that could write every frame (moderate performance increase)
* Add enable/disable keybinding and some settings options
