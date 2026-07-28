using UnityEngine;

/// <summary>대상 배열의 요소 이름을 요소 내 열거형과 일치시킵니다.</summary>
public class EnumAsElementNameAttribute : PropertyAttribute
{
  public string enumName;
  private EnumAsElementNameAttribute() {}
  public EnumAsElementNameAttribute(string enumName) {
    this.enumName = enumName;
  }
}
