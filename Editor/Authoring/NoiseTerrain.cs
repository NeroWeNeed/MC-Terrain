using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace NeroWeNeed.Terrain.Editor
{
    public class NoiseTerrain : MonoBehaviour, IConvertGameObjectToEntity
    {
        public Material terrainMaterial;
        public float cellScale = 1f;
        public float isoValue = 0.5f;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (terrainMaterial != null)
            {
                dstManager.AddComponent<NoiseTerrainProducer>(entity);
                dstManager.AddComponent<TerrainProducer>(entity);
                dstManager.AddComponent<TerrainChunkData>(entity);
                dstManager.AddComponentData<TerrainCellScale>(entity, cellScale);
                dstManager.AddComponentData<TerrainIsoValue>(entity, isoValue);
                dstManager.AddSharedComponentData(entity, new TerrainMaterial { value = terrainMaterial });
            }

        }
    }
}