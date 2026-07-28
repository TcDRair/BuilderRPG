using UnityEngine;
using UnityEditor;

//? 어트리뷰트 정의는 Scripts/Attributes/InspectorNameAttribute.cs (런타임 어셈블리)에 있습니다.
[CustomPropertyDrawer(typeof(InspectorNameAttribute))]
public class InspectorNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        InspectorNameAttribute inspectorNameAttribute = (InspectorNameAttribute)attribute;
        label.text = inspectorNameAttribute.name;
        EditorGUI.PropertyField(position, property, label);
    }
}
