
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Unity.Jobs;
using Unity.Collections;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

using Assets.Map;

using static MainSetting;

#if UNITY_EDITOR
public class RandomTextureGenerator : MonoBehaviour
{
	static RandomTextureGenerator _inst;
	public static RandomTextureGenerator Instance {
		get {
			if (_inst == null) {
				_inst = FindObjectOfType<RandomTextureGenerator>();
				if (_inst == null) {
					GameObject go = new GameObject("Random Texture Generator");
					_inst = go.AddComponent<RandomTextureGenerator>();
				}
			}
			return _inst;
		}
		set { _inst = value; }
	}
    void Awake() {
        Instance = this;
        isRunning = false;
    }

    [InspectorName("Island Map"), Tooltip("섬 이미지를 표시할 대상을 지정합니다.")]
    public Image image;
    [InspectorName("Map File"), Tooltip("섬 이미지를 저장할 파일을 지정합니다.")]
    public Sprite sprite;
    [HideInInspector] public int seed;
    [HideInInspector] public int riverCount;
    [HideInInspector] public bool saveMap, fixSeed, isRunning;
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
    public Map map { get; private set; }
    public MapTexture mapTexture { get; private set; }
    public IEnumerator CreateRandomIslandEditor() {
        if (Application.isPlaying) yield break; // 런타임에서 작동을 보장하지 않습니다.

        isRunning = true;
        if (!fixSeed) seed = Random.Range(0, int.MaxValue);
        Random.InitState(seed);

        map = new Map(mapSize);
        mapTexture = new MapTexture(1);
        yield return EditorCoroutineUtility.StartCoroutineOwnerless(map.graph.InitGraph(lakeThreshold, landRatio, riverCount));
        yield return mapTexture.CreateMapMaterial(map);
        texture = mapTexture.texture;

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
        if (!fixSeed) seed = Random.Range(0, int.MaxValue);
        Random.InitState(seed);

        map = new Map(mapSize);
        mapTexture = new MapTexture(4);

        // yield return mono.StartCoroutine(map.graph.InitGraph(lakeThreshold, landRatio, riverCount, mono));
        yield return mono.StartCoroutine(mapTexture.CreateMapMaterial(map));
        texture = mapTexture.texture;
    }
}
#endif

/*
public class ColorRect {
    public RectInt rect;
    public Color[] colors;
    public ColorRect(int width, int height) {
        rect = new RectInt(0, 0, width, height);
        colors = new Color[width * height];
        System.Array.Fill(colors, emptyColor);
    }
    public ColorRect(RectInt rect) {
        this.rect = rect;
        colors = new Color[rect.width * rect.height];
        System.Array.Fill(colors, emptyColor);
    }
    public ColorRect(RectInt rect, Color[] colors) {
        this.rect = rect;
        this.colors = colors;
    }
    public ColorRect(Texture2D texture) {
        rect = new RectInt(0, 0, texture.width, texture.height);
        colors = texture.GetPixels();
    }
    public ColorRect(Texture2D texture, int xPos, int yPos) {
        rect = new RectInt(xPos, yPos, texture.width, texture.height);
        colors = texture.GetPixels();
    }

    public ColorRect Rotate90(bool clockwise = true) {
        ColorRect result = new ColorRect(new RectInt(rect.x, rect.y, rect.height, rect.width));
        if (clockwise) for (int x = 0; x < rect.width; x++) for (int y = 0; y < rect.height; y++) {
            Color color = colors[x + y * rect.width];
            int index = y + x * rect.height;
            if      (color.Equals(emptyColor)) result.colors[index] = emptyColor;
            else if (color.Equals(emptyColor2)) result.colors[index] = emptyColor;
            else if (color.Equals(floorColor)) result.colors[index] = floorColor;
            else if (color.Equals(wallNColor)) result.colors[index] = wallEColor;
            else if (color.Equals(wallEColor)) result.colors[index] = wallSColor;
            else if (color.Equals(wallWColor)) result.colors[index] = wallNColor;
            else if (color.Equals(wallSColor)) result.colors[index] = wallWColor;
            else if (color.Equals(cornerNEColor)) result.colors[index] = cornerSEColor;
            else if (color.Equals(cornerSEColor)) result.colors[index] = cornerSWColor;
            else if (color.Equals(cornerSWColor)) result.colors[index] = cornerNWColor;
            else if (color.Equals(cornerNWColor)) result.colors[index] = cornerNEColor;
            else if (color.Equals(edgeNEColor)) result.colors[index] = edgeSEColor;
            else if (color.Equals(edgeSEColor)) result.colors[index] = edgeSWColor;
            else if (color.Equals(edgeSWColor)) result.colors[index] = edgeNWColor;
            else if (color.Equals(edgeNWColor)) result.colors[index] = edgeNEColor;
            else if (color.Equals(diagonalColor)) result.colors[index] = diagonalReverseColor;
            else if (color.Equals(diagonalReverseColor)) result.colors[index] = diagonalColor;
            else Debug.Log("알 수 없는 색상입니다.");
        }
        else for (int x = 0; x < rect.width; x++) for (int y = 0; y < rect.height; y++) {
            Color color = colors[x + y * rect.width];
            int index = y + x * rect.height;
            if      (color.Equals(emptyColor)) result.colors[index] = emptyColor;
            else if (color.Equals(emptyColor2)) result.colors[index] = emptyColor;
            else if (color.Equals(floorColor)) result.colors[index] = floorColor;
            else if (color.Equals(wallNColor)) result.colors[index] = wallWColor;
            else if (color.Equals(wallEColor)) result.colors[index] = wallNColor;
            else if (color.Equals(wallWColor)) result.colors[index] = wallEColor;
            else if (color.Equals(wallSColor)) result.colors[index] = wallEColor;
            else if (color.Equals(cornerNEColor)) result.colors[index] = cornerNWColor;
            else if (color.Equals(cornerSEColor)) result.colors[index] = cornerNEColor;
            else if (color.Equals(cornerSWColor)) result.colors[index] = cornerSEColor;
            else if (color.Equals(cornerNWColor)) result.colors[index] = cornerSWColor;
            else if (color.Equals(edgeNEColor)) result.colors[index] = edgeNWColor;
            else if (color.Equals(edgeSEColor)) result.colors[index] = edgeNEColor;
            else if (color.Equals(edgeSWColor)) result.colors[index] = edgeSEColor;
            else if (color.Equals(edgeNWColor)) result.colors[index] = edgeSWColor;
            else if (color.Equals(diagonalColor)) result.colors[index] = diagonalReverseColor;
            else if (color.Equals(diagonalReverseColor)) result.colors[index] = diagonalColor;
            else Debug.Log("알 수 없는 색상입니다.");
        }
        return result;
    }

    public ColorRect Flip(bool horizontal = true) {
        ColorRect result = new ColorRect(rect);
        if (horizontal) for (int x = 0; x < rect.width; x++) for (int y = 0; y < rect.height; y++) {
            Color color = colors[x + y * rect.width];
            int index = x + (rect.height - y - 1) * rect.width;
            if      (color.Equals(emptyColor)) result.colors[index] = emptyColor;
            else if (color.Equals(floorColor)) result.colors[index] = floorColor;
            else if (color.Equals(wallNColor)) result.colors[index] = wallSColor;
            else if (color.Equals(wallEColor)) result.colors[index] = wallEColor;
            else if (color.Equals(wallSColor)) result.colors[index] = wallNColor;
            else if (color.Equals(wallWColor)) result.colors[index] = wallWColor;
            else if (color.Equals(cornerNEColor)) result.colors[index] = cornerSEColor;
            else if (color.Equals(cornerSEColor)) result.colors[index] = cornerNEColor;
            else if (color.Equals(cornerSWColor)) result.colors[index] = cornerNWColor;
            else if (color.Equals(cornerNWColor)) result.colors[index] = cornerSWColor;
            else if (color.Equals(edgeNEColor)) result.colors[index] = edgeSEColor;
            else if (color.Equals(edgeSEColor)) result.colors[index] = edgeNEColor;
            else if (color.Equals(edgeSWColor)) result.colors[index] = edgeNWColor;
            else if (color.Equals(edgeNWColor)) result.colors[index] = edgeSWColor;
            else if (color.Equals(diagonalColor)) result.colors[index] = diagonalColor;
            else if (color.Equals(diagonalReverseColor)) result.colors[index] = diagonalReverseColor;
            else Debug.Log("알 수 없는 색상입니다.");
        }
        // else = vertical
        else for (int x = 0; x < rect.width; x++) for (int y = 0; y < rect.height; y++) {
            Color color = colors[x + y * rect.width];
            int index = (rect.width - x - 1) + y * rect.width;
            if      (color.Equals(emptyColor)) result.colors[index] = emptyColor;
            else if (color.Equals(floorColor)) result.colors[index] = floorColor;
            else if (color.Equals(wallNColor)) result.colors[index] = wallSColor;
            else if (color.Equals(wallEColor)) result.colors[index] = wallEColor;
            else if (color.Equals(wallSColor)) result.colors[index] = wallNColor;
            else if (color.Equals(wallWColor)) result.colors[index] = wallWColor;
            else if (color.Equals(cornerNEColor)) result.colors[index] = cornerSEColor;
            else if (color.Equals(cornerSEColor)) result.colors[index] = cornerNEColor;
            else if (color.Equals(cornerSWColor)) result.colors[index] = cornerNWColor;
            else if (color.Equals(cornerNWColor)) result.colors[index] = cornerSWColor;
            else if (color.Equals(edgeNEColor)) result.colors[index] = edgeSEColor;
            else if (color.Equals(edgeSEColor)) result.colors[index] = edgeNEColor;
            else if (color.Equals(edgeSWColor)) result.colors[index] = edgeNWColor;
            else if (color.Equals(edgeNWColor)) result.colors[index] = edgeSWColor;
            else if (color.Equals(diagonalColor)) result.colors[index] = diagonalColor;
            else if (color.Equals(diagonalReverseColor)) result.colors[index] = diagonalReverseColor;
            else Debug.Log("알 수 없는 색상입니다.");
        }
        return result;
    }

    public ColorRect Paste(ColorRect source, int xPos, int yPos) {
        var result = new ColorRect(rect.width, rect.height);
        for (int y = 0; y < source.rect.height; y++) {
            for (int x = 0; x < source.rect.width; x++) {
                result.colors[(y + yPos) * rect.width + x + xPos] = source.colors[y * source.rect.width + x];
            }
        }
        return result;
    }
    

    public static ColorRect operator +(ColorRect a, ColorRect b) {
        for (int y = 0; y < b.rect.height; y++) {
            for (int x = 0; x < b.rect.width; x++) {
                a.colors[b.rect.x + x + (b.rect.y + y) * a.rect.width] = b.colors[x + y * b.rect.width];
            }
        }
        return a;
    }
}
*/