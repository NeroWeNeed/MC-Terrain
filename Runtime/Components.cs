using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NeroWeNeed.Terrain
{
    public struct FixedTerrain : IComponentData
    {
        public int2 bounds;
    }
    public struct NoiseTerrainProducer : IComponentData { }
    [Serializable]
    public struct TerrainCellScale : IComponentData, IEquatable<TerrainCellScale>, IEquatable<float>
    {
        public float value;
        public TerrainCellScale(float value)
        {
            this.value = value;
        }
        public bool Equals(float other)
        {
            return value.Equals(other);
        }
        public bool Equals(TerrainCellScale other)
        {
            return value.Equals(other.value);
        }
        public override int GetHashCode()
        {
            return 1625094469 + value.GetHashCode();
        }
        public static implicit operator TerrainCellScale(float value) => new TerrainCellScale(value);
        public static implicit operator float(TerrainCellScale component) => component.value;
    }
    public struct TerrainChunkData : IBufferElementData {
        public int2 chunkLocation;
        public MarchingCubeChunk chunkValue;
    }

    [Serializable]
    public struct TerrainIsoValue : IComponentData, IEquatable<TerrainIsoValue>, IEquatable<float>
    {
        public float value;
        public TerrainIsoValue(float value)
        {
            this.value = value;
        }
        public bool Equals(float other)
        {
            return value.Equals(other);
        }
        public bool Equals(TerrainIsoValue other)
        {
            return value.Equals(other.value);
        }
        public override int GetHashCode()
        {
            return 1625094491 + value.GetHashCode();
        }
        public static implicit operator TerrainIsoValue(float value) => new TerrainIsoValue(value);
        public static implicit operator float(TerrainIsoValue component) => component.value;
    }
    
    public struct TerrainProducer : IComponentData { }

    public struct ChunksToLoadBuffer : IBufferElementData
    {
        public int2 chunk;
        public Entity chunkEntity;
    }

    [Serializable]
    public struct TerrainMaterial : ISharedComponentData, IEquatable<TerrainMaterial>, IEquatable<Material>
    {
        public Material value;
        public TerrainMaterial(Material value)
        {
            this.value = value;
        }
        public bool Equals(Material other)
        {
            return EqualityComparer<Material>.Default.Equals(value, other);

        }
        public bool Equals(TerrainMaterial other)
        {
            return EqualityComparer<Material>.Default.Equals(value, other.value);
        }
        public override int GetHashCode()
        {
            return 1624630893 + value.GetHashCode();
        }
        public static implicit operator TerrainMaterial(Material value) => new TerrainMaterial(value);
        public static implicit operator Material(TerrainMaterial component) => component.value;
    }

    public struct ChunkLoader : IComponentData
    {
        public int radius;
    }
}