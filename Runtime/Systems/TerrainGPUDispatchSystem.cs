using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NeroWeNeed.Terrain
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public unsafe sealed class TerrainGPUDispatchSystem : SystemBase
    {
        static readonly ushort[] vertexData = new ushort[3072] {
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
        /*         [MenuItem("Utility/Generate Tables")]
                internal static void GenerateVertexDataIndexTable()
                {
                    var indices = new List<int>();
                    var values = new List<ushort>();
                    int realIndex = 0;
                    for (int i = 0; i < 256; i++)
                    {
                        int firstZero = 0;
                        for (int j = 0; j < 12; j++)
                        {
                            if (vertexData[i * 12 + j] == 0)
                            {
                                firstZero = j;
                                break;
                            }
                            else
                            {
                                values.Add(vertexData[i * 12 + j]);
                            }
                        }
                        indices.Add(realIndex);
                        realIndex += firstZero;
                    }
                    using var fs = File.CreateText("Assets/TableData.txt");

                    fs.WriteLine($"static const min16uint vertexData[{values.Count}] = {{");
                    for (int i = 0; i < values.Count; i++)
                    {
                        fs.WriteLine($"    {values[i]}{(i + 1 < values.Count ? "," : "")}");
                    }
                    fs.WriteLine("};");
                    fs.WriteLine($"static const uint vertexLookupTable[{indices.Count}] = {{");
                    for (int i = 0; i < indices.Count; i++)
                    {
                        fs.WriteLine($"    {indices[i]}{(i + 1 < indices.Count ? "," : "")}");
                    }
                    fs.WriteLine("};");


                }
         */
        [BurstCompile]
        internal unsafe struct UpdateBufferJob : IJob
        {
            public NativeList<byte> output;
            public NativeArray<int2> chunkLocations;
            public NativeArray<MarchingCubeChunk> chunks;
            public NativeReference<Bounds> bounds;
            public int count;
            public void Execute()
            {
                output.Clear();
                var ptr = (byte*)output.GetUnsafePtr();
                int2 minimum = chunkLocations[0], maximum = chunkLocations[0];
                for (int i = 0; i < chunks.Length; i++)
                {
                    var chunkLocation = chunkLocations[i];
                    var chunk = chunks[i];
                    output.AddRange(&chunkLocation, 8);
                    output.AddRange(&chunk, sizeof(MarchingCubeChunk));
                    minimum = math.min(minimum, chunkLocations[i]);
                    maximum = math.max(maximum, chunkLocations[i]);
                }
                bounds.Value = new Bounds(
                    new Vector3((minimum.x + maximum.x) * 0.5f * MarchingCubes.ChunkSizeInCells, MarchingCubes.ChunkSizeInCells * 0.5f, (minimum.y + maximum.y) * 0.5f * MarchingCubes.ChunkSizeInCells),
                    new Vector3(math.abs(minimum.x - maximum.x) * 0.5f * MarchingCubes.ChunkSizeInCells, MarchingCubes.ChunkSizeInCells, math.abs(minimum.y - maximum.y) * 0.5f * MarchingCubes.ChunkSizeInCells)
                );
            }
        }
        private const string COMPUTE_SHADER_ASSET = "Packages/github.neroweneed.marching-cubes-terrain/ComputeBuffers/MarchingCubes.compute";
        private ComputeShader computeShader;
        public static readonly int ChunkDataId = Shader.PropertyToID("_ChunkData");
        public static readonly int ChunkCountId = Shader.PropertyToID("_ChunkCount");
        public static readonly int CellScaleId = Shader.PropertyToID("_CellScale");
        public static readonly int IsoValueId = Shader.PropertyToID("_IsoValue");
        public static readonly int VericesId = Shader.PropertyToID("_Vertices");
        public static readonly int NormalsId = Shader.PropertyToID("_Normals");
        public static readonly int IndicesId = Shader.PropertyToID("_Indices");
        public GraphicsBuffer ChunkData { get; private set; }
        public GraphicsBuffer VertexBuffer { get; private set; }
        public ComputeBuffer NormalBuffer { get; private set; }
        public GraphicsBuffer IndexBuffer { get; private set; }
        
        private TerrainChunkSystem terrainChunkSystem;
        private JobHandle ChunkDataHandle;
        public NativeList<byte> chunkDataBuffer;
        public NativeReference<Bounds> terrainBounds;
        public int lastVersion;
        private int kernal;
        public MaterialPropertyBlock MaterialProperties { get; private set; }

        protected override void OnCreate()
        {
            computeShader = Addressables.LoadAssetAsync<ComputeShader>(COMPUTE_SHADER_ASSET).WaitForCompletion();
            //kernal = computeShader.FindKernel("CSMain");
            kernal = computeShader.FindKernel("MarchingCubes");
            chunkDataBuffer = new NativeList<byte>(8, Allocator.Persistent);
            terrainBounds = new NativeReference<Bounds>(Allocator.Persistent);
            terrainChunkSystem = World.GetOrCreateSystem<TerrainChunkSystem>();
            MaterialProperties = new MaterialPropertyBlock();
            RequireSingletonForUpdate<TerrainIsoValue>();
            RequireSingletonForUpdate<TerrainCellScale>();

        }
        protected override void OnUpdate()
        {
            terrainChunkSystem.WaitForCompletion();
            ChunkDataHandle.Complete();
            var version = terrainChunkSystem.GetLoadedChunkSetVersion();
            if (lastVersion != version)
            {
                int chunkCount = (chunkDataBuffer.Length / (sizeof(MarchingCubeChunk) + sizeof(int2)));
                if (ChunkData == null)
                {
                    ChunkData = new GraphicsBuffer(GraphicsBuffer.Target.Raw , chunkDataBuffer.Length / 4, 4);
                    //ChunkData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, chunkDataBuffer.Length * sizeof(FullMarchingCubeChunk), 1);
                    //computeShader.SetConstantBuffer("ChunkData", ChunkData, 0, chunkDataBuffer.Length);
                    computeShader.SetBuffer(kernal, "ChunkData", ChunkData);
                }
                else if (ChunkData.count < chunkDataBuffer.Length)
                {
                    ChunkData.Release();
                    ChunkData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, chunkDataBuffer.Length / 4, 4);
                    //computeShader.SetConstantBuffer("ChunkData", ChunkData, 0, chunkDataBuffer.Length);
                    computeShader.SetBuffer(kernal, "ChunkData", ChunkData);
                }
                if (VertexBuffer == null)
                {
                    VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Counter, chunkCount * (MarchingCubes.ChunkCubeSizeInCells * 12), sizeof(float3));
                    MaterialProperties.SetBuffer("Vertices", VertexBuffer);
                    computeShader.SetBuffer(kernal, "Vertices", VertexBuffer);
                }
                else if (VertexBuffer.count < chunkCount * (MarchingCubes.ChunkCubeSizeInCells * 12))
                {
                    VertexBuffer.Release();
                    VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Counter, chunkCount * (MarchingCubes.ChunkCubeSizeInCells * 12), sizeof(float3));
                    MaterialProperties.SetBuffer("Vertices", VertexBuffer);
                    computeShader.SetBuffer(kernal, "Vertices", VertexBuffer);
                }
                if (NormalBuffer == null)
                {
                    NormalBuffer = new ComputeBuffer(chunkCount * (MarchingCubes.ChunkCubeSizeInCells * 12), sizeof(float3), ComputeBufferType.Structured);
                    MaterialProperties.SetBuffer("Normals", NormalBuffer);
                    computeShader.SetBuffer(kernal, "Normals", NormalBuffer);
                }
                else if (NormalBuffer.count < chunkCount * (MarchingCubes.ChunkCubeSizeInCells * 12))
                {
                    NormalBuffer.Release();
                    NormalBuffer = new ComputeBuffer(chunkCount * (MarchingCubes.ChunkCubeSizeInCells * 12), sizeof(float3), ComputeBufferType.Structured);
                    MaterialProperties.SetBuffer("Normals", NormalBuffer);
                    computeShader.SetBuffer(kernal, "Normals", NormalBuffer);
                }
                if (IndexBuffer == null)
                {
                    IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Counter, chunkCount * (MarchingCubes.ChunkCubeSizeInCells * 15), sizeof(uint));
                    MaterialProperties.SetBuffer("Indices", IndexBuffer);
                    computeShader.SetBuffer(kernal, "Indices", IndexBuffer);
                }
                else if (IndexBuffer.count < chunkCount * (MarchingCubes.ChunkCubeSizeInCells * 15))
                {
                    IndexBuffer.Release();
                    IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Counter, chunkCount * (MarchingCubes.ChunkCubeSizeInCells * 15), sizeof(uint));
                    MaterialProperties.SetBuffer("Indices", IndexBuffer);
                    computeShader.SetBuffer(kernal, "Indices", IndexBuffer);
                }

                VertexBuffer.SetCounterValue(0);
                NormalBuffer.SetCounterValue(0);
                IndexBuffer.SetCounterValue(0);
                ChunkData.SetCounterValue(0);

                ChunkData.SetData<byte>(chunkDataBuffer);
                computeShader.SetInt("_ChunkCount", chunkCount);
                computeShader.SetFloat(IsoValueId, GetSingleton<TerrainIsoValue>());
                computeShader.SetFloat(CellScaleId, GetSingleton<TerrainCellScale>());
                //computeShader.Dispatch(computeShader.FindKernel("CSMain"), 1, 16, 9);
                //computeShader.Dispatch(kernal, 1, 1,1);
                //computeShader.Dispatch(computeShader.FindKernel("CSMain"), 1,1,1);

                computeShader.Dispatch(kernal, chunkCount, 16, 1);
                lastVersion = version;
            }
        }
        private void SumChunk()
        {
            var ptr = (byte*)chunkDataBuffer.GetUnsafeReadOnlyPtr();
            Debug.Log(sizeof(MarchingCubeChunk));
            for (int i = 0; i < (sizeof(MarchingCubeChunk)); i++)
            {

                Debug.Log($"{i}: {ptr[i]}");


            }

        }
        protected override void OnDestroy()
        {
            chunkDataBuffer.Dispose();
            VertexBuffer?.Release();
            NormalBuffer?.Release();
            ChunkData?.Release();
            IndexBuffer?.Release();
            terrainBounds.Dispose();
            base.OnDestroy();
        }
        public JobHandle SetChunkData(NativeArray<int2> chunkLocations, NativeArray<MarchingCubeChunk> chunks, int count, JobHandle dependency = default)
        {

            return new UpdateBufferJob
            {
                output = chunkDataBuffer,
                chunks = chunks,
                chunkLocations = chunkLocations,
                count = count,
                bounds = terrainBounds
            }.Schedule(dependency);
        }
        public void AddChunkDataJobHandle(JobHandle handle)
        {
            ChunkDataHandle = JobHandle.CombineDependencies(ChunkDataHandle, handle);
        }
    }
}