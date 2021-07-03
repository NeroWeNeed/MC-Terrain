using Unity.Mathematics;

namespace NeroWeNeed.Terrain
{
    public struct ChunkCommandBlock
    {
        public ChunkCommand command;
        public int2 chunk;
        public int extra;
    }
    public enum ChunkCommand : byte
    {
        Load, Update, Unload
    }
}