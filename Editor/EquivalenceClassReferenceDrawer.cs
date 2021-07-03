using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Reactics.Main.Battle.Editor.Map
{

    [CustomPropertyDrawer(typeof(MarchingCubes.MarchingCubesUtility.EquivalenceClassReference))]
    public class EquivalenceClassReferenceDrawer : PropertyDrawer
    {
        private const string UXML = "Packages/reactics.main.battle/Editor/Resources/EquivalenceClassReferenceDrawer.uxml";
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var t = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree();
            t.SetEnabled(false);
            return t;
        }

    }
}