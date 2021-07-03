using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(TerrainSystemGroup),OrderFirst = true)]
    public sealed class TerrainChunkSystem : SystemBase
    {
        [BurstCompile]
        internal struct AccumulateChunksJob : IJobEntityBatch
        {
            [ReadOnly]
            public ComponentTypeHandle<ChunkLoader> chunkLoaderHandle;
            [ReadOnly]
            public ComponentTypeHandle<LocalToWorld> localToWorldHandle;
            public NativeHashMap<int2, int> chunks;
            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var chunkLoaders = batchInChunk.GetNativeArray(chunkLoaderHandle);
                var localToWorlds = batchInChunk.GetNativeArray(localToWorldHandle);
                for (int i = 0; i < batchInChunk.Count; i++)
                {
                    var radius = chunkLoaders[i].radius;
                    var ltw = localToWorlds[i].Position;
                    int2 chunk = math.int2(
                        ((int)(ltw.x < 0 ? ltw.x - MarchingCubes.ChunkSizeInCells : ltw.x)) / MarchingCubes.ChunkSizeInCells,
                        ((int)(ltw.z < 0 ? ltw.z - MarchingCubes.ChunkSizeInCells : ltw.z)) / MarchingCubes.ChunkSizeInCells
                        );

                    for (int y = chunk.y - radius; y <= chunk.y + radius; y++)
                    {
                        for (int x = chunk.x - radius; x <= chunk.x + radius; x++)
                        {
                            var targetChunk = math.int2(x, y);
                            chunks[targetChunk] = chunks.TryGetValue(targetChunk, out var t) ? t + 1 : 1;
                        }
                    }
                }
            }
        }
        [BurstCompile]
        internal struct CalculateChunkSetVersion : IJob
        {
            [ReadOnly]
            public NativeHashMap<int2, int> chunks;
            [WriteOnly]
            public NativeReference<int> version;
            public void Execute()
            {
                var keys = chunks.GetKeyArray(Allocator.Temp);
                int value = -763492301;
                for (int i = 0; i < keys.Length; i++)
                {
                    value = value * -1521134295 + keys[i].GetHashCode();
                }
                version.Value = value;
            }
        }


        private EntityQuery query;
        internal NativeHashMap<int2, int> loadedChunks;
        internal NativeReference<int> loadedChunkVersion;
        internal TerrainGPUDispatchSystem terrainGPUDispatchSystem;
        protected override void OnCreate()
        {
            query = GetEntityQuery(ComponentType.ReadOnly<ChunkLoader>(), ComponentType.ReadOnly<LocalToWorld>());
            loadedChunks = new NativeHashMap<int2, int>(32, Allocator.Persistent);
            loadedChunkVersion = new NativeReference<int>(Allocator.Persistent);
            terrainGPUDispatchSystem = World.GetOrCreateSystem<TerrainGPUDispatchSystem>();
        }
        protected override void OnUpdate()
        {
            loadedChunks.Clear();
            Dependency = new AccumulateChunksJob
            {
                chunkLoaderHandle = GetComponentTypeHandle<ChunkLoader>(true),
                localToWorldHandle = GetComponentTypeHandle<LocalToWorld>(true),
                chunks = loadedChunks
            }.Schedule(query, Dependency);
            Dependency = new CalculateChunkSetVersion
            {
                chunks = loadedChunks,
                version = loadedChunkVersion
            }.Schedule(Dependency);
            
        }
        public void WaitForCompletion() {
            Dependency.Complete();
        }
        public int GetLoadedChunkSetVersion() => loadedChunkVersion.Value;
        public NativeArray<int2> GetLoadedChunks(Allocator allocator = Allocator.TempJob) => loadedChunks.GetKeyArray(allocator);
        protected override void OnDestroy()
        {
            loadedChunkVersion.Dispose();
            loadedChunks.Dispose();
        }

    }
}