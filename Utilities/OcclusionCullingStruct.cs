using Game.Common;
using Game.Rendering;

namespace PerformanceTweaks.Utilities
{
    public struct OcclusionCullingStruct
    {
        public PreCullingData m_Data;

        public QuadTreeBoundsXZ m_Bounds;
    }
}
