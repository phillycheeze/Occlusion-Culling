# Better Culling / FPS Boost

A WIP mod to remove objects from the game's rendering pipeline if they aren't actually in view to the player.

## Remaining Tasks
  * Object Culling: the rectangular bounds encompasses an entire Prefab, but some parts don't take up the whole bounds
  * Settings: expose settings for setting ray steps, boundary padding, max chunks size, or distance settings
  * Adjustable LOD distance settings: the vanilla game's distance LOD settings are used to calculate when an object should be distanced-culled AND when to select mesh variant
	* Instead, allow higher-quality LOD meshes for longer distances but still keep the culling-threshold lower (can increase visual fidelity without lowering performance)
	* Several assets use unusually low minLod values (i.e. Cargo ship containers prefab) that cause high vertex rendering counts at very large distances
  * CPU-targeted optimizations:
	* Tweak cpu culling thresholds in vanilla system
