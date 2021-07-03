using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Reactics.Main.Battle.Editor.MarchingCubes;
using System.Collections.Generic;

using System.Linq;

namespace Reactics.Main.Battle.Editor.Map
{

    [CustomEditor(typeof(MCTileSet))]
    public class MCTileSetEditor : UnityEditor.Editor
    {
        private const string UXML = "Packages/reactics.main.battle/Editor/Resources/MCTileSetEditor.uxml";
        private List<Mesh> meshes;
        
        public override VisualElement CreateInspectorGUI()
        {
            var rootVisualElement = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree();
            InitInspectorGUI(rootVisualElement);
            return rootVisualElement;
        }
        private void InitInspectorGUI(VisualElement rootVisualElement)
        {
            rootVisualElement.Q<EnumFlagsField>("comparison-flags").RegisterValueChangedCallback((evt) => UpdateView((MarchingCubesUtility.TransformationFlags)evt.newValue));
            rootVisualElement.Query<VisualElement>(null, "equivalence-class-reference").ForEach(e => e.SetEnabled(false));
            InitView(rootVisualElement);
        }
        private void UpdateView(MarchingCubesUtility.TransformationFlags value)
        {
            MarchingCubesUtility.CountCases(value, out var result);
            var asset = (MCTileSet)serializedObject.targetObject;
            asset.cases = result.cases;
            asset.equivalenceClasses = result.equivalenceClasses.Select(e => new MCTileSet.EquivalenceClassData
            {
                baseCase = e
            }).ToArray();
            EditorUtility.SetDirty(serializedObject.targetObject);
            serializedObject.UpdateIfRequiredOrScript();

        }
        private void InitView(VisualElement rootVisualElement) {
            var caseContainer = rootVisualElement.Q<ListView>("cases");
            
            /*             for (int i = 0; i < count; i++)
            {
                var p = new PropertyField(casesProperty.GetArrayElementAtIndex(i));
                p.SetEnabled(false);
                caseContainer.Add(p);
            } */
            
        }

    }
}