using UnityEditor.AssetImporters;
using UnityEngine;

namespace NeroWeNeed.Terrain.Editor
{
    [ScriptedImporter(1, MapAsset.Extension)]
    public sealed class MapAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<MapAsset>();
            ctx.AddObjectToAsset("Main", asset);
            ctx.SetMainObject(asset);
        }
    }
}