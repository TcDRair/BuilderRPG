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
    [InspectorName("Map File"), Tooltip("섬 이미지를 저장할 파일을 지정합니다.")]
    public Sprite sprite;
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

    Texture2D texture;
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
        texture = MapTexture.texture;

        Random.InitState(new System.Random().Next());
        if (saveMap) {
            string path = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(sprite);
            if (path != "") File.WriteAllBytes(path, texture.EncodeToPNG());
            image.sprite = sprite;
            image.type = Image.Type.Simple;
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
        texture = MapTexture.texture;
    }
}
#endif