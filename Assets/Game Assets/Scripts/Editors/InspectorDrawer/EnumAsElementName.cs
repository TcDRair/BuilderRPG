using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
/// <summary>대상 배열의 요소 이름을 요소 내 열거형과 일치시킵니다.</summary>
public class EnumAsElementNameAttribute : PropertyAttribute
{
  public string enumName;
  private EnumAsElementNameAttribute() {}
  public EnumAsElementNameAttribute(string enumName) {
    this.enumName = enumName;
  }
}

[CustomPropertyDrawer(typeof(EnumAsElementNameAttribute))]
public class EnumAsElementNameDrawer : PropertyDrawer
{
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    var attr = attribute as EnumAsElementNameAttribute;
    var name = property.FindPropertyRelative(attr.enumName);
    if (name != null) label.text = name.enumDisplayNames[name.enumValueIndex];
    
    EditorGUI.PropertyField(position, property, label, true);
  }

  public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    => EditorGUI.GetPropertyHeight(property, label, true);
}

#endif