// using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
/// <summary>인스펙터상에서 보이는 변수명을 주어진 문자열로 변경합니다.</summary>
public class InspectorNameAttribute : PropertyAttribute
{
    public string name;
    private InspectorNameAttribute() {}
    public InspectorNameAttribute(string name) {
        this.name = name;
    }
}

[CustomPropertyDrawer(typeof(InspectorNameAttribute))]
public class InspectorNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        InspectorNameAttribute inspectorNameAttribute = (InspectorNameAttribute)attribute;
        label.text = inspectorNameAttribute.name;
        EditorGUI.PropertyField(position, property, label);
    }
}
#endif