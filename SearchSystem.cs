using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class SearchSystem : GameSystemBase, IPreDeserialize
{
    [BurstCompile]
    private struct UpdateSearchTreeJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<Owner> m_OwnerType;

        [ReadOnly]
        public ComponentTypeHandle<Transform> m_TransformType;

        [ReadOnly]
        public ComponentTypeHandle<Stack> m_StackType;

        [ReadOnly]
        public ComponentTypeHandle<Marker> m_MarkerType;

        [ReadOnly]
        public ComponentTypeHandle<OutsideConnection> m_OutsideConnectionType;

        [ReadOnly]
        public ComponentTypeHandle<Tree> m_TreeType;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

        [ReadOnly]
        public ComponentTypeHandle<Created> m_CreatedType;

        [ReadOnly]
        public ComponentTypeHandle<Deleted> m_DeletedType;

        [ReadOnly]
        public ComponentTypeHandle<Overridden> m_OverriddenType;

        [ReadOnly]
        public ComponentTypeHandle<CullingInfo> m_CullingInfoType;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

        [ReadOnly]
        public ComponentLookup<StackData> m_PrefabStackData;

        [ReadOnly]
        public ComponentLookup<NetData> m_PrefabNetData;

        [ReadOnly]
        public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

        [ReadOnly]
        public bool m_EditorMode;

        [ReadOnly]
        public bool m_Loaded;

        public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            if (chunk.Has(ref m_DeletedType))
            {
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    Entity entity = nativeArray[i];
                    m_SearchTree.TryRemove(entity);
                }

                return;
            }

            if (m_Loaded || chunk.Has(ref m_CreatedType))
            {
                NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
                NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
                NativeArray<Stack> nativeArray4 = chunk.GetNativeArray(ref m_StackType);
                NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
                bool flag = chunk.Has(ref m_MarkerType) && !chunk.Has(ref m_OutsideConnectionType);
                bool flag2 = chunk.Has(ref m_OverriddenType);
                bool flag3 = chunk.Has(ref m_TreeType);
                bool flag4 = chunk.Has(ref m_CullingInfoType);
                Owner owner = default(Owner);
                for (int j = 0; j < nativeArray.Length; j++)
                {
                    Entity entity2 = nativeArray[j];
                    PrefabRef prefabRef = nativeArray2[j];
                    Transform transform = nativeArray3[j];
                    if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
                    {
                        StackData componentData2;
                        Bounds3 bounds = ((nativeArray4.Length == 0 || !m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out componentData2)) ? ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, componentData) : ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, nativeArray4[j], componentData, componentData2));
                        BoundsMask boundsMask = BoundsMask.Debug;
                        if (!flag)
                        {
                            if (flag3)
                            {
                                boundsMask |= BoundsMask.IsTree;
                            }

                            if ((componentData.m_Flags & GeometryFlags.OccupyZone) != 0)
                            {
                                boundsMask |= BoundsMask.OccupyZone;
                            }

                            if ((componentData.m_Flags & GeometryFlags.WalkThrough) == 0)
                            {
                                boundsMask |= BoundsMask.NotWalkThrough;
                            }

                            if ((componentData.m_Flags & GeometryFlags.HasLot) != 0)
                            {
                                boundsMask |= BoundsMask.HasLot;
                            }
                        }

                        if (!flag2)
                        {
                            boundsMask |= BoundsMask.NotOverridden;
                            if (!flag || m_EditorMode)
                            {
                                MeshLayer layers = componentData.m_Layers;
                                CollectionUtils.TryGet<Owner>(nativeArray5, j, ref owner);
                                boundsMask |= CommonUtils.GetBoundsMask(Game.Net.SearchSystem.GetLayers(owner, default(Game.Net.UtilityLane), layers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData));
                            }
                        }

                        if (!flag4)
                        {
                            boundsMask &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
                        }

                        m_SearchTree.Add(entity2, new QuadTreeBoundsXZ(bounds, boundsMask, componentData.m_MinLod));
                    }
                    else
                    {
                        Bounds3 bounds2 = new Bounds3(transform.m_Position - 1f, transform.m_Position + 1f);
                        int lod = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(2f)));
                        BoundsMask boundsMask2 = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
                        if (!flag4)
                        {
                            boundsMask2 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
                        }

                        m_SearchTree.Add(entity2, new QuadTreeBoundsXZ(bounds2, boundsMask2, lod));
                    }
                }

                return;
            }

            NativeArray<PrefabRef> nativeArray6 = chunk.GetNativeArray(ref m_PrefabRefType);
            NativeArray<Transform> nativeArray7 = chunk.GetNativeArray(ref m_TransformType);
            NativeArray<Stack> nativeArray8 = chunk.GetNativeArray(ref m_StackType);
            NativeArray<Owner> nativeArray9 = chunk.GetNativeArray(ref m_OwnerType);
            bool flag5 = chunk.Has(ref m_MarkerType) && !chunk.Has(ref m_OutsideConnectionType);
            bool flag6 = chunk.Has(ref m_OverriddenType);
            bool flag7 = chunk.Has(ref m_TreeType);
            bool flag8 = chunk.Has(ref m_CullingInfoType);
            Owner owner2 = default(Owner);
            for (int k = 0; k < nativeArray.Length; k++)
            {
                Entity entity3 = nativeArray[k];
                PrefabRef prefabRef2 = nativeArray6[k];
                Transform transform2 = nativeArray7[k];
                if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData3))
                {
                    StackData componentData4;
                    Bounds3 bounds3 = ((nativeArray8.Length == 0 || !m_PrefabStackData.TryGetComponent(prefabRef2.m_Prefab, out componentData4)) ? ObjectUtils.CalculateBounds(transform2.m_Position, transform2.m_Rotation, componentData3) : ObjectUtils.CalculateBounds(transform2.m_Position, transform2.m_Rotation, nativeArray8[k], componentData3, componentData4));
                    BoundsMask boundsMask3 = BoundsMask.Debug;
                    if (!flag5)
                    {
                        if (flag7)
                        {
                            boundsMask3 |= BoundsMask.IsTree;
                        }

                        if ((componentData3.m_Flags & GeometryFlags.OccupyZone) != 0)
                        {
                            boundsMask3 |= BoundsMask.OccupyZone;
                        }

                        if ((componentData3.m_Flags & GeometryFlags.WalkThrough) == 0)
                        {
                            boundsMask3 |= BoundsMask.NotWalkThrough;
                        }

                        if ((componentData3.m_Flags & GeometryFlags.HasLot) != 0)
                        {
                            boundsMask3 |= BoundsMask.HasLot;
                        }
                    }

                    if (!flag6)
                    {
                        boundsMask3 |= BoundsMask.NotOverridden;
                        if (!flag5 || m_EditorMode)
                        {
                            MeshLayer layers2 = componentData3.m_Layers;
                            CollectionUtils.TryGet<Owner>(nativeArray9, k, ref owner2);
                            boundsMask3 |= CommonUtils.GetBoundsMask(Game.Net.SearchSystem.GetLayers(owner2, default(Game.Net.UtilityLane), layers2, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData));
                        }
                    }

                    if (!flag8)
                    {
                        boundsMask3 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
                    }

                    m_SearchTree.Update(entity3, new QuadTreeBoundsXZ(bounds3, boundsMask3, componentData3.m_MinLod));
                }
                else
                {
                    Bounds3 bounds4 = new Bounds3(transform2.m_Position - 1f, transform2.m_Position + 1f);
                    int lod2 = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(2f)));
                    BoundsMask boundsMask4 = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
                    if (!flag8)
                    {
                        boundsMask4 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
                    }

                    m_SearchTree.Update(entity3, new QuadTreeBoundsXZ(bounds4, boundsMask4, lod2));
                }
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Stack> __Game_Objects_Stack_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Marker> __Game_Objects_Marker_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Tree> __Game_Objects_Tree_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Overridden> __Game_Common_Overridden_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
            __Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
            __Game_Objects_Stack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stack>(isReadOnly: true);
            __Game_Objects_Marker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Marker>(isReadOnly: true);
            __Game_Objects_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OutsideConnection>(isReadOnly: true);
            __Game_Objects_Tree_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Tree>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
            __Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
            __Game_Common_Overridden_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Overridden>(isReadOnly: true);
            __Game_Rendering_CullingInfo_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CullingInfo>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
            __Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
            __Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
            __Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
        }
    }

    private ToolSystem m_ToolSystem;

    private EntityQuery m_UpdatedStaticsQuery;

    private EntityQuery m_AllStaticsQuery;

    private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticSearchTree;

    private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingSearchTree;

    private JobHandle m_StaticReadDependencies;

    private JobHandle m_StaticWriteDependencies;

    private JobHandle m_MovingReadDependencies;

    private JobHandle m_MovingWriteDependencies;

    private bool m_Loaded;

    private TypeHandle __TypeHandle;

    [Preserve]
    protected override void OnCreate()
    {
        //IL_00d0: Unknown result type (might be due to invalid IL or missing references)
        //IL_00d5: Unknown result type (might be due to invalid IL or missing references)
        //IL_00e1: Unknown result type (might be due to invalid IL or missing references)
        //IL_00e6: Unknown result type (might be due to invalid IL or missing references)
        base.OnCreate();
        m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
        m_UpdatedStaticsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[2]
            {
                ComponentType.ReadOnly<Object>(),
                ComponentType.ReadOnly<Static>()
            },
            Any = new ComponentType[2]
            {
                ComponentType.ReadOnly<Updated>(),
                ComponentType.ReadOnly<Deleted>()
            },
            None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
        });
        m_AllStaticsQuery = GetEntityQuery(ComponentType.ReadOnly<Object>(), ComponentType.ReadOnly<Static>(), ComponentType.Exclude<Temp>());
        m_StaticSearchTree = new NativeQuadTree<Entity, QuadTreeBoundsXZ>(1f, Allocator.Persistent);
        m_MovingSearchTree = new NativeQuadTree<Entity, QuadTreeBoundsXZ>(1f, Allocator.Persistent);
    }

    [Preserve]
    protected override void OnDestroy()
    {
        m_StaticReadDependencies.Complete();
        m_StaticWriteDependencies.Complete();
        m_StaticSearchTree.Dispose();
        m_MovingReadDependencies.Complete();
        m_MovingWriteDependencies.Complete();
        m_MovingSearchTree.Dispose();
        base.OnDestroy();
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
        //IL_023d: Unknown result type (might be due to invalid IL or missing references)
        //IL_0242: Unknown result type (might be due to invalid IL or missing references)
        bool loaded = GetLoaded();
        EntityQuery query = (loaded ? m_AllStaticsQuery : m_UpdatedStaticsQuery);
        if (!query.IsEmptyIgnoreFilter)
        {
            UpdateSearchTreeJob updateSearchTreeJob = default(UpdateSearchTreeJob);
            updateSearchTreeJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_StackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_MarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Marker_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_TreeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_OverriddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Overridden_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentTypeHandle, ref base.CheckedStateRef);
            updateSearchTreeJob.m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
            updateSearchTreeJob.m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef);
            updateSearchTreeJob.m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef);
            updateSearchTreeJob.m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef);
            updateSearchTreeJob.m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef);
            updateSearchTreeJob.m_EditorMode = m_ToolSystem.actionMode.IsEditor();
            updateSearchTreeJob.m_Loaded = loaded;
            updateSearchTreeJob.m_SearchTree = GetStaticSearchTree(readOnly: false, out var dependencies);
            UpdateSearchTreeJob jobData = updateSearchTreeJob;
            base.Dependency = JobChunkExtensions.Schedule(jobData, query, JobHandle.CombineDependencies(base.Dependency, dependencies));
            AddStaticSearchTreeWriter(base.Dependency);
        }
    }

    public NativeQuadTree<Entity, QuadTreeBoundsXZ> GetStaticSearchTree(bool readOnly, out JobHandle dependencies)
    {
        //IL_0023: Unknown result type (might be due to invalid IL or missing references)
        dependencies = (readOnly ? m_StaticWriteDependencies : JobHandle.CombineDependencies(m_StaticReadDependencies, m_StaticWriteDependencies));
        return m_StaticSearchTree;
    }

    public NativeQuadTree<Entity, QuadTreeBoundsXZ> GetMovingSearchTree(bool readOnly, out JobHandle dependencies)
    {
        //IL_0023: Unknown result type (might be due to invalid IL or missing references)
        dependencies = (readOnly ? m_MovingWriteDependencies : JobHandle.CombineDependencies(m_MovingReadDependencies, m_MovingWriteDependencies));
        return m_MovingSearchTree;
    }

    public void AddStaticSearchTreeReader(JobHandle jobHandle)
    {
        m_StaticReadDependencies = JobHandle.CombineDependencies(m_StaticReadDependencies, jobHandle);
    }

    public void AddStaticSearchTreeWriter(JobHandle jobHandle)
    {
        m_StaticWriteDependencies = jobHandle;
    }

    public void AddMovingSearchTreeReader(JobHandle jobHandle)
    {
        m_MovingReadDependencies = JobHandle.CombineDependencies(m_MovingReadDependencies, jobHandle);
    }

    public void AddMovingSearchTreeWriter(JobHandle jobHandle)
    {
        m_MovingWriteDependencies = jobHandle;
    }

    public void PreDeserialize(Context context)
    {
        //IL_0004: Unknown result type (might be due to invalid IL or missing references)
        //IL_0009: Unknown result type (might be due to invalid IL or missing references)
        //IL_000e: Unknown result type (might be due to invalid IL or missing references)
        //IL_0013: Unknown result type (might be due to invalid IL or missing references)
        JobHandle dependencies;
        NativeQuadTree<Entity, QuadTreeBoundsXZ> staticSearchTree = GetStaticSearchTree(readOnly: false, out dependencies);
        JobHandle dependencies2;
        NativeQuadTree<Entity, QuadTreeBoundsXZ> movingSearchTree = GetMovingSearchTree(readOnly: false, out dependencies2);
        dependencies.Complete();
        dependencies2.Complete();
        staticSearchTree.Clear();
        movingSearchTree.Clear();
        m_Loaded = true;
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
    public SearchSystem()
    {
    }
}

