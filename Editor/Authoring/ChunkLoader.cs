using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace NeroWeNeed.Terrain.Editor
{
    public class ChunkLoader : MonoBehaviour, IConvertGameObjectToEntity
    {
        public int radius;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData<NeroWeNeed.Terrain.ChunkLoader>(entity, new NeroWeNeed.Terrain.ChunkLoader
            {
                radius = radius
            });

        }
    }
}