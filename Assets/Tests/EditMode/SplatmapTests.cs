using System;
using System.Linq;
using NUnit.Framework;

using Assets.Maps;
using Rair.Field.Maps;

namespace Rair.Tests
{
  /// <summary>
  /// 바이옴 → 터레인 레이어 가중치 표(<see cref="TerrainGenerator.BiomeWeights"/>)의 불변 조건.
  /// </summary>
  /// <remarks>
  /// 문서 05 P3-1의 마지막 항목입니다.
  /// 원래는 수동으로 검산했고, 그래서 `TODO` 주석이 몇 달간 남아 있었습니다.
  /// <br/>
  /// 이 테스트는 <see cref="Biome"/> 열거형을 직접 순회하므로,
  /// <b>바이옴을 새로 추가하고 가중치 행을 빠뜨리면 자동으로 실패합니다.</b>
  /// (기본값이 전부 0이라 합이 1이 되지 않기 때문)
  /// 표를 손으로 세는 것과 달리 이 성질은 시간이 지나도 유지됩니다.
  /// </remarks>
  public class SplatmapTests
  {
    const int LAYER_COUNT = 8;
    const float EPS = 1e-4f;

    static Biome[] AllBiomes => (Biome[])Enum.GetValues(typeof(Biome));

    [Test]
    public void 모든_바이옴의_가중치_합이_1이다() {
      foreach (var biome in AllBiomes) {
        var weights = TerrainGenerator.BiomeWeights(biome);
        Assert.That(weights.Sum(), Is.EqualTo(1f).Within(EPS),
          $"{biome}의 가중치 합이 1이 아닙니다. 표에 행이 누락되었거나 값이 잘못되었습니다: [{string.Join(", ", weights)}]");
      }
    }

    [Test]
    public void 모든_바이옴의_가중치_개수가_터레인_레이어_수와_같다() {
      foreach (var biome in AllBiomes)
        Assert.That(TerrainGenerator.BiomeWeights(biome).Length, Is.EqualTo(LAYER_COUNT),
          $"{biome}의 가중치 개수가 터레인 레이어 수({LAYER_COUNT})와 다릅니다.");
    }

    [Test]
    public void 가중치에_음수가_없다() {
      foreach (var biome in AllBiomes)
        foreach (var w in TerrainGenerator.BiomeWeights(biome))
          Assert.That(w, Is.GreaterThanOrEqualTo(0f), $"{biome}에 음수 가중치가 있습니다.");
    }

    [Test]
    public void 정의되지_않은_바이옴은_전부_0을_반환한다() {
      //? switch의 기본 분기입니다. 합이 1이 아니므로 위 테스트의 탐지 수단이 됩니다.
      var undefined = (Biome)9999;
      var weights = TerrainGenerator.BiomeWeights(undefined);

      Assert.That(weights.Length, Is.EqualTo(LAYER_COUNT));
      Assert.That(weights.All(w => w == 0f), Is.True, "미지정 바이옴은 전부 0이어야 합니다.");
    }
  }
}
