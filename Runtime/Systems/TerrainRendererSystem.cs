using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
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
        protected override void OnCreate()
        {
            terrainGPUDispatchSystem = World.GetOrCreateSystem<TerrainGPUDispatchSystem>();
            RequireSingletonForUpdate<TerrainMaterial>();
        }
        protected unsafe override void OnUpdate()
        {
            if (terrainGPUDispatchSystem.IndexBuffer != null)
            {
                var material = EntityManager.GetSharedComponentData<TerrainMaterial>(GetSingletonEntity<TerrainMaterial>()).value;
                Graphics.DrawProceduralIndirect(material, terrainGPUDispatchSystem.terrainBounds.Value, MeshTopology.Triangles, terrainGPUDispatchSystem.IndexBuffer, terrainGPUDispatchSystem.DrawArguments, 0, null, terrainGPUDispatchSystem.MaterialProperties, UnityEngine.Rendering.ShadowCastingMode.On, true, 0);

            }
        }
    }
}