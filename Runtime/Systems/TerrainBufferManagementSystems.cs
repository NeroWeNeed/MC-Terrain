using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Entities;
using UnityEngine;

namespace NeroWeNeed.Terrain
{
    public abstract class BaseTerrainBufferManagementSystem<TBuffer, TBufferSize> : SystemBase where TBuffer : struct, ISystemStateComponentData, IGraphicsBufferComponentData where TBufferSize : struct, ISystemStateComponentData, IGraphicsBufferSizeComponentData
    {
        protected Dictionary<ulong, GraphicsBuffer> buffers;
        protected EntityQuery query;
        protected override void OnCreate()
        {
            base.OnCreate();
            buffers = new Dictionary<ulong, GraphicsBuffer>();
            query = GetEntityQuery(ComponentType.ReadWrite<TBuffer>(), ComponentType.ReadOnly<TBufferSize>());
            query.AddChangedVersionFilter(ComponentType.ReadOnly<TBufferSize>());
            query.AddChangedVersionFilter(ComponentType.ReadWrite<TBuffer>());
        }
        protected override void OnUpdate()
        {

        }
        protected override void OnDestroy()
        {
            foreach (var kv in buffers)
            {
                kv.Value.Release();
            }
        }
        internal struct UpdateBuffersJob : IJobEntityBatch
        {
            public Dictionary<ulong, GraphicsBuffer> buffers;

            public ComponentTypeHandle<TBuffer> bufferTypeHandle;
            public ComponentTypeHandle<TBufferSize> bufferSizeTypeHandle;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var buffers = batchInChunk.GetNativeArray(bufferTypeHandle);
                var bufferSizes = batchInChunk.GetNativeArray(bufferSizeTypeHandle);
                for (int i = 0; i < batchInChunk.Count;i++) {

                }
            }
        }

    }
}