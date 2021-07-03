/* using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(TerrainSystemGroup))]
    [UpdateAfter(typeof(TerrainChunkStateSystem))]
    public sealed class TerrainNoiseChunkUpdateSystem : SystemBase
    {
        private EntityQuery query;
        private List<Mesh> meshBuffer;
        private EntityCommandBufferSystem entityCommandBufferSystem;
        private ComputeShader computeShader;
        private ComputeBuffer buffer;
        private static readonly ProfilerMarker marker = new ProfilerMarker("MC Pass");
        protected override void OnCreate()
        {

            RequireSingletonForUpdate<ChunksToLoadBuffer>();
            RequireSingletonForUpdate<TerrainProducer>();
            RequireSingletonForUpdate<NoiseTerrainProducer>();
            RequireSingletonForUpdate<TerrainMaterial>();
            query = GetEntityQuery(ComponentType.ReadOnly<NoiseTerrainChunk>(), ComponentType.ReadOnly<TerrainEntityData>(), ComponentType.ReadOnly<RenderMesh>());
            query.AddChangedVersionFilter(ComponentType.ReadOnly<TerrainEntityData>());
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            meshBuffer = new List<Mesh>();
        }
        protected override void OnUpdate()
        {
            var entities = query.ToEntityArray(Unity.Collections.Allocator.TempJob);
            if (entities.Length > 0)
            {
                var ecb = entityCommandBufferSystem.CreateCommandBuffer();

                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    meshBuffer.Add(EntityManager.GetSharedComponentData<RenderMesh>(entity).mesh);
                }
                var chunkLocations = query.ToComponentDataArray<TerrainEntityData>(Allocator.TempJob);
                var chunks = new NativeArray<MarchingCubeChunk>(entities.Length, Allocator.TempJob);
                marker.Begin();
                var generateMCChunksJob = new MCCubeGeneration
                {
                    chunks = chunks,
                    chunkInfo = chunkLocations,
                    IsoValue = 0.5f
                }.Schedule(chunkLocations.Length * MarchingCubes.PaddedChunkCubeSizeInCells, 1);

                var mcJob = MarchingCubes.GenerateMesh(chunks, out var meshDataArray, generateMCChunksJob);
                var cleanup = JobHandle.CombineDependencies(chunks.Dispose(mcJob), chunkLocations.Dispose(mcJob), entities.Dispose(mcJob));
                cleanup.Complete();
                marker.End();
                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, meshBuffer, UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds | UnityEngine.Rendering.MeshUpdateFlags.DontValidateIndices);
                for (int i = 0; i < entities.Length; i++)
                {
                    var chunk = chunkLocations[i].chunk;
                    ecb.SetComponent<LocalToWorld>(entities[i], new LocalToWorld
                    {
                        Value = float4x4.TRS(math.float3(chunk.x * MarchingCubes.ChunkSizeInCells, 0, chunk.y * MarchingCubes.ChunkSizeInCells), quaternion.identity, 1)
                    });
                }
                meshBuffer.Clear();
                entityCommandBufferSystem.AddJobHandleForProducer(Dependency);

            }
            else
            {
                entities.Dispose();
            }
        }


        [BurstCompile]
        internal unsafe struct MCCubeGeneration : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<MarchingCubeChunk> chunks;
            [ReadOnly]
            public NativeArray<TerrainEntityData> chunkInfo;

            public float IsoValue;
            private static float EaseIn(float v)
            {
                return v * v;
            }
            public void Execute(int index)
            {
                var chunkIndex = index / (MarchingCubes.PaddedChunkCubeSizeInCells);
                var relativeValue = index % (MarchingCubes.PaddedChunkCubeSizeInCells);
                var currentChunkInfo = chunkInfo[chunkIndex];
                int3 cellPos = math.int3(
                                (relativeValue % (MarchingCubes.PaddedChunkLayerSizeInCells)) % MarchingCubes.PaddedChunkSizeInCells,
                                relativeValue / (MarchingCubes.PaddedChunkLayerSizeInCells),
                                (relativeValue % (MarchingCubes.PaddedChunkLayerSizeInCells)) / MarchingCubes.PaddedChunkSizeInCells
                ) - 1;
                int3 chunkPos = math.int3(
                    currentChunkInfo.chunk.x * MarchingCubes.ChunkSizeInCells,
                    0,
                    currentChunkInfo.chunk.y * MarchingCubes.ChunkSizeInCells
                );

                byte cubeCase = 0;
                for (int i = 0; i < 8; i++)
                {
                    MarchingCubes.GetVertex(i, out var t);
                    var p = cellPos + t;
                    float3 offset = math.float3(
                      p.x / ((float)MarchingCubes.ChunkSizeInCells),
                      p.y / ((float)MarchingCubes.ChunkSizeInCells),
                      p.z / ((float)MarchingCubes.ChunkSizeInCells)
                    );
                    //* (((float)(MarchingCubes.ChunkSize - p.y)) / MarchingCubes.ChunkSize);
                    //var samplePoint = offset + math.int3(chunkEntity.chunk.x, 0, chunkEntity.chunk.y);
                    //float value = noise.pnoise(samplePoint,MarchingCubes.ChunkSize);
                    var sample1 = math.unlerp(-1, 1, noise.pnoise(math.float2(offset.x + currentChunkInfo.chunk.x, offset.z + currentChunkInfo.chunk.y), MarchingCubes.ChunkSizeInCells));
                    var sample2 = noise.cellular(math.float2(offset.x + currentChunkInfo.chunk.x, offset.z + currentChunkInfo.chunk.y)).x;
                    var sample3 = noise.cellular(math.float3(offset.x + currentChunkInfo.chunk.x, offset.y, offset.z + currentChunkInfo.chunk.y)).x;
                    var height = sample1 * 6;
                    if ((p.y < 1) || (p.y < sample1 * 12 && sample2 < 0.7f))
                    {
                        cubeCase |= (byte)(1 << i);
                    }
                }
                if (cubeCase != 0)
                {
                    var ptr = (MarchingCubeChunk*)chunks.GetUnsafePtr();
                    UnsafeUtility.WriteArrayElement<byte>(ptr + chunkIndex, relativeValue, cubeCase);
                }



            }
        }
    }
} */