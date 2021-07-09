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

        internal static readonly MarchingCubes.MCCellData[] CellData = {
                    new MarchingCubes.MCCellData(0),
                    new MarchingCubes.MCCellData(0x31,0,1,2),
                    new MarchingCubes.MCCellData(0x62,0,1,2,3,4,5),
                    new MarchingCubes.MCCellData(0x42,0,1,2,0,2,3),
                    new MarchingCubes.MCCellData(0x53,0,1,4,1,3,4,1,2,3),
                    new MarchingCubes.MCCellData(0x73,0,1,2,0,2,3,4,5,6),
                    new MarchingCubes.MCCellData(0x93,0,1,2,3,4,5,6,7,8),
                    new MarchingCubes.MCCellData(0x84,0,1,4,1,3,4,1,2,3,5,6,7),
                    new MarchingCubes.MCCellData(0x84,0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7),
                    new MarchingCubes.MCCellData(0xC4,0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11),
                    new MarchingCubes.MCCellData(0x64,0, 4, 5, 0, 1, 4, 1, 3, 4, 1, 2, 3),
                    new MarchingCubes.MCCellData(0x64,0, 5, 4, 0, 4, 1, 1, 4, 3, 1, 3, 2),
                    new MarchingCubes.MCCellData(0x64,0, 4, 5, 0, 3, 4, 0, 1, 3, 1, 2, 3),
                    new MarchingCubes.MCCellData(0x64,0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5),
                    new MarchingCubes.MCCellData(0x75,0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 6),
                    new MarchingCubes.MCCellData(0x95,0, 4, 5, 0, 3, 4, 0, 1, 3, 1, 2, 3, 6, 7, 8),
                };
        [MenuItem("Utility/Generate Tables")]
        internal static void GenerateVertexDataIndexTable()
        {
            var indices = new List<int>();
            var values = new List<ushort>();
            int offset = 0;
            for (int i = 0; i < CellClassData.Length; i++)
            {
                var vertexCount = CellData[CellClassData[i]].GetVertexCount();
                for (int j = 0; j < vertexCount; j++)
                {
                    values.Add(vertexData[i * 12 + j]);
                }
                indices.Add(offset);
                offset += vertexCount;
            }
            using var fs = File.CreateText("Assets/TableData.txt");

            fs.WriteLine($"static const min16uint vertexData[{values.Count}] = {{");
            for (int i = 0; i < values.Count; i++)
            {
                fs.WriteLine($"    0x{values[i]:X}{(i + 1 < values.Count ? "," : "")}");
            }
            fs.WriteLine("};");
            fs.WriteLine($"static const uint vertexLookupTable[{indices.Count}] = {{");
            for (int i = 0; i < indices.Count; i++)
            {
                fs.WriteLine($"    {indices[i]}{(i + 1 < indices.Count ? "," : "")}");
            }
            fs.WriteLine("};");
            /*                     var indices = new List<int>();
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
                                    fs.WriteLine($"    0x{values[i]:X}{(i + 1 < values.Count ? "," : "")}");
                                }
                                fs.WriteLine("};");
                                fs.WriteLine($"static const uint vertexLookupTable[{indices.Count}] = {{");
                                for (int i = 0; i < indices.Count; i++)
                                {
                                    fs.WriteLine($"    {indices[i]}{(i + 1 < indices.Count ? "," : "")}");
                                }
                                fs.WriteLine("};"); */


        }

        [BurstCompile]
        internal unsafe struct UpdateBufferJob : IJob
        {
            public NativeList<byte> output;
            public NativeArray<int2> chunkLocations;
            public NativeArray<MarchingCubeChunk> chunks;
            public NativeReference<Bounds> bounds;
            public int count;
            public float cellScale;
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
                    new Vector3((minimum.x + maximum.x) * 0.5f * MarchingCubes.ChunkHorizontalSpanInCells, MarchingCubes.ChunkHorizontalSpanInCells * 0.5f, (minimum.y + maximum.y) * 0.5f * MarchingCubes.ChunkHorizontalSpanInCells) * cellScale,
                    new Vector3(math.abs(minimum.x - maximum.x) * 0.5f * MarchingCubes.ChunkHorizontalSpanInCells, MarchingCubes.ChunkHorizontalSpanInCells, math.abs(minimum.y - maximum.y) * 0.5f * MarchingCubes.ChunkHorizontalSpanInCells) * cellScale
                );
            }
        }
        private const string COMPUTE_SHADER_ASSET = "Packages/github.neroweneed.marching-cubes-terrain/ComputeBuffers/MarchingCubes.compute";
        private static readonly uint[] InitialDrawArguments = new uint[] { 0, 1, 0, 0, 0 };
        private static readonly uint[] InitialChunkArguments = new uint[] { 0 };
        private ComputeShader computeShader;
        public static readonly int ChunkDataId = Shader.PropertyToID("ChunkData");
        public static readonly int CellScaleId = Shader.PropertyToID("CellScale");
        public static readonly int IsoValueId = Shader.PropertyToID("IsoValue");
        public static readonly int VericesId = Shader.PropertyToID("Vertices");
        public static readonly int NormalsId = Shader.PropertyToID("Normals");
        public static readonly int IndicesId = Shader.PropertyToID("Indices");
        public static readonly int DrawArgumentsId = Shader.PropertyToID("DrawArguments");
        public static readonly int ChunkArgumentsId = Shader.PropertyToID("ChunkArguments");
        public GraphicsBuffer ChunkData { get; private set; }
        public GraphicsBuffer VertexBuffer { get; private set; }
        public ComputeBuffer NormalBuffer { get; private set; }
        public GraphicsBuffer IndexBuffer { get; private set; }
        public ComputeBuffer DrawArguments { get; private set; }
        public ComputeBuffer ChunkArguments { get; private set; }

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
            kernal = computeShader.FindKernel("MarchingCubes");
            chunkDataBuffer = new NativeList<byte>(8, Allocator.Persistent);
            terrainBounds = new NativeReference<Bounds>(Allocator.Persistent);
            terrainChunkSystem = World.GetOrCreateSystem<TerrainChunkSystem>();
            MaterialProperties = new MaterialPropertyBlock();
            DrawArguments = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments | ComputeBufferType.Structured);
            computeShader.SetBuffer(kernal, DrawArgumentsId, DrawArguments);
            ChunkArguments = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);

            ChunkArguments.SetData(InitialChunkArguments);
            computeShader.SetBuffer(kernal, ChunkArgumentsId, ChunkArguments);
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
                    ChunkData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, chunkDataBuffer.Length / 4, 4);
                    computeShader.SetBuffer(kernal, ChunkDataId, ChunkData);
                }
                else if (ChunkData.count < chunkDataBuffer.Length)
                {
                    ChunkData.Release();
                    ChunkData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, chunkDataBuffer.Length / 4, 4);
                    computeShader.SetBuffer(kernal, ChunkDataId, ChunkData);
                }
                if (VertexBuffer == null)
                {
                    VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12), sizeof(float3));
                    MaterialProperties.SetBuffer(VericesId, VertexBuffer);
                    computeShader.SetBuffer(kernal, VericesId, VertexBuffer);
                }
                else if (VertexBuffer.count < chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12))
                {
                    VertexBuffer.Release();
                    VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12), sizeof(float3));
                    MaterialProperties.SetBuffer(VericesId, VertexBuffer);
                    computeShader.SetBuffer(kernal, VericesId, VertexBuffer);
                }
                if (NormalBuffer == null)
                {
                    NormalBuffer = new ComputeBuffer(chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12), sizeof(float3), ComputeBufferType.Structured);
                    MaterialProperties.SetBuffer(NormalsId, NormalBuffer);
                    computeShader.SetBuffer(kernal, NormalsId, NormalBuffer);
                }
                else if (NormalBuffer.count < chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12))
                {
                    NormalBuffer.Release();
                    NormalBuffer = new ComputeBuffer(chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 12), sizeof(float3), ComputeBufferType.Structured);
                    MaterialProperties.SetBuffer(NormalsId, NormalBuffer);
                    computeShader.SetBuffer(kernal, NormalsId, NormalBuffer);
                }
                if (IndexBuffer == null)
                {
                    IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 15), sizeof(uint));
                    computeShader.SetBuffer(kernal, IndicesId, IndexBuffer);
                }
                else if (IndexBuffer.count < chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 15))
                {
                    IndexBuffer.Release();
                    IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkCount * (MarchingCubes.ChunkBoxSizeInCells * 15), sizeof(uint));
                    computeShader.SetBuffer(kernal, IndicesId, IndexBuffer);
                }

                ChunkData.SetData<byte>(chunkDataBuffer);
                DrawArguments.SetData(InitialDrawArguments);
                ChunkArguments.SetData(InitialChunkArguments);
                computeShader.SetFloat(IsoValueId, GetSingleton<TerrainIsoValue>());
                computeShader.SetFloat(CellScaleId, GetSingleton<TerrainCellScale>());
                computeShader.Dispatch(kernal, chunkCount, 1, 1);
                lastVersion = version;
            }

        }
        protected override void OnDestroy()
        {

            chunkDataBuffer.Dispose();
            VertexBuffer?.Release();
            NormalBuffer?.Release();
            ChunkData?.Release();
            IndexBuffer?.Release();
            DrawArguments?.Release();
            ChunkArguments?.Release();
            terrainBounds.Dispose();
            base.OnDestroy();
        }
        public JobHandle SetChunkData(NativeArray<int2> chunkLocations, NativeArray<MarchingCubeChunk> chunks, int count, float cellScale, JobHandle dependency = default)
        {

            return new UpdateBufferJob
            {
                output = chunkDataBuffer,
                chunks = chunks,
                chunkLocations = chunkLocations,
                count = count,
                bounds = terrainBounds,
                cellScale = cellScale
            }.Schedule(dependency);
        }
        public bool IsCompleted() => terrainChunkSystem.IsCompleted() && ChunkDataHandle.IsCompleted;
        public void AddChunkDataJobHandle(JobHandle handle)
        {
            ChunkDataHandle = JobHandle.CombineDependencies(ChunkDataHandle, handle);
        }
    }
}