using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

using Assets.Maps;
public class TerrainGenerator : MonoBehaviour
{
  [Tooltip("Map의 텍스처를 지정합니다. 해당 텍스처로 지형을 생성합니다.")]
  public Texture2D Map;
  [Tooltip("Height Map을 지정합니다.")]
  public Texture2D HeightMap;
  [Tooltip("지형을 생성할 대상을 지정합니다.")]
  public Terrain terrain;
  [Tooltip("지형의 경계를 지정합니다.")]
  public Terrain BorderTerrain;

  [Range(0, 150)] public int height;

  public class Progress {
    /// <summary>Graph 작업의 전체적인 진행도를 나타냅니다.<br/>0에서 1 사이의 비율로 나타나며, 실제 시간과 일치하지 않을 수 있습니다.</summary>
    public float TotalProgress => state switch {
      State.NotStarted => 0,
      State.SettingHeights => .25f * CurrentProgress,
      State.GettingBiomeData => .5f * CurrentProgress + .25f,
      State.SettingAlphaMaps => .25f * CurrentProgress + .75f,
      State.Finished => 1,
      _ => 0
    };
    public float CurrentProgress => (float)cPC.x/cPC.y;
    public Vector2Int cPC;
    public enum State {
      NotStarted,
      SettingHeights, GettingBiomeData, SettingAlphaMaps,
      Finished,
    } public State state = State.NotStarted;
    public bool HasStarted => state is not State.NotStarted;

    public override string ToString() => state is State.NotStarted or State.Finished
      ? "[Terrain] " + $"{state}".ToNiceString()
      : "[Terrain] " + $"{state}".ToNiceString() + /*$" ({CurrentProgress*100:F0}%)"*/ $" ({cPC.x}/{cPC.y})";
  }
  public Progress progress = new();
  
  private float _prevTime = 0;
  const float deltaTime = .05f;
  private bool Elapsed {
    get {
      bool e = Time.realtimeSinceStartup - _prevTime > deltaTime;
      if (e) _prevTime = Time.realtimeSinceStartup;
      return e;
    }
  }
  
  public void Reset() { progress = new(); }

  public IEnumerator GenerateTerrain(Map mapData) {
    
    // 지형의 크기를 지정합니다.
    terrain.terrainData.size = new Vector3(Map.width, height, Map.height);
    terrain.terrainData.heightmapResolution = HeightMap.width;

    BorderTerrain.terrainData.size = new Vector3(Map.width, 5, Map.height);
    BorderTerrain.terrainData.heightmapResolution = HeightMap.width;
    // 지형의 높이를 지정합니다.
    progress.state = Progress.State.SettingHeights;
    float[,] heights1 = new float[HeightMap.height, HeightMap.width];
    progress.cPC.y = HeightMap.height * HeightMap.width * 2;
    for (int x = 0; x < HeightMap.width; x++) {
      for (int y = 0; y < HeightMap.height; y++) {
        heights1[y, x] = HeightMap.GetPixel(x, y).grayscale;
        if (Elapsed) { progress.cPC.x = x * HeightMap.height + y; yield return null; }
      }
    }
    terrain.terrainData.SetHeights(0, 0, heights1);
    // 맵 경계를 지정합니다.
    float[,] heights2 = new float[HeightMap.height, HeightMap.width];
    for (int x = 0; x < HeightMap.width; x++) {
      for (int y = 0; y < HeightMap.height; y++) {
        heights2[y, x] = HeightMap.GetPixel(x, y).grayscale == 0 ? 0 : 1;
        if (Elapsed) { progress.cPC.x = x * HeightMap.height + y + progress.cPC.y/2; yield return null; }
      }
    }
    BorderTerrain.terrainData.SetHeights(0, 0, heights2);

    // yield return SetAlphaMaps(terrain, mapData);

    progress.state = Progress.State.Finished;
  }

  //* Basic Methods from : https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splatmapping/
  /*IEnumerator SetAlphaMaps(Terrain terrain, Map map) {
    // Get a reference to the terrain data
    TerrainData tD = terrain.terrainData;
    int width = tD.alphamapWidth, height = tD.alphamapHeight;
    var biomes = new Biome[width, height];
    float sX = map.Width/width, sY = map.Height/height;

    progress.state = Progress.State.GettingBiomeData;
    progress.cPC = new Vector2Int(0, width * height);
    //TODO 아무래도 바이옴 다시 짜는 건 미친 짓이야. Polygon 만들 때 터레인 해상도로 같이 알파맵+높이맵 만들어야겠어...
    //! CornerMap / CenterMap도 다시 구축하는 과정에서 뭔가 문제가 생긴 걸지도 몰라...
    for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) {
      biomes[x, y] = map.Graph.GetNearestCenter(new Vector2(x * sX, y * sY)).biome;
      if (Elapsed) { progress.cPC.x = x * height + y; yield return null; }
    }
    // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
    progress.state = Progress.State.SettingAlphaMaps;
    progress.cPC = new Vector2Int(0, width * height);
    float[,,] splatmapData = new float[width, height, tD.alphamapLayers];
    
    for (int y = 0; y < tD.alphamapHeight; y++) {
      for (int x = 0; x < tD.alphamapWidth; x++) {        
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
        if (Elapsed) { progress.cPC.x = y * tD.alphamapHeight + x; yield return null; }
      }
    }
    
    // Finally assign the new splatmap to the terrainData:
    progress.cPC = Vector2Int.one;
    tD.SetAlphamaps(0, 0, splatmapData);
  }*/
}