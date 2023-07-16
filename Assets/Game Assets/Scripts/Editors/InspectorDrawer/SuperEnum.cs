using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Assets.Maps;

/// <summary>
/// 암시적 캐스트 등의 편의를 위해 정의된 열거형 클래스입니다.
/// </summary>
public abstract class SuperEnum<T> where T : Enum {
  public T Value;

  public static readonly int Length2Pow = 2 << (int)Mathf.Log(Enum.GetValues(typeof(T)).Length, 2);

  public static implicit operator int(SuperEnum<T> e) => (int)(object)e.Value;
  public static implicit operator T(SuperEnum<T> e) => e.Value;
}

#if UNITY_EDITOR
//? 제네릭 클래스의 경우 각 타입의 Drawer를 만들어 주어야 합니다.
[CustomPropertyDrawer(typeof(Biome))]
public class SuperEnumDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    var enumProp = property.FindPropertyRelative("Value");
    EditorGUI.PropertyField(position, enumProp, label);
  }
}
#endif