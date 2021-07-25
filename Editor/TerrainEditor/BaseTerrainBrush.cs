using System;
using Unity.Mathematics;
using UnityEngine;
namespace NeroWeNeed.Terrain.Editor
{
    public unsafe abstract class BaseTerrainBrush : ScriptableObject
    {

        public abstract void DoBrush(TerrainBrushInput input);
    }
    public unsafe struct TerrainBrushInput
    {
        public void* data;
        public int2 chunk;
        public int3 point;
        public TerrainBrushInputType type;
    }
    public enum TerrainBrushInputType : byte
    {
        Hover, Start, Update, End
    }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TerrainBrushAttribute : Attribute
    {
        public string name;
        public string icon;
    }
}