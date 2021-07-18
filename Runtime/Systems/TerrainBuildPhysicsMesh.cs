using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(SimulationSystemGroup),OrderFirst =true)]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public class TerrainBuildPhysicsMesh : SystemBase
    {
        private int lastVersion = 0;
        private TerrainGPUDispatchSystem terrainGPUDispatchSystem;
        private TerrainRendererSystem terrainRendererSystem;
        private EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            terrainGPUDispatchSystem = World.GetOrCreateSystem<TerrainGPUDispatchSystem>();
            terrainRendererSystem = World.GetOrCreateSystem<TerrainRendererSystem>();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginFixedStepSimulationEntityCommandBufferSystem>();
            RequireSingletonForUpdate<TerrainProducer>();
        }
        protected override void OnUpdate()
        {
            if (lastVersion != terrainGPUDispatchSystem.lastVersion &&
            terrainRendererSystem.argumentRequest.done &&
            terrainRendererSystem.indexRequest.done &&
            terrainRendererSystem.vertexRequest.done
            )
            {
                
/*                 terrainRendererSystem.argumentRequest.WaitForCompletion();
                terrainRendererSystem.indexRequest.WaitForCompletion();
                terrainRendererSystem.vertexRequest.WaitForCompletion(); */
                var args = terrainRendererSystem.argumentRequest.GetData<uint>();
                var vertices = terrainRendererSystem.vertexRequest.GetData<float3>().GetSubArray(0, (int)args[0]);
                var indices = terrainRendererSystem.indexRequest.GetData<int3>().GetSubArray(0, ((int)args[1])/3);
                var entity = GetSingletonEntity<TerrainProducer>();
                var ecb = entityCommandBufferSystem.CreateCommandBuffer();
                Dependency = Job.WithCode(() =>
                {
                    if (HasComponent<PhysicsCollider>(entity))
                    {
                        var oldCollider = GetComponent<PhysicsCollider>(entity);
                        oldCollider.Value.Dispose();
                        ecb.SetComponent(entity, new PhysicsCollider
                        {
                            Value = Unity.Physics.MeshCollider.Create(vertices, indices)
                        });
                    }
                    else
                    {
                        ecb.AddComponent(entity, new PhysicsCollider
                        {
                            Value = Unity.Physics.MeshCollider.Create(vertices, indices)
                        });
                    }
                }).Schedule(Dependency);

                lastVersion = terrainGPUDispatchSystem.lastVersion;
                entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            }

        }

    }
}