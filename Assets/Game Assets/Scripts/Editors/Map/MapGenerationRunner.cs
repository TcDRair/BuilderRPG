using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Unity.EditorCoroutines.Editor;

using Assets.Maps;

namespace Rair.Field.Maps
{
  /// <summary>
  /// <see cref="RandomTextureGenerator"/>의 설정으로 실제 생성을 수행합니다.
  /// </summary>
  /// <remarks>
  /// 설정(런타임)과 로직(여기)이 갈라진 이유는 <see cref="RandomTextureGenerator"/> 주석에 있습니다.
  /// <para>
  /// <c>AssetDatabase</c>와 <c>EditorCoroutine</c>에 묶여 있어 에디터에서만 동작합니다.
  /// 인게임 생성을 하려면 이 둘을 걷어내야 합니다. (문서 05 P3-4)
  /// </para>
  /// 한 번에 하나의 생성만 돌아가므로 진행 상태를 정적으로 들고 있습니다.
  /// </remarks>
  public static class MapGenerationRunner
  {
    static EditorCoroutine coroutine;

    /// <summary>현재 생성 중인 대상. 없으면 <c>null</c>.</summary>
    public static RandomTextureGenerator Target { get; private set; }
    /// <summary>현재 생성에 쓰이는 지형 생성기. 진행률 표시에 씁니다.</summary>
    public static TerrainGenerator Terrain { get; private set; }

    public static bool IsRunning(RandomTextureGenerator rtg)
      => rtg != null && rtg.IsGenerating;

    public static void Begin(RandomTextureGenerator rtg, object owner) {
      if (rtg.IsGenerating) { Debug.Log("이미 작업중입니다."); return; }
      coroutine = EditorCoroutineUtility.StartCoroutine(Generate(rtg), owner);
    }

    public static void Cancel(RandomTextureGenerator rtg) {
      if (coroutine != null) EditorCoroutineUtility.StopCoroutine(coroutine);
      coroutine = null;
      if (rtg != null) rtg.IsGenerating = false;
    }

    /// <summary>다음 생성을 처음부터 다시 시작할 수 있도록 결과를 비웁니다.</summary>
    public static void Reset(RandomTextureGenerator rtg) {
      if (rtg != null) rtg.MapInst = null;
      Target = null;
      Terrain = null;
    }

    static IEnumerator Generate(RandomTextureGenerator rtg) {
      if (Application.isPlaying) yield break; // 런타임에서 작동을 보장하지 않습니다.

      if (!rtg.fixSeed) rtg.seed = Random.Range(0, int.MaxValue);
      Random.InitState(rtg.seed);
      rtg.IsGenerating = true;
      Target = rtg;

      #region Map Generator
      rtg.MapInst = new(rtg.mapSize);
      Terrain = new(rtg);
      yield return rtg.MapInst.Initialize();
      yield return rtg.MapInst.Graph.GenerateGraph(rtg.landRatio, rtg.riverCount);
      yield return rtg.MapInst.MapTexture.CreateMapMaterial(rtg.MapInst);

      Random.InitState(new System.Random().Next());
      var vars = rtg.mapVariables;
      if (rtg.saveMap) {
        //* 1. Save Map Sprite
        string path1 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(vars.MapSprite);
        File.WriteAllBytes(path1, rtg.Map.EncodeToPNG());
        vars.image.sprite = vars.MapSprite;
        //* 2. Save Height Map
        string path3 = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(vars.MapDataSprite);
        File.WriteAllBytes(path3, rtg.MapInst.MapTexture.MapData.EncodeToPNG());
        // string path4 = path3.Replace(".png", ".raw");
        // File.WriteAllBytes(path4, MapTexture.HeightmapRaw);

        vars.image.type = Image.Type.Simple;
        vars.mapMaterial.mainTexture = vars.image.sprite.texture;
        yield return null;
        AssetDatabase.Refresh();
      }
      else vars.image.sprite = Sprite.Create(rtg.Map, new Rect(0, 0, rtg.Map.width, rtg.Map.height), Vector2.zero);
      #endregion

      yield return Terrain.GenerateTerrain();

      rtg.IsGenerating = false;
    }
  }
}
