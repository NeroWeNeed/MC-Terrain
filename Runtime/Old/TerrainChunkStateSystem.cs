/* 
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(TerrainSystemGroup))]
    public sealed class TerrainChunkStateSystem : SystemBase
    {
        private EntityQuery chunkLoaderQuery;
        private EntityQuery terrainChunkQuery;
        private EntityCommandBufferSystem entityCommandBufferSystem;
        public JobHandle ChunkStateHandle { get; private set; }
        protected override void OnCreate()
        {
            chunkLoaderQuery = GetEntityQuery(ComponentType.ReadOnly<ChunkLoader>(), ComponentType.ReadOnly<LocalToWorld>());
            terrainChunkQuery = GetEntityQuery(ComponentType.ReadOnly<TerrainEntityData>());
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            RequireSingletonForUpdate<ChunksToLoadBuffer>();
            RequireSingletonForUpdate<TerrainProducer>();
        }

        protected override void OnUpdate()
        {
            var chunkEntities = new NativeHashMap<int2, Entity>(32, Allocator.TempJob);
            var freeChunkEntities = new NativeQueue<Entity>(Allocator.TempJob);
            var chunksToLoad = new NativeList<ChunksToLoadBuffer>(32, Allocator.TempJob);
            var chunksToLoadEntity = GetSingletonEntity<ChunksToLoadBuffer>();
            var ecb = entityCommandBufferSystem.CreateCommandBuffer();
            var job1 = new AccumulateActiveChunks
            {
                chunkLoaderHandle = GetComponentTypeHandle<ChunkLoader>(true),
                localToWorldHandle = GetComponentTypeHandle<LocalToWorld>(true),
                chunkEntities = chunkEntities
            }.Schedule(chunkLoaderQuery, Dependency);
            var job2 = new UpdateChunkEntities
            {
                terrainEntityDataHandle = GetComponentTypeHandle<TerrainEntityData>(true),
                entityTypeHandle = GetEntityTypeHandle(),
                chunkEntities = chunkEntities,
                freeChunkEntities = freeChunkEntities
            }.Schedule(terrainChunkQuery, job1);
            var job3 = new RemapChunkEntities
            {
                chunksToLoadBuffer = chunksToLoad,
                chunkEntities = chunkEntities,
                freeChunkEntities = freeChunkEntities
            }.Schedule(job2);
            var job4 = new DeleteExcessEntities
            {
                freeChunkEntities = freeChunkEntities,
                entityCommandBuffer = ecb
            }.Schedule(job3);
            var job5 = Job.WithCode(() =>
            {
                var loadBuffer = GetBuffer<ChunksToLoadBuffer>(chunksToLoadEntity);
                loadBuffer.AddRange(chunksToLoad.AsArray());
            }).Schedule(job4);
            var job6 = JobHandle.CombineDependencies(freeChunkEntities.Dispose(job5), chunkEntities.Dispose(job5), chunksToLoad.Dispose(job5));
            Dependency = job6;
            ChunkStateHandle = Dependency;
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);


        }

        [BurstCompile]
        internal struct AccumulateActiveChunks : IJobEntityBatch
        {
            [ReadOnly]
            public ComponentTypeHandle<ChunkLoader> chunkLoaderHandle;
            [ReadOnly]
            public ComponentTypeHandle<LocalToWorld> localToWorldHandle;
            [WriteOnly]
            public NativeHashMap<int2, Entity> chunkEntities;
            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var localToWorlds = batchInChunk.GetNativeArray(localToWorldHandle);
                var chunkLoaders = batchInChunk.GetNativeArray(chunkLoaderHandle);
                for (int i = 0; i < batchInChunk.Count; i++)
                {
                    var ltw = localToWorlds[i];
                    var radius = chunkLoaders[i].radius;

                    int2 chunk = math.int2(
                        ((int)(ltw.Position.x < 0 ? ltw.Position.x - MarchingCubes.ChunkSizeInCells : ltw.Position.x)) / MarchingCubes.ChunkSizeInCells,
                        ((int)(ltw.Position.z < 0 ? ltw.Position.z - MarchingCubes.ChunkSizeInCells : ltw.Position.z)) / MarchingCubes.ChunkSizeInCells
                        );
                    for (int y = chunk.y - radius; y <= chunk.y + radius; y++)
                    {
                        for (int x = chunk.x - radius; x <= chunk.x + radius; x++)
                        {
                            chunkEntities[math.int2(x, y)] = Entity.Null;
                        }
                    }

                }
            }
        }
        [BurstCompile]
        internal struct UpdateChunkEntities : IJobEntityBatch
        {
            [ReadOnly]
            public ComponentTypeHandle<TerrainEntityData> terrainEntityDataHandle;
            [ReadOnly]
            public EntityTypeHandle entityTypeHandle;
            public NativeHashMap<int2, Entity> chunkEntities;
            [WriteOnly]
            public NativeQueue<Entity> freeChunkEntities;
            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var entities = batchInChunk.GetNativeArray(entityTypeHandle);
                var terrainEntityData = batchInChunk.GetNativeArray(terrainEntityDataHandle);
                for (int i = 0; i < batchInChunk.Count; i++)
                {
                    var entity = entities[i];
                    var chunk = terrainEntityData[i].chunk;
                    if (chunkEntities.TryGetValue(chunk, out var chunkEntity) && chunkEntity == Entity.Null)
                    {
                        chunkEntities[chunk] = entity;
                    }
                    else
                    {
                        freeChunkEntities.Enqueue(entity);
                    }
                }
            }
        }
        [BurstCompile]
        internal struct RemapChunkEntities : IJob
        {
            [WriteOnly]
            public NativeList<ChunksToLoadBuffer> chunksToLoadBuffer;
            [ReadOnly]
            public NativeHashMap<int2, Entity> chunkEntities;
            public NativeQueue<Entity> freeChunkEntities;

            public void Execute()
            {
                var kvs = chunkEntities.GetKeyValueArrays(Allocator.Temp);
                for (int i = 0; i < kvs.Length; i++)
                {
                    var key = kvs.Keys[i];
                    var value = kvs.Values[i];
                    if (value == Entity.Null)
                    {
                        if (freeChunkEntities.TryDequeue(out var chunkEntity))
                        {
                            chunksToLoadBuffer.Add(new ChunksToLoadBuffer
                            {
                                chunk = key,
                                chunkEntity = chunkEntity
                            });
                        }
                        else
                        {
                            chunksToLoadBuffer.Add(new ChunksToLoadBuffer
                            {
                                chunk = key,
                                chunkEntity = Entity.Null
                            });
                        }
                    }
                }
            }
        }
        [BurstCompile]
        internal struct DeleteExcessEntities : IJob
        {
            public NativeQueue<Entity> freeChunkEntities;
            public EntityCommandBuffer entityCommandBuffer;
            public void Execute()
            {
                while (freeChunkEntities.TryDequeue(out var entity))
                {
                    entityCommandBuffer.DestroyEntity(entity);
                }
            }
        }


    }
} */