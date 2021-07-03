using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace NeroWeNeed.Terrain.Editor
{
    public class MarchingCubesTerrain : MonoBehaviour, IConvertGameObjectToEntity
    {
        public Vector2Int size;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var position = gameObject.transform.position;
            var chunkPosition = math.int2((int)((position.x > 0 ? position.x : position.x - MarchingCubes.ChunkSizeInCells) / (float)MarchingCubes.ChunkSizeInCells), (int)((position.z > 0 ? position.z : position.z - MarchingCubes.ChunkSizeInCells) / (float)MarchingCubes.ChunkSizeInCells));
            
        }
    }
}