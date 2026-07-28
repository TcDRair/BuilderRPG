using UnityEngine;
using UnityEditor;

//? 어트리뷰트 정의는 Scripts/Attributes/ReadOnlyAttribute.cs (런타임 어셈블리)에 있습니다.
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer {
  public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
    return EditorGUI.GetPropertyHeight(property, label, true);
  }

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    GUI.enabled = false;
    EditorGUI.PropertyField(position, property, label);
    GUI.enabled = true;
  }
}
