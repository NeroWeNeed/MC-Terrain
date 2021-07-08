using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(TerrainSystemGroup))]
    public sealed class TerrainNoiseChunkGenerationSystem : SystemBase
    {
        [BurstCompile]
        internal unsafe struct ChunkGenerationInitJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeHashMap<int2, Guid> chunkInfo;
            [ReadOnly]
            public NativeArray<int2> chunks;
            [WriteOnly]
            public NativeList<int3>.ParallelWriter chunksToLoad;
            [WriteOnly]
            public NativeList<int3>.ParallelWriter chunksToCreate;
            public void Execute(int index)
            {
                var chunk = chunks[index];
                if (chunkInfo.ContainsKey(chunk))
                {
                    chunksToLoad.AddNoResize(math.int3(chunk, index));
                }
                else
                {
                    chunksToCreate.AddNoResize(math.int3(chunk, index));
                }
            }
        }
        [BurstCompile]
        internal unsafe struct ChunkLoad : IJob
        {
            [ReadOnly]
            public NativeArray<byte> chunkBuffer;
            public int chunkCount;
            [WriteOnly]
            public NativeHashMap<int2, MarchingCubeChunk> chunkOutput;
            public void Execute()
            {
                var ptr = (byte*)chunkBuffer.GetUnsafeReadOnlyPtr();
                for (int i = 0;i<chunkCount;i++) {
                    int2 chunkLocation = default;
                    MarchingCubeChunk chunk = default;
                    UnsafeUtility.MemCpy(&chunkLocation, ptr + (i * (sizeof(MarchingCubeChunk) + sizeof(int2))), sizeof(int2));
                    UnsafeUtility.MemCpy(&chunk, ptr + (i * (sizeof(MarchingCubeChunk) + sizeof(int2)))+sizeof(int2), sizeof(MarchingCubeChunk));
                    chunkOutput[chunkLocation] = chunk;
                }
            }
        }

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
                var chunkIndex = index / (MarchingCubes.PaddedChunkBoxSizeInCells);
                var relativeValue = index % (MarchingCubes.PaddedChunkBoxSizeInCells);
                var currentChunkInfo = chunkLocation[chunkIndex];
                int3 cellPos = math.int3(
                                (relativeValue % (MarchingCubes.PaddedChunkLayerSizeInCells)) % MarchingCubes.PaddedChunkHorizontalSpanInCells,
                                relativeValue / (MarchingCubes.PaddedChunkLayerSizeInCells),
                                (relativeValue % (MarchingCubes.PaddedChunkLayerSizeInCells)) / MarchingCubes.PaddedChunkHorizontalSpanInCells
                ) - 1;
                int3 chunkPos = math.int3(
                    currentChunkInfo.x * MarchingCubes.ChunkHorizontalSpanInCells,
                    0,
                    currentChunkInfo.y * MarchingCubes.ChunkHorizontalSpanInCells
                );


                byte cubeCase = 0;
                for (int i = 0; i < 8; i++)
                {
                    MarchingCubes.GetVertex(i, out var t);
                    var p = cellPos + t;
                    float3 offset = math.float3(
                      p.x / ((float)MarchingCubes.ChunkHorizontalSpanInCells),
                      p.y / ((float)MarchingCubes.ChunkVerticalSpanInCells),
                      p.z / ((float)MarchingCubes.ChunkHorizontalSpanInCells)
                    );
                    //* (((float)(MarchingCubes.ChunkSize - p.y)) / MarchingCubes.ChunkSize);
                    //var samplePoint = offset + math.int3(chunkEntity.chunk.x, 0, chunkEntity.chunk.y);
                    //float value = noise.pnoise(samplePoint,MarchingCubes.ChunkSize);
                    var sample1 = math.unlerp(-1, 1, noise.pnoise(math.float2(offset.x + currentChunkInfo.x, offset.z + currentChunkInfo.y), MarchingCubes.ChunkHorizontalSpanInCells));
                    var sample2 = noise.cellular(math.float2(offset.x + currentChunkInfo.x, offset.z + currentChunkInfo.y)).x;
                    //var sample3 = noise.cellular(math.float3(offset.x + currentChunkInfo.x, offset.y, offset.z + currentChunkInfo.y)).x;
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
        /*         [Unity.Burst.BurstCompile]
                internal unsafe struct ChunkGeneration : IJobParallelFor
                {
                    [WriteOnly]
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
                        MarchingCubeChunk targetChunk;
                        var currentChunkInfo = chunkLocation[index];
                        int3 chunkPos = math.int3(currentChunkInfo.x * MarchingCubes.ChunkSizeInCells, 0, currentChunkInfo.y * MarchingCubes.ChunkSizeInCells);
                        for (int relativeValue = 0; relativeValue < sizeof(MarchingCubeChunk); relativeValue++)
                        {
                            int3 cellPos = math.int3(
                                               (relativeValue % (MarchingCubes.PaddedChunkLayerSizeInCells)) % MarchingCubes.PaddedChunkSizeInCells,
                                               relativeValue / (MarchingCubes.PaddedChunkLayerSizeInCells),
                                               (relativeValue % (MarchingCubes.PaddedChunkLayerSizeInCells)) / MarchingCubes.PaddedChunkSizeInCells
                               ) - 1;


                            byte cubeCase = 0;
                            for (int j = 0; j < 8; j++)
                            {
                                MarchingCubes.GetVertex(j, out var t);
                                var p = cellPos + t;
                                float3 offset = math.float3(
                                  p.x / ((float)MarchingCubes.ChunkSizeInCells),
                                  p.y / ((float)MarchingCubes.ChunkSizeInCells),
                                  p.z / ((float)MarchingCubes.ChunkSizeInCells)
                                );
                                var sample1 = math.unlerp(-1, 1, noise.pnoise(math.float2(offset.x + currentChunkInfo.x, offset.z + currentChunkInfo.y), MarchingCubes.ChunkSizeInCells));
                                var sample2 = noise.cellular(math.float2(offset.x + currentChunkInfo.x, offset.z + currentChunkInfo.y)).x;
                                var sample3 = noise.cellular(math.float3(offset.x + currentChunkInfo.x, offset.y, offset.z + currentChunkInfo.y)).x;
                                var height = sample1 * 6;

                                if ((p.y < 1) || (p.y < sample1 * 12 && sample2 < 0.7f))
                                {
                                    cubeCase |= (byte)(1 << j);
                                }
                            }
                            if (cubeCase != 0)
                            {
                                targetChunk[relativeValue] = cubeCase;
                            }
                        }
                        chunkData[index] = targetChunk;





                    }
                } */
        private int lastVersion;
        private TerrainChunkSystem terrainChunkSystem;
        private TerrainGPUDispatchSystem terrainGPUDispatchSystem;
        private NativeHashMap<int2, Guid> chunkFiles;
        private NativeArray<MarchingCubeChunk> chunkCacheData;
        public NativeHashMap<int2, int> chunkCache;
        private string chunkDir;
        private string chunkInfoFile;
        private byte[] chunkInfoBuffer = new byte[24];
        protected override void OnCreate()
        {
            terrainChunkSystem = World.GetOrCreateSystem<TerrainChunkSystem>();
            terrainGPUDispatchSystem = World.GetOrCreateSystem<TerrainGPUDispatchSystem>();
            chunkFiles = new NativeHashMap<int2, Guid>(64, Allocator.Persistent);
            RequireSingletonForUpdate<TerrainChunkData>();
            RequireSingletonForUpdate<TerrainIsoValue>();
            RequireSingletonForUpdate<TerrainCellScale>();
            RequireSingletonForUpdate<RuntimeTerrainSettings>();

        }
        protected unsafe override void OnStartRunning()
        {
            var settings = GetSingleton<RuntimeTerrainSettings>().value;
            chunkInfoFile = settings.Value.chunkInfoFile.ToString();
            chunkDir = settings.Value.chunkStoragePath.ToString();
            if (File.Exists(chunkInfoFile))
            {
                chunkFiles.Clear();
                using var fs = File.OpenRead(chunkInfoFile);
                fixed (byte* ptr = chunkInfoBuffer)
                {
                    while (fs.Read(chunkInfoBuffer, 0, sizeof(int2) + sizeof(Guid)) != 0)
                    {
                        int2 chunk = default;
                        Guid guid = default;
                        UnsafeUtility.MemCpy(&chunk, ptr, sizeof(int2));
                        UnsafeUtility.MemCpy(&guid, ptr + sizeof(int2), sizeof(Guid));
                        chunkFiles[chunk] = guid;
                    }
                }
            }
        }
        protected override unsafe void OnStopRunning()
        {
            if (!chunkFiles.IsEmpty)
            {
                using StreamBinaryWriter writer = new StreamBinaryWriter(chunkInfoFile);
                foreach (var chunkFile in chunkFiles)
                {
                    var chunk = chunkFile.Key;
                    var id = chunkFile.Value;
                    writer.WriteBytes(&chunk, sizeof(int2));
                    writer.WriteBytes(&id, sizeof(Guid));
                }

            }
        }
        protected override void OnDestroy()
        {
            chunkFiles.Dispose();
            base.OnDestroy();
        }
        protected unsafe override void OnUpdate()
        {

            terrainChunkSystem.WaitForCompletion();
            var version = terrainChunkSystem.GetLoadedChunkSetVersion();
            if (lastVersion != version)
            {

                var chunks = terrainChunkSystem.GetLoadedChunks();
                var chunkData = new NativeArray<MarchingCubeChunk>(chunks.Length, Allocator.TempJob);
                var chunksToLoad = new NativeList<int3>(chunks.Length, Allocator.TempJob);
                var chunksToCreate = new NativeList<int3>(chunks.Length, Allocator.TempJob);
                new ChunkGenerationInitJob
                {
                    chunkInfo = chunkFiles,
                    chunks = chunks,
                    chunksToCreate = chunksToCreate.AsParallelWriter(),
                    chunksToLoad = chunksToLoad.AsParallelWriter()
                }.Run(chunks.Length);
                //Load Chunks from disk
                var reads = new NativeArray<ReadCommand>(chunksToLoad.Length, Allocator.TempJob);
                var readPtr = (ReadCommand*)reads.GetUnsafePtr();
                var buffer = new NativeArray<byte>(chunksToLoad.Length * (sizeof(MarchingCubeChunk) + sizeof(int2)), Allocator.TempJob);
                var bufferPtr = (byte*)buffer.GetUnsafePtr();
                JobHandle readHandle = default;
                for (int i = 0; i < chunksToLoad.Length; i++)
                {
                    var chunk = chunksToLoad[i].xy;
                    UnsafeUtility.CopyStructureToPtr(ref chunk, bufferPtr + (i * (sizeof(MarchingCubeChunk) + sizeof(int2))));
                    reads[i] = new ReadCommand
                    {
                        Buffer = bufferPtr + (i * (sizeof(MarchingCubeChunk) + sizeof(int2))) + sizeof(int2),
                        Offset = 0,
                        Size = sizeof(MarchingCubeChunk)
                    };
                    readHandle = JobHandle.CombineDependencies(readHandle, AsyncReadManager.Read($"{chunkDir}/{chunkFiles[chunksToLoad[i].xy]:N}.bin", readPtr + i, 1, $"Chunk({chunksToLoad[i].x},{chunksToLoad[i].y})").JobHandle);
                }


                Dependency = new ChunkGeneration
                {
                    chunkLocation = chunks,
                    chunkData = chunkData,
                    IsoValue = GetSingleton<TerrainIsoValue>()
                }.Schedule(chunks.Length * sizeof(MarchingCubeChunk), 1, Dependency);
                Dependency = terrainGPUDispatchSystem.SetChunkData(chunks, chunkData, chunks.Length, GetSingleton<TerrainCellScale>(), Dependency);
                Dependency = JobHandle.CombineDependencies(chunks.Dispose(Dependency), chunkData.Dispose(Dependency));
                terrainGPUDispatchSystem.AddChunkDataJobHandle(Dependency);
                lastVersion = version;
            }

        }

        public struct CacheEntry
        {
            public int index;
            public ulong inputTime;
        }
    }
}