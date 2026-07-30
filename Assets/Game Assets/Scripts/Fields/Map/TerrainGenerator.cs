using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Maps;
using Assets.Util;

//? 이 파일에는 에디터 의존이 없습니다. (문서 05 P3-4)
//? 한동안 Rair.Editor에 있었으나 UnityEditor/EditorCoroutine using이 잔재였을 뿐,
//? 실제로 쓰는 곳은 없어 런타임으로 되돌렸습니다.
namespace Rair.Field.Maps {

public class TerrainGenerator : IProgressTimerProvider
{
  #region Inspector
  public RandomTextureGenerator Generator;
  TerrainVar vars;
  #endregion

  #region Properties
  bool run;
  public bool IsRunning() => run;
  public readonly int gridScale = 4, mapScale = 1;
  private int MapSize => MapData.width * mapScale;
  private Values.OccupyGrid grid; //TODO 데이터 저장 필요
  private Texture2D MapData => Generator.MapData;
  public Terrain MapTerrain => Generator.mapVariables.MapTerrain;
  private TerrainData MapTData => Generator.mapVariables.MapTerrain.terrainData;
  #endregion

  //? 지형 높이는 TerrainVar.totalHeight(인스펙터)로 옮겼습니다.
  //? 상수 10이던 시절에는 512 폭 맵에서 육지 기복이 5유닛뿐이라 평면처럼 보였습니다.

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
    MapTData.size = new(MapData.width, vars.TotalHeight, MapData.height);

    //! 해수면을 월드 y=0에 맞춥니다. 고도가 [-1,1] -> [0,1]로 매핑되므로
    //! 지형의 절반 높이만큼 내려야 물 평면(y≈0)과 해안선이 일치합니다.
    //! 높이를 바꾸고 이 오프셋을 갱신하지 않으면 해저가 물 위로 드러나고,
    //! 프롭은 GetWorldPos가 쓰는 기준점(터레인 위치)을 따라가므로 공중에 뜹니다.
    var tr = MapTerrain.transform;
    tr.position = new Vector3(tr.position.x, -vars.TotalHeight / 2f, tr.position.z);
    
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
      //! 이웃 평균을 같은 배열에 바로 쓰면 이미 갱신된 이웃이 다시 읽혀
      //! 주사 방향(좌상 → 우하)으로 편향이 생깁니다. 버퍼를 교대합니다.
      var buffer = new float[MapSize, MapSize];
      for (int c = 0; c < n; c++) {
        for (int i = 0; i < MapSize; i++) for (int j = 0; j < MapSize; j++) {
          float sum = 0, count = 0;
          RectInt rect = new(i - r - 1, j - r - 1, 2 * r + 3, 2 * r + 3);
          foreach (var p in rect.allPositionsWithin) if (bound.Contains(p)) {
            sum += heights[p.x, p.y];
            count++;
          }
          buffer[i, j] = sum / count;
          if (Timer.Elapsed) {
            Timer.SetDetail(i * MapSize + j + c * MapSize * MapSize, heights.Length * n);
            yield return null;
          }
        }
        (heights, buffer) = (buffer, heights);
      }
    }

    MapTData.SetHeights(0, 0, heights);
    // for (int k=0;k<MapSize;k++) for (int l=0;l<MapSize;l++) heights[k,l] = Mathf.Min(heights[k,l], 0.5f);
  }
  
  IEnumerator GenerateProps() {
    Timer.Next();
    yield return null; // Wait for SetHeights to finish
    grid = new(
      MapTerrain,
      Generator.Map.width,
      Generator.MapData.GetPixels32().Select(p => (Values.Occupy)p.a),
      gridScale
    );

    Timer.Next();
    for (int i = 0; i < grid.Size; i++) { for (int j = 0; j < grid.Size; j++) {
      var biome = Generator.MapData.GetPixel(i * gridScale, j* gridScale).b.ToBiome();
      foreach (var prop in vars.data.props) foreach (var condition in prop.conditions) {
        if (condition.biome == biome) {
          if (Random.value > condition.density) continue;
          var pos = grid.GetWorldPos(new(i, j), .2f);
          pos += Vector3.up * MapTerrain.SampleHeight(pos);
          var rotation = Quaternion.Euler(0, Random.value * 360, 0);
          var obj = Object.Instantiate(prop.prefabs.Random(), pos, rotation, vars.propParent);
          obj.transform.localScale *= Random.Range(1 - condition.scale, 1 + condition.scale) * gridScale;
          break;
        }
      }
      if (Timer.Elapsed) { Timer.SetDetail(i * grid.Size + j, grid.Size * grid.Size); yield return null; }
    }}
  }

  /// <summary>바이옴 하나가 8개 터레인 레이어에 배분되는 가중치를 반환합니다.</summary>
  /// <remarks>
  /// <b>불변 조건 — 모든 행의 합은 1이어야 합니다.</b> (미지정 바이옴을 뜻하는 기본값은 예외)
  /// 합이 1이 아니면 해당 지점의 터레인 블렌딩이 어두워지거나 과포화됩니다.
  /// <br/>
  /// 이 표는 원래 <see cref="SetSplatMaps"/>의 이중 루프 안에 인라인으로 있었습니다.
  /// 검증할 방법이 없어 밖으로 꺼냈을 뿐, 값과 동작은 그대로입니다.
  /// </remarks>
  public static float[] BiomeWeights(Biome biome) => biome switch {
    //? Note this :                                   Grass  Dirt  Sand  Snow  Lush Bleak  Dark  Water
    Biome.Ice                     => new float[] {    0,    0,    0, .70f,    0,    0,    0, .30f },
    Biome.Lake                    => new float[] {    0,    0,    0,    0,    0,    0,    0,    1 },
    Biome.Bare                    => new float[] { .05f, .75f,    0,    0,    0, .10f, .10f,    0 },
    Biome.Snow                    => new float[] {    0,    0,    0,    1,    0,    0,    0,    0 },
    Biome.Ocean                   => new float[] {    0,    0,    0,    0,    0,    0, .20f, .80f },
    Biome.Beach                   => new float[] {    0, .15f, .70f,    0,    0,    0, .15f,    0 },
    Biome.Marsh                   => new float[] {    0, .60f,    0,    0,    0, .10f, .30f,    0 },
    Biome.Taiga                   => new float[] { .25f, .25f,    0, .40f, .10f,    0,    0,    0 },
    Biome.Tundra                  => new float[] {    0, .45f,    0, .45f, .10f,    0,    0,    0 },
    Biome.Scorched                => new float[] {    0, .40f, .10f,    0,    0, .50f,    0,    0 },
    Biome.Grassland               => new float[] {    1,    0,    0,    0,    0,    0,    0,    0 },
    Biome.Shrubland               => new float[] { .40f, .10f,    0,    0, .50f,    0,    0,    0 },
    Biome.TemperateDesert         => new float[] {    0,    0, .90f,    0,    0, .10f,    0,    0 },
    Biome.SubtropicalDesert       => new float[] {    0, .20f, .70f,    0,    0, .10f,    0,    0 },
    Biome.TropicalRainyForest     => new float[] {    0, .30f, .60f,    0, .10f,    0,    0,    0 },
    Biome.TemperateRainyForest    => new float[] {    0, .50f,    0,    0, .50f,    0,    0,    0 },
    Biome.TropicalSeasonForest    => new float[] { .30f, .20f, .10f,    0, .40f,    0,    0,    0 },
    Biome.TemperateDecidousForest => new float[] { .40f,    0,    0,    0, .60f,    0,    0,    0 },
    _                             => new float[] {    0,    0,    0,    0,    0,    0,    0,    0 }
  };

  IEnumerator SetSplatMaps() {
    // Get a reference to the terrain data
    int width = MapData.width;
    MapTData.alphamapResolution = width;
    var biomes = new Biome[width, width];
    RectInt bound = new(0, 0, width, width);

    Timer.Next();
    int total = width * width;
    for (int x = 0; x < width; x++) for (int y = 0; y < width; y++) {
      biomes[x, y] = MapData.GetPixel(x, y).b.ToBiome();
      if (Timer.Elapsed) { Timer.SetDetail(x * width + y, total); yield return null; }
    }
    // SplamapTerrain data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splamapTerrain data:
    Timer.Next();
    var splatmap = new float[width, width][];
    
    for (int y = 0; y < width; y++) for (int x = 0; x < width; x++) {
      splatmap[x, y] = BiomeWeights(biomes[y, x]);
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
          RectInt rect = new(j - r - 1, k - r - 1, 2 * r + 3, 2 * r + 3);
          foreach (var p in rect.allPositionsWithin) if (bound.Contains(p)) {
            for (int l = 0; l < 8; l++) sum[l] += splatmap[p.x, p.y][l];
            count++;
          }
          for (int l = 0; l < 8; l++) sum[l] /= count;
          result[j, k] = sum;
          if (Timer.Elapsed) { Timer.SetDetail(i * width * width + j * width + k, n * width * width); yield return null; }
        }
        //! 예전에는 `splatmap = result` 한 방향으로만 대입해서, 2회차부터 두 변수가
        //! 같은 배열을 가리켜 하이트맵과 똑같이 in-place가 되었습니다.
        //! (iterations 기본값이 1이라 그때만 무사했습니다.)
        (splatmap, result) = (result, splatmap);
      }
    }
    var splatmapData = new float[width, width, 8];
    for (int y = 0; y < width; y++) for (int x = 0; x < width; x++) for (int i = 0; i < 8; i++)
      splatmapData[x, y, i] = splatmap[x, y][i];
    MapTData.SetAlphamaps(0, 0, splatmapData);
  }
}
}
