# Performance Tweaks (Beta)

This Cities Skylines 2 mod contains various tweaks that increase performance.

## GPU-based Adjustments

* **LOD Adjustments**: decrease the lod min distances on many objects in the game. These objects were chosen because they either have LOD thresholds that are too high OR the game did not include additional LOD meshes.
* **Shadow Patches**: the game currently doesn't use some of your Settings > Graphics > Shadow config when rendering shadow resolutions. This patches that and also allows you to increase shadow performance beyond the allowed settings.
* **Terrain Culling**: a system that removes objects from the rendering pipeline if they are obstructed by terrain. Enabling this increases CPU usage.

# Roadmap

* **GPU: Tree Age LOD Adjustments**: the minimum LOD thresholds are tied to the parent Tree mesh, not the age-specific rendered mesh. Stumps, young trees, and dead trees should be culled more aggressively due to their lower fidelity and vertex count.
* **GPU: Simple LOD Mesh Generator**: use some of the in-game logic for generating bounding boxes based on a mesh config and repurpose it for generating LOD mesh models for simple objects that don't currently have any: shipping containers, boulders, etc.
* **CPU: Taxi AI System Config**: modify the ResidentTickJob in the AI system to limit Taxi pathfinding flags, as well as allow the user to disable the entire TaxiAISystem if desired.
* **CPU: Lifepath and Chirp System Removal**: expose a setting to allow disabling the lifepath and chirp systems (maybe other optional systems as well) to make tiny cpu performance improvements.

## Credits
* phillycheeze - mod author
* krzychu124 - feedback, code sharing
* Also to Necko1996, elGendo87, Honu, and other for helping with alpha testing
