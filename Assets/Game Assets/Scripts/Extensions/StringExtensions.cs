using System.Collections;

using UnityEngine;

public static class StringExtensions
{
	/// <summary>
	/// 주어진 양수 시간을 단위를 포함한 짧은 길이의 문자열로 변환합니다.<br/>
	/// 시간은 초 단위로 간주하고 밀리초 이하나 연 이상의 단위는 고려하지 않습니다.
	/// </summary>
	/// <example>
	/// <code>
	/// string str = 9.2f.TimeToString(); // str = "9.2초"
	/// string str = 10.5f.TimeToString(); // str = "10초"
	/// string str = 61.0f.TimeToString(); // str = "1분"
	/// //* 시간, 일 단위로도 같은 매커니즘을 적용합니다.
	/// </code>
	/// </example>
	public static string ToTimeString(this float time)
	{
		if (time < 0) return ""; // 음수는 정상적인 시간으로 판단하지 않으므로, 빈 문자열을 반환합니다.
		if (time < 10) return time.ToString("F1") + "초";
		if (time < 60) return time.ToString("F0") + "초";
		if (time < 3600) return (time / 60f).ToString("F0") + "분";
		if (time < 86400) return (time / 3600f).ToString("F0") + "시간";
		return (time / 86400f).ToString("F0") + "일";
	}
	//? ToNiceString(this string)은 UnityEditor.ObjectNames.NicifyVariableName 래퍼였으나
	//? 호출부가 없어 제거했습니다. 인스펙터 라벨 정규화가 필요하면 Rair.Editor 쪽에 두십시오.
	public static string ToNiceString(this Vector2Int vec) => $"({vec.x}/{vec.y})";
	public static string ToColonNotation(this float time)
		=> $"{(int)(time/3600):D1}:{(int)(time/60)%60:D2}:{(int)time%60:D2}";
}

public static class RichTextExtensions
{
  /// <summary>
  /// 텍스트를 강조하여 설명이 제공됨을 나타냅니다.<br/>
  /// 강조 색상은 <see cref="MainSetting"/>에서 설정할 수 있습니다.
  /// </summary>
  public static string Highlight(this string str) => $"<color={MainSetting.TextColor_Interested}><b><u>{str}</u></b></color>";

  /// <summary>
  /// 텍스트를 어둡게 표시하여 비활성화/무시 상태임을 나타냅니다.<br/>
  /// 무시 색상은 <see cref="MainSetting"/>에서 설정할 수 있습니다.
  /// </summary>
  public static string Ignore(this string str) => $"<color={MainSetting.TextColor_Ignored}>{str}</color>";
  public static string Ignore(this string str, bool ignore) => ignore ? str.Ignore() : str;
  public static string Italic(this string str) => $"<i>{str}</i>";
  public static string Flavor(this string str) => $"\"{str}\"".Ignore().Italic();

  public static string Color(this string str, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";
  public static string Color(this string str, Color color, bool condition) => condition ? str.Color(color) : str;
  public static string ColorBox(this string str, Color color) {
    color.a = Mathf.Min(.25f, color.a);
    return $"<mark=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</mark>";
  }
}