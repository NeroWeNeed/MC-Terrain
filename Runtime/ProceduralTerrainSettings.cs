using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NeroWeNeed.Terrain
{
    public struct TerrainSettingsData
    {
        public enum StorageType : byte
        {
            Persistent, Temporary
        }
        public static readonly int4 DefaultBounds = new int4(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue);
        public BlobString storagePath;
        public int4 bounds;
        public StorageType storageType;
        public uint cacheSize;

    }
    public struct RuntimeTerrainSettingsData
    {
        public const string ChunkDirectory = "chunks";
        public const string ChunkInfo = "chunkInfo.bin";
        public BlobString chunkStoragePath;
        public BlobString chunkInfoFile;
        public int4 bounds;
        public uint cacheSize;
        public static BlobAssetReference<RuntimeTerrainSettingsData> Create(BlobAssetReference<TerrainSettingsData> settings, Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref RuntimeTerrainSettingsData root = ref builder.ConstructRoot<RuntimeTerrainSettingsData>();
            root.bounds = settings.Value.bounds;
            switch (settings.Value.storageType)
            {
                case TerrainSettingsData.StorageType.Persistent:
                    if (!Directory.Exists($"{Application.persistentDataPath}/{ChunkDirectory}"))
                    {
                        Directory.CreateDirectory($"{Application.persistentDataPath}/{ChunkDirectory}");
                    }
                    builder.AllocateString(ref root.chunkStoragePath, $"{Application.persistentDataPath}/{ChunkDirectory}");
                    builder.AllocateString(ref root.chunkInfoFile, $"{Application.persistentDataPath}/{ChunkInfo}");
                    break;
                case TerrainSettingsData.StorageType.Temporary:
                    if (!Directory.Exists($"{Application.temporaryCachePath}/{ChunkDirectory}"))
                    {
                        Directory.CreateDirectory($"{Application.temporaryCachePath}/{ChunkDirectory}");
                    }
                    builder.AllocateString(ref root.chunkStoragePath, $"{Application.temporaryCachePath}/{ChunkDirectory}");
                    builder.AllocateString(ref root.chunkInfoFile, $"{Application.temporaryCachePath}/{ChunkInfo}");
                    break;
                default:
                    break;
            }
            root.cacheSize = settings.Value.cacheSize;

            return builder.CreateBlobAssetReference<RuntimeTerrainSettingsData>(allocator);
        }
    }
}