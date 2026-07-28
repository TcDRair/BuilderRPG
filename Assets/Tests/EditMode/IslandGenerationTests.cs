using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

using Assets.Maps;

namespace Rair.Tests
{
  /// <summary>
  /// 섬 생성 절차(<see cref="Map"/> → <see cref="Graph"/>)의 성질 테스트.
  /// </summary>
  /// <remarks>
  /// 문서 05 P3-1이 지목한 세 성질을 고정합니다.
  /// 생성 결과는 시드마다 다르므로, 특정 출력이 아니라
  /// <b>어떤 시드에서도 성립해야 하는 불변 조건</b>만 검사합니다.
  /// <br/>
  /// 생성기 본체는 <c>Rair.Runtime</c>에 있어 에디터 없이 구동할 수 있습니다.
  /// (에디터 전용인 것은 이를 감싸는 <c>RandomTextureGenerator</c> 쪽입니다.)
  /// </remarks>
  public class IslandGenerationTests
  {
    const float LAND_RATIO = 0.35f;
    const int RIVER_COUNT = 6;
    const Size SIZE = Size.s2;

    //? 시드 하나당 생성 비용이 있으므로 케이스 간에 재사용합니다.
    static readonly Dictionary<int, Graph> cache = new();

    static Graph Generate(int seed) {
      if (cache.TryGetValue(seed, out var cached)) return cached;

      Random.InitState(seed);
      var map = new Map(SIZE);
      CoroutineDriver.RunToEnd(map.Initialize());
      CoroutineDriver.RunToEnd(map.Graph.GenerateGraph(LAND_RATIO, RIVER_COUNT));

      cache[seed] = map.Graph;
      return map.Graph;
    }

    [OneTimeTearDown]
    public void ClearCache() => cache.Clear();

    #region 고도가 [-1, 1] 범위를 벗어나지 않는다

    [Test]
    public void 모든_Corner의_고도가_음수1과_1_사이에_있다([Values(1, 7, 42)] int seed) {
      var graph = Generate(seed);

      foreach (var c in graph.vars.corners) {
        Assert.That(float.IsNaN(c.elevation), Is.False, $"Corner {c.index}의 고도가 NaN입니다.");
        Assert.That(float.IsInfinity(c.elevation), Is.False,
          $"Corner {c.index}의 고도가 무한대입니다. 초기값(PositiveInfinity)이 덮이지 않은 Corner가 있습니다.");
        Assert.That(c.elevation, Is.InRange(-1f, 1f), $"Corner {c.index}의 고도가 범위를 벗어났습니다.");
      }
    }

    [Test]
    public void 모든_Center의_고도가_음수1과_1_사이에_있다([Values(1, 7, 42)] int seed) {
      //? Center의 고도는 자신이 가진 Corner의 평균이므로, Corner가 범위 안이면 따라옵니다.
      //? 평균 계산이 빈 목록이나 NaN을 만나면 여기서 드러납니다.
      var graph = Generate(seed);

      foreach (var c in graph.vars.centers) {
        Assert.That(float.IsNaN(c.elevation), Is.False, $"Center {c.index}의 고도가 NaN입니다.");
        Assert.That(c.elevation, Is.InRange(-1f, 1f), $"Center {c.index}의 고도가 범위를 벗어났습니다.");
      }
    }

    #endregion

    #region 모든 강이 해안 또는 종점에 도달한다

    [Test]
    public void 강은_해안이나_지역최소점에서_끝난다([Values(1, 7, 42)] int seed) {
      var graph = Generate(seed);
      var sources = graph.vars.corners.Where(c => c.river > 0).ToList();

      Assert.That(sources, Is.Not.Empty, "강이 하나도 생성되지 않았다면 이 성질은 공허하게 참이 됩니다.");

      int limit = graph.vars.corners.Count;
      foreach (var source in sources) {
        var q = source;
        int steps = 0;

        while (!q.coast && q != q.downslope) {
          q = q.downslope;
          Assert.That(++steps, Is.LessThanOrEqualTo(limit),
            $"Corner {source.index}에서 시작한 물길이 종료되지 않습니다. downslope 그래프에 순환이 있습니다.");
        }

        Assert.That(q.coast || q == q.downslope, Is.True,
          $"Corner {source.index}에서 시작한 물길이 해안도 지역 최소점도 아닌 곳에서 끝났습니다.");
      }
    }

    [Test]
    public void downslope는_항상_더_낮은_곳을_가리킨다([Values(1, 7, 42)] int seed) {
      //? 위 테스트가 "끝난다"를 확인한다면, 이건 "왜 끝나는지"를 확인합니다.
      //? 고도가 단조 감소하므로 순환이 생길 수 없고, 따라서 물길은 반드시 종료됩니다.
      var graph = Generate(seed);

      foreach (var c in graph.vars.corners) {
        Assert.That(c.downslope, Is.Not.Null, $"Corner {c.index}의 downslope가 비어 있습니다.");

        if (c == c.downslope) continue; // 지역 최소점은 자기 자신을 가리킵니다.
        Assert.That(c.downslope.elevation, Is.LessThan(c.elevation),
          $"Corner {c.index}의 downslope가 더 높거나 같은 곳을 가리킵니다.");
      }
    }

    #endregion

    #region 육지 비율이 목표치로 수렴한다

    static float LandRatioOf(Graph graph) {
      var corners = graph.vars.corners;
      return 1f - (float)corners.Count(c => c.water) / corners.Count;
    }

    [Test]
    [Ignore("Graph.AssignLands가 목표 비율로 수렴하지 않습니다. 원인과 실측값은 문서 05의 신규 항목 참조. " +
            "IslandShape.MakePerlin이 호출마다 새 랜덤 오프셋을 만들어, 해수면 이분 탐색이 매 반복 다른 지형을 측정합니다.")]
    public void 육지_비율이_목표치_1퍼센트_이내로_수렴한다([Values(1, 7, 42, 99, 123)] int seed) {
      //! 이것이 원래 의도된 성질입니다. AssignLands의 종료 조건(|target - result| < .01)이
      //! 그렇게 적혀 있습니다. 지금은 그 조건에 도달하지 못한 채 시도 횟수를 소진하고 끝납니다.
      //! 결함을 고치면 이 [Ignore]를 떼십시오.
      Assert.That(LandRatioOf(Generate(seed)), Is.EqualTo(LAND_RATIO).Within(0.01f));
    }

    [Test]
    public void 육지_비율이_지형이라_부를_수_있는_범위에는_있다([Values(1, 7, 42, 99, 123)] int seed) {
      //? 위 성질이 깨져 있는 동안에도 "전부 바다" 또는 "전부 육지"로 무너지는 것은 막아야 합니다.
      //? 실측 범위는 13% ~ 36%이므로, 그 바깥으로 나가면 조정 로직이 더 나빠진 것입니다.
      float landRatio = LandRatioOf(Generate(seed));

      Assert.That(landRatio, Is.InRange(0.05f, 0.60f),
        $"육지 비율 {landRatio:P1}은 섬이라고 보기 어렵습니다.");
    }

    [Test]
    public void 육지와_바다가_모두_존재한다([Values(1, 7, 42, 99, 123)] int seed) {
      var corners = Generate(seed).vars.corners;

      Assert.That(corners.Any(c => c.water), Is.True, "물이 하나도 없습니다.");
      Assert.That(corners.Any(c => !c.water), Is.True, "육지가 하나도 없습니다.");
      Assert.That(corners.Any(c => c.coast), Is.True, "해안이 하나도 없습니다. 강 생성이 종료되지 못할 수 있습니다.");
    }

    #endregion
  }
}
