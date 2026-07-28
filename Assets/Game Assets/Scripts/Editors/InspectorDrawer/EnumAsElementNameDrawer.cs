using UnityEngine;
using UnityEditor;

//? 어트리뷰트 정의는 Scripts/Attributes/EnumAsElementNameAttribute.cs (런타임 어셈블리)에 있습니다.
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
