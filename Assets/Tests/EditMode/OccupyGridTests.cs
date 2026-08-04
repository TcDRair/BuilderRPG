using System.Linq;
using NUnit.Framework;
using UnityEngine;

using Rair.Field.Values;

namespace Rair.Tests
{
  /// <summary>
  /// <see cref="Occupy"/>의 의미와 <see cref="OccupyGrid"/>의 축소 규칙을 고정합니다.
  /// </summary>
  /// <remarks>
  /// 보완 기록 P1-7 처리 결과입니다.
  /// <b>플래그가 서 있으면 그만큼 이미 차 있다</b>는 뜻으로 통일했습니다.
  /// <para>
  /// 이 값을 실제로 소비하는 코드가 아직 없어(건축 시스템이 빠져 있음)
  /// 회귀를 잡을 다른 수단이 없습니다. 그래서 의미 자체를 테스트로 못박습니다.
  /// </para>
  /// </remarks>
  public class OccupyGridTests
  {
    /// <summary>
    /// <see cref="OccupyGrid"/>는 생성자에서 <see cref="Terrain"/>의 transform과
    /// terrainData를 읽으므로, 최소한의 실물이 필요합니다.
    /// </summary>
    static Terrain CreateTerrain(out GameObject go) {
      go = new GameObject("TestTerrain");
      var terrain = go.AddComponent<Terrain>();
      terrain.terrainData = new TerrainData { heightmapResolution = 33, size = new Vector3(64, 10, 64) };
      return terrain;
    }

    static void Destroy(GameObject go, Terrain terrain) {
      if (terrain != null && terrain.terrainData != null) Object.DestroyImmediate(terrain.terrainData);
      if (go != null) Object.DestroyImmediate(go);
    }

    #region 의미

    [Test]
    public void 비어_있음은_None이고_완전_점유는_FULL이다() {
      //? 이름이 뜻을 배반하지 않는지 봅니다. None은 0이어야 하고 FULL은 모든 하위 비트를 덮어야 합니다.
      Assert.That((short)Occupy.None, Is.EqualTo(0));
      Assert.That(Occupy.FULL.HasFlag(Occupy.Floor), Is.True);
      Assert.That(Occupy.FULL.HasFlag(Occupy.Ceiling), Is.True);
      Assert.That(Occupy.FULL.HasFlag(Occupy.WallN | Occupy.WallE | Occupy.WallS | Occupy.WallW), Is.True);
      Assert.That(Occupy.FULL.HasFlag(Occupy.Inside), Is.True);
      Assert.That(Occupy.FULL.HasFlag(Occupy.Other), Is.True);
    }

    [Test]
    public void 범위_밖은_완전_점유로_취급한다() {
      //? 보수적 기준 — 모르는 곳에는 짓지 않습니다.
      var terrain = CreateTerrain(out var go);
      try {
        var grid = new OccupyGrid(terrain, 4, Enumerable.Repeat(Occupy.None, 16));

        Assert.That(grid[new Vector2Int(0, 0)], Is.EqualTo(Occupy.None), "범위 안은 데이터를 그대로 돌려줘야 합니다.");
        Assert.That(grid[new Vector2Int(-1, 0)], Is.EqualTo(Occupy.FULL));
        Assert.That(grid[new Vector2Int(0, 99)], Is.EqualTo(Occupy.FULL));
      }
      finally { Destroy(go, terrain); }
    }

    #endregion

    #region 축소 변환

    [Test]
    public void 축소하면_포함된_칸들의_합집합이_된다() {
      //! 한때 `&=`로 누적해 결과가 항상 비어 있었습니다(보완 기록 8절).
      //! "하나라도 차 있으면 찬 것"이 범위 밖을 FULL로 두는 기준과 일관됩니다.
      var terrain = CreateTerrain(out var go);
      try {
        //? 4x4를 2x2로 줄입니다. 좌상단 2x2 블록에만 한 칸 Floor를 둡니다.
        var data = Enumerable.Repeat(Occupy.None, 16).ToArray();
        data[0] = Occupy.Floor;
        data[5] = Occupy.WallN; // (1,1) — 역시 좌상단 블록

        var grid = new OccupyGrid(terrain, 4, data, 2);

        Assert.That(grid.Size, Is.EqualTo(2));
        Assert.That(grid.Grid[0], Is.EqualTo(Occupy.Floor | Occupy.WallN), "두 플래그가 합쳐져야 합니다.");
        Assert.That(grid.Grid[1], Is.EqualTo(Occupy.None), "빈 블록은 비어 있어야 합니다.");
      }
      finally { Destroy(go, terrain); }
    }

    [Test]
    public void 축소해도_빈_칸만_모이면_비어_있다() {
      var terrain = CreateTerrain(out var go);
      try {
        var grid = new OccupyGrid(terrain, 4, Enumerable.Repeat(Occupy.None, 16), 2);
        Assert.That(grid.Grid.All(g => g == Occupy.None), Is.True);
      }
      finally { Destroy(go, terrain); }
    }

    #endregion

    #region 지형 → 점유 변환 (MapOverlay가 쓰는 규칙)

    //? MapOverlay.InitGrid의 변환을 그대로 옮긴 것입니다.
    //? MonoBehaviour를 EditMode에서 세우기 어려워 규칙만 검사합니다.
    static Occupy FromLandAlpha(float alpha) => alpha > .5f ? Occupy.None : Occupy.FULL;

    [Test]
    public void 육지는_비어_있고_바다는_완전_점유다() {
      //! 알파 채널은 land입니다 (육지 1, 바다 0).
      //! 예전에는 (Occupy)p.a로 그대로 캐스트해 육지가 Floor(1), 바다가 None(0)이 되었고,
      //! 그 결과 물 위가 건축 가능으로 읽혔습니다.
      Assert.That(FromLandAlpha(1f), Is.EqualTo(Occupy.None), "육지에는 지을 수 있어야 합니다.");
      Assert.That(FromLandAlpha(0f), Is.EqualTo(Occupy.FULL), "바다에는 지을 수 없어야 합니다.");
    }

    [Test]
    public void 경계값은_바다_쪽으로_기운다() {
      //? 폴리곤 경계에서 알파가 보간될 수 있습니다. 애매하면 짓지 못하는 쪽이 안전합니다.
      Assert.That(FromLandAlpha(.5f), Is.EqualTo(Occupy.FULL));
      Assert.That(FromLandAlpha(.51f), Is.EqualTo(Occupy.None));
    }

    #endregion
  }
}
