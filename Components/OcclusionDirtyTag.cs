using Unity.Entities;

namespace PerformanceTweaks
{
    // Enableable component ensures no structural changes between setting true/false, better for performance
    public struct OcclusionDirtyTag : IComponentData, IEnableableComponent { }
}
