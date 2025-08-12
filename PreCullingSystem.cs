#region Assembly Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Effects;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class PreCullingSystem : GameSystemBase, IPostDeserialize
{
    [Flags]
    private enum QueryFlags
    {
        Unspawned = 1,
        Zones = 2
    }

    [BurstCompile]
    private struct TreeCullingJob1 : IJobParallelFor
    {
        [ReadOnly]
        public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

        [ReadOnly]
        public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

        [ReadOnly]
        public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_LaneSearchTree;

        [ReadOnly]
        public float4 m_LodParameters;

        [ReadOnly]
        public float4 m_PrevLodParameters;

        [ReadOnly]
        public float3 m_CameraPosition;

        [ReadOnly]
        public float3 m_PrevCameraPosition;

        [ReadOnly]
        public float3 m_CameraDirection;

        [ReadOnly]
        public float3 m_PrevCameraDirection;

        [ReadOnly]
        public BoundsMask m_VisibleMask;

        [ReadOnly]
        public BoundsMask m_PrevVisibleMask;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> m_NodeBuffer;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> m_SubDataBuffer;

        [NativeDisableContainerSafetyRestriction]
        public Writer<CullingAction> m_ActionQueue;

        public void Execute(int index)
        {
            //IL_0073: Unknown result type (might be due to invalid IL or missing references)
            //IL_0078: Unknown result type (might be due to invalid IL or missing references)
            TreeCullingIterator treeCullingIterator = default(TreeCullingIterator);
            treeCullingIterator.m_LodParameters = m_LodParameters;
            treeCullingIterator.m_PrevLodParameters = m_PrevLodParameters;
            treeCullingIterator.m_CameraPosition = m_CameraPosition;
            treeCullingIterator.m_PrevCameraPosition = m_PrevCameraPosition;
            treeCullingIterator.m_CameraDirection = m_CameraDirection;
            treeCullingIterator.m_PrevCameraDirection = m_PrevCameraDirection;
            treeCullingIterator.m_VisibleMask = m_VisibleMask;
            treeCullingIterator.m_PrevVisibleMask = m_PrevVisibleMask;
            treeCullingIterator.m_ActionQueue = m_ActionQueue;
            TreeCullingIterator treeCullingIterator2 = treeCullingIterator;
            int num = m_NodeBuffer.Length / 3;
            switch (index)
            {
                case 0:
                    m_StaticObjectSearchTree.Iterate<TreeCullingIterator, int>(ref treeCullingIterator2, 3, m_NodeBuffer.GetSubArray(0, num), m_SubDataBuffer.GetSubArray(0, num));
                    break;
                case 1:
                    m_NetSearchTree.Iterate<TreeCullingIterator, int>(ref treeCullingIterator2, 3, m_NodeBuffer.GetSubArray(num, num), m_SubDataBuffer.GetSubArray(num, num));
                    break;
                case 2:
                    m_LaneSearchTree.Iterate<TreeCullingIterator, int>(ref treeCullingIterator2, 3, m_NodeBuffer.GetSubArray(num * 2, num), m_SubDataBuffer.GetSubArray(num * 2, num));
                    break;
            }
        }
    }

    [BurstCompile]
    private struct TreeCullingJob2 : IJobParallelFor
    {
        [ReadOnly]
        public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

        [ReadOnly]
        public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

        [ReadOnly]
        public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_LaneSearchTree;

        [ReadOnly]
        public float4 m_LodParameters;

        [ReadOnly]
        public float4 m_PrevLodParameters;

        [ReadOnly]
        public float3 m_CameraPosition;

        [ReadOnly]
        public float3 m_PrevCameraPosition;

        [ReadOnly]
        public float3 m_CameraDirection;

        [ReadOnly]
        public float3 m_PrevCameraDirection;

        [ReadOnly]
        public BoundsMask m_VisibleMask;

        [ReadOnly]
        public BoundsMask m_PrevVisibleMask;

        [ReadOnly]
        public NativeArray<int> m_NodeBuffer;

        [ReadOnly]
        public NativeArray<int> m_SubDataBuffer;

        [NativeDisableContainerSafetyRestriction]
        public Writer<CullingAction> m_ActionQueue;

        public void Execute(int index)
        {
            //IL_0073: Unknown result type (might be due to invalid IL or missing references)
            //IL_0078: Unknown result type (might be due to invalid IL or missing references)
            TreeCullingIterator treeCullingIterator = default(TreeCullingIterator);
            treeCullingIterator.m_LodParameters = m_LodParameters;
            treeCullingIterator.m_PrevLodParameters = m_PrevLodParameters;
            treeCullingIterator.m_CameraPosition = m_CameraPosition;
            treeCullingIterator.m_PrevCameraPosition = m_PrevCameraPosition;
            treeCullingIterator.m_CameraDirection = m_CameraDirection;
            treeCullingIterator.m_PrevCameraDirection = m_PrevCameraDirection;
            treeCullingIterator.m_VisibleMask = m_VisibleMask;
            treeCullingIterator.m_PrevVisibleMask = m_PrevVisibleMask;
            treeCullingIterator.m_ActionQueue = m_ActionQueue;
            TreeCullingIterator treeCullingIterator2 = treeCullingIterator;
            switch (index * 3 / m_NodeBuffer.Length)
            {
                case 0:
                    m_StaticObjectSearchTree.Iterate<TreeCullingIterator, int>(ref treeCullingIterator2, m_SubDataBuffer[index], m_NodeBuffer[index]);
                    break;
                case 1:
                    m_NetSearchTree.Iterate<TreeCullingIterator, int>(ref treeCullingIterator2, m_SubDataBuffer[index], m_NodeBuffer[index]);
                    break;
                case 2:
                    m_LaneSearchTree.Iterate<TreeCullingIterator, int>(ref treeCullingIterator2, m_SubDataBuffer[index], m_NodeBuffer[index]);
                    break;
            }
        }
    }

    private struct TreeCullingIterator : INativeQuadTreeIteratorWithSubData<Entity, QuadTreeBoundsXZ, int>, IUnsafeQuadTreeIteratorWithSubData<Entity, QuadTreeBoundsXZ, int>
    {
        public float4 m_LodParameters;

        public float3 m_CameraPosition;

        public float3 m_CameraDirection;

        public float3 m_PrevCameraPosition;

        public float4 m_PrevLodParameters;

        public float3 m_PrevCameraDirection;

        public BoundsMask m_VisibleMask;

        public BoundsMask m_PrevVisibleMask;

        public Writer<CullingAction> m_ActionQueue;

        public bool Intersect(QuadTreeBoundsXZ bounds, ref int subData)
        {
            switch (subData)
            {
                case 1:
                    {
                        BoundsMask boundsMask4 = m_VisibleMask & bounds.m_Mask;
                        float num13 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                        int num14 = RenderingUtils.CalculateLod(num13 * num13, m_LodParameters);
                        if (boundsMask4 == (BoundsMask)0 || num14 < bounds.m_MinLod)
                        {
                            return false;
                        }

                        float num15 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
                        int num16 = RenderingUtils.CalculateLod(num15 * num15, m_PrevLodParameters);
                        if (((uint)boundsMask4 & (uint)(ushort)(~(int)m_PrevVisibleMask)) == 0)
                        {
                            if (num16 < bounds.m_MaxLod)
                            {
                                return num14 > num16;
                            }

                            return false;
                        }

                        return true;
                    }
                case 2:
                    {
                        BoundsMask boundsMask3 = m_PrevVisibleMask & bounds.m_Mask;
                        float num9 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
                        int num10 = RenderingUtils.CalculateLod(num9 * num9, m_PrevLodParameters);
                        if (boundsMask3 == (BoundsMask)0 || num10 < bounds.m_MinLod)
                        {
                            return false;
                        }

                        float num11 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                        int num12 = RenderingUtils.CalculateLod(num11 * num11, m_LodParameters);
                        if (((uint)boundsMask3 & (uint)(ushort)(~(int)m_VisibleMask)) == 0)
                        {
                            if (num12 < bounds.m_MaxLod)
                            {
                                return num10 > num12;
                            }

                            return false;
                        }

                        return true;
                    }
                default:
                    {
                        BoundsMask boundsMask = m_VisibleMask & bounds.m_Mask;
                        BoundsMask boundsMask2 = m_PrevVisibleMask & bounds.m_Mask;
                        float num = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                        float num2 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
                        int num3 = RenderingUtils.CalculateLod(num * num, m_LodParameters);
                        int num4 = RenderingUtils.CalculateLod(num2 * num2, m_PrevLodParameters);
                        subData = 0;
                        if (boundsMask != 0 && num3 >= bounds.m_MinLod)
                        {
                            float num5 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
                            int num6 = RenderingUtils.CalculateLod(num5 * num5, m_PrevLodParameters);
                            subData |= math.select(0, 1, ((uint)boundsMask & (uint)(ushort)(~(uint)m_PrevVisibleMask)) != 0 || (num6 < bounds.m_MaxLod && num3 > num6));
                        }

                        if (boundsMask2 != 0 && num4 >= bounds.m_MinLod)
                        {
                            float num7 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                            int num8 = RenderingUtils.CalculateLod(num7 * num7, m_LodParameters);
                            subData |= math.select(0, 2, ((uint)boundsMask2 & (uint)(ushort)(~(uint)m_VisibleMask)) != 0 || (num8 < bounds.m_MaxLod && num4 > num8));
                        }

                        return subData != 0;
                    }
            }
        }

        public void Iterate(QuadTreeBoundsXZ bounds, int subData, Entity entity)
        {
            switch (subData)
            {
                case 1:
                    {
                        float num5 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                        int num6 = RenderingUtils.CalculateLod(num5 * num5, m_LodParameters);
                        if ((m_VisibleMask & bounds.m_Mask) != 0 && num6 >= bounds.m_MinLod)
                        {
                            float num7 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
                            int num8 = RenderingUtils.CalculateLod(num7 * num7, m_PrevLodParameters);
                            if ((m_PrevVisibleMask & bounds.m_Mask) == 0 || num8 < bounds.m_MaxLod)
                            {
                                m_ActionQueue.Enqueue(new CullingAction
                                {
                                    m_Entity = entity,
                                    m_Flags = ActionFlags.PassedCulling,
                                    m_UpdateFrame = -1
                                });
                            }
                        }

                        return;
                    }
                case 2:
                    {
                        float num = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
                        int num2 = RenderingUtils.CalculateLod(num * num, m_PrevLodParameters);
                        if ((m_PrevVisibleMask & bounds.m_Mask) != 0 && num2 >= bounds.m_MinLod)
                        {
                            float num3 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                            int num4 = RenderingUtils.CalculateLod(num3 * num3, m_LodParameters);
                            if ((m_VisibleMask & bounds.m_Mask) == 0 || num4 < bounds.m_MaxLod)
                            {
                                m_ActionQueue.Enqueue(new CullingAction
                                {
                                    m_Entity = entity,
                                    m_Flags = (((m_VisibleMask & bounds.m_Mask) != 0) ? ActionFlags.CrossFade : ((ActionFlags)0)),
                                    m_UpdateFrame = -1
                                });
                            }
                        }

                        return;
                    }
            }

            float num9 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
            float num10 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
            int num11 = RenderingUtils.CalculateLod(num9 * num9, m_LodParameters);
            int num12 = RenderingUtils.CalculateLod(num10 * num10, m_PrevLodParameters);
            bool flag = (m_VisibleMask & bounds.m_Mask) != 0 && num11 >= bounds.m_MinLod;
            bool flag2 = (m_PrevVisibleMask & bounds.m_Mask) != 0 && num12 >= bounds.m_MaxLod;
            if (flag != flag2)
            {
                CullingAction cullingAction = default(CullingAction);
                cullingAction.m_Entity = entity;
                cullingAction.m_UpdateFrame = -1;
                CullingAction cullingAction2 = cullingAction;
                if (flag)
                {
                    cullingAction2.m_Flags = ActionFlags.PassedCulling;
                }
                else if ((m_VisibleMask & bounds.m_Mask) != 0)
                {
                    cullingAction2.m_Flags = ActionFlags.CrossFade;
                }

                m_ActionQueue.Enqueue(cullingAction2);
            }
        }
    }

    [BurstCompile]
    public struct InitializeCullingJob : IJobChunk
    {
        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        [ReadOnly]
        public ComponentTypeHandle<Owner> m_OwnerType;

        [ReadOnly]
        public ComponentTypeHandle<Updated> m_UpdatedType;

        [ReadOnly]
        public ComponentTypeHandle<BatchesUpdated> m_BatchesUpdatedType;

        [ReadOnly]
        public ComponentTypeHandle<Overridden> m_OverriddenType;

        [ReadOnly]
        public ComponentTypeHandle<Transform> m_TransformType;

        [ReadOnly]
        public ComponentTypeHandle<Stack> m_StackType;

        [ReadOnly]
        public ComponentTypeHandle<Game.Objects.Marker> m_ObjectMarkerType;

        [ReadOnly]
        public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

        [ReadOnly]
        public ComponentTypeHandle<Unspawned> m_UnspawnedType;

        [ReadOnly]
        public ComponentTypeHandle<Node> m_NodeType;

        [ReadOnly]
        public ComponentTypeHandle<Edge> m_EdgeType;

        [ReadOnly]
        public ComponentTypeHandle<NodeGeometry> m_NodeGeometryType;

        [ReadOnly]
        public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometryType;

        [ReadOnly]
        public ComponentTypeHandle<StartNodeGeometry> m_StartNodeGeometryType;

        [ReadOnly]
        public ComponentTypeHandle<EndNodeGeometry> m_EndNodeGeometryType;

        [ReadOnly]
        public ComponentTypeHandle<Composition> m_CompositionType;

        [ReadOnly]
        public ComponentTypeHandle<Orphan> m_OrphanType;

        [ReadOnly]
        public ComponentTypeHandle<Curve> m_CurveType;

        [ReadOnly]
        public ComponentTypeHandle<Game.Net.UtilityLane> m_UtilityLaneType;

        [ReadOnly]
        public ComponentTypeHandle<Game.Net.Marker> m_NetMarkerType;

        [ReadOnly]
        public ComponentTypeHandle<Block> m_ZoneBlockType;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

        [ReadOnly]
        public BufferTypeHandle<TransformFrame> m_TransformFrameType;

        public ComponentTypeHandle<CullingInfo> m_CullingInfoType;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

        [ReadOnly]
        public ComponentLookup<StackData> m_PrefabStackData;

        [ReadOnly]
        public ComponentLookup<NetLaneGeometryData> m_PrefabLaneGeometryData;

        [ReadOnly]
        public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

        [ReadOnly]
        public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

        [ReadOnly]
        public ComponentLookup<NetCompositionMeshRef> m_PrefabCompositionMeshRef;

        [ReadOnly]
        public ComponentLookup<NetCompositionMeshData> m_PrefabCompositionMeshData;

        [ReadOnly]
        public ComponentLookup<NetData> m_PrefabNetData;

        [ReadOnly]
        public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

        [ReadOnly]
        public bool m_EditorMode;

        [ReadOnly]
        public bool m_UpdateAll;

        [ReadOnly]
        public bool m_UnspawnedVisible;

        [ReadOnly]
        public bool m_Loaded;

        [ReadOnly]
        public UtilityTypes m_DilatedUtilityTypes;

        [ReadOnly]
        public TerrainHeightData m_TerrainHeightData;

        [NativeDisableParallelForRestriction]
        public NativeList<PreCullingData> m_CullingData;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<CullingInfo> nativeArray = chunk.GetNativeArray(ref m_CullingInfoType);
            NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
            BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
            bool flag = chunk.Has(ref m_UpdatedType);
            bool batchesUpdated = chunk.Has(ref m_BatchesUpdatedType);
            if (bufferAccessor.Length != 0)
            {
                NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
                uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
                bool flag2 = chunk.Has(ref m_ObjectMarkerType) && !chunk.Has(ref m_OutsideConnectionType);
                bool remove = !m_UnspawnedVisible && chunk.Has(ref m_UnspawnedType);
                Owner owner = default(Owner);
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    ref CullingInfo reference = ref CollectionUtils.ElementAt<CullingInfo>(nativeArray, i);
                    if (m_UpdateAll || flag)
                    {
                        PrefabRef prefabRef = nativeArray2[i];
                        reference.m_Bounds = default(Bounds3);
                        if (m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
                        {
                            ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
                            reference.m_Radius = math.length(math.max(-objectGeometryData.m_Bounds.min, objectGeometryData.m_Bounds.max));
                            reference.m_Mask = BoundsMask.Debug;
                            if (!flag2 || m_EditorMode)
                            {
                                MeshLayer layers = objectGeometryData.m_Layers;
                                CollectionUtils.TryGet<Owner>(nativeArray3, i, ref owner);
                                reference.m_Mask |= CommonUtils.GetBoundsMask(Game.Net.SearchSystem.GetLayers(owner, default(Game.Net.UtilityLane), layers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData));
                            }

                            reference.m_MinLod = (byte)objectGeometryData.m_MinLod;
                        }
                        else
                        {
                            reference.m_Radius = 1f;
                            reference.m_Mask = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
                            reference.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(2f)));
                        }
                    }

                    SetFlags(ref reference, (int)index, flag, batchesUpdated, remove);
                }

                return;
            }

            NativeArray<Transform> nativeArray4 = chunk.GetNativeArray(ref m_TransformType);
            NativeArray<Node> nativeArray5 = chunk.GetNativeArray(ref m_NodeType);
            NativeArray<Edge> nativeArray6 = chunk.GetNativeArray(ref m_EdgeType);
            NativeArray<Curve> nativeArray7 = chunk.GetNativeArray(ref m_CurveType);
            NativeArray<Block> nativeArray8 = chunk.GetNativeArray(ref m_ZoneBlockType);
            if (nativeArray4.Length != 0)
            {
                NativeArray<Owner> nativeArray9 = chunk.GetNativeArray(ref m_OwnerType);
                NativeArray<Stack> nativeArray10 = chunk.GetNativeArray(ref m_StackType);
                bool flag3 = chunk.Has(ref m_ObjectMarkerType) && !chunk.Has(ref m_OutsideConnectionType);
                bool flag4 = chunk.Has(ref m_OverriddenType);
                bool remove2 = !m_UnspawnedVisible && chunk.Has(ref m_UnspawnedType);
                Owner owner2 = default(Owner);
                for (int j = 0; j < nativeArray.Length; j++)
                {
                    ref CullingInfo reference2 = ref CollectionUtils.ElementAt<CullingInfo>(nativeArray, j);
                    if (m_UpdateAll || flag)
                    {
                        Transform transform = nativeArray4[j];
                        PrefabRef prefabRef2 = nativeArray2[j];
                        if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData))
                        {
                            if (nativeArray10.Length != 0 && m_PrefabStackData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
                            {
                                Stack stack = nativeArray10[j];
                                reference2.m_Bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, stack, componentData, componentData2);
                                reference2.m_Radius = 0f;
                            }
                            else
                            {
                                reference2.m_Bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, componentData);
                                reference2.m_Radius = 0f;
                            }

                            if ((componentData.m_Flags & Game.Objects.GeometryFlags.HasBase) != 0)
                            {
                                reference2.m_Bounds.min.y = math.min(reference2.m_Bounds.min.y, TerrainUtils.GetHeightRange(ref m_TerrainHeightData, reference2.m_Bounds).min);
                            }

                            reference2.m_Mask = BoundsMask.Debug;
                            if (!flag4 && (!flag3 || m_EditorMode))
                            {
                                MeshLayer layers2 = componentData.m_Layers;
                                CollectionUtils.TryGet<Owner>(nativeArray9, j, ref owner2);
                                reference2.m_Mask |= CommonUtils.GetBoundsMask(Game.Net.SearchSystem.GetLayers(owner2, default(Game.Net.UtilityLane), layers2, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData));
                            }

                            reference2.m_MinLod = (byte)componentData.m_MinLod;
                        }
                        else
                        {
                            reference2.m_Bounds = new Bounds3(transform.m_Position - 1f, transform.m_Position + 1f);
                            reference2.m_Radius = 0f;
                            reference2.m_Mask = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
                            reference2.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(2f)));
                        }
                    }

                    SetFlags(ref reference2, -1, flag, batchesUpdated, remove2);
                }
            }
            else if (nativeArray5.Length != 0)
            {
                NativeArray<NodeGeometry> nativeArray11 = chunk.GetNativeArray(ref m_NodeGeometryType);
                NativeArray<Orphan> nativeArray12 = chunk.GetNativeArray(ref m_OrphanType);
                bool flag5 = chunk.Has(ref m_NetMarkerType);
                for (int k = 0; k < nativeArray.Length; k++)
                {
                    ref CullingInfo reference3 = ref CollectionUtils.ElementAt<CullingInfo>(nativeArray, k);
                    if (m_UpdateAll || flag)
                    {
                        if (nativeArray11.Length != 0)
                        {
                            reference3.m_Bounds = nativeArray11[k].m_Bounds;
                            reference3.m_Radius = 0f;
                            reference3.m_Mask = BoundsMask.Debug;
                            if (nativeArray12.Length != 0)
                            {
                                Orphan orphan = nativeArray12[k];
                                reference3.m_MinLod = (byte)m_PrefabCompositionData[orphan.m_Composition].m_MinLod;
                                if (!flag5 || m_EditorMode)
                                {
                                    NetCompositionMeshRef netCompositionMeshRef = m_PrefabCompositionMeshRef[orphan.m_Composition];
                                    if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef.m_Mesh, out var componentData3))
                                    {
                                        reference3.m_Mask |= ((componentData3.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData3.m_DefaultLayers));
                                    }
                                }
                            }
                            else
                            {
                                reference3.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(2f)));
                            }
                        }
                        else
                        {
                            Node node = nativeArray5[k];
                            reference3.m_Bounds = new Bounds3(node.m_Position - 1f, node.m_Position + 1f);
                            reference3.m_Radius = 0f;
                            reference3.m_Mask = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
                            reference3.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(2f)));
                        }
                    }

                    SetFlags(ref reference3, -1, flag, batchesUpdated, remove: false);
                }
            }
            else if (nativeArray6.Length != 0)
            {
                NativeArray<EdgeGeometry> nativeArray13 = chunk.GetNativeArray(ref m_EdgeGeometryType);
                NativeArray<StartNodeGeometry> nativeArray14 = chunk.GetNativeArray(ref m_StartNodeGeometryType);
                NativeArray<EndNodeGeometry> nativeArray15 = chunk.GetNativeArray(ref m_EndNodeGeometryType);
                NativeArray<Composition> nativeArray16 = chunk.GetNativeArray(ref m_CompositionType);
                bool flag6 = chunk.Has(ref m_NetMarkerType);
                for (int l = 0; l < nativeArray.Length; l++)
                {
                    ref CullingInfo reference4 = ref CollectionUtils.ElementAt<CullingInfo>(nativeArray, l);
                    if (m_UpdateAll || flag)
                    {
                        if (nativeArray13.Length != 0)
                        {
                            EdgeGeometry edgeGeometry = nativeArray13[l];
                            StartNodeGeometry startNodeGeometry = nativeArray14[l];
                            EndNodeGeometry endNodeGeometry = nativeArray15[l];
                            Composition composition = nativeArray16[l];
                            reference4.m_Bounds = edgeGeometry.m_Bounds | startNodeGeometry.m_Geometry.m_Bounds | endNodeGeometry.m_Geometry.m_Bounds;
                            NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
                            NetCompositionData netCompositionData2 = m_PrefabCompositionData[composition.m_StartNode];
                            NetCompositionData netCompositionData3 = m_PrefabCompositionData[composition.m_EndNode];
                            reference4.m_Radius = 0f;
                            reference4.m_Mask = BoundsMask.Debug;
                            if (!flag6 || m_EditorMode)
                            {
                                if (math.any(edgeGeometry.m_Start.m_Length + edgeGeometry.m_End.m_Length > 0.1f))
                                {
                                    NetCompositionMeshRef netCompositionMeshRef2 = m_PrefabCompositionMeshRef[composition.m_Edge];
                                    if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef2.m_Mesh, out var componentData4))
                                    {
                                        reference4.m_Mask |= ((componentData4.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData4.m_DefaultLayers));
                                    }
                                }

                                if (math.any(startNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(startNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
                                {
                                    NetCompositionMeshRef netCompositionMeshRef3 = m_PrefabCompositionMeshRef[composition.m_StartNode];
                                    if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef3.m_Mesh, out var componentData5))
                                    {
                                        reference4.m_Mask |= ((componentData5.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData5.m_DefaultLayers));
                                    }
                                }

                                if (math.any(endNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(endNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
                                {
                                    NetCompositionMeshRef netCompositionMeshRef4 = m_PrefabCompositionMeshRef[composition.m_EndNode];
                                    if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef4.m_Mesh, out var componentData6))
                                    {
                                        reference4.m_Mask |= ((componentData6.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData6.m_DefaultLayers));
                                    }
                                }
                            }

                            reference4.m_MinLod = (byte)math.min(netCompositionData.m_MinLod, math.min(netCompositionData2.m_MinLod, netCompositionData3.m_MinLod));
                        }
                        else
                        {
                            reference4.m_Bounds = MathUtils.Expand(MathUtils.Bounds(nativeArray7[l].m_Bezier), 0.5f);
                            reference4.m_Radius = 0f;
                            reference4.m_Mask = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
                            reference4.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(2f)));
                        }
                    }

                    SetFlags(ref reference4, -1, flag, batchesUpdated, remove: false);
                }
            }
            else if (nativeArray7.Length != 0)
            {
                NativeArray<Owner> nativeArray17 = chunk.GetNativeArray(ref m_OwnerType);
                NativeArray<Game.Net.UtilityLane> nativeArray18 = chunk.GetNativeArray(ref m_UtilityLaneType);
                bool flag7 = chunk.Has(ref m_OverriddenType);
                Owner owner3 = default(Owner);
                Game.Net.UtilityLane utilityLane = default(Game.Net.UtilityLane);
                for (int m = 0; m < nativeArray.Length; m++)
                {
                    ref CullingInfo reference5 = ref CollectionUtils.ElementAt<CullingInfo>(nativeArray, m);
                    if (m_UpdateAll || flag)
                    {
                        Curve curve = nativeArray7[m];
                        PrefabRef prefabRef3 = nativeArray2[m];
                        if (m_PrefabLaneGeometryData.HasComponent(prefabRef3.m_Prefab))
                        {
                            NetLaneGeometryData netLaneGeometryData = m_PrefabLaneGeometryData[prefabRef3.m_Prefab];
                            reference5.m_Bounds = MathUtils.Expand(MathUtils.Bounds(curve.m_Bezier), netLaneGeometryData.m_Size.xyx * 0.5f);
                            reference5.m_Radius = 0f;
                            reference5.m_Mask = BoundsMask.Debug;
                            if (!flag7 && curve.m_Length > 0.1f)
                            {
                                MeshLayer defaultLayers = (m_EditorMode ? netLaneGeometryData.m_EditorLayers : netLaneGeometryData.m_GameLayers);
                                CollectionUtils.TryGet<Owner>(nativeArray17, m, ref owner3);
                                CollectionUtils.TryGet<Game.Net.UtilityLane>(nativeArray18, m, ref utilityLane);
                                reference5.m_Mask |= CommonUtils.GetBoundsMask(Game.Net.SearchSystem.GetLayers(owner3, utilityLane, defaultLayers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData));
                            }

                            int num = netLaneGeometryData.m_MinLod;
                            if (m_PrefabUtilityLaneData.TryGetComponent(prefabRef3.m_Prefab, out var componentData7) && (componentData7.m_UtilityTypes & m_DilatedUtilityTypes) != 0)
                            {
                                float renderingSize = RenderingUtils.GetRenderingSize(new float2(componentData7.m_VisualCapacity));
                                num = math.min(num, RenderingUtils.CalculateLodLimit(renderingSize));
                            }

                            reference5.m_MinLod = (byte)num;
                        }
                        else
                        {
                            reference5.m_Bounds = MathUtils.Expand(MathUtils.Bounds(curve.m_Bezier), 0.5f);
                            reference5.m_Radius = 0f;
                            reference5.m_Mask = BoundsMask.Debug;
                            reference5.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(1f)));
                        }
                    }

                    SetFlags(ref reference5, -1, flag, batchesUpdated, remove: false);
                }
            }
            else
            {
                if (nativeArray8.Length == 0)
                {
                    return;
                }

                for (int n = 0; n < nativeArray.Length; n++)
                {
                    ref CullingInfo reference6 = ref CollectionUtils.ElementAt<CullingInfo>(nativeArray, n);
                    if (m_UpdateAll || flag)
                    {
                        Block block = nativeArray8[n];
                        float3 size = new float3(block.m_Size.x, math.cmax(block.m_Size), block.m_Size.y) * 8f;
                        reference6.m_Bounds = new Bounds3(block.m_Position, block.m_Position);
                        reference6.m_Bounds.xz = ZoneUtils.CalculateBounds(block);
                        reference6.m_Radius = 0f;
                        reference6.m_Mask = BoundsMask.Debug | BoundsMask.NormalLayers;
                        reference6.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(size), 0f);
                    }

                    SetFlags(ref reference6, -1, flag, batchesUpdated, remove: false);
                }
            }
        }

        private void SetFlags(ref CullingInfo cullingInfo, int updateFrame, bool isUpdated, bool batchesUpdated, bool remove)
        {
            if (cullingInfo.m_CullingIndex != 0)
            {
                ref PreCullingData reference = ref m_CullingData.ElementAt(cullingInfo.m_CullingIndex);
                reference.m_UpdateFrame = (sbyte)updateFrame;
                if (isUpdated)
                {
                    reference.m_Flags |= PreCullingFlags.Updated;
                }

                if (batchesUpdated)
                {
                    reference.m_Flags |= PreCullingFlags.BatchesUpdated;
                }

                if (remove)
                {
                    cullingInfo.m_PassedCulling = 0;
                    reference.m_Flags &= ~PreCullingFlags.PassedCulling;
                    reference.m_Timer = byte.MaxValue;
                }
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    [BurstCompile]
    private struct EventCullingJob : IJobChunk
    {
        [ReadOnly]
        public ComponentTypeHandle<RentersUpdated> m_RentersUpdatedType;

        [ReadOnly]
        public ComponentTypeHandle<ColorUpdated> m_ColorUpdatedType;

        [ReadOnly]
        public ComponentLookup<CullingInfo> m_CullingInfoData;

        [ReadOnly]
        public BufferLookup<Game.Objects.SubObject> m_SubObjects;

        [ReadOnly]
        public BufferLookup<Game.Net.SubLane> m_SubLanes;

        [ReadOnly]
        public BufferLookup<RouteVehicle> m_RouteVehicles;

        [ReadOnly]
        public BufferLookup<LayoutElement> m_LayoutElements;

        public NativeList<PreCullingData> m_CullingData;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<RentersUpdated> nativeArray = chunk.GetNativeArray(ref m_RentersUpdatedType);
            if (nativeArray.Length != 0)
            {
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    Entity property = nativeArray[i].m_Property;
                    SetFlags(property);
                    AddSubObjects(property);
                    AddSubLanes(property);
                }

                return;
            }

            NativeArray<ColorUpdated> nativeArray2 = chunk.GetNativeArray(ref m_ColorUpdatedType);
            if (nativeArray2.Length != 0)
            {
                for (int j = 0; j < nativeArray2.Length; j++)
                {
                    Entity route = nativeArray2[j].m_Route;
                    AddRouteVehicles(route);
                }
            }
        }

        private void AddSubObjects(Entity owner)
        {
            if (m_SubObjects.TryGetBuffer(owner, out var bufferData))
            {
                for (int i = 0; i < bufferData.Length; i++)
                {
                    Entity subObject = bufferData[i].m_SubObject;
                    SetFlags(subObject);
                    AddSubObjects(subObject);
                    AddSubLanes(subObject);
                }
            }
        }

        private void AddSubLanes(Entity owner)
        {
            if (m_SubLanes.TryGetBuffer(owner, out var bufferData))
            {
                for (int i = 0; i < bufferData.Length; i++)
                {
                    Entity subLane = bufferData[i].m_SubLane;
                    SetFlags(subLane);
                }
            }
        }

        private void AddRouteVehicles(Entity owner)
        {
            if (!m_RouteVehicles.TryGetBuffer(owner, out var bufferData))
            {
                return;
            }

            for (int i = 0; i < bufferData.Length; i++)
            {
                Entity vehicle = bufferData[i].m_Vehicle;
                if (m_LayoutElements.TryGetBuffer(vehicle, out var bufferData2) && bufferData2.Length != 0)
                {
                    for (int j = 0; j < bufferData2.Length; j++)
                    {
                        SetFlags(bufferData2[j].m_Vehicle);
                    }
                }
                else
                {
                    SetFlags(vehicle);
                }
            }
        }

        private void SetFlags(Entity entity)
        {
            if (m_CullingInfoData.TryGetComponent(entity, out var componentData) && componentData.m_CullingIndex != 0)
            {
                m_CullingData.ElementAt(componentData.m_CullingIndex).m_Flags |= PreCullingFlags.ColorsUpdated;
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    [BurstCompile]
    private struct QueryCullingJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        [ReadOnly]
        public ComponentTypeHandle<Transform> m_TransformType;

        [ReadOnly]
        public BufferTypeHandle<TransformFrame> m_TransformFrameType;

        public ComponentTypeHandle<CullingInfo> m_CullingInfoType;

        [ReadOnly]
        public float4 m_LodParameters;

        [ReadOnly]
        public float3 m_CameraPosition;

        [ReadOnly]
        public float3 m_CameraDirection;

        [ReadOnly]
        public uint m_FrameIndex;

        [ReadOnly]
        public float m_FrameTime;

        [ReadOnly]
        public BoundsMask m_VisibleMask;

        [NativeDisableContainerSafetyRestriction]
        public Writer<CullingAction> m_ActionQueue;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<CullingInfo> nativeArray2 = chunk.GetNativeArray(ref m_CullingInfoType);
            BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
            if (bufferAccessor.Length != 0)
            {
                NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
                uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
                ObjectInterpolateSystem.CalculateUpdateFrames(m_FrameIndex, m_FrameTime, index, out var updateFrame, out var updateFrame2, out var framePosition);
                for (int i = 0; i < nativeArray2.Length; i++)
                {
                    ref CullingInfo reference = ref CollectionUtils.ElementAt<CullingInfo>(nativeArray2, i);
                    if ((m_VisibleMask & reference.m_Mask) == 0)
                    {
                        if (reference.m_CullingIndex != 0)
                        {
                            m_ActionQueue.Enqueue(new CullingAction
                            {
                                m_Entity = nativeArray[i],
                                m_UpdateFrame = (sbyte)index
                            });
                        }

                        continue;
                    }

                    if (reference.m_PassedCulling != 0)
                    {
                        DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
                        TransformFrame transformFrame = dynamicBuffer[(int)updateFrame];
                        TransformFrame transformFrame2 = dynamicBuffer[(int)updateFrame2];
                        float3 @float = math.lerp(transformFrame.m_Position, transformFrame2.m_Position, framePosition);
                        reference.m_Bounds = new Bounds3(@float - reference.m_Radius, @float + reference.m_Radius);
                        float num = RenderingUtils.CalculateMinDistance(reference.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                        if (RenderingUtils.CalculateLod(num * num, m_LodParameters) < reference.m_MinLod)
                        {
                            m_ActionQueue.Enqueue(new CullingAction
                            {
                                m_Entity = nativeArray[i],
                                m_Flags = ActionFlags.CrossFade,
                                m_UpdateFrame = (sbyte)index
                            });
                        }

                        continue;
                    }

                    float3 position = nativeArray3[i].m_Position;
                    reference.m_Bounds = new Bounds3(position - reference.m_Radius, position + reference.m_Radius);
                    float num2 = math.max(0f, RenderingUtils.CalculateMinDistance(reference.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters) - 277.777771f);
                    if (RenderingUtils.CalculateLod(num2 * num2, m_LodParameters) >= reference.m_MinLod)
                    {
                        DynamicBuffer<TransformFrame> dynamicBuffer2 = bufferAccessor[i];
                        TransformFrame transformFrame3 = dynamicBuffer2[(int)updateFrame];
                        TransformFrame transformFrame4 = dynamicBuffer2[(int)updateFrame2];
                        position = math.lerp(transformFrame3.m_Position, transformFrame4.m_Position, framePosition);
                        reference.m_Bounds = new Bounds3(position - reference.m_Radius, position + reference.m_Radius);
                        float num3 = RenderingUtils.CalculateMinDistance(reference.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                        if (RenderingUtils.CalculateLod(num3 * num3, m_LodParameters) >= reference.m_MinLod)
                        {
                            m_ActionQueue.Enqueue(new CullingAction
                            {
                                m_Entity = nativeArray[i],
                                m_Flags = ActionFlags.PassedCulling,
                                m_UpdateFrame = (sbyte)index
                            });
                        }
                    }
                }

                return;
            }

            for (int j = 0; j < nativeArray2.Length; j++)
            {
                ref CullingInfo reference2 = ref CollectionUtils.ElementAt<CullingInfo>(nativeArray2, j);
                if ((m_VisibleMask & reference2.m_Mask) == 0)
                {
                    if (reference2.m_CullingIndex != 0)
                    {
                        m_ActionQueue.Enqueue(new CullingAction
                        {
                            m_Entity = nativeArray[j],
                            m_UpdateFrame = -1
                        });
                    }

                    continue;
                }

                float num4 = RenderingUtils.CalculateMinDistance(reference2.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                int num5 = RenderingUtils.CalculateLod(num4 * num4, m_LodParameters);
                if (reference2.m_PassedCulling != 0)
                {
                    if (num5 < reference2.m_MinLod)
                    {
                        m_ActionQueue.Enqueue(new CullingAction
                        {
                            m_Entity = nativeArray[j],
                            m_Flags = ActionFlags.CrossFade,
                            m_UpdateFrame = -1
                        });
                    }
                }
                else if (num5 >= reference2.m_MinLod)
                {
                    m_ActionQueue.Enqueue(new CullingAction
                    {
                        m_Entity = nativeArray[j],
                        m_Flags = ActionFlags.PassedCulling,
                        m_UpdateFrame = -1
                    });
                }
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    [BurstCompile]
    private struct QueryRemoveJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<Deleted> m_DeletedType;

        [ReadOnly]
        public ComponentTypeHandle<Applied> m_AppliedType;

        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        [ReadOnly]
        public BufferTypeHandle<TransformFrame> m_TransformFrameType;

        public ComponentTypeHandle<CullingInfo> m_CullingInfoType;

        [NativeDisableContainerSafetyRestriction]
        public Writer<CullingAction> m_ActionQueue;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<CullingInfo> nativeArray2 = chunk.GetNativeArray(ref m_CullingInfoType);
            bool flag = chunk.Has(ref m_DeletedType);
            bool flag2 = false;
            if (flag)
            {
                flag2 = chunk.Has(ref m_AppliedType);
            }

            if (chunk.Has(ref m_TransformFrameType))
            {
                uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
                for (int i = 0; i < nativeArray2.Length; i++)
                {
                    if (CollectionUtils.ElementAt<CullingInfo>(nativeArray2, i).m_CullingIndex != 0)
                    {
                        ActionFlags flags = (ActionFlags)0;
                        if (flag)
                        {
                            flags = (flag2 ? (ActionFlags.Deleted | ActionFlags.Applied) : ActionFlags.Deleted);
                        }

                        m_ActionQueue.Enqueue(new CullingAction
                        {
                            m_Entity = nativeArray[i],
                            m_Flags = flags,
                            m_UpdateFrame = (sbyte)index
                        });
                    }
                }

                return;
            }

            for (int j = 0; j < nativeArray2.Length; j++)
            {
                if (CollectionUtils.ElementAt<CullingInfo>(nativeArray2, j).m_CullingIndex != 0)
                {
                    ActionFlags flags2 = (ActionFlags)0;
                    if (flag)
                    {
                        flags2 = (flag2 ? (ActionFlags.Deleted | ActionFlags.Applied) : ActionFlags.Deleted);
                    }

                    m_ActionQueue.Enqueue(new CullingAction
                    {
                        m_Entity = nativeArray[j],
                        m_Flags = flags2,
                        m_UpdateFrame = -1
                    });
                }
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    [BurstCompile]
    private struct RelativeCullingJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<Owner> m_OwnerType;

        [ReadOnly]
        public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<CullingInfo> m_CullingInfoData;

        [ReadOnly]
        public float4 m_LodParameters;

        [ReadOnly]
        public float3 m_CameraPosition;

        [ReadOnly]
        public float3 m_CameraDirection;

        [ReadOnly]
        public BoundsMask m_VisibleMask;

        [NativeDisableContainerSafetyRestriction]
        public Writer<CullingAction> m_ActionQueue;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<CurrentVehicle> nativeArray2 = chunk.GetNativeArray(ref m_CurrentVehicleType);
            if (nativeArray2.Length != 0)
            {
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    Entity entity = nativeArray[i];
                    UpdateCulling(entity, nativeArray2[i].m_Vehicle);
                }

                return;
            }

            NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
            for (int j = 0; j < nativeArray.Length; j++)
            {
                Entity entity2 = nativeArray[j];
                UpdateCulling(entity2, nativeArray3[j].m_Owner);
            }
        }

        private void UpdateCulling(Entity entity, Entity parent)
        {
            ref CullingInfo valueRW = ref m_CullingInfoData.GetRefRW(entity).ValueRW;
            valueRW.m_Bounds = m_CullingInfoData[parent].m_Bounds;
            if ((m_VisibleMask & valueRW.m_Mask) == 0)
            {
                if (valueRW.m_CullingIndex != 0)
                {
                    m_ActionQueue.Enqueue(new CullingAction
                    {
                        m_Entity = entity,
                        m_UpdateFrame = -1
                    });
                }

                return;
            }

            float num = RenderingUtils.CalculateMinDistance(valueRW.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
            int num2 = RenderingUtils.CalculateLod(num * num, m_LodParameters);
            if (valueRW.m_PassedCulling != 0)
            {
                if (num2 < valueRW.m_MinLod)
                {
                    m_ActionQueue.Enqueue(new CullingAction
                    {
                        m_Entity = entity,
                        m_Flags = ActionFlags.CrossFade,
                        m_UpdateFrame = -1
                    });
                }
            }
            else if (num2 >= valueRW.m_MinLod)
            {
                m_ActionQueue.Enqueue(new CullingAction
                {
                    m_Entity = entity,
                    m_Flags = ActionFlags.PassedCulling,
                    m_UpdateFrame = -1
                });
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    [BurstCompile]
    private struct TempCullingJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

        [ReadOnly]
        public ComponentTypeHandle<Transform> m_TransformType;

        [ReadOnly]
        public ComponentTypeHandle<Stack> m_StackType;

        [ReadOnly]
        public ComponentTypeHandle<Static> m_StaticType;

        [ReadOnly]
        public ComponentTypeHandle<Stopped> m_StoppedType;

        [ReadOnly]
        public ComponentTypeHandle<Temp> m_TempType;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

        [ReadOnly]
        public ComponentLookup<StackData> m_PrefabStackData;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<CullingInfo> m_CullingInfoData;

        [ReadOnly]
        public float4 m_LodParameters;

        [ReadOnly]
        public float3 m_CameraPosition;

        [ReadOnly]
        public float3 m_CameraDirection;

        [ReadOnly]
        public BoundsMask m_VisibleMask;

        [ReadOnly]
        public TerrainHeightData m_TerrainHeightData;

        [NativeDisableContainerSafetyRestriction]
        public Writer<CullingAction> m_ActionQueue;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            bool flag = chunk.Has(ref m_InterpolatedTransformType);
            bool flag2 = false;
            bool flag3 = false;
            NativeArray<Transform> nativeArray2 = default(NativeArray<Transform>);
            NativeArray<Stack> nativeArray3 = default(NativeArray<Stack>);
            NativeArray<Temp> nativeArray4 = default(NativeArray<Temp>);
            NativeArray<PrefabRef> nativeArray5 = default(NativeArray<PrefabRef>);
            if (flag)
            {
                flag2 = chunk.Has(ref m_StaticType);
                flag3 = chunk.Has(ref m_StoppedType);
                nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
                nativeArray3 = chunk.GetNativeArray(ref m_StackType);
                nativeArray4 = chunk.GetNativeArray(ref m_TempType);
                nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
            }

            for (int i = 0; i < nativeArray.Length; i++)
            {
                Entity entity = nativeArray[i];
                ref CullingInfo valueRW = ref m_CullingInfoData.GetRefRW(entity).ValueRW;
                if (flag)
                {
                    Temp temp = nativeArray4[i];
                    if (temp.m_Original != Entity.Null && (temp.m_Flags & TempFlags.Dragging) == 0 && ((!flag2 && !flag3) || (temp.m_Flags & (TempFlags.Create | TempFlags.Modify)) == 0) && m_CullingInfoData.TryGetComponent(temp.m_Original, out var componentData))
                    {
                        valueRW.m_Bounds = componentData.m_Bounds;
                    }
                    else
                    {
                        Transform transform = nativeArray2[i];
                        PrefabRef prefabRef = nativeArray5[i];
                        if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
                        {
                            if (nativeArray3.Length != 0 && m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
                            {
                                Stack stack = nativeArray3[i];
                                valueRW.m_Bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, stack, componentData2, componentData3);
                            }
                            else
                            {
                                valueRW.m_Bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, componentData2);
                            }

                            if ((componentData2.m_Flags & Game.Objects.GeometryFlags.HasBase) != 0)
                            {
                                valueRW.m_Bounds.min.y = math.min(valueRW.m_Bounds.min.y, TerrainUtils.GetHeightRange(ref m_TerrainHeightData, valueRW.m_Bounds).min);
                            }
                        }
                        else
                        {
                            valueRW.m_Bounds = new Bounds3(transform.m_Position - 1f, transform.m_Position + 1f);
                        }
                    }
                }

                if ((m_VisibleMask & valueRW.m_Mask) == 0)
                {
                    if (valueRW.m_CullingIndex != 0)
                    {
                        m_ActionQueue.Enqueue(new CullingAction
                        {
                            m_Entity = nativeArray[i],
                            m_UpdateFrame = -1
                        });
                    }

                    continue;
                }

                float num = RenderingUtils.CalculateMinDistance(valueRW.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                int num2 = RenderingUtils.CalculateLod(num * num, m_LodParameters);
                if (valueRW.m_PassedCulling != 0)
                {
                    if (num2 < valueRW.m_MinLod)
                    {
                        m_ActionQueue.Enqueue(new CullingAction
                        {
                            m_Entity = nativeArray[i],
                            m_Flags = ActionFlags.CrossFade,
                            m_UpdateFrame = -1
                        });
                    }
                }
                else if (num2 >= valueRW.m_MinLod)
                {
                    m_ActionQueue.Enqueue(new CullingAction
                    {
                        m_Entity = nativeArray[i],
                        m_Flags = ActionFlags.PassedCulling,
                        m_UpdateFrame = -1
                    });
                }
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    [BurstCompile]
    private struct VerifyVisibleJob : IJobParallelFor
    {
        [ReadOnly]
        public ComponentLookup<CullingInfo> m_CullingInfoData;

        [ReadOnly]
        public float4 m_LodParameters;

        [ReadOnly]
        public float3 m_CameraPosition;

        [ReadOnly]
        public float3 m_CameraDirection;

        [ReadOnly]
        public BoundsMask m_VisibleMask;

        [ReadOnly]
        public NativeList<PreCullingData> m_CullingData;

        [NativeDisableContainerSafetyRestriction]
        public Writer<CullingAction> m_ActionQueue;

        public void Execute(int index)
        {
            PreCullingData preCullingData = m_CullingData[index];
            if ((preCullingData.m_Flags & PreCullingFlags.FadeContainer) != 0)
            {
                return;
            }

            CullingInfo cullingInfo = m_CullingInfoData[preCullingData.m_Entity];
            if ((m_VisibleMask & cullingInfo.m_Mask) == 0)
            {
                m_ActionQueue.Enqueue(new CullingAction
                {
                    m_Entity = preCullingData.m_Entity,
                    m_UpdateFrame = preCullingData.m_UpdateFrame
                });
            }
            else if (cullingInfo.m_PassedCulling != 0)
            {
                float num = RenderingUtils.CalculateMinDistance(cullingInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                if (RenderingUtils.CalculateLod(num * num, m_LodParameters) < cullingInfo.m_MinLod)
                {
                    m_ActionQueue.Enqueue(new CullingAction
                    {
                        m_Entity = preCullingData.m_Entity,
                        m_UpdateFrame = preCullingData.m_UpdateFrame
                    });
                }
            }
        }
    }

    [Flags]
    public enum ActionFlags : byte
    {
        PassedCulling = 1,
        CrossFade = 2,
        Deleted = 4,
        Applied = 8
    }

    private struct CullingAction
    {
        public Entity m_Entity;

        public ActionFlags m_Flags;

        public sbyte m_UpdateFrame;

        public override int GetHashCode()
        {
            return m_Entity.GetHashCode();
        }
    }

    private struct OverflowAction
    {
        public int m_DataIndex;

        public Entity m_Entity;

        public sbyte m_UpdateFrame;
    }

    [BurstCompile]
    private struct CullingActionJob : IJobParallelFor
    {
        [ReadOnly]
        public Reader<CullingAction> m_CullingActions;

        public NativeQueue<OverflowAction>.ParallelWriter m_OverflowActions;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<CullingInfo> m_CullingInfo;

        [NativeDisableParallelForRestriction]
        public NativeList<PreCullingData> m_CullingData;

        [NativeDisableParallelForRestriction]
        public NativeReference<int> m_CullingDataIndex;

        public void Execute(int index)
        {
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            //IL_000c: Unknown result type (might be due to invalid IL or missing references)
            Enumerator<CullingAction> enumerator = m_CullingActions.GetEnumerator(index);
            while (enumerator.MoveNext())
            {
                CullingAction current = enumerator.Current;
                if ((current.m_Flags & ActionFlags.PassedCulling) != 0)
                {
                    PassedCulling(current);
                }
                else
                {
                    FailedCulling(current);
                }
            }

            enumerator.Dispose();
        }

        private unsafe void PassedCulling(CullingAction cullingAction)
        {
            ref CullingInfo valueRW = ref m_CullingInfo.GetRefRW(cullingAction.m_Entity).ValueRW;
            valueRW.m_PassedCulling = 1;
            if (valueRW.m_CullingIndex == 0)
            {
                valueRW.m_CullingIndex = Interlocked.Increment(ref UnsafeUtility.AsRef<int>(m_CullingDataIndex.GetUnsafePtr())) - 1;
                if (valueRW.m_CullingIndex >= m_CullingData.Capacity)
                {
                    m_OverflowActions.Enqueue(new OverflowAction
                    {
                        m_DataIndex = valueRW.m_CullingIndex,
                        m_Entity = cullingAction.m_Entity,
                        m_UpdateFrame = cullingAction.m_UpdateFrame
                    });
                }
                else
                {
                    ref PreCullingData reference = ref UnsafeUtility.ArrayElementAsRef<PreCullingData>(m_CullingData.GetUnsafePtr(), valueRW.m_CullingIndex);
                    reference.m_Entity = cullingAction.m_Entity;
                    reference.m_UpdateFrame = cullingAction.m_UpdateFrame;
                    reference.m_Flags = PreCullingFlags.PassedCulling | PreCullingFlags.NearCamera | PreCullingFlags.NearCameraUpdated;
                    reference.m_Timer = 0;
                }
            }
            else if (valueRW.m_CullingIndex < m_CullingData.Length)
            {
                ref PreCullingData reference2 = ref UnsafeUtility.ArrayElementAsRef<PreCullingData>(m_CullingData.GetUnsafePtr(), valueRW.m_CullingIndex);
                reference2.m_Entity = cullingAction.m_Entity;
                reference2.m_UpdateFrame = cullingAction.m_UpdateFrame;
                reference2.m_Flags |= PreCullingFlags.PassedCulling;
                reference2.m_Timer = 0;
                if ((reference2.m_Flags & PreCullingFlags.NearCamera) == 0)
                {
                    reference2.m_Flags |= PreCullingFlags.NearCamera | PreCullingFlags.NearCameraUpdated;
                }
            }
        }

        private unsafe void FailedCulling(CullingAction cullingAction)
        {
            ref CullingInfo valueRW = ref m_CullingInfo.GetRefRW(cullingAction.m_Entity).ValueRW;
            valueRW.m_PassedCulling = 0;
            if (valueRW.m_CullingIndex != 0 && valueRW.m_CullingIndex < m_CullingData.Length)
            {
                ref PreCullingData reference = ref UnsafeUtility.ArrayElementAsRef<PreCullingData>(m_CullingData.GetUnsafePtr(), valueRW.m_CullingIndex);
                reference.m_UpdateFrame = cullingAction.m_UpdateFrame;
                reference.m_Flags &= ~PreCullingFlags.PassedCulling;
                if ((cullingAction.m_Flags & ActionFlags.Deleted) != 0)
                {
                    reference.m_Flags |= PreCullingFlags.Deleted;
                }

                if ((cullingAction.m_Flags & ActionFlags.Applied) != 0)
                {
                    reference.m_Flags |= PreCullingFlags.Applied;
                }

                if ((cullingAction.m_Flags & ActionFlags.CrossFade) == 0)
                {
                    reference.m_Timer = byte.MaxValue;
                }
            }
        }
    }

    [BurstCompile]
    private struct ResizeCullingDataJob : IJob
    {
        [ReadOnly]
        public NativeReference<int> m_CullingDataIndex;

        public NativeList<PreCullingData> m_CullingData;

        public NativeList<PreCullingData> m_UpdatedData;

        public NativeQueue<OverflowAction> m_OverflowActions;

        public void Execute()
        {
            m_CullingData.Resize(math.min(m_CullingDataIndex.Value, m_CullingData.Capacity), NativeArrayOptions.UninitializedMemory);
            m_CullingData.Resize(m_CullingDataIndex.Value, NativeArrayOptions.UninitializedMemory);
            m_UpdatedData.Clear();
            if (m_CullingData.Length > m_UpdatedData.Capacity)
            {
                m_UpdatedData.Capacity = m_CullingData.Length;
            }

            OverflowAction item;
            while (m_OverflowActions.TryDequeue(out item))
            {
                ref PreCullingData reference = ref m_CullingData.ElementAt(item.m_DataIndex);
                reference.m_Entity = item.m_Entity;
                reference.m_UpdateFrame = item.m_UpdateFrame;
                reference.m_Flags = PreCullingFlags.PassedCulling | PreCullingFlags.NearCamera | PreCullingFlags.NearCameraUpdated;
                reference.m_Timer = 0;
            }
        }
    }

    [BurstCompile]
    private struct FilterUpdatesJob : IJobParallelForDefer
    {
        [ReadOnly]
        public ComponentLookup<Created> m_CreatedData;

        [ReadOnly]
        public ComponentLookup<Updated> m_UpdatedData;

        [ReadOnly]
        public ComponentLookup<Applied> m_AppliedData;

        [ReadOnly]
        public ComponentLookup<BatchesUpdated> m_BatchesUpdatedData;

        [ReadOnly]
        public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

        [ReadOnly]
        public ComponentLookup<Owner> m_OwnerData;

        [ReadOnly]
        public ComponentLookup<Temp> m_TempData;

        [ReadOnly]
        public ComponentLookup<Game.Objects.Object> m_ObjectData;

        [ReadOnly]
        public ComponentLookup<ObjectGeometry> m_ObjectGeometryData;

        [ReadOnly]
        public ComponentLookup<Game.Objects.Color> m_ObjectColorData;

        [ReadOnly]
        public ComponentLookup<Plant> m_PlantData;

        [ReadOnly]
        public ComponentLookup<Tree> m_TreeData;

        [ReadOnly]
        public ComponentLookup<Relative> m_RelativeData;

        [ReadOnly]
        public ComponentLookup<Damaged> m_DamagedData;

        [ReadOnly]
        public ComponentLookup<Building> m_BuildingData;

        [ReadOnly]
        public ComponentLookup<Extension> m_ExtensionData;

        [ReadOnly]
        public ComponentLookup<Edge> m_EdgeData;

        [ReadOnly]
        public ComponentLookup<Node> m_NodeData;

        [ReadOnly]
        public ComponentLookup<Lane> m_LaneData;

        [ReadOnly]
        public ComponentLookup<NodeColor> m_NodeColorData;

        [ReadOnly]
        public ComponentLookup<EdgeColor> m_EdgeColorData;

        [ReadOnly]
        public ComponentLookup<LaneColor> m_LaneColorData;

        [ReadOnly]
        public ComponentLookup<LaneCondition> m_LaneConditionData;

        [ReadOnly]
        public ComponentLookup<Block> m_ZoneData;

        [ReadOnly]
        public ComponentLookup<OnFire> m_OnFireData;

        [ReadOnly]
        public BufferLookup<Animated> m_AnimatedData;

        [ReadOnly]
        public BufferLookup<Skeleton> m_SkeletonData;

        [ReadOnly]
        public BufferLookup<Emissive> m_EmissiveData;

        [ReadOnly]
        public BufferLookup<MeshColor> m_MeshColorData;

        [ReadOnly]
        public BufferLookup<LayoutElement> m_LayoutElements;

        [ReadOnly]
        public BufferLookup<EnabledEffect> m_EffectInstances;

        [ReadOnly]
        public int m_TimerDelta;

        [NativeDisableParallelForRestriction]
        public NativeList<PreCullingData> m_CullingData;

        public NativeList<PreCullingData>.ParallelWriter m_UpdatedCullingData;

        public void Execute(int index)
        {
            ref PreCullingData reference = ref m_CullingData.ElementAt(index);
            if ((reference.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated)) != 0)
            {
                reference.m_Flags &= ~(PreCullingFlags.Updated | PreCullingFlags.Created | PreCullingFlags.Applied | PreCullingFlags.BatchesUpdated | PreCullingFlags.Temp | PreCullingFlags.Object | PreCullingFlags.Net | PreCullingFlags.Lane | PreCullingFlags.Zone | PreCullingFlags.InfoviewColor | PreCullingFlags.BuildingState | PreCullingFlags.TreeGrowth | PreCullingFlags.LaneCondition | PreCullingFlags.InterpolatedTransform | PreCullingFlags.Animated | PreCullingFlags.Skeleton | PreCullingFlags.Emissive | PreCullingFlags.VehicleLayout | PreCullingFlags.EffectInstances | PreCullingFlags.Relative | PreCullingFlags.SurfaceState | PreCullingFlags.SurfaceDamage | PreCullingFlags.SmoothColor);
                if (m_CreatedData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.Updated | PreCullingFlags.Created;
                }

                if ((reference.m_Flags & PreCullingFlags.Updated) != 0 || m_UpdatedData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.Updated;
                }

                if (m_AppliedData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.Applied;
                }

                if (m_BatchesUpdatedData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.BatchesUpdated;
                }

                if (m_TempData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.Temp;
                }

                if (m_EffectInstances.HasBuffer(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.EffectInstances;
                }

                if (m_ObjectData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.Object;
                    if (m_ObjectGeometryData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.SurfaceState;
                    }

                    if (m_InterpolatedTransformData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.InterpolatedTransform;
                    }

                    if (m_AnimatedData.HasBuffer(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.Animated;
                    }

                    if (m_ObjectColorData.HasComponent(reference.m_Entity) || m_OwnerData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.InfoviewColor;
                    }

                    if (m_BuildingData.HasComponent(reference.m_Entity) || m_ExtensionData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.BuildingState;
                    }

                    if (m_PlantData.HasComponent(reference.m_Entity))
                    {
                        if (m_TreeData.HasComponent(reference.m_Entity))
                        {
                            reference.m_Flags |= PreCullingFlags.TreeGrowth;
                        }

                        if (m_MeshColorData.HasBuffer(reference.m_Entity))
                        {
                            reference.m_Flags |= PreCullingFlags.SmoothColor;
                        }
                    }

                    if (m_SkeletonData.HasBuffer(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.Skeleton;
                    }

                    if (m_EmissiveData.HasBuffer(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.Emissive;
                    }

                    if (m_LayoutElements.HasBuffer(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.VehicleLayout;
                    }

                    if (m_RelativeData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.Relative;
                    }

                    if (m_DamagedData.HasComponent(reference.m_Entity) || m_OnFireData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.SurfaceDamage;
                    }
                }
                else if (m_EdgeData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.Net;
                    if (m_EdgeColorData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.InfoviewColor;
                    }
                }
                else if (m_NodeData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.Net;
                    if (m_NodeColorData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.InfoviewColor;
                    }
                }
                else if (m_LaneData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.Lane;
                    if (m_PlantData.HasComponent(reference.m_Entity) && m_MeshColorData.HasBuffer(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.SmoothColor;
                    }

                    if (m_LaneColorData.HasComponent(reference.m_Entity) || m_OwnerData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.InfoviewColor;
                    }

                    if (m_LaneConditionData.HasComponent(reference.m_Entity))
                    {
                        reference.m_Flags |= PreCullingFlags.LaneCondition;
                    }
                }
                else if (m_ZoneData.HasComponent(reference.m_Entity))
                {
                    reference.m_Flags |= PreCullingFlags.Zone;
                }
            }

            if ((reference.m_Flags & PreCullingFlags.PassedCulling) == 0)
            {
                int num = reference.m_Timer + m_TimerDelta;
                if (num >= 255)
                {
                    reference.m_Flags &= ~PreCullingFlags.NearCamera;
                    reference.m_Flags |= PreCullingFlags.NearCameraUpdated;
                    reference.m_Timer = byte.MaxValue;
                }
                else
                {
                    reference.m_Timer = (byte)num;
                }
            }

            if ((reference.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated | PreCullingFlags.BatchesUpdated | PreCullingFlags.FadeContainer | PreCullingFlags.ColorsUpdated)) != 0)
            {
                m_UpdatedCullingData.AddNoResize(reference);
            }
        }
    }

    private struct TypeHandle
    {
        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Updated> __Game_Common_Updated_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<BatchesUpdated> __Game_Common_BatchesUpdated_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Overridden> __Game_Common_Overridden_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Stack> __Game_Objects_Stack_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Game.Objects.Marker> __Game_Objects_Marker_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Orphan> __Game_Net_Orphan_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Game.Net.UtilityLane> __Game_Net_UtilityLane_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Game.Net.Marker> __Game_Net_Marker_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Block> __Game_Zones_Block_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RO_BufferTypeHandle;

        public ComponentTypeHandle<CullingInfo> __Game_Rendering_CullingInfo_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<NetLaneGeometryData> __Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<NetCompositionMeshRef> __Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<NetCompositionMeshData> __Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentTypeHandle<RentersUpdated> __Game_Buildings_RentersUpdated_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ColorUpdated> __Game_Routes_ColorUpdated_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<RouteVehicle> __Game_Routes_RouteVehicle_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Applied> __Game_Common_Applied_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

        public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RW_ComponentLookup;

        [ReadOnly]
        public ComponentTypeHandle<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Static> __Game_Objects_Static_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Stopped> __Game_Objects_Stopped_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<Created> __Game_Common_Created_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Applied> __Game_Common_Applied_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<BatchesUpdated> __Game_Common_BatchesUpdated_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Objects.Object> __Game_Objects_Object_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ObjectGeometry> __Game_Objects_ObjectGeometry_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Objects.Color> __Game_Objects_Color_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Plant> __Game_Objects_Plant_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Relative> __Game_Objects_Relative_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Extension> __Game_Buildings_Extension_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<NodeColor> __Game_Net_NodeColor_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<EdgeColor> __Game_Net_EdgeColor_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<LaneColor> __Game_Net_LaneColor_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<LaneCondition> __Game_Net_LaneCondition_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<Animated> __Game_Rendering_Animated_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<Skeleton> __Game_Rendering_Skeleton_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<Emissive> __Game_Rendering_Emissive_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<MeshColor> __Game_Rendering_MeshColor_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<EnabledEffect> __Game_Effects_EnabledEffect_RO_BufferLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            __Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
            __Game_Common_Updated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Updated>(isReadOnly: true);
            __Game_Common_BatchesUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BatchesUpdated>(isReadOnly: true);
            __Game_Common_Overridden_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Overridden>(isReadOnly: true);
            __Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
            __Game_Objects_Stack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stack>(isReadOnly: true);
            __Game_Objects_Marker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Marker>(isReadOnly: true);
            __Game_Objects_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
            __Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
            __Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
            __Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
            __Game_Net_NodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NodeGeometry>(isReadOnly: true);
            __Game_Net_EdgeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
            __Game_Net_StartNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StartNodeGeometry>(isReadOnly: true);
            __Game_Net_EndNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EndNodeGeometry>(isReadOnly: true);
            __Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
            __Game_Net_Orphan_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Orphan>(isReadOnly: true);
            __Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
            __Game_Net_UtilityLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.UtilityLane>(isReadOnly: true);
            __Game_Net_Marker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Marker>(isReadOnly: true);
            __Game_Zones_Block_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Block>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Objects_TransformFrame_RO_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>(isReadOnly: true);
            __Game_Rendering_CullingInfo_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CullingInfo>();
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
            __Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
            __Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetLaneGeometryData>(isReadOnly: true);
            __Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
            __Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
            __Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup = state.GetComponentLookup<NetCompositionMeshRef>(isReadOnly: true);
            __Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionMeshData>(isReadOnly: true);
            __Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
            __Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
            __Game_Buildings_RentersUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RentersUpdated>(isReadOnly: true);
            __Game_Routes_ColorUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ColorUpdated>(isReadOnly: true);
            __Game_Rendering_CullingInfo_RO_ComponentLookup = state.GetComponentLookup<CullingInfo>(isReadOnly: true);
            __Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
            __Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
            __Game_Routes_RouteVehicle_RO_BufferLookup = state.GetBufferLookup<RouteVehicle>(isReadOnly: true);
            __Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
            __Game_Common_Applied_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Applied>(isReadOnly: true);
            __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
            __Game_Rendering_CullingInfo_RW_ComponentLookup = state.GetComponentLookup<CullingInfo>();
            __Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InterpolatedTransform>(isReadOnly: true);
            __Game_Objects_Static_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Static>(isReadOnly: true);
            __Game_Objects_Stopped_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stopped>(isReadOnly: true);
            __Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
            __Game_Common_Created_RO_ComponentLookup = state.GetComponentLookup<Created>(isReadOnly: true);
            __Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
            __Game_Common_Applied_RO_ComponentLookup = state.GetComponentLookup<Applied>(isReadOnly: true);
            __Game_Common_BatchesUpdated_RO_ComponentLookup = state.GetComponentLookup<BatchesUpdated>(isReadOnly: true);
            __Game_Rendering_InterpolatedTransform_RO_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>(isReadOnly: true);
            __Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
            __Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
            __Game_Objects_Object_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Object>(isReadOnly: true);
            __Game_Objects_ObjectGeometry_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometry>(isReadOnly: true);
            __Game_Objects_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Color>(isReadOnly: true);
            __Game_Objects_Plant_RO_ComponentLookup = state.GetComponentLookup<Plant>(isReadOnly: true);
            __Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
            __Game_Objects_Relative_RO_ComponentLookup = state.GetComponentLookup<Relative>(isReadOnly: true);
            __Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
            __Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
            __Game_Buildings_Extension_RO_ComponentLookup = state.GetComponentLookup<Extension>(isReadOnly: true);
            __Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
            __Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
            __Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
            __Game_Net_NodeColor_RO_ComponentLookup = state.GetComponentLookup<NodeColor>(isReadOnly: true);
            __Game_Net_EdgeColor_RO_ComponentLookup = state.GetComponentLookup<EdgeColor>(isReadOnly: true);
            __Game_Net_LaneColor_RO_ComponentLookup = state.GetComponentLookup<LaneColor>(isReadOnly: true);
            __Game_Net_LaneCondition_RO_ComponentLookup = state.GetComponentLookup<LaneCondition>(isReadOnly: true);
            __Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
            __Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
            __Game_Rendering_Animated_RO_BufferLookup = state.GetBufferLookup<Animated>(isReadOnly: true);
            __Game_Rendering_Skeleton_RO_BufferLookup = state.GetBufferLookup<Skeleton>(isReadOnly: true);
            __Game_Rendering_Emissive_RO_BufferLookup = state.GetBufferLookup<Emissive>(isReadOnly: true);
            __Game_Rendering_MeshColor_RO_BufferLookup = state.GetBufferLookup<MeshColor>(isReadOnly: true);
            __Game_Effects_EnabledEffect_RO_BufferLookup = state.GetBufferLookup<EnabledEffect>(isReadOnly: true);
        }
    }

    private RenderingSystem m_RenderingSystem;

    private UndergroundViewSystem m_UndergroundViewSystem;

    private BatchMeshSystem m_BatchMeshSystem;

    private BatchDataSystem m_BatchDataSystem;

    private Game.Objects.SearchSystem m_ObjectSearchSystem;

    private Game.Net.SearchSystem m_NetSearchSystem;

    private ToolSystem m_ToolSystem;

    private CameraUpdateSystem m_CameraUpdateSystem;

    private TerrainSystem m_TerrainSystem;

    private EntityQuery m_InitializeQuery;

    private EntityQuery m_EventQuery;

    private EntityQuery m_CullingInfoQuery;

    private EntityQuery m_TempQuery;

    private float3 m_PrevCameraPosition;

    private float3 m_PrevCameraDirection;

    private float4 m_PrevLodParameters;

    private BoundsMask m_PrevVisibleMask;

    private QueryFlags m_PrevQueryFlags;

    private Dictionary<QueryFlags, EntityQuery> m_CullingQueries;

    private Dictionary<QueryFlags, EntityQuery> m_RelativeQueries;

    private Dictionary<QueryFlags, EntityQuery> m_RemoveQueries;

    private NativeList<PreCullingData> m_CullingData;

    private NativeList<PreCullingData> m_UpdatedData;

    private Entity m_FadeContainer;

    private JobHandle m_WriteDependencies;

    private JobHandle m_ReadDependencies;

    private bool m_ResetPrevious;

    private bool m_Loaded;

    private TypeHandle __TypeHandle;

    public BoundsMask visibleMask { get; private set; }

    public BoundsMask becameVisible { get; private set; }

    public BoundsMask becameHidden { get; private set; }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
        m_UndergroundViewSystem = base.World.GetOrCreateSystemManaged<UndergroundViewSystem>();
        m_BatchMeshSystem = base.World.GetOrCreateSystemManaged<BatchMeshSystem>();
        m_BatchDataSystem = base.World.GetOrCreateSystemManaged<BatchDataSystem>();
        m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
        m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
        m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
        m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
        m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
        m_InitializeQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<CullingInfo>() },
            Any = new ComponentType[2]
            {
                ComponentType.ReadOnly<Updated>(),
                ComponentType.ReadOnly<BatchesUpdated>()
            },
            None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
        });
        m_EventQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<Game.Common.Event>() },
            Any = new ComponentType[2]
            {
                ComponentType.ReadOnly<RentersUpdated>(),
                ComponentType.ReadOnly<ColorUpdated>()
            }
        });
        m_CullingInfoQuery = GetEntityQuery(ComponentType.ReadOnly<CullingInfo>(), ComponentType.Exclude<Deleted>());
        m_TempQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[2]
            {
                ComponentType.ReadOnly<Temp>(),
                ComponentType.ReadWrite<CullingInfo>()
            },
            None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
        });
        m_CullingQueries = new Dictionary<QueryFlags, EntityQuery>();
        m_RelativeQueries = new Dictionary<QueryFlags, EntityQuery>();
        m_RemoveQueries = new Dictionary<QueryFlags, EntityQuery>();
        m_PrevCameraDirection = math.forward();
        m_PrevLodParameters = 1f;
        m_CullingData = new NativeList<PreCullingData>(10000, Allocator.Persistent);
        m_UpdatedData = new NativeList<PreCullingData>(10000, Allocator.Persistent);
        m_FadeContainer = base.EntityManager.CreateEntity(ComponentType.ReadWrite<MeshBatch>(), ComponentType.ReadWrite<FadeBatch>());
    }

    [Preserve]
    protected override void OnDestroy()
    {
        m_WriteDependencies.Complete();
        m_ReadDependencies.Complete();
        m_CullingData.Dispose();
        m_UpdatedData.Dispose();
        base.OnDestroy();
    }

    public void PostDeserialize(Context context)
    {
        m_WriteDependencies.Complete();
        m_ReadDependencies.Complete();
        base.EntityManager.GetBuffer<MeshBatch>(m_FadeContainer).Clear();
        base.EntityManager.GetBuffer<FadeBatch>(m_FadeContainer).Clear();
        InitializeCullingData();
        ResetCulling();
        m_Loaded = true;
    }

    public void ResetCulling()
    {
        m_ResetPrevious = true;
    }

    public Entity GetFadeContainer()
    {
        return m_FadeContainer;
    }

    public NativeList<PreCullingData> GetCullingData(bool readOnly, out JobHandle dependencies)
    {
        dependencies = (readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies));
        return m_CullingData;
    }

    public NativeList<PreCullingData> GetUpdatedData(bool readOnly, out JobHandle dependencies)
    {
        dependencies = (readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies));
        return m_UpdatedData;
    }

    public void AddCullingDataReader(JobHandle dependencies)
    {
        m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, dependencies);
    }

    public void AddCullingDataWriter(JobHandle dependencies)
    {
        m_WriteDependencies = dependencies;
    }

    private bool GetLoaded()
    {
        if (m_Loaded)
        {
            m_Loaded = false;
            return true;
        }

        return false;
    }

    [Preserve]
    protected override void OnUpdate()
    {
        //IL_01a8: Unknown result type (might be due to invalid IL or missing references)
        //IL_01ad: Unknown result type (might be due to invalid IL or missing references)
        //IL_01bd: Unknown result type (might be due to invalid IL or missing references)
        //IL_01c2: Unknown result type (might be due to invalid IL or missing references)
        //IL_01d2: Unknown result type (might be due to invalid IL or missing references)
        //IL_01d7: Unknown result type (might be due to invalid IL or missing references)
        //IL_0247: Unknown result type (might be due to invalid IL or missing references)
        //IL_024c: Unknown result type (might be due to invalid IL or missing references)
        //IL_0261: Unknown result type (might be due to invalid IL or missing references)
        //IL_0266: Unknown result type (might be due to invalid IL or missing references)
        //IL_026f: Unknown result type (might be due to invalid IL or missing references)
        //IL_0274: Unknown result type (might be due to invalid IL or missing references)
        //IL_027d: Unknown result type (might be due to invalid IL or missing references)
        //IL_0282: Unknown result type (might be due to invalid IL or missing references)
        //IL_02f2: Unknown result type (might be due to invalid IL or missing references)
        //IL_02f7: Unknown result type (might be due to invalid IL or missing references)
        //IL_0969: Unknown result type (might be due to invalid IL or missing references)
        //IL_096e: Unknown result type (might be due to invalid IL or missing references)
        //IL_0a31: Unknown result type (might be due to invalid IL or missing references)
        //IL_0a36: Unknown result type (might be due to invalid IL or missing references)
        //IL_0ae0: Unknown result type (might be due to invalid IL or missing references)
        //IL_0ae5: Unknown result type (might be due to invalid IL or missing references)
        //IL_0c68: Unknown result type (might be due to invalid IL or missing references)
        //IL_0c6d: Unknown result type (might be due to invalid IL or missing references)
        //IL_0d6f: Unknown result type (might be due to invalid IL or missing references)
        //IL_0d74: Unknown result type (might be due to invalid IL or missing references)
        //IL_0db4: Unknown result type (might be due to invalid IL or missing references)
        //IL_0db9: Unknown result type (might be due to invalid IL or missing references)
        bool loaded = GetLoaded();
        m_WriteDependencies.Complete();
        m_ReadDependencies.Complete();
        float3 @float = m_PrevCameraPosition;
        float3 float2 = m_PrevCameraDirection;
        float4 float3 = m_PrevLodParameters;
        if (m_CameraUpdateSystem.TryGetLODParameters(out var lodParameters))
        {
            @float = lodParameters.cameraPosition;
            IGameCameraController activeCameraController = m_CameraUpdateSystem.activeCameraController;
            float3 = RenderingUtils.CalculateLodParameters(m_BatchDataSystem.GetLevelOfDetail(m_RenderingSystem.frameLod, activeCameraController), lodParameters);
            float2 = m_CameraUpdateSystem.activeViewer.forward;
        }

        BoundsMask boundsMask = BoundsMask.NormalLayers;
        if (m_UndergroundViewSystem.pipelinesOn)
        {
            boundsMask |= BoundsMask.PipelineLayer;
        }

        if (m_UndergroundViewSystem.subPipelinesOn)
        {
            boundsMask |= BoundsMask.SubPipelineLayer;
        }

        if (m_UndergroundViewSystem.waterwaysOn)
        {
            boundsMask |= BoundsMask.WaterwayLayer;
        }

        if (m_RenderingSystem.markersVisible)
        {
            boundsMask |= BoundsMask.Debug;
        }

        if (m_ResetPrevious)
        {
            m_PrevCameraPosition = @float;
            m_PrevCameraDirection = float2;
            m_PrevLodParameters = float3;
            m_PrevVisibleMask = (BoundsMask)0;
            visibleMask = boundsMask;
            becameVisible = boundsMask;
            becameHidden = (BoundsMask)0;
        }
        else
        {
            visibleMask = boundsMask;
            becameVisible = boundsMask & (BoundsMask)(~(uint)m_PrevVisibleMask);
            becameHidden = m_PrevVisibleMask & (BoundsMask)(~(uint)boundsMask);
        }

        int length = m_CullingData.Length;
        NativeParallelQueue<CullingAction> val = default(NativeParallelQueue<CullingAction>);
        val._002Ector((AllocatorManager.AllocatorHandle)Allocator.TempJob);
        NativeReference<int> cullingDataIndex = new NativeReference<int>(length, Allocator.TempJob);
        NativeQueue<OverflowAction> overflowActions = new NativeQueue<OverflowAction>(Allocator.TempJob);
        NativeArray<int> nodeBuffer = new NativeArray<int>(1536, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<int> subDataBuffer = new NativeArray<int>(1536, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        TreeCullingJob1 treeCullingJob = default(TreeCullingJob1);
        treeCullingJob.m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out var dependencies);
        treeCullingJob.m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out var dependencies2);
        treeCullingJob.m_LaneSearchTree = m_NetSearchSystem.GetLaneSearchTree(readOnly: true, out var dependencies3);
        treeCullingJob.m_LodParameters = float3;
        treeCullingJob.m_PrevLodParameters = m_PrevLodParameters;
        treeCullingJob.m_CameraPosition = @float;
        treeCullingJob.m_PrevCameraPosition = m_PrevCameraPosition;
        treeCullingJob.m_CameraDirection = float2;
        treeCullingJob.m_PrevCameraDirection = m_PrevCameraDirection;
        treeCullingJob.m_VisibleMask = boundsMask;
        treeCullingJob.m_PrevVisibleMask = m_PrevVisibleMask;
        treeCullingJob.m_NodeBuffer = nodeBuffer;
        treeCullingJob.m_SubDataBuffer = subDataBuffer;
        treeCullingJob.m_ActionQueue = val.AsWriter();
        TreeCullingJob1 jobData = treeCullingJob;
        TreeCullingJob2 treeCullingJob2 = default(TreeCullingJob2);
        treeCullingJob2.m_StaticObjectSearchTree = jobData.m_StaticObjectSearchTree;
        treeCullingJob2.m_NetSearchTree = jobData.m_NetSearchTree;
        treeCullingJob2.m_LaneSearchTree = jobData.m_LaneSearchTree;
        treeCullingJob2.m_LodParameters = float3;
        treeCullingJob2.m_PrevLodParameters = m_PrevLodParameters;
        treeCullingJob2.m_CameraPosition = @float;
        treeCullingJob2.m_PrevCameraPosition = m_PrevCameraPosition;
        treeCullingJob2.m_CameraDirection = float2;
        treeCullingJob2.m_PrevCameraDirection = m_PrevCameraDirection;
        treeCullingJob2.m_VisibleMask = boundsMask;
        treeCullingJob2.m_PrevVisibleMask = m_PrevVisibleMask;
        treeCullingJob2.m_NodeBuffer = nodeBuffer;
        treeCullingJob2.m_SubDataBuffer = subDataBuffer;
        treeCullingJob2.m_ActionQueue = val.AsWriter();
        JobHandle jobHandle = treeCullingJob2.Schedule(dependsOn: IJobParallelForExtensions.Schedule(jobData, 3, 1, JobHandle.CombineDependencies(dependencies, dependencies2, dependencies3)), arrayLength: nodeBuffer.Length, innerloopBatchCount: 1);
        JobHandle.ScheduleBatchedJobs();
        m_BatchMeshSystem.CompleteCaching();
        QueryFlags queryFlags = GetQueryFlags();
        InitializeCullingJob initializeCullingJob = default(InitializeCullingJob);
        initializeCullingJob.m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_UpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_BatchesUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_OverriddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Overridden_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_StackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_ObjectMarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Marker_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_NodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_StartNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_EndNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_OrphanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_UtilityLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_UtilityLane_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_NetMarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Marker_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_ZoneBlockType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentTypeHandle, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabLaneGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabCompositionMeshRef = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabCompositionMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef);
        initializeCullingJob.m_EditorMode = m_ToolSystem.actionMode.IsEditor();
        initializeCullingJob.m_UpdateAll = loaded;
        initializeCullingJob.m_UnspawnedVisible = m_RenderingSystem.unspawnedVisible;
        initializeCullingJob.m_DilatedUtilityTypes = m_UndergroundViewSystem.utilityTypes;
        initializeCullingJob.m_TerrainHeightData = m_TerrainSystem.GetHeightData();
        initializeCullingJob.m_CullingData = m_CullingData;
        InitializeCullingJob jobData3 = initializeCullingJob;
        EventCullingJob eventCullingJob = default(EventCullingJob);
        eventCullingJob.m_RentersUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RentersUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        eventCullingJob.m_ColorUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_ColorUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        eventCullingJob.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef);
        eventCullingJob.m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef);
        eventCullingJob.m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef);
        eventCullingJob.m_RouteVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, ref base.CheckedStateRef);
        eventCullingJob.m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef);
        eventCullingJob.m_CullingData = m_CullingData;
        EventCullingJob jobData4 = eventCullingJob;
        QueryCullingJob queryCullingJob = default(QueryCullingJob);
        queryCullingJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
        queryCullingJob.m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef);
        queryCullingJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        queryCullingJob.m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, ref base.CheckedStateRef);
        queryCullingJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentTypeHandle, ref base.CheckedStateRef);
        queryCullingJob.m_LodParameters = float3;
        queryCullingJob.m_CameraPosition = @float;
        queryCullingJob.m_CameraDirection = float2;
        queryCullingJob.m_FrameIndex = m_RenderingSystem.frameIndex;
        queryCullingJob.m_FrameTime = m_RenderingSystem.frameTime;
        queryCullingJob.m_VisibleMask = boundsMask;
        queryCullingJob.m_ActionQueue = val.AsWriter();
        QueryCullingJob jobData5 = queryCullingJob;
        QueryRemoveJob queryRemoveJob = default(QueryRemoveJob);
        queryRemoveJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
        queryRemoveJob.m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        queryRemoveJob.m_AppliedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Applied_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        queryRemoveJob.m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef);
        queryRemoveJob.m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, ref base.CheckedStateRef);
        queryRemoveJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentTypeHandle, ref base.CheckedStateRef);
        queryRemoveJob.m_ActionQueue = val.AsWriter();
        QueryRemoveJob jobData6 = queryRemoveJob;
        RelativeCullingJob relativeCullingJob = default(RelativeCullingJob);
        relativeCullingJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
        relativeCullingJob.m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        relativeCullingJob.m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        relativeCullingJob.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, ref base.CheckedStateRef);
        relativeCullingJob.m_LodParameters = float3;
        relativeCullingJob.m_CameraPosition = @float;
        relativeCullingJob.m_CameraDirection = float2;
        relativeCullingJob.m_VisibleMask = boundsMask;
        relativeCullingJob.m_ActionQueue = val.AsWriter();
        RelativeCullingJob jobData7 = relativeCullingJob;
        TempCullingJob tempCullingJob = default(TempCullingJob);
        tempCullingJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
        tempCullingJob.m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        tempCullingJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        tempCullingJob.m_StackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        tempCullingJob.m_StaticType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Static_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        tempCullingJob.m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        tempCullingJob.m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        tempCullingJob.m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
        tempCullingJob.m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef);
        tempCullingJob.m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef);
        tempCullingJob.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, ref base.CheckedStateRef);
        tempCullingJob.m_LodParameters = float3;
        tempCullingJob.m_CameraPosition = @float;
        tempCullingJob.m_CameraDirection = float2;
        tempCullingJob.m_VisibleMask = boundsMask;
        tempCullingJob.m_TerrainHeightData = jobData3.m_TerrainHeightData;
        tempCullingJob.m_ActionQueue = val.AsWriter();
        TempCullingJob jobData8 = tempCullingJob;
        EntityQuery query = (loaded ? m_CullingInfoQuery : m_InitializeQuery);
        EntityQuery cullingQuery = GetCullingQuery(queryFlags);
        EntityQuery relativeQuery = GetRelativeQuery(queryFlags);
        EntityQuery removeQuery = GetRemoveQuery(m_PrevQueryFlags & ~queryFlags);
        JobHandle dependsOn3 = JobChunkExtensions.Schedule(dependsOn: JobChunkExtensions.ScheduleParallel(jobData3, query, base.Dependency), jobData: jobData4, query: m_EventQuery);
        JobHandle dependsOn4 = JobChunkExtensions.ScheduleParallel(jobData5, cullingQuery, dependsOn3);
        JobHandle dependsOn5 = JobChunkExtensions.ScheduleParallel(jobData6, removeQuery, dependsOn4);
        JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.ScheduleParallel(jobData7, relativeQuery, dependsOn5), jobData: jobData8, query: m_TempQuery);
        if (m_ResetPrevious || becameHidden != 0)
        {
            VerifyVisibleJob jobData9 = default(VerifyVisibleJob);
            jobData9.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef);
            jobData9.m_LodParameters = float3;
            jobData9.m_CameraPosition = @float;
            jobData9.m_CameraDirection = float2;
            jobData9.m_VisibleMask = boundsMask;
            jobData9.m_CullingData = m_CullingData;
            jobData9.m_ActionQueue = val.AsWriter();
            JobHandle job = IJobParallelForExtensions.Schedule(jobData9, length, 16, jobHandle2);
            m_WriteDependencies = JobHandle.CombineDependencies(jobHandle, job);
        }
        else
        {
            m_WriteDependencies = JobHandle.CombineDependencies(jobHandle, jobHandle2);
        }

        CullingActionJob cullingActionJob = default(CullingActionJob);
        cullingActionJob.m_CullingActions = val.AsReader();
        cullingActionJob.m_OverflowActions = overflowActions.AsParallelWriter();
        cullingActionJob.m_CullingInfo = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, ref base.CheckedStateRef);
        cullingActionJob.m_CullingData = m_CullingData;
        cullingActionJob.m_CullingDataIndex = cullingDataIndex;
        CullingActionJob jobData10 = cullingActionJob;
        ResizeCullingDataJob resizeCullingDataJob = default(ResizeCullingDataJob);
        resizeCullingDataJob.m_CullingDataIndex = cullingDataIndex;
        resizeCullingDataJob.m_CullingData = m_CullingData;
        resizeCullingDataJob.m_UpdatedData = m_UpdatedData;
        resizeCullingDataJob.m_OverflowActions = overflowActions;
        ResizeCullingDataJob jobData11 = resizeCullingDataJob;
        FilterUpdatesJob filterUpdatesJob = default(FilterUpdatesJob);
        filterUpdatesJob.m_CreatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Created_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_AppliedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Applied_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_BatchesUpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_ObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Object_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_ObjectGeometry_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_ObjectColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Color_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_PlantData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_RelativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_ExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Extension_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_NodeColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_EdgeColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_LaneColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneColor_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_LaneConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneCondition_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_ZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_AnimatedData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Animated_RO_BufferLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_SkeletonData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RO_BufferLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_EmissiveData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Emissive_RO_BufferLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_MeshColorData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_EffectInstances = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Effects_EnabledEffect_RO_BufferLookup, ref base.CheckedStateRef);
        filterUpdatesJob.m_TimerDelta = m_RenderingSystem.lodTimerDelta;
        filterUpdatesJob.m_CullingData = m_CullingData;
        filterUpdatesJob.m_UpdatedCullingData = m_UpdatedData.AsParallelWriter();
        FilterUpdatesJob jobData12 = filterUpdatesJob;
        JobHandle jobHandle3 = IJobParallelForExtensions.Schedule(jobData10, val.HashRange, 1, m_WriteDependencies);
        JobHandle jobHandle4 = IJobExtensions.Schedule(jobData11, jobHandle3);
        JobHandle jobHandle5 = jobData12.Schedule(m_CullingData, 16, jobHandle4);
        m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
        m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
        m_NetSearchSystem.AddLaneSearchTreeReader(jobHandle);
        m_TerrainSystem.AddCPUHeightReader(jobHandle2);
        val.Dispose(jobHandle3);
        cullingDataIndex.Dispose(jobHandle4);
        overflowActions.Dispose(jobHandle4);
        nodeBuffer.Dispose(jobHandle);
        subDataBuffer.Dispose(jobHandle);
        m_PrevCameraPosition = @float;
        m_PrevCameraDirection = float2;
        m_PrevLodParameters = float3;
        m_PrevVisibleMask = boundsMask;
        m_PrevQueryFlags = queryFlags;
        m_ResetPrevious = false;
        m_WriteDependencies = jobHandle5;
        m_ReadDependencies = default(JobHandle);
        base.Dependency = jobHandle5;
    }

    private void InitializeCullingData()
    {
        m_CullingData.Clear();
        ref NativeList<PreCullingData> cullingData = ref m_CullingData;
        PreCullingData value = new PreCullingData
        {
            m_Entity = m_FadeContainer,
            m_Flags = (PreCullingFlags.PassedCulling | PreCullingFlags.NearCamera | PreCullingFlags.FadeContainer)
        };
        cullingData.Add(in value);
    }

    private EntityQuery GetCullingQuery(QueryFlags flags)
    {
        if (!m_CullingQueries.TryGetValue(flags, out var value))
        {
            List<ComponentType> list = new List<ComponentType>
            {
                ComponentType.ReadOnly<Moving>(),
                ComponentType.ReadOnly<Stopped>(),
                ComponentType.ReadOnly<Updated>()
            };
            List<ComponentType> list2 = new List<ComponentType>
            {
                ComponentType.ReadOnly<Relative>(),
                ComponentType.ReadOnly<Temp>(),
                ComponentType.ReadOnly<Deleted>()
            };
            if ((flags & QueryFlags.Unspawned) == 0)
            {
                list2.Add(ComponentType.ReadOnly<Unspawned>());
            }

            if ((flags & QueryFlags.Zones) != 0)
            {
                list.Add(ComponentType.ReadOnly<Block>());
            }
            else
            {
                list2.Add(ComponentType.ReadOnly<Block>());
            }

            value = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadWrite<CullingInfo>() },
                Any = list.ToArray(),
                None = list2.ToArray()
            });
            m_CullingQueries.Add(flags, value);
        }

        return value;
    }

    private EntityQuery GetRelativeQuery(QueryFlags flags)
    {
        flags &= QueryFlags.Unspawned;
        if (!m_RelativeQueries.TryGetValue(flags, out var value))
        {
            List<ComponentType> list = new List<ComponentType>
            {
                ComponentType.ReadOnly<Temp>(),
                ComponentType.ReadOnly<Deleted>()
            };
            if ((flags & QueryFlags.Unspawned) == 0)
            {
                list.Add(ComponentType.ReadOnly<Unspawned>());
            }

            value = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                    ComponentType.ReadOnly<Relative>(),
                    ComponentType.ReadWrite<CullingInfo>()
                },
                None = list.ToArray()
            });
            m_RelativeQueries.Add(flags, value);
        }

        return value;
    }

    private EntityQuery GetRemoveQuery(QueryFlags flags)
    {
        if (!m_RemoveQueries.TryGetValue(flags, out var value))
        {
            List<ComponentType> list = new List<ComponentType> { ComponentType.ReadOnly<Deleted>() };
            if ((flags & QueryFlags.Zones) != 0)
            {
                list.Add(ComponentType.ReadOnly<Block>());
            }

            if ((flags & QueryFlags.Unspawned) != 0)
            {
                list.Add(ComponentType.ReadOnly<Unspawned>());
            }

            value = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadWrite<CullingInfo>() },
                Any = list.ToArray()
            });
            m_RemoveQueries.Add(flags, value);
        }

        return value;
    }

    private QueryFlags GetQueryFlags()
    {
        QueryFlags queryFlags = (QueryFlags)0;
        if (m_RenderingSystem.unspawnedVisible)
        {
            queryFlags |= QueryFlags.Unspawned;
        }

        if (m_ToolSystem.activeTool != null && m_ToolSystem.activeTool.requireZones)
        {
            queryFlags |= QueryFlags.Zones;
        }

        return queryFlags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
        new EntityQueryBuilder(Allocator.Temp).Dispose();
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref base.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
    }

    [Preserve]
    public PreCullingSystem()
    {
    }
}
