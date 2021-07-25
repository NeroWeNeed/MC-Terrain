
using Unity.Mathematics;
using Unity.Physics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeroWeNeed.Terrain.Editor
{
    [CustomEditor(typeof(MapAsset))]
    public sealed class MapEditor : UnityEditor.Editor
    {
        const string UXML = "Packages/github.neroweneed.marching-cubes-terrain/Editor/Resources/MapAssetEditor.uxml";
        private MapEditorInstance editorInstance;
        private SerializedObject proxy;
        private void OnEnable()
        {
            editorInstance = new MapEditorInstance(this,AssetDatabase.GetAssetPath(serializedObject.targetObject));
            var p = CreateInstance<MapAssetProxy>();
            p.span = editorInstance.Span;
            EditorUtility.SetDirty(p);
            proxy = new SerializedObject(p);
            SceneView.duringSceneGui += editorInstance.OnSceneGUI;


        }
        private void OnDisable()
        {
            editorInstance.Serialize(AssetDatabase.GetAssetPath(serializedObject.targetObject));
            SceneView.duringSceneGui -= editorInstance.OnSceneGUI;
            editorInstance.Dispose();
            proxy.Dispose();
        }
        public override VisualElement CreateInspectorGUI()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree();
            uxml.Bind(proxy);
            uxml.Q<PropertyField>("map-span").RegisterValueChangeCallback(evt =>
            {
                Debug.Log("call");
                editorInstance.Span = new int4(
                    evt.changedProperty.FindPropertyRelative(nameof(int4.x)).intValue,
                    evt.changedProperty.FindPropertyRelative(nameof(int4.y)).intValue,
                    evt.changedProperty.FindPropertyRelative(nameof(int4.z)).intValue,
                    evt.changedProperty.FindPropertyRelative(nameof(int4.w)).intValue
                );

            });
            return uxml;
        }

    }
}