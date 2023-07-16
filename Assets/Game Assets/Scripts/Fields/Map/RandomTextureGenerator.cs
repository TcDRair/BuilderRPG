#if UNITY_EDITOR
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Unity.EditorCoroutines.Editor;

using Assets.Maps;

namespace Rair.Field.Maps
{
  [ExecuteInEditMode]
  public class RandomTextureGenerator : MonoBehaviour
  {
  	public static RandomTextureGenerator Instance { get; private set; }
    public void OnEnable () { Instance = this; }

    #region Inspector
    [InspectorName("Island Map"), Tooltip("섬 이미지를 표시할 대상을 지정합니다.")]
    public Image image;
    public Sprite MapSprite, MapDataSprite;
    public Material mapMaterial;
    public Terrain MapTerrain, BorderTerrain;
    [HideInInspector] public int seed, riverCount;
    [HideInInspector] public bool saveMap = true, fixedSeed;
    [HideInInspector] public float lakeThreshold, landRatio;
    [HideInInspector] public Size mapSize;
    public TerrainVar terrainVariables;
    #endregion

    EditorCoroutine coroutine;

    #region Editor
    public void TryMapGenerate(object owner) {
      if (run) Debug.Log("이미 작업중입니다.");
      else coroutine = EditorCoroutineUtility.StartCoroutine(CreateRandomIslandEditor(), owner);
    }
    public void CancelGenerate() {
      if (coroutine != null) EditorCoroutineUtility.StopCoroutine(coroutine);
      run = false;
    }
    public void Restart() {
      MapGraph = null;
      MapTexture = null;
    }
    #endregion

    #region Properties
    bool run;
    public bool IsRunning() => run;
    public Map MapGraph { get; private set; }
    public MapTexture MapTexture { get; private set; }
    public TerrainGenerator TerrainGenerator { get; private set; }
    public Texture2D Map => MapTexture?.Map;
    public Texture2D MapData => MapTexture?.MapData;
    #endregion

    public IEnumerator CreateRandomIslandEditor() {
      if (Application.isPlaying) yield break; // 런타임에서 작동을 보장하지 않습니다.

      if (!fixedSeed) seed = Random.Range(0, int.MaxValue);
      Random.InitState(seed);
      run = true;
      
      #region Map Generator
      MapGraph = new(mapSize);
      yield return MapGraph.Initialize();
      yield return MapGraph.Graph.InitGraph(lakeThreshold, landRatio, riverCount);
      MapTexture = new(1);
      yield return MapTexture.CreateMapMaterial(MapGraph);

      Random.InitState(new System.Random().Next());
      if (saveMap) {
        //* 1. Save Map Sprite
        string path1 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(MapSprite);
        File.WriteAllBytes(path1, Map.EncodeToPNG());
        image.sprite = MapSprite;
        //* 2. Save Height Map
        string path3 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(MapDataSprite);
        File.WriteAllBytes(path3, MapTexture.MapData.EncodeToPNG());
        // string path4 = path3.Replace(".png", ".raw");
        // File.WriteAllBytes(path4, MapTexture.HeightmapRaw);

        image.type = Image.Type.Simple;
        mapMaterial.mainTexture = image.sprite.texture;
        yield return null;
        AssetDatabase.Refresh();
      }
      else image.sprite = Sprite.Create(Map, new Rect(0, 0, Map.width, Map.height), Vector2.zero);
      #endregion

      TerrainGenerator = new(this);
      yield return TerrainGenerator.GenerateTerrain();

      run = false;
    }
  }
}
#endif