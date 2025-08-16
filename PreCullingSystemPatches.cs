using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Game.Rendering;
using Unity.Entities;

namespace OcclusionCulling
{
    [HarmonyPatch(typeof(PreCullingSystem), "GetCullingQuery")]
    static class Patch_GetCullingQuery
    {
        static void Postfix(PreCullingSystem __instance, ref EntityQuery __result)
        {
            var desc = __result.GetEntityQueryDescs()[0];
            var any = new[] { ComponentType.ReadOnly<OcclusionDirtyTag>() };
            desc.Any = ComponentType.Combine(any, desc.Any);
            __result = __instance.EntityManager.CreateEntityQuery(desc);
        }
    }
}
