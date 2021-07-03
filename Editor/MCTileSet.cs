using System;
using System.Collections.Generic;
using Reactics.Main.Battle.Editor.MarchingCubes;
using UnityEngine;
namespace Reactics.Main.Battle.Editor.Map
{

    [CreateAssetMenu(fileName = "MCTileSet", menuName = "Map Editing/MC Tile Set", order = 0)]
    public class MCTileSet : ScriptableObject
    {
        public MarchingCubesUtility.TransformationFlags flags;
        public EquivalenceClassData[] equivalenceClasses = GetDefaultClassData();
        public MarchingCubesUtility.EquivalenceClassReference[] cases = GetDefaultReferenceData();
        [Serializable]
        public struct EquivalenceClassData
        {
            public byte baseCase;
            public Mesh mesh;
        }
        private static EquivalenceClassData[] GetDefaultClassData()
        {
            var t = new EquivalenceClassData[256];
            for (int i = 0; i < 256;i++) {
                t[i] = new EquivalenceClassData
                {
                    baseCase = (byte)i
                };
            }
            return t;
        }
        private static MarchingCubesUtility.EquivalenceClassReference[] GetDefaultReferenceData()
        {
            var t = new MarchingCubesUtility.EquivalenceClassReference[256];
            for (int i = 0; i < 256; i++)
            {
                t[i] = new MarchingCubesUtility.EquivalenceClassReference
                {
                    index = i,
                    transforms = default
                };
            }
            return t;
        }
    }
}