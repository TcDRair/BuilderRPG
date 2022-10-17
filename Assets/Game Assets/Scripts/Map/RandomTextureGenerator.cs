#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Unity.Jobs;
using Unity.Collections;
using Unity.EditorCoroutines.Editor;

using Assets.Map;

using static MainSetting;

public class RandomTextureGenerator : MonoBehaviour
{
	public static RandomTextureGenerator Instance { get; private set; }
  public void Awake() {
    Instance = this;
    isRunning = false;
  }

  [InspectorName("Island Map"), Tooltip("섬 이미지를 표시할 대상을 지정합니다.")]
  public Image image;
  [Tooltip("섬 이미지를 저장할 파일을 지정합니다.")]
  public Sprite textureMap;
  [Tooltip("섬 머티리얼을 저장할 파일을 지정합니다.")]
  public Material mapMaterial;
  [InspectorName("Height Map"), Tooltip("높이 맵을 저장할 파일을 지정합니다.")]
  public Sprite heightMap;
  [HideInInspector] public int seed;
  [HideInInspector] public int riverCount;
  [HideInInspector] public bool saveMap, randomSeed, isRunning;
  [HideInInspector] public float lakeThreshold, landRatio;
  [HideInInspector] public Size mapSize;

  EditorCoroutine _currentEditorCoroutine;
  Coroutine _currentCoroutine;
  public void TryMapGenerate() {
    if (isRunning) Debug.Log("이미 작업중입니다.");
    else _currentEditorCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(CreateRandomIslandEditor());
  }
  public void TryMapGenerate(MonoBehaviour mono) {
    if (isRunning) Debug.Log("이미 작업중입니다.");
    else _currentCoroutine = mono.StartCoroutine(CreateRandomIslandRuntime(mono));
  }
  public void CancelGenerate() {
    if (_currentEditorCoroutine != null) EditorCoroutineUtility.StopCoroutine(_currentEditorCoroutine);
    if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
    isRunning = false;
  }

  Texture2D texture, hMap;
  public Map Map { get; private set; }
  public MapTexture MapTexture { get; private set; }
  public IEnumerator CreateRandomIslandEditor() {
    if (Application.isPlaying) yield break; // 런타임에서 작동을 보장하지 않습니다.

    isRunning = true;
    if (!randomSeed) seed = Random.Range(0, int.MaxValue);
    Random.InitState(seed);

    Map = new(mapSize);
    MapTexture = new(1);
    yield return EditorCoroutineUtility.StartCoroutineOwnerless(Map.Graph.InitGraph(lakeThreshold, landRatio, riverCount));
    yield return MapTexture.CreateMapMaterial(Map);
    texture = MapTexture.Texture;
    hMap = MapTexture.HeightMap;

    Random.InitState(new System.Random().Next());
    if (saveMap) {
      //* 1. Save Map Sprite
      string path1 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(textureMap);
      File.WriteAllBytes(path1, texture.EncodeToPNG());
      image.sprite = textureMap;
      //* 2. Save Height Map
      string path3 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(heightMap);
      string path4 = path3.Replace(".png", ".raw");
      File.WriteAllBytes(path3, hMap.EncodeToPNG());
      File.WriteAllBytes(path4, hMap.GetPixels32().Select(x => x.r).ToArray());

      image.type = Image.Type.Simple;
      mapMaterial.mainTexture = image.sprite.texture;
      yield return null;
      AssetDatabase.Refresh();
    }
    else image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    isRunning = false;
  }

  public IEnumerator CreateRandomIslandRuntime(MonoBehaviour mono) {
    if (!Application.isPlaying) yield break; // 에디터에서 작동을 보장하지 않습니다.

    isRunning = true;
    if (!randomSeed) seed = Random.Range(0, int.MaxValue);
    Random.InitState(seed);

    Map = new Map(mapSize);
    MapTexture = new MapTexture(4);

    // yield return mono.StartCoroutine(map.graph.InitGraph(lakeThreshold, landRatio, riverCount, mono));
    yield return mono.StartCoroutine(MapTexture.CreateMapMaterial(Map));
    texture = MapTexture.Texture;
  }
}
#endif