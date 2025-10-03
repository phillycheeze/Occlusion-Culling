using Game.Rendering;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace OcclusionCulling
{
    [HarmonyPatch(typeof(PreCullingSystem), "GetCullingQuery")]
    static class Patch_GetCullingQuery
    {
        static void Postfix(PreCullingSystem __instance, ref EntityQuery __result, int flags, Dictionary<int, EntityQuery> ___m_CullingQueries)
        {
            var desc = __result.GetEntityQueryDescs()[0];
            if (!desc.Any.Contains(ComponentType.ReadOnly<OcclusionDirtyTag>()))
            {
                var any = new[] { ComponentType.ReadOnly<OcclusionDirtyTag>() };
                desc.Any = ComponentType.Combine(any, desc.Any);
                var query = __instance.EntityManager.CreateEntityQuery(desc);
                ___m_CullingQueries[flags] = query;
                __result = query;
            }
        }
    }

    //// WIP: testing impact of running culling job every other frame
    //[HarmonyPatch(typeof(PreCullingSystem), "OnUpdate")]
    //static class Patch_OnUpdate_Decimate
    //{
    //    private static bool s_run = true;

    //    static bool Prefix()
    //    {
    //        s_run = !s_run;
    //        return s_run;
    //    }
    //}

    // WIP: adjusting LOD distance thresholds
    [HarmonyPatch(typeof(PreCullingSystem), "OnUpdate")]
    static class Patch_OnUpdate_Lod
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var m = new CodeMatcher(instructions, generator);

            var utilsType = Type.GetType("Game.Rendering.RenderingUtils, Game", throwOnError: true);
            var lodParamsType = Type.GetType("UnityEngine.Rendering.LODParameters, UnityEngine.CoreModule", throwOnError: true);

            var calc = utilsType.GetMethod(
                "CalculateLodParameters",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(float), lodParamsType },
                modifiers: null
            );

            m.MatchStartForward(new CodeMatch(ci => ci.Calls(calc)))
                .ThrowIfInvalid("CalculateLodParameters call not found")
                .Advance(1) // AFTER the call (float4 on stack)
                .InsertAndAdvance(CodeInstruction.Call(typeof(MyModHooks), nameof(MyModHooks.AdjustLodParams)));

            return m.Instructions();
        }
    }

    public static class MyModHooks
    {
        public static float4 AdjustLodParams(float4 lod)
        {
            const float bias = 1.25f; // TODO: hook into Setting
            return lod * bias;
        }
    }

    // Use the below harmony snippet to print the underlying IL code references to help with transpiler matching
    //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //{
    //    var list = new List<CodeInstruction>(instructions);
    //    for (int i = 0; i < list.Count; i++)
    //    {
    //        var ci = list[i];
    //        if ((ci.opcode == OpCodes.Call || ci.opcode == OpCodes.Callvirt) && ci.operand is MethodInfo mi)
    //        {
    //            // Swap name
    //            if (mi.Name == "CalculateLodParameters")
    //            {
    //                var ps = string.Join(", ", mi.GetParameters().Select(p => $"{p.ParameterType.AssemblyQualifiedName}"));
    //                Mod.log.Info($"[CalcLod CALL @ {i}] callKind={(ci.opcode == OpCodes.Callvirt ? "callvirt" : "call")}");
    //                Mod.log.Info($"  DeclaringType: {mi.DeclaringType?.AssemblyQualifiedName}");
    //                Mod.log.Info($"  Method:        {mi.Name}");
    //                Mod.log.Info($"  ReturnType:    {mi.ReturnType.AssemblyQualifiedName}");
    //                Mod.log.Info($"  Params:        ({ps})");
    //                Mod.log.Info($"  Module:        {mi.Module.Name}");
    //                Mod.log.Info($"  MetadataToken: 0x{mi.MetadataToken:X8}");
    //                Mod.log.Info($"  IsStatic={mi.IsStatic}, IsVirtual={mi.IsVirtual}");
    //            }
    //        }
    //    }
    //    return list;
    //}
}
