using Game.Rendering;
using Game.Settings;
using HarmonyLib;
using Unity.Mathematics;

namespace PerformanceTweaks.Patches
{
    [HarmonyPatch(typeof(RenderingSystem), "GetShadowCullingData")]
    static class Patch_GetShadowCullingData
    {
		public static bool Enabled = true;
		public static bool DoubleYZ = false;

        static void Postfix(RenderingSystem __instance, ref float3 __result)
        {
			if (!Enabled)
				return;

            var shadows = SharedSettings.instance.graphics.GetQualitySetting<ShadowsQualitySettings>();
            __result.x = shadows.directionalShadowResolution;
			if (DoubleYZ)
			{
				__result.y = __result.y * 2;
				__result.z = __result.z * 2;
			}
        }
    }
}
