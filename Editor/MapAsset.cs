using System.IO;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.SearchService;
using UnityEngine;
namespace NeroWeNeed.Terrain.Editor
{


    public class MapAsset : ScriptableObject
    {
        public const string Extension = ".mapasset";
        [MenuItem("Assets/Create/Terrain/Map Asset")]
        public static void CreateAsset()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<EndNameEdit>(), "MapAsset", null, null);
        }
        internal class EndNameEdit : EndNameEditAction
        {
            public unsafe override void Action(int instanceId, string pathName, string resourceFile)
            {
                if (!pathName.EndsWith(Extension))
                {
                    pathName += Extension;
                }
                using (var writer = new StreamBinaryWriter(pathName))
                {
                    int4 span = new int4(0, 0, 0, 0);
                    writer.WriteBytes(&span, sizeof(int4));
                }
                AssetDatabase.ImportAsset(pathName);


            }
        }
    }
}