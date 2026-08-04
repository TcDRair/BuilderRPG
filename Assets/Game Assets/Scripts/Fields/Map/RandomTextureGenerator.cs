using UnityEngine;

using Assets.Maps;

namespace Rair.Field.Maps
{
  /// <summary>맵 생성 설정을 담는 씬 컴포넌트입니다.</summary>
  /// <remarks>
  /// <b>이 타입은 반드시 런타임 어셈블리에 있어야 합니다.</b>
  /// <para>
  /// 한때 생성 로직과 함께 <c>Rair.Editor</c>에 두었는데,
  /// 에디터 전용 어셈블리의 MonoBehaviour를 씬에 붙이면
  /// <b>플레이 모드를 오갈 때 직렬화 데이터가 전부 사라집니다.</b>
  /// (seed·landRatio·mapSize·mapVariables·terrainVariables가 실제로 유실된 적이 있습니다.)
  /// 컴파일도 되고 바인딩도 되고 씬 검증도 통과하기 때문에
  /// <b>플레이 모드에 들어가 보기 전까지 드러나지 않습니다.</b>
  /// </para>
  /// 그래서 데이터(여기)와 생성 로직(<c>Rair.Editor</c>의 <c>MapGenerationRunner</c>)을 갈라 두었습니다.
  /// <b>이 파일에 <c>UnityEditor</c>·<c>EditorCoroutine</c> 의존을 넣지 마십시오.</b>
  /// <para>
  /// 생성 자체는 아직 에디터에서만 가능합니다. (보완 기록 P3-4)
  /// </para>
  /// </remarks>
  [ExecuteInEditMode]
  public class RandomTextureGenerator : MonoBehaviour
  {
    public static RandomTextureGenerator Instance { get; private set; }
    public void OnEnable() { Instance = this; }

    #region Inspector — 직렬화 대상
    [HideInInspector] public int seed, riverCount;
    [HideInInspector] public bool saveMap = true, fixSeed;
    [HideInInspector] public float landRatio;
    [HideInInspector] public Size mapSize;
    public MapVar mapVariables;
    public TerrainVar terrainVariables;
    #endregion

    #region 생성 결과 — 생성기가 채웁니다 (직렬화되지 않습니다)
    /// <summary>생성이 진행 중인지 여부.</summary>
    public bool IsGenerating { get; set; }
    /// <summary>생성된 지도.</summary>
    public Map MapInst { get; set; }
    /// <summary>진행 중인 지형 생성기. 진행률 표시에 씁니다.</summary>
    public TerrainGenerator TerrainGen { get; set; }

    public Texture2D Map => MapInst?.MapTexture?.Map;
    public Texture2D MapData => MapInst?.MapTexture?.MapData;
    #endregion

    /// <summary>인게임에서 맵을 생성합니다.</summary>
    /// <remarks>
    /// 플레이 모드·빌드 전용입니다. 에디트 모드에서는 <c>MonoBehaviour</c> 코루틴이 돌지 않으므로
    /// <c>Rair.Editor</c>의 러너(<c>EditorCoroutine</c> 기반)를 쓰십시오.
    /// <para>
    /// 저장 단계는 건너뜁니다. 프로젝트에 PNG를 쓰는 일이라 에디터에서만 의미가 있습니다.
    /// </para>
    /// </remarks>
    public Coroutine Generate() {
      if (IsGenerating) { Debug.LogWarning("이미 생성 중입니다."); return null; }
      return StartCoroutine(MapGeneration.Run(this));
    }
  }
}
