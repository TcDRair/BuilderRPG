using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Unity.EditorCoroutines.Editor;

namespace Rair.Field.Maps
{
  /// <summary>
  /// 에디트 모드에서 맵 생성을 구동하고, 결과를 프로젝트 에셋으로 저장합니다.
  /// </summary>
  /// <remarks>
  /// 절차 본체는 <see cref="MapGeneration"/>(런타임)에 있습니다. 여기 남은 것은
  /// <b>에디트 모드에서만 필요한 두 가지</b>뿐입니다.
  /// <list type="number">
  ///   <item><c>MonoBehaviour</c> 코루틴이 돌지 않으므로 <c>EditorCoroutine</c>으로 구동</item>
  ///   <item>생성된 텍스처를 프로젝트 PNG 에셋에 덮어쓰기</item>
  /// </list>
  /// 한 번에 하나의 생성만 돌아가므로 진행 상태를 정적으로 들고 있습니다.
  /// </remarks>
  [InitializeOnLoad]
  public static class MapGenerationRunner
  {
    static EditorCoroutine coroutine;

    static MapGenerationRunner() {
      //? 런타임 절차가 저장 단계에 도달하면 이 구현을 부릅니다.
      //? 빌드에는 이 어셈블리가 포함되지 않으므로 훅은 비어 있고, 저장은 건너뛰어집니다.
      MapGeneration.SaveHook = SaveGeneratedTextures;
    }

    /// <summary>현재 생성에 쓰이는 지형 생성기. 진행률 표시에 씁니다.</summary>
    public static TerrainGenerator Terrain(RandomTextureGenerator rtg) => rtg?.TerrainGen;

    public static bool IsRunning(RandomTextureGenerator rtg) => rtg != null && rtg.IsGenerating;

    public static void Begin(RandomTextureGenerator rtg, object owner) {
      if (rtg.IsGenerating) { Debug.Log("이미 작업중입니다."); return; }
      coroutine = EditorCoroutineUtility.StartCoroutine(MapGeneration.Run(rtg), owner);
    }

    public static void Cancel(RandomTextureGenerator rtg) {
      if (coroutine != null) EditorCoroutineUtility.StopCoroutine(coroutine);
      coroutine = null;
      if (rtg != null) rtg.IsGenerating = false;
    }

    /// <summary>다음 생성을 처음부터 다시 시작할 수 있도록 결과를 비웁니다.</summary>
    public static void Reset(RandomTextureGenerator rtg) {
      if (rtg == null) return;
      rtg.MapInst = null;
      rtg.TerrainGen = null;
    }

    /// <summary>생성된 텍스처를 프로젝트의 PNG 에셋에 덮어씁니다.</summary>
    static IEnumerator SaveGeneratedTextures(RandomTextureGenerator rtg) {
      var vars = rtg.mapVariables;

      //* 1. Save Map Sprite
      string mapPath = ProjectPathOf(vars.MapSprite);
      File.WriteAllBytes(mapPath, rtg.Map.EncodeToPNG());
      vars.image.sprite = vars.MapSprite;

      //* 2. Save Height Map
      string dataPath = ProjectPathOf(vars.MapDataSprite);
      File.WriteAllBytes(dataPath, rtg.MapInst.MapTexture.MapData.EncodeToPNG());

      vars.image.type = Image.Type.Simple;
      vars.mapMaterial.mainTexture = vars.image.sprite.texture;
      yield return null;
      AssetDatabase.Refresh();
    }

    static string ProjectPathOf(Object asset)
      => Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(asset);
  }
}
