using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

[Serializable]
/// <summary>
/// 인스펙터에 그리드로 2차원 Enum(Flag) 배열을 표시하기 위한 클래스입니다.<br/>
/// (0, 0)셀부터 지정 크기만큼 정사각형 셀로 그리드 격자를 그립니다.<br/>
/// 인접 셀의 정보도 
/// </summary>
public class BuildableGrid : MonoBehaviour {
    #if UNITY_EDITOR
    [Tooltip("건물의 크기를 할당합니다. 그리드는 건물 인접 셀까지 포함합니다.")]
    #endif
    public Vector2Int range = new Vector2Int(1, 1);
    #if UNITY_EDITOR
    [ReadOnly, Tooltip("그리드가 저장된 Json 문자열입니다. BuildableGrid에서 그리드를 저장할 수 있습니다.")]
    #endif
    public string gridJson;

    Buildable[,] _grid;
    /// <summary>그리드 작성 및 저장에 사용되는 배열입니다.<br/>플래그 데이터를 상대적 위치로 접근하려는 경우 <see cref="this"/>를 사용하십시오.</summary>
    internal Buildable[,] grid {
        get {
            if (_grid is null && gridJson.Length != 0) _grid = JsonConvert.DeserializeObject<Buildable[,]>(gridJson);
            return _grid;
        }
        set {
            _grid = value;
        }
    }

    /// <summary>실제 상대적 셀 위치의 건설 가능성 데이터를 반환합니다. [-1, -1]에서 <see cref="range"/>까지 지원합니다.</summary>
    public Buildable this[int x, int y] {
        get => grid[x+1, y+1];
    }
    /// <summary>격자 데이터를 저장합니다.</summary>
    internal void SaveGrid() { gridJson = JsonConvert.SerializeObject(_grid); }
}




#if UNITY_EDITOR

/// https://stackoverflow.com/questions/49353971/how-to-create-multidimensional-array-in-unity-inspector 코드 사용
[CustomEditor(typeof(BuildableGrid))]
public class GridDictionaryEditor : Editor {
    private Vector2Int currRange = new Vector2Int(1, 1);
    private bool showJsonData = false;
    public override void OnInspectorGUI() {
        BuildableGrid buildable = (BuildableGrid)target;
        // 가급적 프리팹 편집 모드에서만 동작하도록...
        if (!Application.isPlaying) EditorUtility.SetDirty(buildable);
        else EditorUtility.ClearDirty(buildable);
        buildable.range = EditorGUILayout.Vector2IntField("Range", buildable.range);

        // Json Data
        showJsonData = EditorGUILayout.Foldout(showJsonData, "Json Data");
        if (showJsonData) {
            GUI.enabled = false;
            buildable.gridJson = EditorGUILayout.TextArea(buildable.gridJson);
            GUI.enabled = true;
        }
        
        // Resize and initialize grid
        if (GUILayout.Button("Resize Grid")) {
            currRange = buildable.range;
            buildable.grid = new Buildable[buildable.range.x + 2, buildable.range.y + 2];
        }

        // Save grid
        if (GUILayout.Button("Save Grid")) buildable.SaveGrid();

        // Draw grid
        else if (buildable.grid is null) buildable.grid = new Buildable[3, 3];
        EditorGUILayout.Space();

        /*GUIStyle tableStyle = new GUIStyle("box");
        tableStyle.padding = new RectOffset(0, 10, 10, 10);
        tableStyle.margin.left = 32;*/

        GUIStyle headerColumnStyle = new GUIStyle();
        headerColumnStyle.fixedWidth = 35;

        GUIStyle columnStyle = new GUIStyle();
        columnStyle.fixedWidth = 35;

        GUIStyle rowStyle = new GUIStyle();
        rowStyle.fixedHeight = 25;

        GUIStyle rowHeaderStyle = new GUIStyle();
        rowHeaderStyle.fixedWidth = columnStyle.fixedWidth - 1;

        GUIStyle columnHeaderStyle = new GUIStyle();
        columnHeaderStyle.fixedWidth = 30;
        columnHeaderStyle.fixedHeight = 25.5f;

        GUIStyle columnLabelStyle = new GUIStyle();
        columnLabelStyle.fixedWidth = rowHeaderStyle.fixedWidth - 6;
        columnLabelStyle.alignment = TextAnchor.MiddleCenter;
        columnLabelStyle.fontStyle = FontStyle.Bold;

        GUIStyle cornerLabelStyle = new GUIStyle();
        cornerLabelStyle.fixedWidth = 42;
        cornerLabelStyle.alignment = TextAnchor.MiddleRight;
        cornerLabelStyle.fontStyle = FontStyle.BoldAndItalic;
        cornerLabelStyle.fontSize = 14;
        cornerLabelStyle.padding.top = -5;

        GUIStyle rowLabelStyle = new GUIStyle();
        rowLabelStyle.fixedWidth = 25;
        rowLabelStyle.alignment = TextAnchor.MiddleRight;
        rowLabelStyle.fontStyle = FontStyle.Bold;

        GUIStyle enumStyle = new GUIStyle("popup");
        rowStyle.fixedWidth = 35;

        EditorGUILayout.BeginHorizontal();
        for(int x = -1; x < currRange.x+2; x++) {
            EditorGUILayout.BeginVertical((x == -1) ? headerColumnStyle : columnStyle);
            for(int y = currRange.y+1; y > -2; y--) {
                if(x == -1 && y == -1) {
                    EditorGUILayout.BeginVertical(rowHeaderStyle);
                    EditorGUILayout.LabelField("[X,Y]", cornerLabelStyle);
                    EditorGUILayout.EndHorizontal();
                } else if (x == -1) {
                    EditorGUILayout.BeginVertical(columnHeaderStyle);
                    EditorGUILayout.LabelField($"{y-1}", rowLabelStyle);
                    EditorGUILayout.EndHorizontal();
                } else if (y == -1) {
                    EditorGUILayout.BeginVertical(rowHeaderStyle);
                    EditorGUILayout.LabelField($"{x-1}", columnLabelStyle);
                    EditorGUILayout.EndHorizontal();
                } else {
                    EditorGUILayout.BeginHorizontal(rowStyle);
                    buildable.grid[x, y] = (Buildable)EditorGUILayout.EnumFlagsField(buildable.grid[x, y], enumStyle, GUILayout.Width(35));
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif