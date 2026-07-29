using System;
using System.Collections;
using UnityEngine;

using Assets.Maps;

namespace Rair.Field.Maps
{
  /// <summary>맵 생성 절차 본체입니다. 런타임에서도 돌아갑니다.</summary>
  /// <remarks>
  /// 예전에는 절차 전체가 <c>Rair.Editor</c>에 있어 인게임 생성이 불가능했습니다. (문서 05 P3-4)
  /// 남아 있던 에디터 의존은 두 가지였고, 각각 이렇게 갈랐습니다.
  /// <list type="bullet">
  ///   <item>
  ///     <b>코루틴 구동</b> — 여기서는 <see cref="IEnumerator"/>를 돌려주기만 합니다.
  ///     플레이 모드·빌드에서는 <see cref="RandomTextureGenerator.Generate"/>가
  ///     <c>MonoBehaviour.StartCoroutine</c>으로 돌리고,
  ///     에디트 모드에서는 <c>Rair.Editor</c>의 러너가 <c>EditorCoroutine</c>으로 돌립니다.
  ///   </item>
  ///   <item>
  ///     <b>생성 결과를 에셋으로 저장</b> — 프로젝트에 PNG를 쓰는 일이라 본질적으로 에디터 전용입니다.
  ///     <see cref="SaveHook"/>으로 빼서 에디터가 채워 넣습니다. 런타임에서는 비어 있고, 저장 단계를 건너뜁니다.
  ///   </item>
  /// </list>
  /// </remarks>
  public static class MapGeneration
  {
    /// <summary>생성 결과를 프로젝트 에셋으로 저장하는 단계입니다.</summary>
    /// <remarks>
    /// 에디터에서만 채워집니다. 비어 있으면 저장을 건너뛰고 메모리상의 텍스처만 씁니다.
    /// </remarks>
    public static Func<RandomTextureGenerator, IEnumerator> SaveHook;

    /// <summary>설정에 따라 섬과 지형을 생성합니다.</summary>
    public static IEnumerator Run(RandomTextureGenerator rtg) {
      if (!rtg.fixSeed) rtg.seed = UnityEngine.Random.Range(0, int.MaxValue);
      UnityEngine.Random.InitState(rtg.seed);
      rtg.IsGenerating = true;

      rtg.MapInst = new(rtg.mapSize);
      rtg.TerrainGen = new(rtg);
      yield return rtg.MapInst.Initialize();
      yield return rtg.MapInst.Graph.GenerateGraph(rtg.landRatio, rtg.riverCount);
      yield return rtg.MapInst.MapTexture.CreateMapMaterial(rtg.MapInst);

      //? 시드를 고정해 생성했으므로, 이후 난수는 다시 풀어 둡니다.
      UnityEngine.Random.InitState(new System.Random().Next());

      if (rtg.saveMap && SaveHook != null) yield return SaveHook(rtg);
      else ApplyMapInMemory(rtg);

      yield return rtg.TerrainGen.GenerateTerrain();

      rtg.IsGenerating = false;
    }

    /// <summary>저장 없이 생성된 텍스처를 화면에 반영합니다.</summary>
    public static void ApplyMapInMemory(RandomTextureGenerator rtg) {
      var vars = rtg.mapVariables;
      if (vars.image == null || rtg.Map == null) return;

      vars.image.sprite = Sprite.Create(rtg.Map, new Rect(0, 0, rtg.Map.width, rtg.Map.height), Vector2.zero);
    }
  }
}
