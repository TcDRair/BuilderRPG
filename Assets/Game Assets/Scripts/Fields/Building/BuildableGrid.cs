using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;

[Serializable]
/// <summary>
/// 인스펙터에 그리드로 2차원 Enum(Flag) 배열을 표시하기 위한 클래스입니다.<br/>
/// (0, 0)셀부터 지정 크기만큼 정사각형 셀로 그리드 격자를 그립니다.
/// </summary>
public class BuildableGrid : MonoBehaviour {
  public string gridJson;
  public (int x, int y) Size => Data.size; // For convinience

  ((int x, int y) size, Buildable[] occupiedGrid, Buildable[] neededGrid) _data;
  public ((int x, int y) size, Buildable[] occupiedGrid, Buildable[] neededGrid) Data {
    get {
      if (_data.Equals(default)) _data = JsonConvert.DeserializeObject<((int, int), Buildable[], Buildable[])>(gridJson);
      return _data;
    }
    set => _data = value;
  }

  /// <summary>해당 위치의 건축물 정보를 나타냅니다.</summary>
  public Buildable this[int x, int y, bool showOccupiedData] {
    get => showOccupiedData ? Data.occupiedGrid[x + y * Data.size.x] : Data.neededGrid[x + y * Data.size.x];
    set {
      if (showOccupiedData) Data.occupiedGrid[x + y * Data.size.x] = value;
      else Data.neededGrid[x + y * Data.size.x] = value;
    }
  }
  public (Buildable occupied, Buildable needed) this[int x, int y] => (Data.occupiedGrid[x + y * Data.size.x], Data.neededGrid[x + y * Data.size.x]);

  public void LoadGrid() {
    if (gridJson is null || gridJson.Length == 0) _data = new() {
      size = new(2, 2),
      occupiedGrid = new Buildable[4],
      neededGrid = new Buildable[4]
    };
    else _data = JsonConvert.DeserializeObject<((int, int), Buildable[], Buildable[])>(gridJson);
  }
}

[Flags]
public enum Buildable : ushort {
  None = 0,
  //? 기본 구조
  /// <summary>바닥</summary>
  Floor  = 1 << 0,
  /// <summary>천장. <see cref="Wall"/> 플래그 중 하나라도 활성화되어야 합니다.</summary>
  Ceiling  = 1 << 1,
  /// <summary>벽 - 북쪽</summary>
  Wall_North  = 1 << 2,
  /// <summary>벽 - 동쪽</summary>
  Wall_East   = 1 << 3,
  /// <summary>벽 - 남쪽</summary>
  Wall_South  = 1 << 4,
  /// <summary>벽 - 서쪽</summary>
  Wall_West   = 1 << 5,
  //? 부착물 / 설치물 - 종속 비트
  /// <summary>부착물 - 북쪽 벽</summary>
  Attatch_Wall_North = 1 << 6,
  /// <summary>부착물 - 동쪽 벽</summary>
  Attatch_Wall_East  = 1 << 7,
  /// <summary>부착물 - 남쪽 벽</summary>
  Attatch_Wall_South = 1 << 8,
  /// <summary>부착물 - 서쪽 벽</summary>
  Attatch_Wall_West  = 1 << 9,
  /// <summary>설치물 - 바닥</summary>
  Attatch_Floor = 1 << 10,
  /// <summary>설치물 - 천장</summary>
  Attatch_Ceiling = 1 << 11,


  //? 합성 비트
  /// <summary>벽 - 모든 방향</summary>
  Wall = Wall_North | Wall_East | Wall_South | Wall_West,
  /// <summary>부착물이 달린 벽 - 북쪽</summary>
  Attatched_Wall_North = Attatch_Wall_North | Wall_North,
  /// <summary>부착물이 달린 벽 - 동쪽</summary>
  Attatched_Wall_East  = Attatch_Wall_East  | Wall_East,
  /// <summary>부착물이 달린 벽 - 남쪽</summary>
  Attatched_Wall_South = Attatch_Wall_South | Wall_South,
  /// <summary>부착물이 달린 벽 - 서쪽</summary>
  Attatched_Wall_West  = Attatch_Wall_West  | Wall_West,
  /// <summary>부착물이 달린 벽 - 모든 방향</summary>
  Attatched_Wall = Attatched_Wall_North | Attatched_Wall_East | Attatched_Wall_South | Attatched_Wall_West,

  //? 특수 목적 비트
  /// <summary>전체 차지</summary>
  Full = 65535,
  /// <summary>모든 인접 비트</summary>
  Adjacent = Wall_North | Wall_East | Wall_South | Wall_West,
}



#if UNITY_EDITOR
/// https://stackoverflow.com/questions/49353971/how-to-create-multidimensional-array-in-unity-inspector 코드 사용
[CustomEditor(typeof(BuildableGrid))]
public class GridDictionaryEditor : Editor {
  private (int x, int y) currentSize = new(1, 1);
  public override void OnInspectorGUI() {
    if (Application.isPlaying) EditorGUILayout.HelpBox("You can't modify grid during runtime", MessageType.Info);

    BuildableGrid buildable = (BuildableGrid)target;
    if (enumStyle is null) enumStyle = new("popup");

    // Json Data
    EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Json Data", GUILayout.Width(60));
      GUI.enabled = false;
      EditorGUILayout.TextArea(buildable.gridJson);
      GUI.enabled = true;
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Width", GUILayout.Width(50));
      currentSize.x = EditorGUILayout.IntSlider(currentSize.x, 1, 4);
      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Height", GUILayout.Width(50));
      currentSize.y = EditorGUILayout.IntSlider(currentSize.y, 1, 4);
      EditorGUILayout.Space(10);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space();
    
    // Resize and initialize grid
    if (GUILayout.Button("Resize Grid")) {
      buildable.Data = new() {
        size = currentSize,
        occupiedGrid = new Buildable[currentSize.x * currentSize.y],
        neededGrid = new Buildable[currentSize.x * currentSize.y]
      };
    }

    // Save grid
    if (GUILayout.Button("Save Grid")) {
      Undo.RecordObject(buildable, "Save Buildable Grid Data");
      buildable.gridJson = JsonConvert.SerializeObject(buildable.Data);
      var temp = buildable.Data;
      buildable.Data = new();
      buildable.Data = temp;
    }
    
    // Load grid (if inspector is opened but data are not loaded)
    else if (buildable.Data.Equals(default)) {
      buildable.LoadGrid();
      currentSize = buildable.Data.size;
    }

    EditorGUILayout.Space();

    //! 현재 문제점 : 인스펙터의 데이터를 프리팹 파일이 저장하고 있지 않습니다.

    DrawGridInspector(buildable.Data.size, "Occupying", (x, y) => {
      buildable[x, y, true] = (Buildable)EditorGUILayout.EnumFlagsField(buildable[x, y, true], enumStyle);
    });
    EditorGUILayout.Space();
    DrawGridInspector(buildable.Data.size, "Needed", (x, y) => {
      buildable[x, y, false] = (Buildable)EditorGUILayout.EnumFlagsField(buildable[x, y, false], enumStyle);
    });

    /*if (GUI.changed) {
      EditorUtility.SetDirty(buildable);
      EditorSceneManager.MarkSceneDirty(buildable.gameObject.scene);
    }*/
  }

  void DrawGridInspector((int x, int y) size, string gridName, Action<int, int> behavior) { //! Use in OnInspectorGUI
    EditorGUILayout.BeginHorizontal();
    for(int x = -1; x < size.x; x++) {
      EditorGUILayout.BeginVertical((x == -1) ? headerColumnStyle : columnStyle);
      for(int y = -1; y < size.y; y++) {
        if(x == -1 && y == -1) {
          EditorGUILayout.BeginVertical(rowHeaderStyle);
          EditorGUILayout.LabelField(gridName, cornerLabelStyle);
          EditorGUILayout.EndHorizontal();
        } else if (x == -1) {
          EditorGUILayout.BeginVertical(columnHeaderStyle);
          EditorGUILayout.LabelField($"{y+1}", rowLabelStyle);
          EditorGUILayout.EndHorizontal();
        } else if (y == -1) {
          EditorGUILayout.BeginVertical(rowHeaderStyle);
          EditorGUILayout.LabelField($"{x+1}", columnLabelStyle);
          EditorGUILayout.EndHorizontal();
        } else {
          EditorGUILayout.BeginHorizontal(rowStyle);
          EditorGUILayout.BeginVertical();
          behavior?.Invoke(x, y);
          EditorGUILayout.EndVertical();
          EditorGUILayout.EndHorizontal();
        }
      }
      EditorGUILayout.EndVertical();
    }
    EditorGUILayout.EndHorizontal();
  }
  static GUIStyle headerColumnStyle, columnStyle, rowStyle, rowHeaderStyle, columnHeaderStyle, columnLabelStyle, cornerLabelStyle, rowLabelStyle, enumStyle;

  /*tableStyle = new("box") {
    padding = new(0, 10, 10, 10),
    margin = new() { left = 32 }
  };*/

  public void OnEnable() {
    headerColumnStyle = new() { fixedWidth = 80 };
    columnStyle = new() { fixedWidth = 60 };
    rowStyle = new() { fixedWidth = 60, fixedHeight = 25 };
    rowHeaderStyle = new() { fixedWidth = columnStyle.fixedWidth - 1 };
    columnHeaderStyle = new() { fixedWidth = 60, fixedHeight = 26f };
    columnLabelStyle = new() {
      fixedWidth = rowHeaderStyle.fixedWidth - 6,
      alignment = TextAnchor.MiddleCenter,
      normal = new() { textColor = Color.white },
      fontStyle = FontStyle.Bold
    };
    cornerLabelStyle = new() {
      fixedWidth = 80,
      alignment = TextAnchor.MiddleCenter,
      normal = new() { textColor = Color.white },
      fontStyle = FontStyle.Bold,
      fontSize = 14,
      padding = new() { top = -5 },
    };
    rowLabelStyle = new() {
      fixedWidth = 80,
      alignment = TextAnchor.MiddleCenter,
      normal = new() { textColor = Color.white },
      fontStyle = FontStyle.Bold
    };
  }
}
#endif