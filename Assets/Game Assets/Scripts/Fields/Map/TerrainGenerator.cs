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
  [System.Serializable] public struct Prop {
    public string name;
    public GameObject prefab;
    [System.Serializable] public struct Condition {
      public Biome biome;
      [Range(0, .5f)] public float scale;
      [Range(0, .5f)] public float density;
    }
    public Condition[] conditions;
  }
  public Transform propParent;
  public Prop[] props;
} 

[System.Serializable]
public class TerrainGenerator
{
  #region Inspector
  public RandomTextureGenerator Generator;
  TerrainVar vars;
  #endregion

  #region Properties
  bool run;
  public bool IsRunning() => run;
  public readonly int scale = 4;
  private Values.OccupyGrid grid;
  #endregion

  public const int MAP_HEIGHT = 5, BORDER_HEIGHT = 100;

  public TerrainGenerator(RandomTextureGenerator Generator) {
    this.Generator = Generator;
    vars = Generator.terrainVariables;
    run = false;
  }

  public readonly ProgressTimer Timer = new(
    "Terrain",
    ("Setting Heights"   ,    0,  true),
    ("Generating Grids"  , .10f, false),
    ("Generating Props"  , .25f,  true),
    ("Getting Biome Data", .50f,  true),
    ("Setting Alpha Maps", .75f,  true)
  );

  public IEnumerator GenerateTerrain() {
    run = true;
    vars.propParent.RemoveAllChildren();
    Random.InitState(new System.Random().Next());
    
    yield return SetHeights();

    yield return GenerateProps();

    yield return SetAlphaMaps(Generator.MapTerrain.terrainData, Generator.MapTexture);
    Timer.Next();
    run = false;
  }

  IEnumerator SetHeights() {
    var map = Generator.Map;
    var mapData = Generator.MapData;
    var mapTData = Generator.MapTerrain.terrainData;
    var mapBData = Generator.BorderTerrain.terrainData;
    // 지형의 크기를 지정합니다.
    mapTData.heightmapResolution = mapData.width;
    mapBData.heightmapResolution = mapData.width;
    mapTData.size = new(map.width,    MAP_HEIGHT, map.height);
    mapBData.size = new Vector3(map.width, BORDER_HEIGHT, map.height);
    
    // 맵 경계를 지정합니다.
    int total = mapData.height * mapData.width;
    float[,] heights1 = new float[mapData.height, mapData.width];
    float[,] heights2 = new float[mapData.height, mapData.width];
    for (int x = 0; x < mapData.width; x++) {
      for (int y = 0; y < mapData.height; y++) {
        var height = Mathf.Ceil(mapData.GetPixel(x, y).r); // 0 -> 0, 0+ -> 1
        heights1[y, x] = heights2[y, x] = height;
        if (Timer.Elapsed) { Timer.SetDetail(x * mapData.height + y, total); yield return null; }
      }
    }
    mapTData.SetHeights(0, 0, heights1);
    mapBData.SetHeights(0, 0, heights2);
  }

  IEnumerator GenerateProps() {
    Timer.Next();
    grid = new(
      Generator.Map.width,
      Generator.MapData.GetPixels32().Select(p => (Values.Occupy)p.a),
      scale
    );
    grid.SetWorldPivot(Generator.MapTerrain);

    Timer.Next();
    for (int i = 0; i < grid.size; i++) { for (int j = 0; j < grid.size; j++) {
      var biome = (Biome)Generator.MapData.GetPixel(i * scale, j* scale).b;
      foreach (var prop in vars.props) foreach (var cond in prop.conditions) {
        if (cond.biome == biome) {
          if (Random.value > cond.density) continue;
          var pos = grid.GetWorldPos(new(i, j), .5f, MAP_HEIGHT);
          var rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
          var obj = Object.Instantiate(prop.prefab, pos, rotation, vars.propParent);
          obj.transform.localScale *= Random.Range(1 - cond.scale, 1 + cond.scale) * scale;
          break;
        }
      }
      if (Timer.Elapsed) { Timer.SetDetail(i * grid.size + j, grid.size * grid.size); yield return null; }
    }}
  }

  //* Basic Methods from : https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splamapTerrainping/
  IEnumerator SetAlphaMaps(TerrainData data, MapTexture mapData) {
    // Get a reference to the terrain data
    int width = mapData.MapData.width;
    data.alphamapResolution = width;
    var biomes = new BiomeEnum[width, width];

    Timer.Next();
    int total = width * width;
    for (int x = 0; x < width; x++) for (int y = 0; y < width; y++) {
      biomes[x, y] = (Biome)mapData.MapData.GetPixel(x, y).b; //TODO ?
      if (Timer.Elapsed) { Timer.SetDetail(x * width + y, total); yield return null; }
    }
    // SplamapTerrain data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splamapTerrain data:
    Timer.Next();
    float[,,] splatmap = new float[width, width, data.alphamapLayers];
    
    for (int y = 0; y < width; y++) {
      for (int x = 0; x < width; x++) {        
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
        for (int i = 0; i < 8; i++) splatmap[x, y, i] = cellData[i]; // Write to array
        if (Timer.Elapsed) { Timer.SetDetail(y * data.alphamapHeight + x, total); yield return null; }
      }
    }
    data.SetAlphamaps(0, 0, splatmap);
  }
}
}
#endif