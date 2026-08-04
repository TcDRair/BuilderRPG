using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

using Assets.Maps;
using Rair.Field.Maps;

namespace Rair.Tests
{
  /// <summary>
  /// 생성된 맵 데이터를 Unity <see cref="Terrain"/>에 적용하는 단계의 성질 테스트.
  /// </summary>
  /// <remarks>
  /// <b>이 단계에 테스트가 없어서 결함 4건이 통과했습니다.</b>
  /// <see cref="IslandGenerationTests"/>는 그래프 단계(육지 비율·고도·강)만,
  /// <see cref="SplatmapTests"/>는 가중치 <i>표</i>만 검사합니다.
  /// 그 값을 실제로 Terrain API에 넣는 구간은 아무도 보고 있지 않았습니다.
  /// <para>
  /// 보완 기록 기준으로 아래 네 건이 여기서 잡혀야 했습니다.
  /// <list type="table">
  ///   <item><term>P0-9</term><description>레이어 수 불일치로 <c>SetAlphamaps</c>가 예외. 낡은 알파맵이 남아 화면은 그럴듯했다</description></item>
  ///   <item><term>P0-10</term><description>수직 스케일이 상수 10이라 지형이 평면</description></item>
  ///   <item><term>P0-11</term><description>터레인 Y 오프셋이 높이와 수동으로 결합</description></item>
  ///   <item><term>P0-13</term><description>배치물이 지표면에서 벗어남</description></item>
  /// </list>
  /// </para>
  /// 생성 결과는 시드마다 다르므로 특정 출력이 아니라 <b>불변 조건</b>만 검사합니다.
  /// <para>
  /// <b>유효성 검증 — 변이 테스트.</b> 사후에 쓴 테스트가 통과하는 것만으로는
  /// 아무것도 증명되지 않으므로, <see cref="TerrainGenerator"/>에 결함을 넣어
  /// 실제로 잡히는지 확인했습니다.
  /// <list type="table">
  ///   <item>
  ///     <term>높이를 상수 10으로 되돌림 (P0-10 재현)</term>
  ///     <description>4건 실패 — 높이 3건 + <c>해수면이_월드_원점_높이에_온다</c></description>
  ///   </item>
  ///   <item>
  ///     <term>오프셋 설정 제거 (P0-11 재현)</term>
  ///     <description>3건 실패 — 오프셋 2건 + 해수면 1건</description>
  ///   </item>
  ///   <item>
  ///     <term><c>SampleHeight</c> 제거 (P0-13 재현)</term>
  ///     <description>1건 실패 — <c>모든_프롭이_지표면_높이에_놓인다</c></description>
  ///   </item>
  /// </list>
  /// <c>해수면이_월드_원점_높이에_온다</c>가 앞의 두 변이에 모두 걸리는 것이 의도한 바입니다.
  /// 높이와 오프셋은 한 쌍이고, 이 테스트가 그 <b>짝의 일관성</b>을 봅니다.
  /// </para>
  /// </remarks>
  public class TerrainApplicationTests
  {
    const Size SIZE = Size.s2;
    const float LAND_RATIO = 0.45f;
    const int RIVER_COUNT = 8;
    const int SEED = 20260730;
    const int LAYER_COUNT = 8;

    /// <summary>실제 프로젝트가 쓰는 터레인 데이터. P0-9가 발생한 그 에셋입니다.</summary>
    const string PROJECT_TERRAIN_DATA = "Assets/Game Assets/Resources/Terrain/Map Terrain.asset";

    #region 하네스

    /// <summary>
    /// 지형 생성에 필요한 최소 구성을 메모리에 세우고, 끝나면 전부 지웁니다.
    /// </summary>
    /// <remarks>
    /// <see cref="TerrainGenerator"/>는 <see cref="RandomTextureGenerator"/>(씬 컴포넌트)와
    /// <see cref="Terrain"/>을 요구합니다. 둘 다 에디터 없이 만들 수 있으므로
    /// 씬을 열지 않고 EditMode에서 구동할 수 있습니다.
    /// </remarks>
    sealed class Fixture : IDisposable
    {
      public RandomTextureGenerator Rtg { get; private set; }
      public Terrain Terrain { get; private set; }
      public TerrainData Data { get; private set; }
      public Transform PropParent { get; private set; }
      public int MapWidth { get; private set; }

      readonly GameObject rtgGO, terrainGO, propPrefab;
      readonly TerrainLayer[] layers;
      readonly TerrainPropData propData;
      readonly Texture2D layerTex;

      public Fixture(int totalHeight, bool smoothen, bool withProps, int seed = SEED) {
        //* 1. 맵 생성 — 그래프까지 돌린 뒤 데이터 텍스처를 굽습니다.
        UnityEngine.Random.InitState(seed);
        var map = new Map(SIZE);
        CoroutineDriver.RunToEnd(map.Initialize());
        CoroutineDriver.RunToEnd(map.Graph.GenerateGraph(LAND_RATIO, RIVER_COUNT));
        CoroutineDriver.RunToEnd(map.MapTexture.CreateMapMaterial(map));
        MapWidth = map.Width;

        //* 2. 터레인 — 가중치 표의 열 수만큼 레이어를 채웁니다.
        layerTex = new Texture2D(2, 2);
        layers = new TerrainLayer[LAYER_COUNT];
        for (int i = 0; i < layers.Length; i++)
          layers[i] = new TerrainLayer { name = $"TestLayer{i}", diffuseTexture = layerTex };

        Data = new TerrainData { terrainLayers = layers };
        terrainGO = Terrain.CreateTerrainGameObject(Data);
        Terrain = terrainGO.GetComponent<Terrain>();

        //? 오프셋이 생성기에 의해 설정되는지 보려면 의도적으로 틀린 값에서 출발해야 합니다.
        terrainGO.transform.position = new Vector3(0, 12345f, 0);

        //* 3. 프롭 배치 대상
        propParentGO = new GameObject("PropParent");
        PropParent = propParentGO.transform;

        propPrefab = new GameObject("PropPrefab");
        propData = ScriptableObject.CreateInstance<TerrainPropData>();
        propData.props = withProps
          ? new[] { AllBiomeProp(propPrefab) }
          : Array.Empty<Rair.Field.Maps.Prop>();

        //* 4. 생성기 조립 — TerrainGenerator가 생성자에서 구조체를 복사하므로 먼저 채웁니다.
        rtgGO = new GameObject("RTG");
        Rtg = rtgGO.AddComponent<RandomTextureGenerator>();
        Rtg.mapSize = SIZE;
        Rtg.landRatio = LAND_RATIO;
        Rtg.riverCount = RIVER_COUNT;
        Rtg.MapInst = map;
        Rtg.mapVariables = new MapVar { MapTerrain = Terrain };
        Rtg.terrainVariables = new TerrainVar {
          propParent = PropParent,
          data = propData,
          totalHeight = totalHeight,
          smoothenBorder = new TerrainVar.SmoothVar { active = smoothen, range = 1, iterations = 2 },
          //? iterations 2 는 예전에 버퍼 별칭이 생기던 경로입니다.
          smoothenBiomes = new TerrainVar.SmoothVar { active = smoothen, range = 1, iterations = 2 },
        };
      }

      readonly GameObject propParentGO;

      /// <summary>어떤 지형이 나와도 반드시 배치되도록 모든 바이옴을 밀도 1로 등록합니다.</summary>
      static Rair.Field.Maps.Prop AllBiomeProp(GameObject prefab) => new() {
        name = "TestProp",
        prefabs = new[] { prefab },
        conditions = ((Biome[])Enum.GetValues(typeof(Biome)))
          .Select(b => new Rair.Field.Maps.Prop.Condition { biome = b, density = 1f, scale = 0f })
          .ToArray()
      };

      /// <summary>지형 생성을 끝까지 돌립니다.</summary>
      public TerrainGenerator Run() {
        var gen = new TerrainGenerator(Rtg);
        Rtg.TerrainGen = gen;
        CoroutineDriver.RunToEnd(gen.GenerateTerrain());
        return gen;
      }

      public void Dispose() {
        void Kill(UnityEngine.Object o) { if (o != null) UnityEngine.Object.DestroyImmediate(o, true); }

        Kill(terrainGO);
        Kill(propParentGO);
        Kill(rtgGO);
        Kill(propPrefab);
        Kill(propData);
        Kill(Data);
        if (layers != null) foreach (var l in layers) Kill(l);
        Kill(layerTex);
      }
    }

    static Fixture main;
    static TerrainGenerator mainGen;

    [OneTimeSetUp]
    public void BuildMain() {
      //? 생성 비용이 크므로 대부분의 케이스가 한 결과를 공유합니다.
      main = new Fixture(totalHeight: 96, smoothen: false, withProps: true);
      mainGen = main.Run();
    }

    [OneTimeTearDown]
    public void TearDownMain() {
      main?.Dispose();
      main = null;
      mainGen = null;
    }

    #endregion

    #region P0-10 — 수직 스케일이 설정을 따른다

    [Test]
    public void 터레인_높이가_설정한_totalHeight와_같다() {
      //! 이 값이 상수 10으로 박혀 있던 동안 512 폭 맵의 육지 기복이 5유닛이었고,
      //! 지상 시점에서 평면과 구분되지 않았습니다. (P0-10)
      Assert.That(main.Terrain.terrainData.size.y, Is.EqualTo(96f).Within(0.001f));
    }

    [Test]
    public void 터레인_가로세로가_맵_크기와_같다() {
      var size = main.Terrain.terrainData.size;
      Assert.That(size.x, Is.EqualTo((float)main.MapWidth).Within(0.001f));
      Assert.That(size.z, Is.EqualTo((float)main.MapWidth).Within(0.001f));
    }

    [Test]
    public void totalHeight를_바꾸면_터레인_높이도_따라온다() {
      //? 상수였다면 이 테스트가 실패합니다.
      using var alt = new Fixture(totalHeight: 40, smoothen: false, withProps: false);
      alt.Run();
      Assert.That(alt.Terrain.terrainData.size.y, Is.EqualTo(40f).Within(0.001f));
    }

    [Test]
    public void totalHeight가_0이면_기본값으로_대체된다() {
      //? 필드를 새로 추가했으므로 기존 씬에는 0이 들어 있습니다. 그때 지형이 납작해지면 안 됩니다.
      using var zero = new Fixture(totalHeight: 0, smoothen: false, withProps: false);
      zero.Run();
      Assert.That(zero.Terrain.terrainData.size.y,
        Is.EqualTo((float)TerrainVar.DEFAULT_TOTAL_HEIGHT).Within(0.001f));
    }

    #endregion

    #region P0-11 — 오프셋이 높이로부터 유도된다

    [Test]
    public void 터레인_Y_오프셋이_높이의_절반만큼_내려간다() {
      //! 고도가 [-1,1] -> [0,1]로 매핑되므로 해수면이 정확히 절반입니다.
      //! 이 오프셋을 갱신하지 않으면 해저가 물 위로 드러나고, 프롭이 공중에 뜹니다. (P0-11)
      Assert.That(main.Terrain.transform.position.y, Is.EqualTo(-48f).Within(0.001f),
        "생성기가 오프셋을 설정하지 않았습니다. 픽스처는 의도적으로 12345에서 출발합니다.");
    }

    [Test]
    public void 오프셋은_높이에_따라_함께_변한다() {
      using var alt = new Fixture(totalHeight: 40, smoothen: false, withProps: false);
      alt.Run();
      Assert.That(alt.Terrain.transform.position.y, Is.EqualTo(-20f).Within(0.001f));
    }

    [Test]
    public void 해수면이_월드_원점_높이에_온다() {
      //? 위 두 성질의 목적입니다. 물 평면이 y=0에 놓여 있으므로 여기가 해안선이 됩니다.
      var t = main.Terrain;
      float seaLevel = t.transform.position.y + t.terrainData.size.y * 0.5f;
      Assert.That(seaLevel, Is.EqualTo(0f).Within(0.001f));
    }

    #endregion

    #region P0-9 — 스플랫맵이 실제로 기록된다

    [Test]
    public void 터레인_레이어_수가_가중치_표의_열_수와_같다() {
      //! 어긋나면 SetAlphamaps가 "Float array size wrong"으로 던집니다. (P0-9)
      Assert.That(main.Terrain.terrainData.alphamapLayers,
        Is.EqualTo(TerrainGenerator.BiomeWeights(Biome.Grassland).Length));
    }

    [Test]
    public void 스플랫맵_모든_셀의_가중치_합이_1이다() {
      //? 표의 각 행이 합 1이고, 디더링은 행을 교환하며 블러는 행들의 평균을 취합니다.
      //? 두 연산 모두 합을 보존하므로 이 성질은 스무딩 여부와 무관하게 성립해야 합니다.
      AssertAlphamapsSumToOne(main.Terrain.terrainData);
    }

    [Test]
    public void 스무딩을_켜도_가중치_합이_보존된다() {
      //! 블러가 버퍼를 한 방향으로만 대입해 2회차부터 별칭이 되던 경로입니다. (P2-1)
      using var smooth = new Fixture(totalHeight: 96, smoothen: true, withProps: false);
      smooth.Run();
      AssertAlphamapsSumToOne(smooth.Terrain.terrainData);
    }

    static void AssertAlphamapsSumToOne(TerrainData data) {
      int w = data.alphamapWidth, h = data.alphamapHeight, layers = data.alphamapLayers;
      var maps = data.GetAlphamaps(0, 0, w, h);

      for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
        float sum = 0;
        for (int l = 0; l < layers; l++) sum += maps[y, x, l];
        Assert.That(sum, Is.EqualTo(1f).Within(0.01f),
          $"({x}, {y}) 셀의 가중치 합이 {sum}입니다. 미지정 바이옴이 섞였거나 블러가 값을 잃었습니다.");
      }
    }

    [Test]
    public void 스플랫맵이_단일_레이어로_치우치지_않는다() {
      //? 예외로 죽어 예전 알파맵이 남는 경우를 잡습니다.
      //? 실제로 여러 바이옴이 배분되었다면 두 개 이상의 레이어가 유효 가중치를 갖습니다.
      var data = main.Terrain.terrainData;
      var maps = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);

      int used = 0;
      for (int l = 0; l < data.alphamapLayers; l++) {
        bool any = false;
        for (int y = 0; y < data.alphamapHeight && !any; y++)
          for (int x = 0; x < data.alphamapWidth && !any; x++)
            if (maps[y, x, l] > 0.01f) any = true;
        if (any) used++;
      }

      Assert.That(used, Is.GreaterThan(1),
        $"유효 가중치를 가진 레이어가 {used}개뿐입니다. 스플랫맵이 기록되지 않았을 수 있습니다.");
    }

    #endregion

    #region 하이트맵

    [Test]
    public void 하이트맵_값이_0과_1_범위_안에_있다() {
      var data = main.Terrain.terrainData;
      var heights = data.GetHeights(0, 0, main.MapWidth, main.MapWidth);

      foreach (var v in heights) {
        Assert.That(float.IsNaN(v), Is.False, "하이트맵에 NaN이 있습니다.");
        Assert.That(v, Is.InRange(0f, 1f));
      }
    }

    [Test]
    public void 하이트맵에_실제_기복이_있다() {
      //? 전부 같은 값이면 SetHeights가 아무것도 반영하지 못한 것입니다.
      var data = main.Terrain.terrainData;
      var heights = data.GetHeights(0, 0, main.MapWidth, main.MapWidth).Cast<float>().ToArray();

      float min = heights.Min(), max = heights.Max();
      Assert.That(max - min, Is.GreaterThan(0.1f),
        $"하이트맵 편차가 {max - min}입니다. 고도 데이터가 반영되지 않았습니다.");
    }

    [Test]
    public void 해수면_아래와_위가_모두_존재한다() {
      //? 정규화 기준(0.5 = 해수면)이 유지되는지 봅니다.
      var data = main.Terrain.terrainData;
      var heights = data.GetHeights(0, 0, main.MapWidth, main.MapWidth).Cast<float>().ToArray();

      Assert.That(heights.Any(v => v > 0.5f), Is.True, "해수면 위 지형이 없습니다.");
      Assert.That(heights.Any(v => v < 0.5f), Is.True, "해수면 아래 지형이 없습니다.");
    }

    #endregion

    #region P0-13 — 배치물이 지표면에 놓인다

    [Test]
    public void 프롭이_하나_이상_배치된다() {
      //? 아래 접지 테스트가 공허하게 참이 되는 것을 막습니다.
      Assert.That(main.PropParent.childCount, Is.GreaterThan(0));
    }

    [Test]
    public void 모든_프롭이_지표면_높이에_놓인다() {
      //! 배치는 GetWorldPos(터레인 위치 기준) + SampleHeight로 이뤄집니다.
      //! 배치 후 터레인을 옮기면 프롭만 남아 공중에 뜹니다. 실제로 43유닛 떠 있었습니다. (P0-13)
      var t = main.Terrain;
      float baseY = t.transform.position.y;

      foreach (Transform child in main.PropParent) {
        var p = child.position;
        float expected = baseY + t.SampleHeight(p);
        Assert.That(p.y, Is.EqualTo(expected).Within(0.05f),
          $"프롭이 지표면에서 {p.y - expected:F2}유닛 벗어났습니다. (위치 {p})");
      }
    }

    [Test]
    public void 모든_프롭이_터레인_경계_근처에_머문다() {
      //? 정확히 경계 안이라고 단정할 수는 없습니다.
      //? 배치는 GetWorldPos(pos, .2f)로 그리드 단위 0.2만큼 위치를 흩뜨리는데,
      //? 경계 셀(0,0)에서는 이 지터가 지형 밖으로 나갈 수 있습니다.
      //? 최대 이탈은 "0.2 x 셀 크기"이므로 그만큼을 허용 폭으로 둡니다.
      //! 이 테스트의 목적은 지터 검사가 아니라, 프롭이 (0,0,0)이나 엉뚱한 좌표로
      //! 쏟아지는 종류의 사고를 잡는 것입니다.
      var t = main.Terrain;
      var origin = t.transform.position;
      var size = t.terrainData.size;

      int gridSize = main.MapWidth / mainGen.gridScale;
      float cell = size.x / gridSize;
      float margin = 0.2f * cell + 0.01f;

      foreach (Transform child in main.PropParent) {
        var p = child.position;
        Assert.That(p.x, Is.InRange(origin.x - margin, origin.x + size.x + margin),
          $"프롭이 x 경계에서 허용 폭({margin:F2})을 넘어 벗어났습니다. ({p})");
        Assert.That(p.z, Is.InRange(origin.z - margin, origin.z + size.z + margin),
          $"프롭이 z 경계에서 허용 폭({margin:F2})을 넘어 벗어났습니다. ({p})");
      }
    }

    #endregion

    #region 프로젝트 에셋 — P0-9가 실제로 발생한 조건

    [Test]
    public void 프로젝트_터레인_데이터의_레이어_수가_충분하다() {
      //! P0-9의 실제 발생 조건입니다. 팔레트가 레이어 할당을 잃은 상태에서
      //! 미사용 에셋 정리가 돌아 유효 레이어가 2개로 줄었고, SetAlphamaps가 던졌습니다.
      //! 레이어 에셋을 다시 지우면 이 테스트가 먼저 실패합니다.
      var data = AssetDatabase.LoadAssetAtPath<TerrainData>(PROJECT_TERRAIN_DATA);
      Assert.That(data, Is.Not.Null, $"터레인 데이터를 찾지 못했습니다: {PROJECT_TERRAIN_DATA}");

      int required = TerrainGenerator.BiomeWeights(Biome.Grassland).Length;
      Assert.That(data.terrainLayers.Length, Is.GreaterThanOrEqualTo(required),
        $"레이어가 {data.terrainLayers.Length}개뿐입니다. 가중치 표는 {required}개를 요구합니다.");
      Assert.That(data.terrainLayers.Take(required).Any(l => l == null), Is.False,
        "앞쪽 레이어 중 비어 있는 슬롯이 있습니다. 참조가 끊긴 레이어 에셋이 있습니다.");
    }

    #endregion
  }
}
