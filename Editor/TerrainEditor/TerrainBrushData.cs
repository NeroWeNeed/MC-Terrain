using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using ICSharpCode.NRefactory.Ast;
using UnityEditor;

namespace NeroWeNeed.Terrain.Editor
{
    [FilePath("Terrain/TerrainBrushData.asset", FilePathAttribute.Location.PreferencesFolder)]
    public sealed class TerrainBrushData : ScriptableSingleton<TerrainBrushData>
    {

        public static void RefreshBrushes()
        {
            var brushEntries = new List<BrushData>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(BaseTerrainBrush).IsAssignableFrom(type) && !type.IsGenericType && !type.IsAbstract)
                    {
                        var attr = type.GetCustomAttribute<TerrainBrushAttribute>();
                        if (attr != null)
                        {
                            
                            brushEntries.Add(new BrushData
                            {
                                name = attr.name,
                                icon = attr.icon,
                                brush = (BaseTerrainBrush)CreateInstance(type)
                            });
                        }
                    }

                }
            }
            var terrainBrushData = TerrainBrushData.instance;
            terrainBrushData.brushes = brushEntries;
            EditorUtility.SetDirty(terrainBrushData);
        }
        public static ReadOnlyCollection<BrushData> GetBrushes() => instance.brushes.AsReadOnly();
        internal List<BrushData> brushes = new List<BrushData>();


        [Serializable]
        public struct BrushData
        {
            public string name;
            public string icon;
            public BaseTerrainBrush brush;
        }
    }
}