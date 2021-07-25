using Unity.Entities;
using Unity.Mathematics;

namespace NeroWeNeed.Terrain
{
    public struct MapAssetData
    {
        /// <summary>
        /// Stores the span of the map where xy refers to the minimum and zw refers to the maximum.
        /// </summary>
        public int4 span;
        /// <summary>
        /// Linearly stores the chunks in the order of the span in row-major order starting from the minimum and moving to the maximum. This allows efficient arbitrary access of any chunk
        /// </summary>
        public BlobArray<Chunk> chunks;
        public ref Chunk this[int2 chunk] => ref chunks[((chunk.y - span.y) * (span.z - span.x)) + (chunk.x - span.x)];
        public bool TryGetChunk(int2 chunk, ref Chunk result)
        {
            if (span.x <= chunk.x && span.z > chunk.x && span.y <= chunk.y && span.w > chunk.y)
            {
                result = this[chunk];
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
        public unsafe struct Chunk
        {
            /// <summary>
            /// Stores each point in the map chunk without padding. Unlike the standard marching cube algorithm, the distance from the surface is not stored but whether or not it's inside the surface is The first bit denotes whether or not the point is inside (1) or outside (0) the surface. 
            /// The next 3 bits store what type of point it is for texture mapping. Currently supports up to 7 textures (000-111)
            /// The last 4 bits are reserved for hint information that will likely be necessary (i.e. for extra mesh data like grass) but is currently out of scope.
            /// </summary>
            private fixed byte value[MarchingCubes.ChunkBoxSizeInPoints];
            public byte this[int x, int y, int z]
            {
                get => value[y * MarchingCubes.ChunkLayerSizeInPoints + z * MarchingCubes.ChunkHorizontalSpanInPoints + x];
                set
                {
                    this.value[y * MarchingCubes.ChunkLayerSizeInPoints + z * MarchingCubes.ChunkHorizontalSpanInPoints + x] = value;
                }
            }
            public byte this[int3 cell]
            {
                get => value[cell.y * MarchingCubes.ChunkLayerSizeInPoints + cell.z * MarchingCubes.ChunkHorizontalSpanInPoints + cell.x];
                set
                {
                    this.value[cell.y * MarchingCubes.ChunkLayerSizeInPoints + cell.z * MarchingCubes.ChunkHorizontalSpanInPoints + cell.x] = value;
                }
            }
            public static bool IsInSurface(byte value) => (value & 1) != 0;
            public static int GetTerrainIndex(byte value) => ((value >> 1) & 7);
            public static int GetHintInfo(byte value) => ((value >> 4) & 8);
        }
    }
}