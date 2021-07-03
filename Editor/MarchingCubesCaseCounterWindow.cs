using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using Unity.Entities;

namespace Reactics.Main.Battle.Editor.MarchingCubes
{

    public class MarchingCubesCaseCounterWindow : EditorWindow
    {
        private const string UXML = "Packages/reactics.main.battle/Editor/Resources/MarchingCubesCaseCounterWindow.uxml";

        private State state;
        [MenuItem("Window/Utility/Marching Cubes Case Counter")]
        private static void ShowWindow()
        {

            var window = GetWindow<MarchingCubesCaseCounterWindow>();
            window.titleContent = new GUIContent("Marching Cubes Case Counter");
            window.Show();
        }
        private void OnEnable()
        {

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree();
            state = CreateInstance<State>();
            Configure(uxml);
            uxml.Bind(new SerializedObject(state));
            rootVisualElement.Add(uxml);
        }
        private void OnDisable()
        {
            if (state != null)
            {
                DestroyImmediate(state);
                state = null;
            }
        }
        private void Configure(VisualElement visualElement)
        {
            var countCasesButton = visualElement.Q<Button>("count-cases");
            countCasesButton.clicked += CountCases;
        }
        private void CountCases()
        {
            var flags = state?.flags ?? MarchingCubesUtility.TransformationFlags.All;
            MarchingCubesUtility.CountCases(flags, out var cases);
            Debug.Log(cases.equivalenceClasses.Count);
            var meshes = MarchingCubesUtility.BuildCubeRepresentations(cases.equivalenceClasses.ToArray());
            for (int i = 0; i < meshes.Count; i++)
            {
                AssetDatabase.CreateAsset(meshes[i], $"Assets/MarchingCubeMesh_{i}.mesh");
            }
            using var fs = File.CreateText("MarchingCubes.json");
            fs.Write(JsonUtility.ToJson(cases));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private sealed class State : ScriptableObject
        {
            public MarchingCubesUtility.TransformationFlags flags;
            public Mesh[] equivalenceClassMeshes;
        }
    }
}