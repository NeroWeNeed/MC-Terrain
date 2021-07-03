using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
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
        private Mesh mesh;
        private ComputeBuffer argumentBuffer;

        protected override void OnCreate()
        {
            terrainGPUDispatchSystem = World.GetOrCreateSystem<TerrainGPUDispatchSystem>();
            RequireSingletonForUpdate<TerrainMaterial>();
            argumentBuffer = new ComputeBuffer(5, sizeof(int), ComputeBufferType.IndirectArguments);
            argumentBuffer.SetData(new int[] { 0, 1, 0, 0, 0 });
            mesh = new Mesh();
        }
        protected unsafe override void OnUpdate()
        {
            var material = EntityManager.GetSharedComponentData<TerrainMaterial>(GetSingletonEntity<TerrainMaterial>()).value;
            /*             var tempBuffer = new float3[terrainGPUDispatchSystem.VertexBuffer.count];
                        terrainGPUDispatchSystem.VertexBuffer.GetData(tempBuffer); */
            //Debug.Log($"Vertices (0): {tempBuffer[0]}");
            /*             for (int i = 0; i < tempBuffer.Length; i++)
                        {
                            if (!tempBuffer[i].Equals(float3.zero))
                            {
                                Debug.Log($"Vertices ({i}): {tempBuffer[i]}");
                                break;
                            }
                        } */


            /*             GraphicsBuffer.CopyCount(terrainGPUDispatchSystem.IndexBuffer, argumentBuffer, 0);
                        var temp = new int[5];
                        argumentBuffer.GetData(temp);
                        temp[0] *= 3;
                        argumentBuffer.SetData(temp);
                        Graphics.DrawProceduralIndirect(
                            material,
                            terrainGPUDispatchSystem.terrainBounds.Value,
                            MeshTopology.Triangles,
                            terrainGPUDispatchSystem.IndexBuffer,
                            argumentBuffer, 0, null, terrainGPUDispatchSystem.MaterialProperties, UnityEngine.Rendering.ShadowCastingMode.On, true, 0
                        );
             */
            var ibuffer = new int[terrainGPUDispatchSystem.IndexBuffer.count];
            terrainGPUDispatchSystem.IndexBuffer.GetData(ibuffer);
            var vbuffer = new Vector3[terrainGPUDispatchSystem.VertexBuffer.count];
            terrainGPUDispatchSystem.VertexBuffer.GetData(vbuffer);
            mesh.SetIndices(ibuffer, MeshTopology.Triangles, 0);
            mesh.SetVertices(vbuffer);
            

            //Graphics.DrawProcedural(material, terrainGPUDispatchSystem.terrainBounds.Value, MeshTopology.Triangles, terrainGPUDispatchSystem.VertexBuffer.count, 1, null, terrainGPUDispatchSystem.MaterialProperties, UnityEngine.Rendering.ShadowCastingMode.On, true, 0);
        }
        protected override void OnDestroy()
        {
            argumentBuffer.Release();
        }
        internal struct SampleVertex
        {
            public float3 Position;
            public float3 Normal;
        }
    }
}