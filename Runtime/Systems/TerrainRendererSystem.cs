using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(HybridRendererSystem))]
    [UpdateAfter(typeof(TerrainGPUDispatchSystem))]
    [UpdateAfter(typeof(UpdatePresentationSystemGroup))]
    public sealed class TerrainRendererSystem : SystemBase
    {
        private TerrainGPUDispatchSystem terrainGPUDispatchSystem;
        public AsyncGPUReadbackRequest argumentRequest;
        public AsyncGPUReadbackRequest vertexRequest;
        public AsyncGPUReadbackRequest indexRequest;
        private int version = 0;
        protected override void OnCreate()
        {
            terrainGPUDispatchSystem = World.GetOrCreateSystem<TerrainGPUDispatchSystem>();
            RequireSingletonForUpdate<TerrainMaterial>();

        }
        protected unsafe override void OnUpdate()
        {
            if (terrainGPUDispatchSystem.ChunkCount > 0)
            {
                var material = EntityManager.GetSharedComponentData<TerrainMaterial>(GetSingletonEntity<TerrainMaterial>()).value;
                Graphics.DrawProceduralIndirect(material, terrainGPUDispatchSystem.terrainBounds.Value, MeshTopology.Triangles, terrainGPUDispatchSystem.IndexBuffer, terrainGPUDispatchSystem.Arguments, sizeof(uint), null, terrainGPUDispatchSystem.MaterialProperties, UnityEngine.Rendering.ShadowCastingMode.On, true, 0);
            }
            if (terrainGPUDispatchSystem.lastVersion != version)
            {
                argumentRequest.WaitForCompletion();
                indexRequest.WaitForCompletion();
                vertexRequest.WaitForCompletion();
                argumentRequest = AsyncGPUReadback.Request(terrainGPUDispatchSystem.Arguments);
                indexRequest = AsyncGPUReadback.Request(terrainGPUDispatchSystem.IndexBuffer);
                vertexRequest = AsyncGPUReadback.Request(terrainGPUDispatchSystem.VertexBuffer);
                version = terrainGPUDispatchSystem.lastVersion;
            }



        }
    }
}