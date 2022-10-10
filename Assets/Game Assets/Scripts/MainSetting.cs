using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MainSetting
{
  public static int buildLayer = LayerMask.NameToLayer("Building");
  public static int buildMask = 1 << buildLayer;
  public static int mapLayer = LayerMask.NameToLayer("Map");
  public static int mapMask = 1 << mapLayer;
  public static int floorLayer = LayerMask.NameToLayer("Map Floor");
  public static int floorMask = 1 << floorLayer;

  public static int interactableMask = buildMask; //TODO | natureMask | creatureMask;

  #region Map Color
  public static readonly Color32 floorColor = new(0xFF, 0xFF, 0xFF, 0xFF),
    ceilingColor   = new(0x80, 0x80, 0x80, 0xFF),
    wallNColor   = new(0xFF, 0x00, 0x00, 0xFF),
    wallEColor   = new(0xFF, 0x00, 0x20, 0xFF),
    wallSColor   = new(0xFF, 0x00, 0x40, 0xFF),
    wallWColor   = new(0xFF, 0x00, 0x60, 0xFF),
    cornerNEColor  = new(0x00, 0xFF, 0x00, 0xFF),
    cornerSEColor  = new(0x00, 0xFF, 0x20, 0xFF),
    cornerSWColor  = new(0x00, 0xFF, 0x40, 0xFF),
    cornerNWColor  = new(0x00, 0xFF, 0x60, 0xFF),
    diagonalColor  = new(0xFF, 0x00, 0xFF, 0xFF),
    diagonalRColor = new(0xFF, 0x80, 0xFF, 0xFF),
    edgeNEColor  = new(0x00, 0x00, 0xFF, 0xFF),
    edgeSEColor  = new(0x00, 0x20, 0xFF, 0xFF),
    edgeSWColor  = new(0x00, 0x40, 0xFF, 0xFF),
    edgeNWColor  = new(0x00, 0x60, 0xFF, 0xFF),
    emptyColor   = new(0x00, 0x00, 0x00, 0x00),
    emptyColor2  = new(0x00, 0x00, 0x00, 0xFF)
  ;
  #endregion


}




public static class BuildableMethods {
  /// <summary>이 플래그가 해당 플래그와 하나라도 겹치는 것이 있는지 확인합니다.</summary>
  public static bool HasOneFlag(this Buildable buildable, Buildable flags) => (buildable & flags) != Buildable.None;
  /// <summary>이 플래그가 해당 플래그 모두를 갖고 있는지 확인합니다.</summary>
  [System.Obsolete("Use HasFlag instead.")]
  public static bool HasAllFlag(this Buildable buildable, Buildable flags) => buildable.HasFlag(flags);

  /// <summary><paramref name="buildable"/> 플래그가 <paramref name="flags"/>의 비트를 <paramref name="N"/>개 이상 갖고 있는지 확인합니다.</summary>
  public static bool HasNFlag(this Buildable buildable, Buildable flags, int N) {
    if (N == 0) return true;
    int b = (int)buildable, f = (int)flags;
    while (buildable > 0) {
      if ((f & 1) == 0) continue;
      else if ((b & 1) == 1 && --N == 0) return true;
      b >>= 1; f >>= 1;
    }
    return false;
  }
}

public static class TransformMethods {
  /// <summary>해당 트랜스폼의 모든 자식 오브젝트를 삭제합니다.</summary>
  public static void RemoveAllChildren(this Transform parent) {
    if (Application.isPlaying) for (int i=parent.childCount-1; i>=0; i--) {
      GameObject.Destroy(parent.GetChild(i).gameObject);
    }
    else for (int i=parent.childCount; i>0; i--) {
      GameObject.DestroyImmediate(parent.GetChild(0).gameObject);
    }
  }
}

public static class GameObjectMethods {
  /// <summary>게임오브젝트의 <see cref="UnityEngine.HideFlags"/>나 <see cref="GameObject.active"/>와 무관하게 디폴트 상태로 인스턴스화하여 반환합니다.</summary>
  public static GameObject InstantiateDefault(this GameObject prefab) {
    GameObject newObj = GameObject.Instantiate(prefab);
    newObj.hideFlags = HideFlags.None;
    newObj.SetActive(true);
    return newObj;
  }
  /// <summary>게임오브젝트의 <see cref="UnityEngine.HideFlags"/>나 <see cref="GameObject.active"/>와 무관하게 디폴트 상태로 인스턴스화하여 반환합니다.</summary>
  public static GameObject InstantiateDefault(this GameObject prefab, Transform parent) {
    GameObject newObj = GameObject.Instantiate(prefab, parent);
    newObj.hideFlags = HideFlags.None;
    newObj.SetActive(true);
    return newObj;
  }
  /// <summary>
  /// 게임오브젝트를 에디터나 씬에서 보이지 않는 상태로 인스턴스화하여 반환합니다.<br/>
  /// 해당 게임오브젝트는 저장되지 않으며 직접 조작할 수 없습니다.
  /// </summary>
  public static GameObject InstantiateInvisible(this GameObject prefab) {
    GameObject newObj = GameObject.Instantiate(prefab);
    newObj.hideFlags = HideFlags.HideAndDontSave;
    newObj.SetActive(false);
    return newObj;
  }
}

public static class Vector3QuaternionMethods {
  public static float GetHorizontalMagnitude(this Vector3 velocity) {
    velocity.y = 0;
    return velocity.magnitude;
  }

  public static float GetAngularSpeed(this Quaternion currentRotation, Quaternion previousRotation) {
    Vector3 currRV = currentRotation.eulerAngles;
    Vector3 prevRV = previousRotation.eulerAngles;
    return (currRV.y - prevRV.y)%360;
  }
}

public static class CanvasGroupMethods {
  public static void Enable(this CanvasGroup cg) {
    cg.alpha = 1f;
    cg.interactable = true;
    cg.blocksRaycasts = true;
  }
  public static void Disable(this CanvasGroup cg) {
    cg.alpha = 0f;
    cg.interactable = false;
    cg.blocksRaycasts = false;
  }

  public static void Toggle(this CanvasGroup cg) {
    if (cg.alpha == 0f) cg.Enable();
    else cg.Disable();
  }
}

public static class TimerStringExtension {
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
  public static string ToTimeString(this float time) {
    if (time < 0) return ""; // 음수는 정상적인 시간으로 판단하지 않으므로, 빈 문자열을 반환합니다.
    if (time < 10) return time.ToString("F1") + "초";
    if (time < 60) return time.ToString("F0") + "초";
    if (time < 3600) return (time/60f).ToString("F0") + "분";
    if (time < 86400) return (time/3600f).ToString("F0") + "시간";
    return (time/86400f).ToString("F0") + "일";
  }
}

public static class NiceStringMethods {
  public static string ToNiceString(this string str) {
    return UnityEditor.ObjectNames.NicifyVariableName(str);
  }
  public static string ToNiceString(this Vector2Int vec) {
    return $"({vec.x}/{vec.y})";
  }
}