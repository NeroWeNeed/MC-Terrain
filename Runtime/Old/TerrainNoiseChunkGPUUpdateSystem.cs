/* 
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NeroWeNeed.Terrain
{
    public unsafe sealed class TerrainNoiseChunkGPUUpdateSystem : SystemBase
    {
        [BurstCompile]
        internal unsafe struct UpdateBufferJob : IJob
        {
            public NativeList<byte> output;
            public NativeArray<int2> chunkLocations;
            public NativeArray<MarchingCubeChunk> chunks;
            public int count;
            public void Execute()
            {
                output.Capacity = (sizeof(int2) + sizeof(MarchingCubeChunk)) * count;
                var ptr = (byte*)output.GetUnsafePtr();
                for (int i = 0; i < chunks.Length; i++)
                {
                    var location = chunkLocations[i];
                    var chunk = chunks[i];
                    UnsafeUtility.MemCpy(ptr + (i * (sizeof(int2) + sizeof(MarchingCubeChunk))), &location, sizeof(int2));
                    UnsafeUtility.MemCpy(ptr + (i * (sizeof(int2) + sizeof(MarchingCubeChunk))) + sizeof(int2), &chunk, sizeof(MarchingCubeChunk));
                }
            }
        }
        private const string COMPUTE_SHADER_ASSET = "Packages/github.neroweneed.marching-cubes-terrain/ComputeBuffers/MarchingCubes.compute";
        private ComputeShader computeShader;
        public static readonly int ChunkDataId = Shader.PropertyToID("_ChunkData");
        public static readonly int ChunkCountId = Shader.PropertyToID("_ChunkCount");
        public static readonly int CellScaleId = Shader.PropertyToID("_CellScale");
        public static readonly int IsoValueId = Shader.PropertyToID("_IsoValue");
        public static readonly int VericesId = Shader.PropertyToID("_Vertices");
        public static readonly int NormalsId = Shader.PropertyToID("_Normals");
        public static readonly int IndicesId = Shader.PropertyToID("_Indices");
        public ComputeBuffer ChunkData { get; private set; } = null;
        public ComputeBuffer ChunkCount { get; private set; } = null;
        public NativeList<byte> chunkDataBuffer;
        public int lastVersion;
        protected override void OnCreate()
        {
            chunkDataBuffer = new NativeList<byte>(sizeof(MarchingCubeChunk) + sizeof(int2), Allocator.Persistent);
            computeShader = Addressables.LoadAssetAsync<ComputeShader>(COMPUTE_SHADER_ASSET).WaitForCompletion();
        }
        protected override void OnUpdate()
        {
            //throw new System.NotImplementedException();
        }
        protected override void OnDestroy()
        {
            chunkDataBuffer.Dispose();
        }
        public JobHandle SetChunkData(NativeArray<int2> chunkLocations, NativeArray<MarchingCubeChunk> chunks, int count,JobHandle dependency = default)
        {
            return new UpdateBufferJob
            {
                output = chunkDataBuffer,
                chunks = chunks,
                chunkLocations = chunkLocations,
                count = count
            }.Schedule(dependency);
        }
    }
} */