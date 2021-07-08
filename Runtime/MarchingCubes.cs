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

        public const int ChunkHorizontalSpanInCells = 8;
        public const int ChunkVerticalSpanInCells = 16;
        public const int ChunkHorizontalSpanInBits = ChunkHorizontalSpanInCells * 8;
        public const int ChunkVerticalSpanInBits = ChunkVerticalSpanInCells * 8;
        public const int ChunkLayerSizeInCells = ChunkHorizontalSpanInCells * ChunkHorizontalSpanInCells;
        public const int ChunkLayerSizeInBits = ChunkLayerSizeInCells * 8;
        public const int ChunkBoxSizeInCells = ChunkHorizontalSpanInCells * ChunkHorizontalSpanInCells * ChunkVerticalSpanInCells;
        public const int ChunkBoxSizeInBits = ChunkBoxSizeInCells * 8;
        public const int PaddedChunkHorizontalSpanInCells = ChunkHorizontalSpanInCells + 2;
        public const int PaddedChunkVerticalSpanInCells = ChunkVerticalSpanInCells + 2;
        public const int PaddedChunkHorizontalSpanInBits = PaddedChunkHorizontalSpanInCells * 8;
        public const int PaddedChunkVerticalSpanInBits = PaddedChunkVerticalSpanInCells * 8;
        public const int PaddedChunkLayerSizeInCells = PaddedChunkHorizontalSpanInCells * PaddedChunkHorizontalSpanInCells;
        public const int PaddedChunkLayerSizeInBits = PaddedChunkLayerSizeInCells * 8;
        public const int PaddedChunkBoxSizeInCells = PaddedChunkHorizontalSpanInCells * PaddedChunkHorizontalSpanInCells * PaddedChunkVerticalSpanInCells;
        public const int PaddedChunkBoxSizeInBits = PaddedChunkBoxSizeInCells * 8;
        public static readonly Bounds ChunkBounds = new Bounds(
            new Vector3(ChunkHorizontalSpanInCells / 2, ChunkHorizontalSpanInCells / 2, ChunkHorizontalSpanInCells / 2),
            new Vector3(ChunkHorizontalSpanInCells / 2, ChunkHorizontalSpanInCells / 2, ChunkHorizontalSpanInCells / 2)
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

    }

    public unsafe struct MarchingCubeChunk
    {
        internal fixed byte value[MarchingCubes.PaddedChunkBoxSizeInCells];
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
            get => value[y * MarchingCubes.PaddedChunkLayerSizeInCells + z * MarchingCubes.PaddedChunkHorizontalSpanInCells + x];
            set
            {
                this.value[y * MarchingCubes.PaddedChunkLayerSizeInCells + z * MarchingCubes.PaddedChunkHorizontalSpanInCells + x] = value;
            }
        }
        public byte this[int3 cell]
        {
            get => value[cell.y * MarchingCubes.PaddedChunkLayerSizeInCells + cell.z * MarchingCubes.PaddedChunkHorizontalSpanInCells + cell.x];
            set
            {
                this.value[cell.y * MarchingCubes.PaddedChunkLayerSizeInCells + cell.z * MarchingCubes.PaddedChunkHorizontalSpanInCells + cell.x] = value;
            }
        }

    }
}