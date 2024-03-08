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
  [System.Serializable]
  public struct MapVar {
    [InspectorName("Island Map")] public Image image;
    public Sprite MapSprite, MapDataSprite;
    public Material mapMaterial;
    public Terrain MapTerrain;
  }
  [ExecuteInEditMode]
  public class RandomTextureGenerator : MonoBehaviour
  {
  	public static RandomTextureGenerator Instance { get; private set; }
    public void OnEnable () { Instance = this; }

    #region Inspector
    [HideInInspector] public int seed, riverCount;
    [HideInInspector] public bool saveMap = true, fixSeed;
    [HideInInspector] public float landRatio;
    [HideInInspector] public Size mapSize;
    public MapVar mapVariables;
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
      MapInst = null;
    }
    #endregion

    #region Properties
    bool run;
    public bool IsRunning() => run;
    public Map MapInst { get; private set; }
    public TerrainGenerator TerrainGenerator { get; private set; }
    public Texture2D Map => MapInst?.MapTexture?.Map;
    public Texture2D MapData => MapInst?.MapTexture?.MapData;
    #endregion

    public IEnumerator CreateRandomIslandEditor() {
      if (Application.isPlaying) yield break; // 런타임에서 작동을 보장하지 않습니다.

      if (!fixSeed) seed = Random.Range(0, int.MaxValue);
      Random.InitState(seed);
      run = true;
      
      #region Map Generator
      MapInst = new(mapSize);
      TerrainGenerator = new(this);
      yield return MapInst.Initialize();
      yield return MapInst.Graph.GenerateGraph(landRatio, riverCount);
      yield return MapInst.MapTexture.CreateMapMaterial(MapInst);

      Random.InitState(new System.Random().Next());
      if (saveMap) {
        //* 1. Save Map Sprite
        string path1 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(mapVariables.MapSprite);
        File.WriteAllBytes(path1, Map.EncodeToPNG());
        mapVariables.image.sprite = mapVariables.MapSprite;
        //* 2. Save Height Map
        string path3 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(mapVariables.MapDataSprite);
        File.WriteAllBytes(path3, MapInst.MapTexture.MapData.EncodeToPNG());
        // string path4 = path3.Replace(".png", ".raw");
        // File.WriteAllBytes(path4, MapTexture.HeightmapRaw);

        mapVariables.image.type = Image.Type.Simple;
        mapVariables.mapMaterial.mainTexture = mapVariables.image.sprite.texture;
        yield return null;
        AssetDatabase.Refresh();
      }
      else mapVariables.image.sprite = Sprite.Create(Map, new Rect(0, 0, Map.width, Map.height), Vector2.zero);
      #endregion

      yield return TerrainGenerator.GenerateTerrain();

      run = false;
    }
  }
}
#endif
