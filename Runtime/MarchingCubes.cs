using System.IO;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using Unity.Entities.Hybrid;
using Unity.Entities.Serialization;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Profiling;
using Codice.Client.Common;

namespace NeroWeNeed.Terrain
{
    [BurstCompile]
    public static class MarchingCubes
    {
        public unsafe struct MCCellData
        {
            private readonly byte geometryData;
            private fixed byte vertexIndices[15];
            public MCCellData(byte geometryData, byte a = 0, byte b = 0, byte c = 0, byte d = 0, byte e = 0, byte f = 0, byte g = 0, byte h = 0, byte i = 0, byte j = 0, byte k = 0, byte l = 0, byte m = 0, byte n = 0, byte o = 0)
            {
                this.geometryData = geometryData;
                vertexIndices[0] = a;
                vertexIndices[1] = b;
                vertexIndices[2] = c;
                vertexIndices[3] = d;
                vertexIndices[4] = e;
                vertexIndices[5] = f;
                vertexIndices[6] = g;
                vertexIndices[7] = h;
                vertexIndices[8] = i;
                vertexIndices[9] = j;
                vertexIndices[10] = k;
                vertexIndices[11] = l;
                vertexIndices[12] = m;
                vertexIndices[13] = n;
                vertexIndices[14] = o;
            }

            public int GetTriangleCount()
            {
                return geometryData & 15;
            }
            public int GetVertexCount()
            {
                return (geometryData & 240) >> 4;
            }
            public byte this[int index]
            {
                get => vertexIndices[index];
            }
        }

        public const int ChunkSizeInCells = 16;
        public const int ChunkSizeInBits = ChunkSizeInCells * 8;
        public const int ChunkLayerSizeInCells = ChunkSizeInCells * ChunkSizeInCells;
        public const int ChunkLayerSizeInBits = ChunkLayerSizeInCells * 8;
        public const int ChunkCubeSizeInCells = ChunkSizeInCells * ChunkSizeInCells * ChunkSizeInCells;
        public const int ChunkCubeSizeInBits = ChunkCubeSizeInCells * 8;
        public const int PaddedChunkSizeInCells = ChunkSizeInCells + 2;
        public const int PaddedChunkSizeInBits = PaddedChunkSizeInCells * 8;
        public const int PaddedChunkLayerSizeInCells = PaddedChunkSizeInCells * PaddedChunkSizeInCells;
        public const int PaddedChunkLayerSizeInBits = PaddedChunkLayerSizeInCells * 8;
        public const int PaddedChunkCubeSizeInCells = PaddedChunkSizeInCells * PaddedChunkSizeInCells * PaddedChunkSizeInCells;
        public const int PaddedChunkCubeSizeInBits = PaddedChunkCubeSizeInCells * 8;
        public static readonly Bounds ChunkBounds = new Bounds(
            new Vector3(ChunkSizeInCells / 2, ChunkSizeInCells / 2, ChunkSizeInCells / 2),
            new Vector3(ChunkSizeInCells / 2, ChunkSizeInCells / 2, ChunkSizeInCells / 2)
        );
        internal static readonly int3 TriangleWindingOrder = new int3(0, 2, 1);
        /* public static readonly int3[] VertexOrder = {
            new int3(0,0,0),
            new int3(1,0,0),
            new int3(0,0,1),
            new int3(1,0,1),
            new int3(0,1,0),
            new int3(1,1,0),
            new int3(0,1,1),
            new int3(1,1,1)
        }; */
        internal static readonly byte[] VertexOrder = {
            0,0,0,
            1,0,0,
            0,0,1,
            1,0,1,
            0,1,0,
            1,1,0,
            0,1,1,
            1,1,1
        };

        internal static readonly ushort[] VertexData = new ushort[3072] {
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x3304, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x4113, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x3304,  0x2315,  0x4113, 0, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4223,  0x1326, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x6201,  0x4223,  0x1326, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x4113,  0x5102,  0x4223,  0x1326, 0, 0, 0, 0, 0, 0,
    0x4223,  0x1326,  0x3304,  0x2315,  0x4113, 0, 0, 0, 0, 0, 0, 0,
    0x4113,  0x8337,  0x4223, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x3304,  0x4223,  0x4113,  0x8337, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x8337,  0x4223, 0, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x3304,  0x2315,  0x8337,  0x4223, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4113,  0x8337,  0x1326, 0, 0, 0, 0, 0, 0, 0, 0,
    0x4113,  0x8337,  0x1326,  0x3304,  0x6201, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x8337,  0x1326,  0x5102, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x2315,  0x8337,  0x1326, 0, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1146,  0x2245, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x1146,  0x2245, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x4113,  0x3304,  0x1146,  0x2245, 0, 0, 0, 0, 0, 0,
    0x2315,  0x4113,  0x5102,  0x1146,  0x2245, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4223,  0x1326,  0x3304,  0x1146,  0x2245, 0, 0, 0, 0, 0, 0,
    0x1146,  0x2245,  0x6201,  0x4223,  0x1326, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1146,  0x2245,  0x6201,  0x2315,  0x4113,  0x5102,  0x4223,  0x1326, 0, 0, 0,
    0x4223,  0x1326,  0x1146,  0x2245,  0x2315,  0x4113, 0, 0, 0, 0, 0, 0,
    0x4223,  0x4113,  0x8337,  0x3304,  0x1146,  0x2245, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x1146,  0x2245,  0x4223,  0x4113,  0x8337, 0, 0, 0, 0, 0,
    0x4223,  0x6201,  0x2315,  0x8337,  0x3304,  0x1146,  0x2245, 0, 0, 0, 0, 0,
    0x4223,  0x8337,  0x2315,  0x2245,  0x1146,  0x5102, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4113,  0x8337,  0x1326,  0x3304,  0x1146,  0x2245, 0, 0, 0, 0, 0,
    0x4113,  0x8337,  0x1326,  0x1146,  0x2245,  0x6201, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x8337,  0x1326,  0x5102,  0x3304,  0x1146,  0x2245, 0, 0, 0, 0,
    0x2245,  0x2315,  0x8337,  0x1326,  0x1146, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x2245,  0x8157, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x3304,  0x2315,  0x2245,  0x8157, 0, 0, 0, 0, 0, 0,
    0x4113,  0x6201,  0x2245,  0x8157, 0, 0, 0, 0, 0, 0, 0, 0,
    0x2245,  0x8157,  0x4113,  0x5102,  0x3304, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4223,  0x1326,  0x2315,  0x2245,  0x8157, 0, 0, 0, 0, 0, 0,
    0x6201,  0x4223,  0x1326,  0x3304,  0x2315,  0x2245,  0x8157, 0, 0, 0, 0, 0,
    0x6201,  0x2245,  0x8157,  0x4113,  0x5102,  0x4223,  0x1326, 0, 0, 0, 0, 0,
    0x4223,  0x1326,  0x3304,  0x2245,  0x8157,  0x4113, 0, 0, 0, 0, 0, 0,
    0x4223,  0x4113,  0x8337,  0x2315,  0x2245,  0x8157, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x3304,  0x4223,  0x4113,  0x8337,  0x2315,  0x2245,  0x8157, 0, 0, 0,
    0x8337,  0x4223,  0x6201,  0x2245,  0x8157, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x3304,  0x2245,  0x8157,  0x8337,  0x4223, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4113,  0x8337,  0x1326,  0x2315,  0x2245,  0x8157, 0, 0, 0, 0, 0,
    0x4113,  0x8337,  0x1326,  0x3304,  0x6201,  0x2315,  0x2245,  0x8157, 0, 0, 0, 0,
    0x5102,  0x1326,  0x8337,  0x8157,  0x2245,  0x6201, 0, 0, 0, 0, 0, 0,
    0x8157,  0x8337,  0x1326,  0x3304,  0x2245, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x3304,  0x1146,  0x8157, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x1146,  0x8157,  0x2315, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1146,  0x8157,  0x4113,  0x6201, 0, 0, 0, 0, 0, 0, 0,
    0x4113,  0x5102,  0x1146,  0x8157, 0, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x3304,  0x1146,  0x8157,  0x5102,  0x4223,  0x1326, 0, 0, 0, 0, 0,
    0x1326,  0x4223,  0x6201,  0x2315,  0x8157,  0x1146, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1146,  0x8157,  0x4113,  0x6201,  0x5102,  0x4223,  0x1326, 0, 0, 0, 0,
    0x1326,  0x1146,  0x8157,  0x4113,  0x4223, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x3304,  0x1146,  0x8157,  0x4223,  0x4113,  0x8337, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x1146,  0x8157,  0x2315,  0x4223,  0x4113,  0x8337, 0, 0, 0, 0,
    0x3304,  0x1146,  0x8157,  0x8337,  0x4223,  0x6201, 0, 0, 0, 0, 0, 0,
    0x4223,  0x5102,  0x1146,  0x8157,  0x8337, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x3304,  0x1146,  0x8157,  0x5102,  0x4113,  0x8337,  0x1326, 0, 0, 0, 0,
    0x6201,  0x4113,  0x8337,  0x1326,  0x1146,  0x8157,  0x2315, 0, 0, 0, 0, 0,
    0x6201,  0x3304,  0x1146,  0x8157,  0x8337,  0x1326,  0x5102, 0, 0, 0, 0, 0,
    0x1326,  0x1146,  0x8157,  0x8337, 0, 0, 0, 0, 0, 0, 0, 0,
    0x1326,  0x8267,  0x1146, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x3304,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x4113,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0, 0, 0,
    0x5102,  0x3304,  0x2315,  0x4113,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0, 0,
    0x5102,  0x4223,  0x8267,  0x1146, 0, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x6201,  0x4223,  0x8267,  0x1146, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4223,  0x8267,  0x1146,  0x6201,  0x2315,  0x4113, 0, 0, 0, 0, 0,
    0x1146,  0x8267,  0x4223,  0x4113,  0x2315,  0x3304, 0, 0, 0, 0, 0, 0,
    0x4113,  0x8337,  0x4223,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x3304,  0x4223,  0x4113,  0x8337,  0x1326,  0x8267,  0x1146, 0, 0, 0,
    0x6201,  0x2315,  0x8337,  0x4223,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0, 0,
    0x5102,  0x3304,  0x2315,  0x8337,  0x4223,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0,
    0x8267,  0x1146,  0x5102,  0x4113,  0x8337, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x4113,  0x8337,  0x8267,  0x1146,  0x3304, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x8337,  0x8267,  0x1146,  0x5102, 0, 0, 0, 0, 0, 0,
    0x1146,  0x3304,  0x2315,  0x8337,  0x8267, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1326,  0x8267,  0x2245, 0, 0, 0, 0, 0, 0, 0, 0,
    0x1326,  0x8267,  0x2245,  0x6201,  0x5102, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1326,  0x8267,  0x2245,  0x6201,  0x2315,  0x4113, 0, 0, 0, 0, 0,
    0x1326,  0x8267,  0x2245,  0x2315,  0x4113,  0x5102, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4223,  0x8267,  0x2245,  0x3304, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x4223,  0x8267,  0x2245, 0, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4223,  0x8267,  0x2245,  0x3304,  0x6201,  0x2315,  0x4113, 0, 0, 0, 0,
    0x4113,  0x4223,  0x8267,  0x2245,  0x2315, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1326,  0x8267,  0x2245,  0x4223,  0x4113,  0x8337, 0, 0, 0, 0, 0,
    0x1326,  0x8267,  0x2245,  0x6201,  0x5102,  0x4223,  0x4113,  0x8337, 0, 0, 0, 0,
    0x3304,  0x1326,  0x8267,  0x2245,  0x4223,  0x6201,  0x2315,  0x8337, 0, 0, 0, 0,
    0x5102,  0x1326,  0x8267,  0x2245,  0x2315,  0x8337,  0x4223, 0, 0, 0, 0, 0,
    0x3304,  0x2245,  0x8267,  0x8337,  0x4113,  0x5102, 0, 0, 0, 0, 0, 0,
    0x8337,  0x8267,  0x2245,  0x6201,  0x4113, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x6201,  0x2315,  0x8337,  0x8267,  0x2245,  0x3304, 0, 0, 0, 0, 0,
    0x2315,  0x8337,  0x8267,  0x2245, 0, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x2245,  0x8157,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x3304,  0x2315,  0x2245,  0x8157,  0x1326,  0x8267,  0x1146, 0, 0, 0,
    0x6201,  0x2245,  0x8157,  0x4113,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0, 0,
    0x2245,  0x8157,  0x4113,  0x5102,  0x3304,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0,
    0x4223,  0x8267,  0x1146,  0x5102,  0x2315,  0x2245,  0x8157, 0, 0, 0, 0, 0,
    0x3304,  0x6201,  0x4223,  0x8267,  0x1146,  0x2315,  0x2245,  0x8157, 0, 0, 0, 0,
    0x4223,  0x8267,  0x1146,  0x5102,  0x6201,  0x2245,  0x8157,  0x4113, 0, 0, 0, 0,
    0x3304,  0x2245,  0x8157,  0x4113,  0x4223,  0x8267,  0x1146, 0, 0, 0, 0, 0,
    0x4223,  0x4113,  0x8337,  0x2315,  0x2245,  0x8157,  0x1326,  0x8267,  0x1146, 0, 0, 0,
    0x6201,  0x5102,  0x3304,  0x4223,  0x4113,  0x8337,  0x2315,  0x2245,  0x8157,  0x1326,  0x8267,  0x1146,
    0x8337,  0x4223,  0x6201,  0x2245,  0x8157,  0x1326,  0x8267,  0x1146, 0, 0, 0, 0,
    0x4223,  0x5102,  0x3304,  0x2245,  0x8157,  0x8337,  0x1326,  0x8267,  0x1146, 0, 0, 0,
    0x8267,  0x1146,  0x5102,  0x4113,  0x8337,  0x2315,  0x2245,  0x8157, 0, 0, 0, 0,
    0x6201,  0x4113,  0x8337,  0x8267,  0x1146,  0x3304,  0x2315,  0x2245,  0x8157, 0, 0, 0,
    0x8337,  0x8267,  0x1146,  0x5102,  0x6201,  0x2245,  0x8157, 0, 0, 0, 0, 0,
    0x3304,  0x2245,  0x8157,  0x8337,  0x8267,  0x1146, 0, 0, 0, 0, 0, 0,
    0x8157,  0x2315,  0x3304,  0x1326,  0x8267, 0, 0, 0, 0, 0, 0, 0,
    0x8267,  0x8157,  0x2315,  0x6201,  0x5102,  0x1326, 0, 0, 0, 0, 0, 0,
    0x8267,  0x1326,  0x3304,  0x6201,  0x4113,  0x8157, 0, 0, 0, 0, 0, 0,
    0x8267,  0x8157,  0x4113,  0x5102,  0x1326, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4223,  0x8267,  0x8157,  0x2315,  0x3304, 0, 0, 0, 0, 0, 0,
    0x2315,  0x6201,  0x4223,  0x8267,  0x8157, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x5102,  0x4223,  0x8267,  0x8157,  0x4113,  0x6201, 0, 0, 0, 0, 0,
    0x4113,  0x4223,  0x8267,  0x8157, 0, 0, 0, 0, 0, 0, 0, 0,
    0x8157,  0x2315,  0x3304,  0x1326,  0x8267,  0x4223,  0x4113,  0x8337, 0, 0, 0, 0,
    0x8157,  0x2315,  0x6201,  0x5102,  0x1326,  0x8267,  0x4223,  0x4113,  0x8337, 0, 0, 0,
    0x8157,  0x8337,  0x4223,  0x6201,  0x3304,  0x1326,  0x8267, 0, 0, 0, 0, 0,
    0x5102,  0x1326,  0x8267,  0x8157,  0x8337,  0x4223, 0, 0, 0, 0, 0, 0,
    0x8267,  0x8157,  0x2315,  0x3304,  0x5102,  0x4113,  0x8337, 0, 0, 0, 0, 0,
    0x6201,  0x4113,  0x8337,  0x8267,  0x8157,  0x2315, 0, 0, 0, 0, 0, 0,
    0x6201,  0x3304,  0x5102,  0x8337,  0x8267,  0x8157, 0, 0, 0, 0, 0, 0,
    0x8337,  0x8267,  0x8157, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x8337,  0x8157,  0x8267, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x3304,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x4113,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0, 0, 0,
    0x5102,  0x3304,  0x2315,  0x4113,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0, 0,
    0x5102,  0x4223,  0x1326,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0, 0, 0,
    0x6201,  0x4223,  0x1326,  0x3304,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x4113,  0x5102,  0x4223,  0x1326,  0x8337,  0x8157,  0x8267, 0, 0, 0,
    0x4223,  0x1326,  0x3304,  0x2315,  0x4113,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0,
    0x4113,  0x8157,  0x8267,  0x4223, 0, 0, 0, 0, 0, 0, 0, 0,
    0x4223,  0x4113,  0x8157,  0x8267,  0x6201,  0x5102,  0x3304, 0, 0, 0, 0, 0,
    0x8157,  0x8267,  0x4223,  0x6201,  0x2315, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x2315,  0x8157,  0x8267,  0x4223,  0x5102, 0, 0, 0, 0, 0, 0,
    0x1326,  0x5102,  0x4113,  0x8157,  0x8267, 0, 0, 0, 0, 0, 0, 0,
    0x8157,  0x4113,  0x6201,  0x3304,  0x1326,  0x8267, 0, 0, 0, 0, 0, 0,
    0x1326,  0x5102,  0x6201,  0x2315,  0x8157,  0x8267, 0, 0, 0, 0, 0, 0,
    0x8267,  0x1326,  0x3304,  0x2315,  0x8157, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1146,  0x2245,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x1146,  0x2245,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0, 0,
    0x6201,  0x2315,  0x4113,  0x3304,  0x1146,  0x2245,  0x8337,  0x8157,  0x8267, 0, 0, 0,
    0x2315,  0x4113,  0x5102,  0x1146,  0x2245,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0,
    0x5102,  0x4223,  0x1326,  0x3304,  0x1146,  0x2245,  0x8337,  0x8157,  0x8267, 0, 0, 0,
    0x1146,  0x2245,  0x6201,  0x4223,  0x1326,  0x8337,  0x8157,  0x8267, 0, 0, 0, 0,
    0x6201,  0x2315,  0x4113,  0x5102,  0x4223,  0x1326,  0x3304,  0x1146,  0x2245,  0x8337,  0x8157,  0x8267,
    0x4113,  0x4223,  0x1326,  0x1146,  0x2245,  0x2315,  0x8337,  0x8157,  0x8267, 0, 0, 0,
    0x4223,  0x4113,  0x8157,  0x8267,  0x3304,  0x1146,  0x2245, 0, 0, 0, 0, 0,
    0x6201,  0x5102,  0x1146,  0x2245,  0x4223,  0x4113,  0x8157,  0x8267, 0, 0, 0, 0,
    0x8157,  0x8267,  0x4223,  0x6201,  0x2315,  0x3304,  0x1146,  0x2245, 0, 0, 0, 0,
    0x2315,  0x8157,  0x8267,  0x4223,  0x5102,  0x1146,  0x2245, 0, 0, 0, 0, 0,
    0x1326,  0x5102,  0x4113,  0x8157,  0x8267,  0x3304,  0x1146,  0x2245, 0, 0, 0, 0,
    0x1326,  0x1146,  0x2245,  0x6201,  0x4113,  0x8157,  0x8267, 0, 0, 0, 0, 0,
    0x5102,  0x6201,  0x2315,  0x8157,  0x8267,  0x1326,  0x3304,  0x1146,  0x2245, 0, 0, 0,
    0x1326,  0x1146,  0x2245,  0x2315,  0x8157,  0x8267, 0, 0, 0, 0, 0, 0,
    0x2315,  0x2245,  0x8267,  0x8337, 0, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x2245,  0x8267,  0x8337,  0x6201,  0x5102,  0x3304, 0, 0, 0, 0, 0,
    0x4113,  0x6201,  0x2245,  0x8267,  0x8337, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4113,  0x8337,  0x8267,  0x2245,  0x3304, 0, 0, 0, 0, 0, 0,
    0x2315,  0x2245,  0x8267,  0x8337,  0x5102,  0x4223,  0x1326, 0, 0, 0, 0, 0,
    0x6201,  0x4223,  0x1326,  0x3304,  0x8337,  0x2315,  0x2245,  0x8267, 0, 0, 0, 0,
    0x4113,  0x6201,  0x2245,  0x8267,  0x8337,  0x5102,  0x4223,  0x1326, 0, 0, 0, 0,
    0x4113,  0x4223,  0x1326,  0x3304,  0x2245,  0x8267,  0x8337, 0, 0, 0, 0, 0,
    0x2315,  0x2245,  0x8267,  0x4223,  0x4113, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x2245,  0x8267,  0x4223,  0x4113,  0x6201,  0x5102,  0x3304, 0, 0, 0, 0,
    0x6201,  0x2245,  0x8267,  0x4223, 0, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x2245,  0x8267,  0x4223,  0x5102, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4113,  0x2315,  0x2245,  0x8267,  0x1326, 0, 0, 0, 0, 0, 0,
    0x4113,  0x2315,  0x2245,  0x8267,  0x1326,  0x3304,  0x6201, 0, 0, 0, 0, 0,
    0x5102,  0x6201,  0x2245,  0x8267,  0x1326, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x2245,  0x8267,  0x1326, 0, 0, 0, 0, 0, 0, 0, 0,
    0x8267,  0x8337,  0x2315,  0x3304,  0x1146, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x1146,  0x8267,  0x8337,  0x2315,  0x6201, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1146,  0x8267,  0x8337,  0x4113,  0x6201, 0, 0, 0, 0, 0, 0,
    0x8337,  0x4113,  0x5102,  0x1146,  0x8267, 0, 0, 0, 0, 0, 0, 0,
    0x8267,  0x8337,  0x2315,  0x3304,  0x1146,  0x5102,  0x4223,  0x1326, 0, 0, 0, 0,
    0x1146,  0x8267,  0x8337,  0x2315,  0x6201,  0x4223,  0x1326, 0, 0, 0, 0, 0,
    0x8267,  0x8337,  0x4113,  0x6201,  0x3304,  0x1146,  0x5102,  0x4223,  0x1326, 0, 0, 0,
    0x4113,  0x4223,  0x1326,  0x1146,  0x8267,  0x8337, 0, 0, 0, 0, 0, 0,
    0x3304,  0x2315,  0x4113,  0x4223,  0x8267,  0x1146, 0, 0, 0, 0, 0, 0,
    0x2315,  0x6201,  0x5102,  0x1146,  0x8267,  0x4223,  0x4113, 0, 0, 0, 0, 0,
    0x1146,  0x8267,  0x4223,  0x6201,  0x3304, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x1146,  0x8267,  0x4223, 0, 0, 0, 0, 0, 0, 0, 0,
    0x8267,  0x1326,  0x5102,  0x4113,  0x2315,  0x3304,  0x1146, 0, 0, 0, 0, 0,
    0x6201,  0x4113,  0x2315,  0x1326,  0x1146,  0x8267, 0, 0, 0, 0, 0, 0,
    0x6201,  0x3304,  0x1146,  0x8267,  0x1326,  0x5102, 0, 0, 0, 0, 0, 0,
    0x1326,  0x1146,  0x8267, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x1326,  0x8337,  0x8157,  0x1146, 0, 0, 0, 0, 0, 0, 0, 0,
    0x8337,  0x8157,  0x1146,  0x1326,  0x6201,  0x5102,  0x3304, 0, 0, 0, 0, 0,
    0x8337,  0x8157,  0x1146,  0x1326,  0x6201,  0x2315,  0x4113, 0, 0, 0, 0, 0,
    0x4113,  0x5102,  0x3304,  0x2315,  0x1326,  0x8337,  0x8157,  0x1146, 0, 0, 0, 0,
    0x8337,  0x8157,  0x1146,  0x5102,  0x4223, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x4223,  0x8337,  0x8157,  0x1146,  0x3304, 0, 0, 0, 0, 0, 0,
    0x8337,  0x8157,  0x1146,  0x5102,  0x4223,  0x6201,  0x2315,  0x4113, 0, 0, 0, 0,
    0x4223,  0x8337,  0x8157,  0x1146,  0x3304,  0x2315,  0x4113, 0, 0, 0, 0, 0,
    0x4223,  0x4113,  0x8157,  0x1146,  0x1326, 0, 0, 0, 0, 0, 0, 0,
    0x4223,  0x4113,  0x8157,  0x1146,  0x1326,  0x6201,  0x5102,  0x3304, 0, 0, 0, 0,
    0x1146,  0x8157,  0x2315,  0x6201,  0x4223,  0x1326, 0, 0, 0, 0, 0, 0,
    0x4223,  0x5102,  0x3304,  0x2315,  0x8157,  0x1146,  0x1326, 0, 0, 0, 0, 0,
    0x4113,  0x8157,  0x1146,  0x5102, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x4113,  0x8157,  0x1146,  0x3304, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x8157,  0x1146,  0x5102,  0x6201, 0, 0, 0, 0, 0, 0, 0,
    0x2315,  0x8157,  0x1146,  0x3304, 0, 0, 0, 0, 0, 0, 0, 0,
    0x2245,  0x3304,  0x1326,  0x8337,  0x8157, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2245,  0x8157,  0x8337,  0x1326,  0x5102, 0, 0, 0, 0, 0, 0,
    0x2245,  0x3304,  0x1326,  0x8337,  0x8157,  0x6201,  0x2315,  0x4113, 0, 0, 0, 0,
    0x2245,  0x2315,  0x4113,  0x5102,  0x1326,  0x8337,  0x8157, 0, 0, 0, 0, 0,
    0x4223,  0x8337,  0x8157,  0x2245,  0x3304,  0x5102, 0, 0, 0, 0, 0, 0,
    0x8157,  0x2245,  0x6201,  0x4223,  0x8337, 0, 0, 0, 0, 0, 0, 0,
    0x2245,  0x3304,  0x5102,  0x4223,  0x8337,  0x8157,  0x4113,  0x6201,  0x2315, 0, 0, 0,
    0x4223,  0x8337,  0x8157,  0x2245,  0x2315,  0x4113, 0, 0, 0, 0, 0, 0,
    0x4113,  0x8157,  0x2245,  0x3304,  0x1326,  0x4223, 0, 0, 0, 0, 0, 0,
    0x1326,  0x4223,  0x4113,  0x8157,  0x2245,  0x6201,  0x5102, 0, 0, 0, 0, 0,
    0x8157,  0x2245,  0x3304,  0x1326,  0x4223,  0x6201,  0x2315, 0, 0, 0, 0, 0,
    0x5102,  0x1326,  0x4223,  0x2315,  0x8157,  0x2245, 0, 0, 0, 0, 0, 0,
    0x3304,  0x5102,  0x4113,  0x8157,  0x2245, 0, 0, 0, 0, 0, 0, 0,
    0x4113,  0x8157,  0x2245,  0x6201, 0, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x6201,  0x2315,  0x8157,  0x2245,  0x3304, 0, 0, 0, 0, 0, 0,
    0x2315,  0x8157,  0x2245, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x1146,  0x1326,  0x8337,  0x2315,  0x2245, 0, 0, 0, 0, 0, 0, 0,
    0x1146,  0x1326,  0x8337,  0x2315,  0x2245,  0x6201,  0x5102,  0x3304, 0, 0, 0, 0,
    0x6201,  0x2245,  0x1146,  0x1326,  0x8337,  0x4113, 0, 0, 0, 0, 0, 0,
    0x2245,  0x1146,  0x1326,  0x8337,  0x4113,  0x5102,  0x3304, 0, 0, 0, 0, 0,
    0x5102,  0x1146,  0x2245,  0x2315,  0x8337,  0x4223, 0, 0, 0, 0, 0, 0,
    0x1146,  0x3304,  0x6201,  0x4223,  0x8337,  0x2315,  0x2245, 0, 0, 0, 0, 0,
    0x8337,  0x4113,  0x6201,  0x2245,  0x1146,  0x5102,  0x4223, 0, 0, 0, 0, 0,
    0x4223,  0x8337,  0x4113,  0x3304,  0x2245,  0x1146, 0, 0, 0, 0, 0, 0,
    0x4113,  0x2315,  0x2245,  0x1146,  0x1326,  0x4223, 0, 0, 0, 0, 0, 0,
    0x1146,  0x1326,  0x4223,  0x4113,  0x2315,  0x2245,  0x6201,  0x5102,  0x3304, 0, 0, 0,
    0x1326,  0x4223,  0x6201,  0x2245,  0x1146, 0, 0, 0, 0, 0, 0, 0,
    0x4223,  0x5102,  0x3304,  0x2245,  0x1146,  0x1326, 0, 0, 0, 0, 0, 0,
    0x2245,  0x1146,  0x5102,  0x4113,  0x2315, 0, 0, 0, 0, 0, 0, 0,
    0x4113,  0x2315,  0x2245,  0x1146,  0x3304,  0x6201, 0, 0, 0, 0, 0, 0,
    0x6201,  0x2245,  0x1146,  0x5102, 0, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x2245,  0x1146, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1326,  0x8337,  0x2315, 0, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x1326,  0x8337,  0x2315,  0x6201, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x3304,  0x1326,  0x8337,  0x4113, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x1326,  0x8337,  0x4113, 0, 0, 0, 0, 0, 0, 0, 0,
    0x4223,  0x8337,  0x2315,  0x3304,  0x5102, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x4223,  0x8337,  0x2315, 0, 0, 0, 0, 0, 0, 0, 0,
    0x3304,  0x5102,  0x4223,  0x8337,  0x4113,  0x6201, 0, 0, 0, 0, 0, 0,
    0x4113,  0x4223,  0x8337, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x4113,  0x2315,  0x3304,  0x1326,  0x4223, 0, 0, 0, 0, 0, 0, 0,
    0x1326,  0x4223,  0x4113,  0x2315,  0x6201,  0x5102, 0, 0, 0, 0, 0, 0,
    0x3304,  0x1326,  0x4223,  0x6201, 0, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x1326,  0x4223, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x5102,  0x4113,  0x2315,  0x3304, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x4113,  0x2315, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0x6201,  0x3304,  0x5102, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
};

        /*         internal static readonly MCCellData[] CellData = {
                    new MCCellData(0),
                    new MCCellData(0x31,0,1,2),
                    new MCCellData(0x62,0,1,2,3,4,5),
                    new MCCellData(0x42,0,1,2,0,2,3),
                    new MCCellData(0x53,0,1,4,1,3,4,1,2,3),
                    new MCCellData(0x73,0,1,2,0,2,3,4,5,6),
                    new MCCellData(0x93,0,1,2,3,4,5,6,7,8),
                    new MCCellData(0x84,0,1,4,1,3,4,1,2,3,5,6,7),
                    new MCCellData(0x84,0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7),
                    new MCCellData(0xC4,0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11),
                    new MCCellData(0x64,0, 4, 5, 0, 1, 4, 1, 3, 4, 1, 2, 3),
                    new MCCellData(0x64,0, 5, 4, 0, 4, 1, 1, 4, 3, 1, 3, 2),
                    new MCCellData(0x64,0, 4, 5, 0, 3, 4, 0, 1, 3, 1, 2, 3),
                    new MCCellData(0x64,0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5),
                    new MCCellData(0x75,0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 6),
                    new MCCellData(0x95,0, 4, 5, 0, 3, 4, 0, 1, 3, 1, 2, 3, 6, 7, 8),
                };
         */
        internal static readonly byte[] CellData = new byte[256] {
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0x31,0,1,2,0,0,0,0,0,0,0,0,0,0,0,0,
            0x62,0,1,2,3,4,5,0,0,0,0,0,0,0,0,0,
            0x42,0,1,2,0,2,3,0,0,0,0,0,0,0,0,0,
            0x53,0,1,4,1,3,4,1,2,3,0,0,0,0,0,0,
            0x73,0,1,2,0,2,3,4,5,6,0,0,0,0,0,0,
            0x93,0,1,2,3,4,5,6,7,8,0,0,0,0,0,0,
            0x84,0,1,4,1,3,4,1,2,3,5,6,7,0,0,0,
            0x84,0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7,0,0,0,
            0xC4,0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,0,0,0,
            0x64,0, 4, 5, 0, 1, 4, 1, 3, 4, 1, 2, 3,0,0,0,
            0x64,0, 5, 4, 0, 4, 1, 1, 4, 3, 1, 3, 2,0,0,0,
            0x64,0, 4, 5, 0, 3, 4, 0, 1, 3, 1, 2, 3,0,0,0,
            0x64,0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5,0,0,0,
            0x75,0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 6,
            0x95,0, 4, 5, 0, 3, 4, 0, 1, 3, 1, 2, 3, 6, 7, 8,
        };

        internal static readonly byte[] CellClassData = new byte[256] {
            0x00, 0x01, 0x01, 0x03, 0x01, 0x03, 0x02, 0x04, 0x01, 0x02, 0x03, 0x04, 0x03, 0x04, 0x04, 0x03,
    0x01, 0x03, 0x02, 0x04, 0x02, 0x04, 0x06, 0x0C, 0x02, 0x05, 0x05, 0x0B, 0x05, 0x0A, 0x07, 0x04,
    0x01, 0x02, 0x03, 0x04, 0x02, 0x05, 0x05, 0x0A, 0x02, 0x06, 0x04, 0x0C, 0x05, 0x07, 0x0B, 0x04,
    0x03, 0x04, 0x04, 0x03, 0x05, 0x0B, 0x07, 0x04, 0x05, 0x07, 0x0A, 0x04, 0x08, 0x0E, 0x0E, 0x03,
    0x01, 0x02, 0x02, 0x05, 0x03, 0x04, 0x05, 0x0B, 0x02, 0x06, 0x05, 0x07, 0x04, 0x0C, 0x0A, 0x04,
    0x03, 0x04, 0x05, 0x0A, 0x04, 0x03, 0x07, 0x04, 0x05, 0x07, 0x08, 0x0E, 0x0B, 0x04, 0x0E, 0x03,
    0x02, 0x06, 0x05, 0x07, 0x05, 0x07, 0x08, 0x0E, 0x06, 0x09, 0x07, 0x0F, 0x07, 0x0F, 0x0E, 0x0D,
    0x04, 0x0C, 0x0B, 0x04, 0x0A, 0x04, 0x0E, 0x03, 0x07, 0x0F, 0x0E, 0x0D, 0x0E, 0x0D, 0x02, 0x01,
    0x01, 0x02, 0x02, 0x05, 0x02, 0x05, 0x06, 0x07, 0x03, 0x05, 0x04, 0x0A, 0x04, 0x0B, 0x0C, 0x04,
    0x02, 0x05, 0x06, 0x07, 0x06, 0x07, 0x09, 0x0F, 0x05, 0x08, 0x07, 0x0E, 0x07, 0x0E, 0x0F, 0x0D,
    0x03, 0x05, 0x04, 0x0B, 0x05, 0x08, 0x07, 0x0E, 0x04, 0x07, 0x03, 0x04, 0x0A, 0x0E, 0x04, 0x03,
    0x04, 0x0A, 0x0C, 0x04, 0x07, 0x0E, 0x0F, 0x0D, 0x0B, 0x0E, 0x04, 0x03, 0x0E, 0x02, 0x0D, 0x01,
    0x03, 0x05, 0x05, 0x08, 0x04, 0x0A, 0x07, 0x0E, 0x04, 0x07, 0x0B, 0x0E, 0x03, 0x04, 0x04, 0x03,
    0x04, 0x0B, 0x07, 0x0E, 0x0C, 0x04, 0x0F, 0x0D, 0x0A, 0x0E, 0x0E, 0x02, 0x04, 0x03, 0x0D, 0x01,
    0x04, 0x07, 0x0A, 0x0E, 0x0B, 0x0E, 0x0E, 0x02, 0x0C, 0x0F, 0x04, 0x0D, 0x04, 0x0D, 0x03, 0x01,
    0x03, 0x04, 0x04, 0x03, 0x04, 0x03, 0x0D, 0x01, 0x04, 0x0D, 0x03, 0x01, 0x03, 0x01, 0x01, 0x00
        };



        [BurstCompile]
        public static void GetVertex(int index, out int3 vertex)
        {
            vertex = math.int3(VertexOrder[index * 3], VertexOrder[(index * 3) + 1], VertexOrder[(index * 3) + 2]);
        }
        [BurstCompile]
        public unsafe static void GetCellData(int index, out MCCellData cell)
        {
            fixed (byte* ptr1 = &CellData[index * 16])
            {
                MCCellData t = default;
                UnsafeUtility.MemCpy(&t, ptr1, 16);
                cell = t;
            }

        }
        [BurstDiscard]
        public static JobHandle GenerateMesh(NativeArray<MarchingCubeChunk> chunks, out Mesh.MeshDataArray meshDataArray, JobHandle dependsOn = default)
        {
            var cellOffsets = new NativeArray<int2>(chunks.Length * ChunkCubeSizeInCells, Allocator.TempJob);
            var descriptor = MCCubeVertexInfo.GetVertexAttributeDescriptor(Allocator.TempJob);
            var meshInfo = new NativeArray<MCChunkInfo>(chunks.Length, Allocator.TempJob);
            meshDataArray = Mesh.AllocateWritableMeshData(chunks.Length);

            var initJob = new MarchingCubesJobInit
            {
                chunks = chunks,
                meshDataArray = meshDataArray,
                cellOffsets = cellOffsets,
                vertexAttributeDescriptor = descriptor,
                chunkInfo = meshInfo
            }.Schedule(chunks.Length, 1, dependsOn);
            var job = new MarchingCubesJob
            {
                cubeChunks = chunks,
                cellOffsets = cellOffsets,
                meshDataArray = meshDataArray
            }.Schedule(chunks.Length, ChunkCubeSizeInCells, initJob);
            //.Schedule(chunks.Length * TotalChunkSizeInCells, 1, initJob);
            var handle1 = cellOffsets.Dispose(job);
            var handle2 = descriptor.Dispose(job);
            var handle3 = meshInfo.Dispose(job);
            return JobHandle.CombineDependencies(handle1, handle2, handle3);
        }
    }
    //Initializes Mesh data for chunks.
    [BurstCompile]
    public struct MarchingCubesJobInit : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<MarchingCubeChunk> chunks;
        public Mesh.MeshDataArray meshDataArray;
        /// <summary>
        /// Offset data for writing
        /// </summary>
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int2> cellOffsets;
        [WriteOnly]
        public NativeArray<MCChunkInfo> chunkInfo;
        [ReadOnly]
        public NativeArray<VertexAttributeDescriptor> vertexAttributeDescriptor;

        public void Execute(int index)
        {

            var chunk = chunks[index];
            int triCount = 0;
            int vertCount = 0;
            for (int y = 0; y < MarchingCubes.ChunkSizeInCells; y++)
            {
                for (int z = 0; z < MarchingCubes.ChunkSizeInCells; z++)
                {
                    for (int x = 0; x < MarchingCubes.ChunkSizeInCells; x++)
                    {
                        var cubeCase = chunk[x + 1, y + 1, z + 1];
                        MarchingCubes.GetCellData(MarchingCubes.CellClassData[cubeCase], out var cellData);
                        cellOffsets[MarchingCubes.ChunkCubeSizeInCells * index + y * MarchingCubes.ChunkSizeInCells * MarchingCubes.ChunkSizeInCells + z * MarchingCubes.ChunkSizeInCells + x] = math.int2(triCount, vertCount);
                        triCount += cellData.GetTriangleCount();
                        vertCount += cellData.GetVertexCount();
                    }
                }
            }
            var meshData = meshDataArray[index];
            meshData.SetIndexBufferParams(triCount * 3, IndexFormat.UInt16);
            meshData.SetVertexBufferParams(vertCount, vertexAttributeDescriptor);
            meshData.subMeshCount = 1;
            chunkInfo[index] = new MCChunkInfo
            {
                triangleCount = triCount,
                vertexCount = vertCount
            };
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triCount * 3)
            {
                bounds = new Bounds(new Vector3(MarchingCubes.ChunkSizeInCells * 0.5f, MarchingCubes.ChunkSizeInCells * 0.5f, MarchingCubes.ChunkSizeInCells * 0.5f), new Vector3(MarchingCubes.ChunkSizeInCells * 0.5f, MarchingCubes.ChunkSizeInCells * 0.5f, MarchingCubes.ChunkSizeInCells * 0.5f))
            }, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);

        }
    }

    public struct MCCubeVertexInfo
    {

        public static NativeArray<VertexAttributeDescriptor> GetVertexAttributeDescriptor(Allocator allocator)
        {
            var r = new NativeArray<VertexAttributeDescriptor>(4, allocator, NativeArrayOptions.UninitializedMemory);
            r[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
            r[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
            r[2] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4);
            r[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2);
            return r;
        }
        public float3 position;
        public float3 normal;
        public Color color;
        public float2 uv;

        public MCCubeVertexInfo(float3 position = default, float3 normal = default, Color color = default, float2 uv = default)
        {
            this.position = position;
            this.normal = normal;
            this.color = color;
            this.uv = uv;
        }
    }
    /// <summary>
    /// Processes each cell in a chunk in parallel.
    /// </summary>
    [BurstCompile]
    public struct MarchingCubesJob : IJobParallelFor
    {

        public ProfilerMarker marker;
        public const float IsoValue = 0.5f;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<MarchingCubeChunk> cubeChunks;

        [ReadOnly]
        public NativeArray<int2> cellOffsets;
        [NativeDisableParallelForRestriction]
        public Mesh.MeshDataArray meshDataArray;
        public unsafe void Execute(int index)
        {

            var chunkIndex = index / (MarchingCubes.ChunkCubeSizeInCells);
            var chunk = cubeChunks[chunkIndex];
            int3 cell = math.int3(
                            ((index % MarchingCubes.ChunkCubeSizeInCells) % MarchingCubes.ChunkLayerSizeInCells) % (MarchingCubes.ChunkSizeInCells),
                            (index % MarchingCubes.ChunkCubeSizeInCells) / MarchingCubes.ChunkLayerSizeInCells,
                            ((index % MarchingCubes.ChunkCubeSizeInCells) % MarchingCubes.ChunkLayerSizeInCells) / (MarchingCubes.ChunkSizeInCells)
                        );
            int3 accessCell = cell + 1;
            var cubeCase = chunk[accessCell];

            if (cubeCase == 0 || cubeCase == 255)
            {
                return;
            }
            var meshData = meshDataArray[chunkIndex];
            MarchingCubes.GetCellData(MarchingCubes.CellClassData[cubeCase], out var cellData);
            var offsets = cellOffsets[index];
            var indexData = meshData.GetIndexData<ushort>();
            var vertexData = meshData.GetVertexData<MCCubeVertexInfo>();
            for (int i = 0; i < 12 && MarchingCubes.VertexData[cubeCase * 12 + i] != 0; i++)
            {
                var edgeInfo = MarchingCubes.VertexData[cubeCase * 12 + i];
                var vertexLowIndex = edgeInfo & 15;
                var vertexHighIndex = (edgeInfo & 240) >> 4;
                MarchingCubes.GetVertex(vertexLowIndex, out int3 vertexLow);
                MarchingCubes.GetVertex(vertexHighIndex, out int3 vertexHigh);
                var vertex = cell + math.lerp(vertexLow, vertexHigh, IsoValue);
                var normal = math.normalize(math.lerp(chunk.GetNormal(accessCell, vertexLowIndex), chunk.GetNormal(accessCell, vertexHighIndex), IsoValue));
                vertexData[offsets.y + i] = new MCCubeVertexInfo(vertex, normal, new Color32(196, 250, 98, 255));
            }
            var triCount = cellData.GetTriangleCount();
            for (int i = 0; i < triCount; i++)
            {
                indexData[(offsets.x * 3) + (i * 3)] = (ushort)(offsets.y + cellData[(i * 3) + MarchingCubes.TriangleWindingOrder[0]]);
                indexData[(offsets.x * 3) + (i * 3) + 1] = (ushort)(offsets.y + cellData[(i * 3) + MarchingCubes.TriangleWindingOrder[1]]);
                indexData[(offsets.x * 3) + (i * 3) + 2] = (ushort)(offsets.y + cellData[(i * 3) + MarchingCubes.TriangleWindingOrder[2]]);
            }

        }
    }

    public struct MCChunkInfo
    {
        public int triangleCount;
        public int vertexCount;
    }
    public unsafe struct MarchingCubeChunk
    {
        internal fixed byte value[MarchingCubes.PaddedChunkCubeSizeInCells];
        public byte this[int index]
        {
            get => value[index];
            set
            {
                this.value[index] = value;
            }
        }
        public byte this[int x, int y, int z]
        {
            get => value[y * MarchingCubes.PaddedChunkLayerSizeInCells + z * MarchingCubes.PaddedChunkSizeInCells + x];
            set
            {
                this.value[y * MarchingCubes.PaddedChunkLayerSizeInCells + z * MarchingCubes.PaddedChunkSizeInCells + x] = value;
            }
        }
        public byte this[int3 cell]
        {
            get => value[cell.y * MarchingCubes.PaddedChunkLayerSizeInCells + cell.z * MarchingCubes.PaddedChunkSizeInCells + cell.x];
            set
            {
                this.value[cell.y * MarchingCubes.PaddedChunkLayerSizeInCells + cell.z * MarchingCubes.PaddedChunkSizeInCells + cell.x] = value;
            }
        }
        public FullMarchingCubeChunk SetChunkLocation(int2 chunk) => new FullMarchingCubeChunk
        {
            chunk = chunk,
            data = this
        };
        public float3 GetNormal(int3 cell, int vertex)
        {
            byte b = (byte)(1 << vertex);
            var xN = ((this[cell.x + 1, cell.y, cell.z] & b) != 0) ? 1 : 0;
            var xP = ((this[cell.x - 1, cell.y, cell.z] & b) != 0) ? 1 : 0;
            var yN = ((this[cell.x, cell.y + 1, cell.z] & b) != 0) ? 1 : 0;
            var yP = ((this[cell.x, cell.y - 1, cell.z] & b) != 0) ? 1 : 0;
            var zN = ((this[cell.x, cell.y, cell.z + 1] & b) != 0) ? 1 : 0;
            var zP = ((this[cell.x, cell.y, cell.z - 1] & b) != 0) ? 1 : 0;
            return math.float3(xP - xN, yP - yN, zP - zN) * 0.5f;
        }

    }

    public unsafe struct FullMarchingCubeChunk
    {
        public int2 chunk;
        internal MarchingCubeChunk data;
        public byte this[int index]
        {
            get => data.value[index];
            set
            {
                data.value[index] = value;
            }
        }
        public byte this[int x, int y, int z]
        {
            get => data.value[y * MarchingCubes.PaddedChunkLayerSizeInCells + z * MarchingCubes.PaddedChunkSizeInCells + x];
            set
            {
                data.value[y * MarchingCubes.PaddedChunkLayerSizeInCells + z * MarchingCubes.PaddedChunkSizeInCells + x] = value;
            }
        }
        public byte this[int3 cell]
        {
            get => data.value[cell.y * MarchingCubes.PaddedChunkLayerSizeInCells + cell.z * MarchingCubes.PaddedChunkSizeInCells + cell.x];
            set
            {
                data.value[cell.y * MarchingCubes.PaddedChunkLayerSizeInCells + cell.z * MarchingCubes.PaddedChunkSizeInCells + cell.x] = value;
            }
        }
    }
}