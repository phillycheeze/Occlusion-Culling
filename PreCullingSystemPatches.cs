using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Mathematics;
using HarmonyLib;
using Game.Rendering;
using Unity.Entities;
using UnityEngine.Rendering;

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

    // TODO: patch minLod distance-based culling calculation but keeping mesh choice logic intact
    //[HarmonyPatch(typeof(PreCullingSystem), "OnUpdate")]
    //static class PreCullingLODScalePatch
    //{
    //    static readonly MethodInfo Orig = AccessTools.Method(
    //        typeof(RenderingUtils),
    //        nameof(RenderingUtils.CalculateLodParameters),
    //        new[] { typeof(float), typeof(LODParameters) }
    //    );

    //    static readonly MethodInfo Replacement = AccessTools.Method(
    //        typeof(PreCullingLODScalePatch),
    //        nameof(ScaleLodParameters)
    //    );

    //    [HarmonyTranspiler]
    //    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        foreach (var instr in instructions)
    //        {
    //            if (instr.opcode == OpCodes.Call && instr.operand as MethodInfo == Orig)
    //                instr.operand = Replacement;
    //            yield return instr;
    //        }
    //    }

    //    public static float4 ScaleLodParameters(float lodFactor, LODParameters p)
    //    {
    //        float4 baseParams = RenderingUtils.CalculateLodParameters(lodFactor, p);
    //        // pull scale (1.0 = default)
    //        float userScale = 0.5f; //Put behind user setting
    //        // shorten cull-distance by dividing x, then recompute y = 1/(x*x)
    //        baseParams.x /= userScale;
    //        baseParams.y = 1f / (baseParams.x * baseParams.x);
    //        return baseParams;
    //    }
    //}
}
