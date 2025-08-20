using Unity.Entities;

namespace OcclusionCulling
{
    // Enableable component ensures no structural changes between setting true/false, better for performance
    public struct OcclusionDirtyTag : IComponentData, IEnableableComponent { }
}
