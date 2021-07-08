using Unity.Collections;
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
        public string storagePath;
        public int4 bounds = Terrain.TerrainSettingsData.DefaultBounds;
        public Terrain.TerrainSettingsData.StorageType storageType;
        public uint cacheSize = 128;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (terrainMaterial != null && !string.IsNullOrWhiteSpace(storagePath) && cellScale > 0)
            {
                dstManager.AddComponent<NoiseTerrainProducer>(entity);
                dstManager.AddComponent<TerrainProducer>(entity);
                dstManager.AddComponent<TerrainChunkData>(entity);
                dstManager.AddComponentData<TerrainCellScale>(entity, cellScale);
                dstManager.AddComponentData<TerrainIsoValue>(entity, math.clamp(isoValue, 0, 1));
                dstManager.AddSharedComponentData(entity, new TerrainMaterial { value = terrainMaterial });
                var blob = Create();
                conversionSystem.BlobAssetStore.AddUniqueBlobAsset(ref blob);
                dstManager.AddComponentData(entity, new TerrainSettings
                {
                    value = blob
                });
            }

        }
        private BlobAssetReference<Terrain.TerrainSettingsData> Create(Allocator allocator = Allocator.Persistent) {
            var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref Terrain.TerrainSettingsData root = ref builder.ConstructRoot<TerrainSettingsData>();
            root.bounds = bounds;
            builder.AllocateString(ref root.storagePath, storagePath);
            root.storageType = storageType;
            root.cacheSize = cacheSize;
            return builder.CreateBlobAssetReference<TerrainSettingsData>(allocator);
        }
    }
}