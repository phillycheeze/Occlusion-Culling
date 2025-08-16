using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Game.Rendering;
using Unity.Entities;

//namespace OcclusionCulling
//{
//    [HarmonyPatch(typeof(PreCullingSystem), "GetCullingQuery")]
//    static class Patch_GetCullingQuery
//    {
//        static void Postfix(PreCullingSystem __instance, ref EntityQuery __result)
//        {
//            var desc = __result.GetEntityQueryDescs()[0];
//            var any = new List<ComponentType>(desc.Any) { ComponentType.ReadOnly<OcclusionDirtyTag>() };
//            desc.Any = any.ToArray();
//            __result = __instance.EntityManager.CreateEntityQuery(desc);
//        }
//    }
//}
