using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

using Assets.Maps;
using Assets.Util;
using Rair.Field.Values;

[CustomEditor(typeof(TerrainGenerator))]
public class TGEditor : Editor {
  GUIStyle bold;
  protected void OnEnable() {
    bold = new() {
      fontStyle = FontStyle.Bold,
      normal = new() { textColor = Color.white }
    };
  }
  public override void OnInspectorGUI() {
    var inst = (TerrainGenerator)target;

    EditorGUILayout.LabelField("Properties", bold);
      EditorGUI.indentLevel++;
      base.OnInspectorGUI();
      EditorGUI.indentLevel--;
    EditorGUILayout.LabelField("Status", bold);
      EditorGUI.indentLevel++;
      bool loaded = inst.MapData != null;
      EditorGUILayout.LabelField("Map Data : " + (loaded ? $"{inst.MapData.Width} x {inst.MapData.Height}" : "Not Loaded"));
      if (inst.IsRunning)
        EditorGUI.ProgressBar(Indented, inst.Timer.CurrentRatio, $"{inst.Timer}");
      GUI.enabled = loaded && !inst.IsRunning;
      if (GUI.Button(Indented, "Apply Terrain Texture")) {
        EditorCoroutineUtility.StartCoroutine(inst.GenerateTerrain(), this);
        //TODO : 코루틴 진행 상황 표시
      }
      EditorGUI.indentLevel--;
    GUI.enabled = true;
  }
  Rect Indented => EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
}

public class TerrainGenerator : MonoBehaviour, IProgressTimerProvider
{
  public Texture2D Map, HeightMoistureMap;
  public Terrain MapTerrain, BorderTerrain;
  TerrainData TMap => MapTerrain.terrainData;
  TerrainData BMap => BorderTerrain.terrainData;

  [Range(0, 150)] public int height;

  public ProgressTimer Timer { get; private set; } = new(
    "Terrain",
    ("Setting Heights"   ,    0,  true),
    ("Generating Grids"  , .10f, false),
    ("Generating Props"  , .25f,  true)/*,
    ("Getting Biome Data", .50f, false),
    ("Setting Alpha Maps", .75f, false)*/
  );
  
  public void Reset() { Timer.Reset(); }
  public bool IsRunning { get; private set; }
  public Map MapData;
  private OccupyGrid grid;
  public IEnumerator GenerateTerrain() {
    //TODO 메서드 분리
    IsRunning = true;
    if (MapData is null) yield break;
    // 지형의 크기를 지정합니다.
    TMap.size = new Vector3(Map.width, height, Map.height);
    TMap.heightmapResolution = HeightMoistureMap.width;
    BMap.heightmapResolution = HeightMoistureMap.width;

    // 지형의 높이를 지정합니다.
    float[,] heights1 = new float[HeightMoistureMap.height, HeightMoistureMap.width];
    int total = HeightMoistureMap.height * HeightMoistureMap.width * 2;
    for (int x = 0; x < HeightMoistureMap.width; x++) {
      for (int y = 0; y < HeightMoistureMap.height; y++) {
        heights1[y, x] = HeightMoistureMap.GetPixel(x, y).grayscale;
        if (Timer.Elapsed) { Timer.SetDetail(x * HeightMoistureMap.height + y, total); yield return null; }
      }
    }
    TMap.SetHeights(0, 0, heights1);
    // 맵 경계를 지정합니다.
    float[,] heights2 = new float[HeightMoistureMap.height, HeightMoistureMap.width];
    for (int x = 0; x < HeightMoistureMap.width; x++) {
      for (int y = 0; y < HeightMoistureMap.height; y++) {
        heights2[y, x] = Mathf.Ceil(HeightMoistureMap.GetPixel(x, y).r); // 0 -> 0, 0+ -> 1
        if (Timer.Elapsed) { Timer.SetDetail(x * HeightMoistureMap.height + y + total/2, total); yield return null; }
      }
    }
    BMap.SetHeights(0, 0, heights2);
    BMap.size = new Vector3(Map.width, 99.99f, Map.height);

    Timer.Next();
    int scale = 4;
    grid = new(
      Map.width,
      HeightMoistureMap.GetPixels32().Select(p => (Occupy)p.b),
      scale
    );
    grid.SetWorldPivot(MapTerrain);

    yield return GenerateProps();
    // yield return SetAlphaMaps(MapTerrain, mapData);
    Timer.Next();
    IsRunning = false;
  }
  IEnumerator GenerateProps() {
    Timer.Next();
    yield break;
  }

  //* Basic Methods from : https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splatmapping/
  /*IEnumerator SetAlphaMaps(TerrainData data, Map map) {
    // Get a reference to the terrain data
    int width = data.alphamapWidth, height = data.alphamapHeight;
    var biomes = new Biome[width, height];
    float sX = map.Width/width, sY = map.Height/height;

    timer.Next();
    int total = width * height;
    //TODO 아무래도 바이옴 다시 짜는 건 미친 짓이야. Polygon 만들 때 터레인 해상도로 같이 알파맵+높이맵 만들어야겠어...
    //! CornerMap / CenterMap도 다시 구축하는 과정에서 뭔가 문제가 생긴 걸지도 몰라...
    for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) {
      biomes[x, y] = map.Graph.GetNearestCenter(new Vector2(x * sX, y * sY)).biome;
      if (timer.Elapsed) { timer.SetDetail(x * height + y, total); yield return null; }
    }
    // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
    timer.Next();
    float[,,] splatmapData = new float[width, height, data.alphamapLayers];
    
    for (int y = 0; y < data.alphamapHeight; y++) {
      for (int x = 0; x < data.alphamapWidth; x++) {        
        float[] cellData = biomes[y, x] switch { //TODO Fill Weights properly (sum=1)
          //? Note this :                   Map  Ocean Grass Sand  Rock  Snow  Muddy Dark
          Biome.Ocean     => new float[] { .00f, .50f, .00f, .00f, .00f, .00f, .00f, .50f },
          Biome.Marsh     => new float[] { .00f, .20f, .80f, .00f, .00f, .00f, .00f, .00f },
          Biome.Lake      => new float[] { .01f, .99f, .00f, .00f, .00f, .00f, .00f, .00f },
          Biome.Beach     => new float[] { .00f, .20f, .20f, .60f, .00f, .00f, .00f, .00f },
          Biome.Snow      => new float[] { .00f, .01f, .00f, .00f, .00f, .99f, .00f, .00f },
          Biome.Tundra    => new float[] { .00f, .08f, .59f, .00f, .00f, .43f, .00f, .00f },
          Biome.Bare      => new float[] { .00f, .00f, .10f, .10f, .80f, .00f, .00f, .00f },
          Biome.Scorched  => new float[] { .00f, .00f, .30f, .20f, .00f, .00f, .00f, .50f },
          Biome.Taiga     => new float[] { .00f, .01f, .50f, .00f, .00f, .50f, .00f, .00f },
          Biome.Shrubland => new float[] { .00f, .00f, .50f, .50f, .00f, .00f, .00f, .00f },
          Biome.TemperatD => new float[] { .00f, .00f, .30f, .70f, .00f, .00f, .00f, .00f },
          Biome.TempRainF => new float[] { .00f, .20f, .70f, .05f, .05f, .00f, .00f, .00f },
          Biome.TempDeciF => new float[] { .10f, .10f, .60f, .10f, .10f, .00f, .00f, .00f },
          Biome.Grassland => new float[] { .01f, .00f, .99f, .00f, .00f, .00f, .00f, .00f },
          Biome.TropRainF => new float[] { .00f, .30f, .60f, .00f, .10f, .00f, .00f, .00f },
          Biome.TropSeasF => new float[] { .00f, .30f, .40f, .20f, .10f, .00f, .00f, .00f },
          Biome.SubTropiD => new float[] { .00f, .05f, .05f, .90f, .00f, .00f, .00f, .00f },
          _               => new float[] { .00f, .00f, .00f, .00f, .00f, .00f, .00f, .00f }
        };
        for (int i = 0; i < 8; i++) splatmapData[x, y, i] = cellData[i]; // Write to array
        if (timer.Elapsed) { timer.SetDetail(y * data.alphamapHeight + x, total); yield return null; }
      }
    }
    data.SetAlphamaps(0, 0, splatmapData);
  }*/
}