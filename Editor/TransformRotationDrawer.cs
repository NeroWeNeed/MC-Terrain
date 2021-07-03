using System.Collections.Generic;
using Reactics.Main.Battle.Editor.MarchingCubes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Reactics.Main.Battle.Editor.Map
{

    [CustomPropertyDrawer(typeof(MarchingCubes.MarchingCubesUtility.TransformRotation))]
    public class TransformRotationDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var p = property.FindPropertyRelative(nameof(MarchingCubesUtility.TransformRotation.count));
            return new PopupField<int>(property.displayName, new List<int> { 0, 1, 2, 3 }, p.intValue, (x) => $"{x * 90}°", (x) => $"{x * 90}°")
            {
                bindingPath = p.propertyPath
            };
        }

    }
    [CustomPropertyDrawer(typeof(MarchingCubes.MarchingCubesUtility.TransformData))]
    public class TransformDataDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement element = new VisualElement();

            element.style.flexDirection = FlexDirection.Row;
            element.style.flexGrow = 1;
            var x = property.FindPropertyRelative($"{nameof(MarchingCubes.MarchingCubesUtility.TransformData.xRotations)}.{nameof(MarchingCubesUtility.TransformRotation.count)}");
            var y = property.FindPropertyRelative($"{nameof(MarchingCubes.MarchingCubesUtility.TransformData.yRotations)}.{nameof(MarchingCubesUtility.TransformRotation.count)}");
            var z = property.FindPropertyRelative($"{nameof(MarchingCubes.MarchingCubesUtility.TransformData.zRotations)}.{nameof(MarchingCubesUtility.TransformRotation.count)}");
            var i = property.FindPropertyRelative(nameof(MarchingCubes.MarchingCubesUtility.TransformData.invert));
            element.Add(CreateRotationField(x, "X"));
            element.Add(CreateRotationField(y, "Y"));
            element.Add(CreateRotationField(z, "Z"));
            element.Add(CreateInvertField(i, "Inverted"));

            return element;


        }
        private PopupField<int> CreateRotationField(SerializedProperty property, string label)
        {
            var t = new PopupField<int>(label, new List<int> { 0, 1, 2, 3 }, property.intValue, (x) => $"{x * 90}°", (x) => $"{x * 90}°")
            {
                bindingPath = property.propertyPath
            };
            t.labelElement.style.minWidth = 20;
            t.style.flexGrow = 1;
            return t;
        }
        private Toggle CreateInvertField(SerializedProperty property, string label)
        {
            var t = new Toggle
            {
                bindingPath = property.propertyPath,
                value = property.boolValue,
                label = label
            };
            t.labelElement.style.minWidth = 20;
            t.style.flexGrow = 1;
            return t;
        }
    }
}