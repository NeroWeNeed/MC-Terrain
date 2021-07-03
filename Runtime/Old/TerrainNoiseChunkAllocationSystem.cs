/* using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(TerrainSystemGroup))]
    [UpdateAfter(typeof(TerrainChunkStateSystem))]
    public sealed class TerrainNoiseChunkAllocationSystem : SystemBase
    {
        private EntityCommandBufferSystem entityCommandBufferSystem;
        private TerrainChunkStateSystem chunkStateSystem;
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<ChunksToLoadBuffer>();
            RequireSingletonForUpdate<TerrainProducer>();
            RequireSingletonForUpdate<TerrainMaterial>();
            RequireSingletonForUpdate<NoiseTerrainProducer>();
            chunkStateSystem = World.GetOrCreateSystem<TerrainChunkStateSystem>();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            chunkStateSystem.ChunkStateHandle.Complete();
            var chunksToLoad = GetBuffer<ChunksToLoadBuffer>(GetSingletonEntity<ChunksToLoadBuffer>());
            if (chunksToLoad.Length <= 0)
                return;
            var ecb = entityCommandBufferSystem.CreateCommandBuffer();
            var material = EntityManager.GetSharedComponentData<TerrainMaterial>(GetSingletonEntity<TerrainMaterial>());

                        var data = chunksToLoad[0];
                        if (data.chunkEntity == Entity.Null)
                        {
                            var e = ecb.CreateEntity();
                            ecb.AddComponent<NoiseTerrainChunk>(e);
                            ecb.AddComponent(e, new TerrainEntityData
                            {
                                chunk = data.chunk
                            });
                            ecb.AddComponent<LocalToWorld>(e, new LocalToWorld
                            {
                                Value = float4x4.TRS(math.float3(data.chunk.x * MarchingCubes.ChunkSizeInCells, 0, data.chunk.y * MarchingCubes.ChunkSizeInCells), quaternion.identity, 1)
                            });
                            ecb.AddComponent<RenderBounds>(e, new RenderBounds
                            {
                                Value = MarchingCubes.ChunkBounds.ToAABB()
                            });
                            ecb.AddComponent<Static>(e);
                            RenderMeshUtility.AddComponents(e, ecb, new RenderMeshDescription(new Mesh
                            {
                                bounds = MarchingCubes.ChunkBounds
                            }, material,
                                        UnityEngine.Rendering.ShadowCastingMode.On, true, MotionVectorGenerationMode.Camera, 0, 0, 1, UnityEngine.Rendering.LightProbeUsage.BlendProbes));
                        }
                        else
                        {
                            ecb.SetComponent<TerrainEntityData>(data.chunkEntity, new TerrainEntityData
                            {
                                chunk = data.chunk
                            });
                            ecb.SetComponent<LocalToWorld>(data.chunkEntity, new LocalToWorld
                            {
                                Value = float4x4.TRS(math.float3(data.chunk.x * MarchingCubes.ChunkSizeInCells, 0, data.chunk.y * MarchingCubes.ChunkSizeInCells), quaternion.identity, 1)
                            });
                        }
                        chunksToLoad.RemoveAtSwapBack(0);

            for (int i = 0; i < chunksToLoad.Length; i++)
            {
                var data = chunksToLoad[i];
                if (data.chunkEntity == Entity.Null)
                {
                    var e = ecb.CreateEntity();
                    ecb.AddComponent<NoiseTerrainChunk>(e);
                    ecb.AddComponent(e, new TerrainEntityData
                    {
                        chunk = data.chunk
                    });
                    ecb.AddComponent<LocalToWorld>(e, new LocalToWorld
                    {
                        Value = float4x4.TRS(math.float3(data.chunk.x * MarchingCubes.ChunkSizeInCells, 0, data.chunk.y * MarchingCubes.ChunkSizeInCells), quaternion.identity, 1)
                    });
                    ecb.AddComponent<RenderBounds>(e, new RenderBounds
                    {
                        Value = MarchingCubes.ChunkBounds.ToAABB()
                    });
                    ecb.AddComponent<Static>(e);
                    RenderMeshUtility.AddComponents(e, ecb, new RenderMeshDescription(new Mesh
                    {
                        bounds = MarchingCubes.ChunkBounds
                    }, material,
                                UnityEngine.Rendering.ShadowCastingMode.On, true, MotionVectorGenerationMode.Camera, 0, 0, 1, UnityEngine.Rendering.LightProbeUsage.BlendProbes));
                }
                else
                {
                    ecb.SetComponent<TerrainEntityData>(data.chunkEntity, new TerrainEntityData
                    {
                        chunk = data.chunk
                    });
                    ecb.SetComponent<LocalToWorld>(data.chunkEntity, new LocalToWorld
                    {
                        Value = float4x4.TRS(math.float3(data.chunk.x * MarchingCubes.ChunkSizeInCells, 0, data.chunk.y * MarchingCubes.ChunkSizeInCells), quaternion.identity, 1)
                    });
                }

            }
            chunksToLoad.Clear();
        }
    }
} */