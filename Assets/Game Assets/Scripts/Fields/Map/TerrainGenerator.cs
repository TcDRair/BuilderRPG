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
public class TerrainGenerator
{
  #region Inspector
  //! 유니티 쓰발럼들은 제정신으로 지원하는 꼴을 못 보겠네 그냥 !\\
  public RandomTextureGenerator Generator;
  [System.Serializable] public struct TerrainVar {
    [Range(0, 150)] public int height;
    public Transform propParent;
    public GameObject grass;
  } TerrainVar vars;
  #endregion

  #region Properties
  bool run;
  public bool IsRunning() => run;
  public readonly int scale = 4;
  private Values.OccupyGrid grid;
  #endregion

  public TerrainGenerator(RandomTextureGenerator Generator) {
    this.Generator = Generator;
    vars = Generator.terrainVariables;
    run = false;
  }

  public readonly ProgressTimer Timer = new(
    "Terrain",
    ("Setting Heights"   ,    0,  true),
    ("Generating Grids"  , .10f, false),
    ("Generating Props"  , .25f,  true)/*,
    ("Getting Biome Data", .50f, false),
    ("Setting Alpha Maps", .75f, false)*/
  );

  public IEnumerator GenerateTerrain() {
    //TODO 메서드 분리
    run = true;
    vars.propParent.RemoveAllChildren();
    // 변수 할당 파트
    var map = Generator.Map;
    var mapData = Generator.MapData;
    var mapTData = Generator.MapTerrain.terrainData;
    var mapBData = Generator.BorderTerrain.terrainData;
    // 지형의 크기를 지정합니다.
    mapTData.heightmapResolution = mapData.width;
    mapBData.heightmapResolution = mapData.width;
    mapTData.size = new(map.width, vars.height, map.height);
    mapBData.size = new Vector3(map.width, 99.99f, map.height);

    // 지형의 높이를 지정합니다.
    float[,] heights1 = new float[mapData.height, mapData.width];
    int total = mapData.height * mapData.width * 2;
    for (int x = 0; x < mapData.width; x++) {
      for (int y = 0; y < mapData.height; y++) {
        heights1[y, x] = mapData.GetPixel(x, y).grayscale;
        if (Timer.Elapsed) { Timer.SetDetail(x * mapData.height + y, total); yield return null; }
      }
    }
    mapTData.SetHeights(0, 0, heights1);
    // 맵 경계를 지정합니다.
    float[,] heights2 = new float[mapData.height, mapData.width];
    for (int x = 0; x < mapData.width; x++) {
      for (int y = 0; y < mapData.height; y++) {
        heights2[y, x] = Mathf.Ceil(mapData.GetPixel(x, y).r); // 0 -> 0, 0+ -> 1
        if (Timer.Elapsed) { Timer.SetDetail(x * mapData.height + y + total/2, total); yield return null; }
      }
    }
    mapBData.SetHeights(0, 0, heights2);

    Timer.Next();
    grid = new(
      map.width,
      mapData.GetPixels32().Select(p => (Values.Occupy)p.a),
      scale
    );
    grid.SetWorldPivot(Generator.MapTerrain);

    yield return GenerateProps();
    // yield return SetAlphaMaps(MapTerrain, mapData);
    Timer.Next();
    run = false;
  }

  IEnumerator GenerateProps() {
    Timer.Next();
    for (int i = 0; i < grid.size; i++) { for (int j = 0; j < grid.size; j++) {
      var biome = (Biome)(Generator.MapData.GetPixel(i*scale, j*scale).b * BiomeProperties.Length2Pow);
      switch (biome) {
        case Biome.Grassland : {
          if (Random.value < .75f) break;
          var pos = grid.GetWorldPos(new(i, j));
          var rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
          var prop = Object.Instantiate(vars.grass, pos, rotation, vars.propParent);
          prop.transform.localScale = Vector3.one * Random.Range(.8f, 1.2f) * scale;
          break;
        }
      }
      if (Timer.Elapsed) { Timer.SetDetail(i * grid.size + j, grid.size * grid.size); yield return null; }
    }}
    yield break;
  }

  //* Basic Methods from : https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splamapTerrainping/
  /*IEnumerator SetAlphaMaps(TerrainData data, Map map) {
    // Get a reference to the terrain data
    int width = data.alphamapWidth, height = data.alphamapHeight;
    var biomes = new Biome[width, height];
    float sX = map.Width/width, sY = map.Height/height;

    timer.Next();
    int total = width * height;
    for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) {
      biomes[x, y] = //TODO ?
      if (timer.Elapsed) { timer.SetDetail(x * height + y, total); yield return null; }
    }
    // SplamapTerrain data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splamapTerrain data:
    timer.Next();
    float[,,] splamapTerrainData = new float[width, height, data.alphamapLayers];
    
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
        for (int i = 0; i < 8; i++) splamapTerrainData[x, y, i] = cellData[i]; // Write to array
        if (timer.Elapsed) { timer.SetDetail(y * data.alphamapHeight + x, total); yield return null; }
      }
    }
    data.SetAlphamaps(0, 0, splamapTerrainData);
  }*/
}
}
#endif