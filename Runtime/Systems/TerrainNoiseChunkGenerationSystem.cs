using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(TerrainSystemGroup))]
    public sealed class TerrainNoiseChunkGenerationSystem : SystemBase
    {
        [Unity.Burst.BurstCompile]
        internal unsafe struct ChunkGeneration : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<MarchingCubeChunk> chunkData;
            [ReadOnly]
            public NativeArray<int2> chunkLocation;
            public float IsoValue;
            private static float EaseIn(float v)
            {
                return v * v;
            }
            public void Execute(int index)
            {
                var chunkIndex = index / (MarchingCubes.PaddedChunkCubeSizeInCells);
                var relativeValue = index % (MarchingCubes.PaddedChunkCubeSizeInCells);
                var currentChunkInfo = chunkLocation[chunkIndex];
                int3 cellPos = math.int3(
                                (relativeValue % (MarchingCubes.PaddedChunkLayerSizeInCells)) % MarchingCubes.PaddedChunkSizeInCells,
                                relativeValue / (MarchingCubes.PaddedChunkLayerSizeInCells),
                                (relativeValue % (MarchingCubes.PaddedChunkLayerSizeInCells)) / MarchingCubes.PaddedChunkSizeInCells
                ) - 1;
                int3 chunkPos = math.int3(
                    currentChunkInfo.x * MarchingCubes.ChunkSizeInCells,
                    0,
                    currentChunkInfo.y * MarchingCubes.ChunkSizeInCells
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
                    var sample1 = math.unlerp(-1, 1, noise.pnoise(math.float2(offset.x + currentChunkInfo.x, offset.z + currentChunkInfo.y), MarchingCubes.ChunkSizeInCells));
                    var sample2 = noise.cellular(math.float2(offset.x + currentChunkInfo.x, offset.z + currentChunkInfo.y)).x;
                    var sample3 = noise.cellular(math.float3(offset.x + currentChunkInfo.x, offset.y, offset.z + currentChunkInfo.y)).x;
                    var height = sample1 * 6;

                    if ((p.y < 1) || (p.y < sample1 * 12 && sample2 < 0.7f))
                    {
                        cubeCase |= (byte)(1 << i);
                    }
                }
                if (cubeCase != 0)
                {
                    var ptr = (MarchingCubeChunk*)chunkData.GetUnsafePtr();
                    UnsafeUtility.WriteArrayElement<byte>(ptr + chunkIndex, relativeValue, cubeCase);
                }



            }
        }

        private int lastVersion;
        private TerrainChunkSystem terrainChunkSystem;
        private TerrainGPUDispatchSystem terrainGPUDispatchSystem;

        protected override void OnCreate()
        {
            terrainChunkSystem = World.GetOrCreateSystem<TerrainChunkSystem>();
            terrainGPUDispatchSystem = World.GetOrCreateSystem<TerrainGPUDispatchSystem>();
            RequireSingletonForUpdate<TerrainChunkData>();
            RequireSingletonForUpdate<TerrainIsoValue>();

        }
        protected unsafe override void OnUpdate()
        {
            terrainChunkSystem.WaitForCompletion();
            var version = terrainChunkSystem.GetLoadedChunkSetVersion();
            if (lastVersion != version)
            {
                
                var chunks = terrainChunkSystem.GetLoadedChunks();
                var chunkData = new NativeArray<MarchingCubeChunk>(chunks.Length, Allocator.TempJob);
                Dependency = new ChunkGeneration
                {
                    chunkLocation = chunks,
                    chunkData = chunkData,
                    IsoValue = GetSingleton<TerrainIsoValue>()
                }.Schedule(chunks.Length*sizeof(MarchingCubeChunk), 1, Dependency);
                Dependency = terrainGPUDispatchSystem.SetChunkData(chunks, chunkData, chunks.Length, Dependency);
                Dependency = JobHandle.CombineDependencies(chunks.Dispose(Dependency), chunkData.Dispose(Dependency));
                terrainGPUDispatchSystem.AddChunkDataJobHandle(Dependency);
                lastVersion = version;
            }

        }
    }
}