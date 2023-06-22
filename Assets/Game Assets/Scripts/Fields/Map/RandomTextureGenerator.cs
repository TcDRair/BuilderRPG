#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Unity.EditorCoroutines.Editor;

using Assets.Maps;

namespace Rair.Field.Maps
{
  public class RandomTextureGenerator : MonoBehaviour
  {
  	public static RandomTextureGenerator Instance { get; private set; }
    public void Awake() {
      Instance = this;
      isRunning = false;
    }

    #region Inspector
    [InspectorName("Island Map"), Tooltip("섬 이미지를 표시할 대상을 지정합니다.")]
    public Image image;
    [Tooltip("섬 이미지를 저장할 파일을 지정합니다.")]
    public Sprite MapSprite;
    [Tooltip("섬 머티리얼을 저장할 파일을 지정합니다.")]
    public Material mapMaterial;
    [Tooltip("각종 정보를 저장할 파일을 지정합니다.")]
    public Sprite MapDataSprite; // R: height, G : moisture, B : occupy, A : 
    [HideInInspector] public int seed, riverCount;
    [HideInInspector] public bool saveMap = true, fixedSeed, isRunning;
    [HideInInspector] public float lakeThreshold, landRatio;
    [HideInInspector] public Size mapSize;
    [HideInInspector] public TerrainGenerator terrainGenerator;
    #endregion

    EditorCoroutine coroutine;
    // Coroutine _currentCoroutine;

    #region Editor
    public void TryMapGenerate(object owner) {
      if (isRunning) Debug.Log("이미 작업중입니다.");
      else coroutine = EditorCoroutineUtility.StartCoroutine(CreateRandomIslandEditor(), owner);
    }
    /*public void TryMapGenerate(MonoBehaviour mono) {
      if (isRunning) Debug.Log("이미 작업중입니다.");
      else _currentCoroutine = mono.StartCoroutine(CreateRandomIslandRuntime(mono));
    }*/
    public void CancelGenerate() {
      if (coroutine != null) EditorCoroutineUtility.StopCoroutine(coroutine);
      // if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
      isRunning = false;
    }
    public void Reset() {
      Map = null;
      MapTexture = null;
    }
    #endregion

    Texture2D texture;
    public Map Map { get; private set; }
    public MapTexture MapTexture { get; private set; }
    public IEnumerator CreateRandomIslandEditor() {
      if (Application.isPlaying) yield break; // 런타임에서 작동을 보장하지 않습니다.

      if (!fixedSeed) seed = Random.Range(0, int.MaxValue);
      Random.InitState(seed);
      isRunning = true;

      Map = new(mapSize);
      yield return Map.Initialize();
      MapTexture = new(1);
      yield return Map.Graph.InitGraph(lakeThreshold, landRatio, riverCount);
      yield return MapTexture.CreateMapMaterial(Map);
      texture = MapTexture.Texture;

      Random.InitState(new System.Random().Next());
      if (saveMap) {
        //* 1. Save Map Sprite
        string path1 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(MapSprite);
        File.WriteAllBytes(path1, texture.EncodeToPNG());
        image.sprite = MapSprite;
        //* 2. Save Height Map
        string path3 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(MapDataSprite);
        string path4 = path3.Replace(".png", ".raw");
        File.WriteAllBytes(path3, MapTexture.DataMap.EncodeToPNG());
        File.WriteAllBytes(path4, MapTexture.HeightmapRaw);

        image.type = Image.Type.Simple;
        mapMaterial.mainTexture = image.sprite.texture;
        yield return null;
        AssetDatabase.Refresh();
      }
      else image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

      //? Begin Terrain Generator if allocated
      terrainGenerator.MapData = Map;
      //TODO if (bool) terrainGenerator.GenerateTerrain();

      isRunning = false;
      Map.Reset();
      terrainGenerator.Reset();
    }

    /*public IEnumerator CreateRandomIslandRuntime(MonoBehaviour mono) {
      if (!Application.isPlaying) yield break; // 에디터에서 작동을 보장하지 않습니다.

      isRunning = true;
      if (!randomSeed) seed = Random.Range(0, int.MaxValue);
      Random.InitState(seed);

            Map = new Assets.Maps.Map(mapSize);
      MapTexture = new MapTexture(4);

      yield return mono.StartCoroutine(Map.Graph.InitGraph(lakeThreshold, landRatio, riverCount, mono));
      yield return mono.StartCoroutine(MapTexture.CreateMapMaterial(Map));
      texture = MapTexture.Texture;
    }*/
  }
  #endif
}