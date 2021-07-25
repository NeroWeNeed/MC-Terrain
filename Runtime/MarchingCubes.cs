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
        public const int ChunkHorizontalSpanInCells = 8;
        public const int ChunkHorizontalSpanInPoints = ChunkHorizontalSpanInCells + 1;
        public const int ChunkVerticalSpanInCells = 16;
        public const int ChunkVerticalSpanInPoints = ChunkVerticalSpanInCells + 1;
        public const int ChunkHorizontalSpanInBits = ChunkHorizontalSpanInCells * 8;
        public const int ChunkVerticalSpanInBits = ChunkVerticalSpanInCells * 8;
        public const int ChunkLayerSizeInCells = ChunkHorizontalSpanInCells * ChunkHorizontalSpanInCells;
        public const int ChunkLayerSizeInBits = ChunkLayerSizeInCells * 8;
        public const int ChunkLayerSizeInPoints = ChunkHorizontalSpanInPoints * ChunkHorizontalSpanInPoints;
        public const int ChunkBoxSizeInCells = ChunkHorizontalSpanInCells * ChunkHorizontalSpanInCells * ChunkVerticalSpanInCells;
        public const int ChunkBoxSizeInBits = ChunkBoxSizeInCells * 8;
        public const int ChunkBoxSizeInPoints = ChunkHorizontalSpanInPoints * ChunkHorizontalSpanInPoints * ChunkVerticalSpanInPoints;
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




        [BurstCompile]
        public static void GetVertex(int index, out int3 vertex)
        {
            vertex = math.int3(VertexOrder[index * 3], VertexOrder[(index * 3) + 1], VertexOrder[(index * 3) + 2]);
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