# Better Culling

A WIP mod to remove objects from the game's rendering pipeline if they aren't actually in view to the player.

## Remaining Tasks
  * Performance Optimizations: place in burst-compiled job, earlier guards, possibly doing a cache-based multi-ray system (to remove O(n x m))
  * Object Culling: the rectangular bounds encompasses an entire Prefab, but some parts don't take up the whole bounds - culling still treats it as something visually there
  * Settings: expose settings for setting ray steps, boundary padding, max chunks size, or distance settings
	* 

## Roadmap
  * Adjustable LOD distance settings: the vanilla game's distance LOD settings are used to calculate when an object should be distanced-culled AND when to select mesh variant
	* Instead, allow higher-quality LOD meshes for longer distances but still keep the culling-threshold lower (can increase visual fidelity without lowering performance)
  * Multi-ray casting: instead of a single ray from the center of the camera fulcrum, generate many of them to increase the number of objects that can be culled
  * Override "Relative" culling logic: relative objects inherit the LOD settings of their parent. This is still needed for the vanilla fulcrum-based culling logic, but for distance-based culling we should use the object's own value