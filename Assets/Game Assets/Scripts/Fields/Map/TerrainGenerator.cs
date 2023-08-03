using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

using Assets.Maps;
using Assets.Util;

#if UNITY_EDITOR
namespace Rair.Field.Maps {
[System.Serializable]
public struct TerrainVar {
  public Transform propParent;
  public TerrainPropData data;
  public SmoothVar smoothenBorder, smoothenBiomes;

  [System.Serializable]
  public struct SmoothVar {
    public bool active;
    [Range(0, .2f)] public float randomize;
    [Range(1, 5)] public int range;
    [Range(1, 3)] public int iterations;
  }
} 

[System.Serializable]
public class TerrainGenerator : IProgressTimerProvider
{
  #region Inspector
  public RandomTextureGenerator Generator;
  TerrainVar vars;
  #endregion

  #region Properties
  bool run;
  public bool IsRunning() => run;
  public readonly int gridScale = 4, mapScale = 4;
  private int MapSize => MapData.width * mapScale;
  private Values.OccupyGrid grid; //TODO 데이터 저장 필요
  private Texture2D MapData => Generator.MapData;
  private TerrainData MapTData => Generator.mapVariables.MapTerrain.terrainData;
  #endregion

  public const int TOTAL_HEIGHT = 10;

  public TerrainGenerator(RandomTextureGenerator Generator) {
    this.Generator = Generator;
    vars = Generator.terrainVariables;
    run = false;
  }

  public ProgressTimer Timer { get; private set; } = new(
    "Terrain",
    ("Setting Heights"   ,    0,  true),
    ("Smoothing Borders" , .05f,  true),
    ("Generating Grids"  , .60f, false),
    ("Generating Props"  , .62f,  true),
    ("Getting Biome Data", .66f,  true),
    ("Setting Alpha Maps", .68f,  true),
    ("Randomizing Biomes", .70f, false),
    ("Smoothing Biomes"  , .75f,  true)
  );

  public IEnumerator GenerateTerrain() {
    run = true;
    Timer.Reset();
    vars.propParent.RemoveAllChildren();
    yield return SetHeights();
    yield return GenerateProps();
    yield return SetSplatMaps();
    Timer.Next();
    run = false;
  }

  IEnumerator SetHeights() {
    // 지형의 크기를 지정합니다.
    MapTData.heightmapResolution = MapSize;
    MapTData.size = new(MapData.width, TOTAL_HEIGHT, MapData.height);
    
    // 맵 경계를 지정합니다.
    float[,] heights = new float[MapSize, MapSize];
    for (int x = 0; x < MapData.width; x++) for (int y = 0; y < MapData.height; y++) {
      var height = /*Mathf.Min(*/MapData.GetPixel(x, y).r/*, 0.5f)*/;
      for (int j = 0; j < mapScale; j++) for (int k = 0; k < mapScale; k++)
        heights[y * mapScale + j, x * mapScale + k] = height;
      if (Timer.Elapsed) {
        Timer.SetDetail(x * MapData.height + y, heights.Length);
        yield return null;
      }
    }
    
    Timer.Next();
    if (vars.smoothenBorder.active) {
      int n = vars.smoothenBorder.iterations, r = vars.smoothenBorder.range;
      RectInt bound = new(0, 0, MapSize, MapSize);
      for (int c = 0; c < n; c++) for (int i = 0; i < MapSize; i++) for (int j = 0; j < MapSize; j++) {
        float sum = 0, count = 0;
        RectInt rect = new(i - r, j - r, 2 * r + 1, 2 * r + 1);
        foreach (var p in rect.allPositionsWithin) if (bound.Contains(p)) {
          sum += heights[p.x, p.y];
          count++;
        }
        heights[i, j] = sum / count;
        if (Timer.Elapsed) {
          Timer.SetDetail(i * MapSize + j + c * MapSize * MapSize, heights.Length * n);
          yield return null;
        }
      }
    }

    for (int k=0;k<MapSize;k++) for (int l=0;l<MapSize;l++) heights[k,l] = Mathf.Min(heights[k,l], 0.5f);

    MapTData.SetHeights(0, 0, heights);
  }

  IEnumerator GenerateProps() {
    Timer.Next();
    grid = new(
      Generator.Map.width,
      Generator.MapData.GetPixels32().Select(p => (Values.Occupy)p.a),
      gridScale
    );
    grid.SetWorldPivot(Generator.mapVariables.MapTerrain);

    Timer.Next();
    for (int i = 0; i < grid.size; i++) { for (int j = 0; j < grid.size; j++) {
      var biome = (Biome)Generator.MapData.GetPixel(i * gridScale, j* gridScale).b;
      foreach (var prop in vars.data.props) foreach (var condition in prop.conditions) {
        if (condition.biome == biome) {
          if (Random.value > condition.density) continue;
          var pos = grid.GetWorldPos(new(i, j), .5f, MapTData.GetHeight(i, j));
          var rotation = Quaternion.Euler(0, Random.value * 360, 0);
          var obj = Object.Instantiate(prop.prefabs.Random(), pos, rotation, vars.propParent);
          obj.transform.localScale *= Random.Range(1 - condition.scale, 1 + condition.scale) * gridScale;
          break;
        }
      }
      if (Timer.Elapsed) { Timer.SetDetail(i * grid.size + j, grid.size * grid.size); yield return null; }
    }}
  }

  //* Basic Methods from : https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splamapTerrainping/
  IEnumerator SetSplatMaps() {
    // Get a reference to the terrain data
    int width = MapData.width;
    MapTData.alphamapResolution = width;
    var biomes = new BiomeEnum[width, width];
    RectInt bound = new(0, 0, width, width);

    Timer.Next();
    int total = width * width;
    for (int x = 0; x < width; x++) for (int y = 0; y < width; y++) {
      biomes[x, y] = (Biome)MapData.GetPixel(x, y).b;
      if (Timer.Elapsed) { Timer.SetDetail(x * width + y, total); yield return null; }
    }
    // SplamapTerrain data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splamapTerrain data:
    Timer.Next();
    var splatmap = new float[width, width][];
    
    for (int y = 0; y < width; y++) for (int x = 0; x < width; x++) {        
      float[] cellData = biomes[y, x] switch { //TODO Fill Weights properly (sum=1)
        //? Note this :                                   Grass  Dirt  Sand  Snow  Lush Bleak  Dark  Water
        BiomeEnum.Ice                     => new float[] {    0,    0,    0, .70f,    0,    0,    0, .30f },
        BiomeEnum.Lake                    => new float[] {    0,    0,    0,    0,    0,    0,    0,    1 },
        BiomeEnum.Bare                    => new float[] { .05f, .75f,    0,    0,    0, .10f, .10f,    0 },
        BiomeEnum.Snow                    => new float[] {    0,    0,    0,    1,    0,    0,    0,    0 },
        BiomeEnum.Ocean                   => new float[] {    0,    0,    0,    0,    0,    0, .20f, .80f },
        BiomeEnum.Beach                   => new float[] {    0, .15f, .70f,    0,    0,    0, .15f,    0 },
        BiomeEnum.Marsh                   => new float[] {    0, .60f,    0,    0,    0, .10f, .30f,    0 },
        BiomeEnum.Taiga                   => new float[] { .25f, .25f,    0, .40f, .10f,    0,    0,    0 },
        BiomeEnum.Tundra                  => new float[] {    0, .45f,    0, .45f, .10f,    0,    0,    0 },
        BiomeEnum.Scorched                => new float[] {    0, .40f, .10f,    0,    0, .50f,    0,    0 },
        BiomeEnum.Grassland               => new float[] {    1,    0,    0,    0,    0,    0,    0,    0 },
        BiomeEnum.Shrubland               => new float[] { .40f, .10f,    0,    0, .50f,    0,    0,    0 },
        BiomeEnum.TemperateDesert         => new float[] {    0,    0, .90f,    0,    0, .10f,    0,    0 },
        BiomeEnum.SubtropicalDesert       => new float[] {    0, .20f, .70f,    0,    0, .10f,    0,    0 },
        BiomeEnum.TropicalRainyForest     => new float[] {    0, .30f, .60f,    0, .10f,    0,    0,    0 },
        BiomeEnum.TemperateRainyForest    => new float[] {    0, .50f,    0,    0, .50f,    0,    0,    0 },
        BiomeEnum.TropicalSeasonForest    => new float[] { .30f, .20f, .10f,    0, .40f,    0,    0,    0 },
        BiomeEnum.TemperateDecidousForest => new float[] { .40f,    0,    0,    0, .60f,    0,    0,    0 },
        _                                 => new float[] {    0,    0,    0,    0,    0,    0,    0,    0 }
      };
      splatmap[x, y] = cellData;
      if (Timer.Elapsed) { Timer.SetDetail(y * MapTData.alphamapHeight + x, total); yield return null; }
    }

    Timer.Next();
    if (vars.smoothenBiomes.randomize > 0) {
      float q = vars.smoothenBiomes.randomize;
      int r = 2;
      for (int i=r; i < width-r; i++) for (int j=r; j < width-r; j++) {
        Vector2Int p = new(Random.Range(-r, r), Random.Range(-r, r));
        if (q > Random.value) 
          (splatmap[i, j], splatmap[i + p.x, j + p.y]) = (splatmap[i + p.x, j + p.y], splatmap[i, j]);
        if (Timer.Elapsed) yield return null;
      }
    }
    Timer.Next();
    if (vars.smoothenBiomes.active) {
      int n = vars.smoothenBiomes.iterations, r = vars.smoothenBiomes.range;
      var result = new float[width, width][];
      for (int i=0; i < n; i++) {
        for (int j=0; j < width; j++) for (int k=0; k < width; k++) {
          var sum = new float[8];
          int count = 0;
          RectInt rect = new(j - r, k - r, 2 * r + 1, 2 * r + 1);
          foreach (var p in rect.allPositionsWithin) if (bound.Contains(p)) {
            for (int l = 0; l < 8; l++) sum[l] += splatmap[p.x, p.y][l];
            count++;
          }
          for (int l = 0; l < 8; l++) sum[l] /= count;
          result[j, k] = sum;
          if (Timer.Elapsed) { Timer.SetDetail(i * width * width + j * width + k, n * width * width); yield return null; }
        }
        splatmap = result;
      }
    }
    var splatmapData = new float[width, width, 8];
    for (int y = 0; y < width; y++) for (int x = 0; x < width; x++) for (int i = 0; i < 8; i++)
      splatmapData[x, y, i] = splatmap[x, y][i];
    MapTData.SetAlphamaps(0, 0, splatmapData);
  }
}
}
#endif