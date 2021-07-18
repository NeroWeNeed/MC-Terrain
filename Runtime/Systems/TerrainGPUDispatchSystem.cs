using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public unsafe sealed class TerrainGPUDispatchSystem : SystemBase
    {
        [BurstCompile]
        internal unsafe struct UpdateBufferJob : IJob
        {
            public NativeList<byte> output;
            public NativeArray<int2> chunkLocations;
            public NativeArray<MarchingCubeChunk> chunks;
            public NativeReference<Bounds> bounds;
            public int count;
            public float cellScale;
            public void Execute()
            {
                output.Clear();
                var ptr = (byte*)output.GetUnsafePtr();
                int2 minimum = chunkLocations[0], maximum = chunkLocations[0];
                for (int i = 0; i < chunks.Length; i++)
                {
                    var chunkLocation = chunkLocations[i];
                    var chunk = chunks[i];
                    output.AddRange(&chunkLocation, 8);
                    output.AddRange(&chunk, sizeof(MarchingCubeChunk));
                    minimum = math.min(minimum, chunkLocations[i]);
                    maximum = math.max(maximum, chunkLocations[i]);
                }
                bounds.Value = new Bounds(
                    new Vector3((minimum.x + maximum.x) * 0.5f * MarchingCubes.ChunkHorizontalSpanInCells, MarchingCubes.ChunkHorizontalSpanInCells * 0.5f, (minimum.y + maximum.y) * 0.5f * MarchingCubes.ChunkHorizontalSpanInCells) * cellScale,
                    new Vector3(math.abs(minimum.x - maximum.x) * 0.5f * MarchingCubes.ChunkHorizontalSpanInCells, MarchingCubes.ChunkHorizontalSpanInCells, math.abs(minimum.y - maximum.y) * 0.5f * MarchingCubes.ChunkHorizontalSpanInCells) * cellScale
                );
            }
        }
        private const string COMPUTE_SHADER_ASSET = "Packages/github.neroweneed.marching-cubes-terrain/ComputeBuffers/MarchingCubes.compute";
        private static readonly uint[] InitialDrawArguments = new uint[] { 0, 0, 1, 0, 0, 0 };
        private ComputeShader computeShader;
        public static readonly int ChunkDataId = Shader.PropertyToID("ChunkData");
        public static readonly int CellScaleId = Shader.PropertyToID("CellScale");
        public static readonly int IsoValueId = Shader.PropertyToID("IsoValue");
        public static readonly int VericesId = Shader.PropertyToID("Vertices");
        public static readonly int NormalsId = Shader.PropertyToID("Normals");
        public static readonly int IndicesId = Shader.PropertyToID("Indices");
        public static readonly int ArgumentsId = Shader.PropertyToID("Arguments");
        
        public GraphicsBuffer ChunkData { get; private set; }
        public GraphicsBuffer VertexBuffer { get; private set; }
        public ComputeBuffer NormalBuffer { get; private set; }
        public GraphicsBuffer IndexBuffer { get; private set; }
        public ComputeBuffer Arguments { get; private set; }
        internal MarchingCubesLookupTables LookupTableBuffers { get; private set; }
        private TerrainChunkSystem terrainChunkSystem;
        private JobHandle ChunkDataHandle;
        public NativeList<byte> chunkDataBuffer;
        public NativeReference<Bounds> terrainBounds;
        public int lastVersion;
        private int kernal;
        public MaterialPropertyBlock MaterialProperties { get; private set; }
        public int ChunkCount { get => (chunkDataBuffer.Length / (sizeof(MarchingCubeChunk) + sizeof(int2))); }
        protected override void OnCreate()
        {
            computeShader = Addressables.LoadAssetAsync<ComputeShader>(COMPUTE_SHADER_ASSET).WaitForCompletion();
            kernal = computeShader.FindKernel("MarchingCubes");
            chunkDataBuffer = new NativeList<byte>(8, Allocator.Persistent);
            terrainBounds = new NativeReference<Bounds>(Allocator.Persistent);
            terrainChunkSystem = World.GetOrCreateSystem<TerrainChunkSystem>();
            MaterialProperties = new MaterialPropertyBlock();
            Arguments = new ComputeBuffer(6, sizeof(uint), ComputeBufferType.IndirectArguments | ComputeBufferType.Structured);
            LookupTableBuffers = new MarchingCubesLookupTables();
            LookupTableBuffers.SetBuffers(kernal, computeShader);
            computeShader.SetBuffer(kernal, ArgumentsId, Arguments);
            RequireSingletonForUpdate<TerrainIsoValue>();
            RequireSingletonForUpdate<TerrainCellScale>();

        }


        protected override void OnUpdate()
        {
            terrainChunkSystem.WaitForCompletion();
            ChunkDataHandle.Complete();
            var version = terrainChunkSystem.GetLoadedChunkSetVersion();
            if (lastVersion != version)
            {
                int chunkCount = ChunkCount;
                if (chunkCount > 0)
                {
                    if (ChunkData == null)
                    {
                        ChunkData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, chunkDataBuffer.Length / 4, 4);
                        computeShader.SetBuffer(kernal, ChunkDataId, ChunkData);
                    }
                    else if (ChunkData.count < chunkDataBuffer.Length)
                    {
                        ChunkData.Release();
                        ChunkData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, chunkDataBuffer.Length / 4, 4);
                        computeShader.SetBuffer(kernal, ChunkDataId, ChunkData);
                    }
                    if (VertexBuffer == null)
                    {
                        VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12), sizeof(float3));
                        MaterialProperties.SetBuffer(VericesId, VertexBuffer);
                        computeShader.SetBuffer(kernal, VericesId, VertexBuffer);
                    }
                    else if (VertexBuffer.count < chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12))
                    {
                        VertexBuffer.Release();
                        VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12), sizeof(float3));
                        MaterialProperties.SetBuffer(VericesId, VertexBuffer);
                        computeShader.SetBuffer(kernal, VericesId, VertexBuffer);
                    }
                    if (NormalBuffer == null)
                    {
                        NormalBuffer = new ComputeBuffer(chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12), sizeof(float3), ComputeBufferType.Structured);
                        MaterialProperties.SetBuffer(NormalsId, NormalBuffer);
                        computeShader.SetBuffer(kernal, NormalsId, NormalBuffer);
                    }
                    else if (NormalBuffer.count < chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12))
                    {
                        NormalBuffer.Release();
                        NormalBuffer = new ComputeBuffer(chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12), sizeof(float3), ComputeBufferType.Structured);
                        MaterialProperties.SetBuffer(NormalsId, NormalBuffer);
                        computeShader.SetBuffer(kernal, NormalsId, NormalBuffer);
                    }
                    if (IndexBuffer == null)
                    {
                        IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 15), sizeof(uint));
                        computeShader.SetBuffer(kernal, IndicesId, IndexBuffer);
                    }
                    else if (IndexBuffer.count < chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 15))
                    {
                        IndexBuffer.Release();
                        IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 15), sizeof(uint));
                        computeShader.SetBuffer(kernal, IndicesId, IndexBuffer);
                    }

                    ChunkData.SetData<byte>(chunkDataBuffer);
                    Arguments.SetData(InitialDrawArguments);
                    computeShader.SetFloat(IsoValueId, GetSingleton<TerrainIsoValue>());
                    computeShader.SetFloat(CellScaleId, GetSingleton<TerrainCellScale>());
                    computeShader.Dispatch(kernal, chunkCount, 1, 1);
                    lastVersion = version;
                }
            }

        }
        protected override void OnDestroy()
        {

            chunkDataBuffer.Dispose();
            VertexBuffer?.Release();
            NormalBuffer?.Release();
            ChunkData?.Release();
            IndexBuffer?.Release();
            Arguments?.Release();
            LookupTableBuffers.Dispose();
            terrainBounds.Dispose();
            base.OnDestroy();
        }
        public JobHandle SetChunkData(NativeArray<int2> chunkLocations, NativeArray<MarchingCubeChunk> chunks, int count, float cellScale, JobHandle dependency = default)
        {

            return new UpdateBufferJob
            {
                output = chunkDataBuffer,
                chunks = chunks,
                chunkLocations = chunkLocations,
                count = count,
                bounds = terrainBounds,
                cellScale = cellScale
            }.Schedule(dependency);
        }
        public bool IsCompleted() => terrainChunkSystem.IsCompleted() && ChunkDataHandle.IsCompleted;
        public void AddChunkDataJobHandle(JobHandle handle)
        {
            ChunkDataHandle = JobHandle.CombineDependencies(ChunkDataHandle, handle);
        }
    }
}