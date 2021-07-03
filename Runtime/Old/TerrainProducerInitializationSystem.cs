using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace NeroWeNeed.Terrain
{
/*     [UpdateInGroup(typeof(InitializationSystemGroup))]
    public sealed class TerrainProducerInitializationSystem : SystemBase
    {
        private EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = entityCommandBufferSystem.CreateCommandBuffer();
            Entities.WithAll<TerrainProducer>().WithNone<TerrainLoadedChunks>().ForEach((Entity entity) =>
            {
                ecb.AddComponent<TerrainLoadedChunks>(entity, new TerrainLoadedChunks { chunkReferences = new UnsafeHashMap<int2, int>(8, Unity.Collections.Allocator.Persistent) });
            }).WithoutBurst().Run();
            Entities.WithNone<TerrainProducer>().WithAll<TerrainLoadedChunks>().ForEach((Entity entity, in TerrainLoadedChunks tlc) =>
            {
                tlc.Dispose();
                ecb.RemoveComponent<TerrainLoadedChunks>(entity);
            }).WithoutBurst().Run();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        protected override void OnDestroy()
        {
            Entities.WithAll<TerrainLoadedChunks>().ForEach((Entity entity, in TerrainLoadedChunks tlc) =>
            {
                tlc.Dispose();
                EntityManager.RemoveComponent<TerrainLoadedChunks>(entity);
            }).WithoutBurst().WithStructuralChanges().Run();
        }
    } */
}