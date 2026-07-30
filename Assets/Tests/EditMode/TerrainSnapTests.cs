using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

using Rair.EditorTools;

namespace Rair.Tests
{
  /// <summary>지표면 스냅 도구(<see cref="TerrainSnap"/>)의 성질 테스트.</summary>
  /// <remarks>
  /// <see cref="TerrainApplicationTests"/>와 달리 맵을 생성하지 않고
  /// <b>높이를 직접 지정한 인공 지형</b>을 씁니다.
  /// 검사할 것이 "생성 결과가 타당한가"가 아니라 "주어진 지형에 대해 계산이 맞는가"이므로,
  /// 지형을 통제하면 기대값을 손으로 계산해 쓸 수 있습니다.
  /// <para>
  /// <b>지형을 인수로 넘깁니다.</b> 열려 있는 씬에 이미 터레인이 있으면
  /// <see cref="Terrain.activeTerrains"/>에 둘이 잡히고, 둘 다 같은 좌표를 포함하므로
  /// 자동 탐색은 어느 쪽을 고를지 정해지지 않습니다.
  /// 그래서 <see cref="TerrainSnap.Snap"/>·<see cref="TerrainSnap.FindBuried"/>는
  /// 지형을 받는 매개변수를 두었습니다.
  /// </para>
  /// <b>씬 전체를 훑는 부분(<see cref="TerrainSnap.FindBuried"/>)은 여기서 검사하지 않습니다.</b>
  /// 대상 목록이 열려 있는 씬의 최상위 오브젝트라서 테스트가 통제할 수 없습니다.
  /// 빈 씬을 새로 열면 통제할 수 있지만 그러면 작업 중인 씬을 닫게 되므로,
  /// 판정의 실체인 <see cref="TerrainSnap.TryGetGap"/>의 부호와 크기를 대신 검사합니다.
  /// <para>
  /// <b>유효성 검증 — 변이 테스트.</b>
  /// <list type="table">
  ///   <item>
  ///     <term><c>SurfaceHeight</c>에서 터레인 y 오프셋 제거 (P0-11 재현)</term>
  ///     <description>7건 실패</description>
  ///   </item>
  ///   <item>
  ///     <term><c>GetBottom</c>이 항상 피벗을 반환</term>
  ///     <description>6건 실패</description>
  ///   </item>
  ///   <item>
  ///     <term><c>Contains</c>가 항상 <c>true</c></term>
  ///     <description>3건 실패</description>
  ///   </item>
  ///   <item>
  ///     <term><c>Undo.RecordObject</c> 제거</term>
  ///     <description>1건 실패</description>
  ///   </item>
  ///   <item>
  ///     <term><c>Undo.CollapseUndoOperations</c> 제거</term>
  ///     <description><b>0건 — 잡지 못했습니다</b></description>
  ///   </item>
  ///   <item>
  ///     <term>Undo 그룹 관리 전체 제거</term>
  ///     <description><b>0건 — 잡지 못했습니다</b></description>
  ///   </item>
  /// </list>
  /// 마지막 두 건은 테스트의 구멍이 아니라 <b>변이가 동작을 바꾸지 않았다</b>는 뜻입니다.
  /// 한 호출 안의 <c>RecordObject</c>들은 프레임 경계가 없어 이미 같은 그룹에 들어갑니다.
  /// 그룹 관리가 기여하는 것은 되돌리기 이력의 이름과 직전 편집과의 분리인데,
  /// 둘 다 에디터 UI에서만 보이는 것이라 테스트로 고정되지 않습니다.
  /// (<see cref="TerrainSnap.Snap"/> 주석 참조)
  /// </para>
  /// </remarks>
  public class TerrainSnapTests
  {
    /// <summary>인공 지형과 그 위에 놓을 오브젝트들을 만들고 정리합니다.</summary>
    sealed class Fixture : IDisposable
    {
      /// <summary>실제 지형 설정과 같은 값입니다. 해수면이 월드 y=0에 오는 배치를 재현합니다.</summary>
      public const int TOTAL_HEIGHT = 96;
      public const float WIDTH = 100f;
      const int RESOLUTION = 33;

      /// <summary>정규화 높이 0.5가 해수면이므로, 평지의 지표면 월드 y는 0입니다.</summary>
      public const float SEA_LEVEL_NORMALIZED = .5f;

      public readonly Terrain Terrain;
      readonly TerrainData data;
      readonly List<GameObject> spawned = new();

      /// <param name="height">
      /// 정규화 높이를 (x, y) 하이트맵 좌표로 주는 함수입니다. <c>null</c>이면 해수면 평지입니다.
      /// </param>
      public Fixture(Func<int, int, float> height = null) {
        //? 해상도를 먼저 정해야 합니다. 나중에 바꾸면 size가 초기화됩니다.
        data = new TerrainData { heightmapResolution = RESOLUTION };
        data.size = new(WIDTH, TOTAL_HEIGHT, WIDTH);

        var heights = new float[RESOLUTION, RESOLUTION];
        for (int y = 0; y < RESOLUTION; y++)
          for (int x = 0; x < RESOLUTION; x++)
            heights[y, x] = height?.Invoke(x, y) ?? SEA_LEVEL_NORMALIZED;
        data.SetHeights(0, 0, heights);

        var go = Terrain.CreateTerrainGameObject(data);
        go.name = "[Test] Terrain";
        go.transform.position = new(0, -TOTAL_HEIGHT / 2f, 0);
        Terrain = go.GetComponent<Terrain>();
        spawned.Add(go);
      }

      /// <summary>지표면의 월드 y입니다. 기대값을 손으로 계산할 때 씁니다.</summary>
      public float Surface(Vector3 worldPos) => TerrainSnap.SurfaceHeight(Terrain, worldPos);

      /// <summary>변 길이 <paramref name="size"/>의 큐브입니다. 피벗은 중심에 있습니다.</summary>
      public GameObject Cube(Vector3 pos, float size = 2f) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "[Test] Cube";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;
        spawned.Add(go);
        return go;
      }

      /// <summary>렌더러도 콜라이더도 없는 오브젝트입니다. 카메라·조명 같은 배치물을 대신합니다.</summary>
      public GameObject Formless(Vector3 pos) {
        var go = new GameObject("[Test] Formless");
        go.transform.position = pos;
        spawned.Add(go);
        return go;
      }

      public void Dispose() {
        //? 테스트가 만든 Undo 항목이 남으면, 파괴된 오브젝트를 참조하는 되돌리기가 스택에 쌓입니다.
        Undo.ClearAll();
        foreach (var go in spawned) if (go != null) UnityEngine.Object.DestroyImmediate(go);
        if (data != null) UnityEngine.Object.DestroyImmediate(data);
      }
    }

    /// <summary>가운데를 향해 x축으로 올라가는 경사입니다. 지점마다 지표 높이가 달라집니다.</summary>
    static float Ramp(int x, int y) => .5f + .25f * x / 32f;

    const float EPS = 1e-3f;

    #region 높이 계산
    [Test]
    public void 해수면_평지의_지표면이_월드_원점에_온다() {
      using var f = new Fixture();
      //? 정규화 0.5 × 높이 96 = 48, 터레인 원점 -48 → 0.
      Assert.That(f.Surface(new(50, 0, 50)), Is.EqualTo(0f).Within(EPS));
    }

    [Test]
    public void SurfaceHeight가_터레인_오프셋을_포함한다() {
      using var f = new Fixture(Ramp);
      var pos = new Vector3(50, 0, 50);

      //! SampleHeight만 쓰면 터레인을 내려 둔 만큼(-48) 어긋납니다. P0-11과 같은 실수입니다.
      float raw = f.Terrain.SampleHeight(pos);
      Assert.That(f.Surface(pos), Is.EqualTo(raw - Fixture.TOTAL_HEIGHT / 2f).Within(EPS),
        "지표면 높이에 터레인 원점의 y가 더해지지 않았습니다.");
    }

    [Test]
    public void 경사에서는_지점마다_지표_높이가_다르다() {
      using var f = new Fixture(Ramp);
      float low = f.Surface(new(5, 0, 50));
      float high = f.Surface(new(95, 0, 50));

      Assert.That(high, Is.GreaterThan(low + 1f),
        "경사 지형인데 두 지점의 지표 높이가 같습니다. 스냅이 일정한 값을 더하는 것으로 대체될 수 없는 이유입니다.");
    }

    [Test]
    public void Contains가_터레인_범위_밖을_거른다() {
      using var f = new Fixture();
      Assert.That(TerrainSnap.Contains(f.Terrain, new(50, 0, 50)), Is.True);
      Assert.That(TerrainSnap.Contains(f.Terrain, new(-1, 0, 50)), Is.False);
      Assert.That(TerrainSnap.Contains(f.Terrain, new(50, 0, Fixture.WIDTH + 1)), Is.False);
    }
    #endregion

    #region 밑면 판정
    [Test]
    public void 밑면은_피벗이_아니라_경계의_최저점이다() {
      using var f = new Fixture();
      var cube = f.Cube(new(50, 10, 50), size: 4f);

      float bottom = TerrainSnap.GetBottom(cube.transform, out bool fromBounds);
      Assert.That(fromBounds, Is.True);
      Assert.That(bottom, Is.EqualTo(10f - 2f).Within(EPS), "변 4인 큐브의 밑면은 피벗보다 2 아래입니다.");
    }

    [Test]
    public void 회전해도_경계의_최저점을_쓴다() {
      using var f = new Fixture();
      var cube = f.Cube(new(50, 10, 50), size: 2f);
      cube.transform.rotation = Quaternion.Euler(0, 0, 45);

      //? 45도 기울인 변 2의 정사각 단면은 대각선(√2)이 수직이 되어 반높이가 커집니다.
      float bottom = TerrainSnap.GetBottom(cube.transform, out _);
      Assert.That(bottom, Is.EqualTo(10f - Mathf.Sqrt(2f)).Within(.01f));
    }

    [Test]
    public void 자손의_형상까지_포함한다() {
      using var f = new Fixture();
      var parent = f.Formless(new(50, 10, 50));
      var child = f.Cube(new(50, 4, 50), size: 2f);
      child.transform.SetParent(parent.transform, worldPositionStays: true);

      float bottom = TerrainSnap.GetBottom(parent.transform, out bool fromBounds);
      Assert.That(fromBounds, Is.True, "자손에 렌더러가 있으면 형상이 있는 것으로 봐야 합니다.");
      Assert.That(bottom, Is.EqualTo(3f).Within(EPS), "부모의 피벗이 아니라 자식 큐브의 밑면이 기준입니다.");
    }

    [Test]
    public void 형상이_없으면_피벗으로_대체한다() {
      using var f = new Fixture();
      var go = f.Formless(new(50, 7, 50));

      float bottom = TerrainSnap.GetBottom(go.transform, out bool fromBounds);
      Assert.That(fromBounds, Is.False);
      Assert.That(bottom, Is.EqualTo(7f).Within(EPS));
    }
    #endregion

    #region 간격 판정
    [Test]
    public void 묻힌_것은_간격이_음수다() {
      using var f = new Fixture();
      var cube = f.Cube(new(50, -20, 50));

      Assert.That(TerrainSnap.TryGetGap(cube.transform, f.Terrain, out float gap), Is.True);
      Assert.That(gap, Is.Negative);
      Assert.That(gap, Is.EqualTo(-21f).Within(EPS), "밑면 -21, 지표 0 → 간격 -21.");
    }

    [Test]
    public void 뜬_것은_간격이_양수다() {
      using var f = new Fixture();
      var cube = f.Cube(new(50, 30, 50));

      Assert.That(TerrainSnap.TryGetGap(cube.transform, f.Terrain, out float gap), Is.True);
      Assert.That(gap, Is.EqualTo(29f).Within(EPS));
    }

    [Test]
    public void 범위_밖에서는_판정하지_않는다() {
      using var f = new Fixture();
      var cube = f.Cube(new(-50, 0, 50));

      Assert.That(TerrainSnap.TryGetGap(cube.transform, f.Terrain, out _), Is.False,
        "범위 밖에서도 SampleHeight는 값을 돌려주므로, 판정하면 가장자리 값으로 잘못 맞춥니다.");
    }
    #endregion

    #region 스냅
    [Test]
    public void 묻힌_오브젝트의_밑면이_지표에_닿는다() {
      using var f = new Fixture();
      var cube = f.Cube(new(50, -20, 50), size: 2f);

      var report = TerrainSnap.Snap(new[] { cube.transform }, f.Terrain);

      Assert.That(report.Moved, Is.EqualTo(1));
      Assert.That(TerrainSnap.GetBottom(cube.transform, out _),
        Is.EqualTo(f.Surface(cube.transform.position)).Within(EPS));
    }

    [Test]
    public void 스냅해도_XZ는_움직이지_않는다() {
      using var f = new Fixture(Ramp);
      var cube = f.Cube(new(37.5f, -30, 62.5f));
      var before = cube.transform.position;

      TerrainSnap.Snap(new[] { cube.transform }, f.Terrain);

      Assert.That(cube.transform.position.x, Is.EqualTo(before.x).Within(EPS));
      Assert.That(cube.transform.position.z, Is.EqualTo(before.z).Within(EPS));
      Assert.That(cube.transform.position.y, Is.Not.EqualTo(before.y));
    }

    [Test]
    public void 뜬_오브젝트는_지표까지_내려온다() {
      using var f = new Fixture();
      var cube = f.Cube(new(50, 40, 50));

      TerrainSnap.Snap(new[] { cube.transform }, f.Terrain);

      Assert.That(cube.transform.position.y, Is.LessThan(40f));
      Assert.That(TerrainSnap.TryGetGap(cube.transform, f.Terrain, out float gap), Is.True);
      Assert.That(Mathf.Abs(gap), Is.LessThanOrEqualTo(TerrainSnap.TOLERANCE));
    }

    [Test]
    public void 이미_맞닿은_것은_움직이지_않는다() {
      using var f = new Fixture();
      var cube = f.Cube(new(50, 1f, 50), size: 2f); // 밑면이 정확히 y=0
      var before = cube.transform.position;

      var report = TerrainSnap.Snap(new[] { cube.transform }, f.Terrain);

      Assert.That(report.Moved, Is.Zero);
      Assert.That(report.Skipped, Is.EqualTo(1));
      Assert.That(cube.transform.position, Is.EqualTo(before));
    }

    [Test]
    public void 경사에서는_같은_깊이라도_이동량이_다르다() {
      using var f = new Fixture(Ramp);
      var low = f.Cube(new(5, -40, 50));
      var high = f.Cube(new(95, -40, 50));

      TerrainSnap.Snap(new[] { low.transform, high.transform }, f.Terrain);

      Assert.That(high.transform.position.y, Is.GreaterThan(low.transform.position.y + 1f),
        "지점마다 이동량이 달라야 합니다. 일정한 값을 더하는 방식으로는 둘 중 하나가 반드시 틀립니다.");
    }

    [Test]
    public void 범위_밖_오브젝트는_스냅되지_않는다() {
      using var f = new Fixture();
      var outside = f.Cube(new(-50, -20, 50));
      var before = outside.transform.position;

      var report = TerrainSnap.Snap(new[] { outside.transform }, f.Terrain);

      Assert.That(report.Moved, Is.Zero);
      Assert.That(report.Skipped, Is.EqualTo(1));
      Assert.That(outside.transform.position, Is.EqualTo(before));
    }

    [Test]
    public void 형상이_없는_것은_피벗으로_집계된다() {
      using var f = new Fixture();
      var go = f.Formless(new(50, -20, 50));

      var report = TerrainSnap.Snap(new[] { go.transform }, f.Terrain);

      Assert.That(report.Moved, Is.EqualTo(1));
      Assert.That(report.Pivots, Is.EqualTo(1), "형상이 없는 것은 따로 알려야 합니다. 스냅 결과가 뜻대로인지 사람이 판단해야 합니다.");
      Assert.That(go.transform.position.y, Is.EqualTo(0f).Within(EPS));
    }

    [Test]
    public void 여러_개를_한_번에_스냅한다() {
      using var f = new Fixture(Ramp);
      var cubes = new[] {
        f.Cube(new(10, -30, 20)).transform,
        f.Cube(new(50, 25, 50)).transform,
        f.Cube(new(80, -5, 70)).transform,
      };

      var report = TerrainSnap.Snap(cubes, f.Terrain);

      Assert.That(report.Moved, Is.EqualTo(3));
      foreach (var t in cubes) {
        Assert.That(TerrainSnap.TryGetGap(t, f.Terrain, out float gap), Is.True);
        Assert.That(Mathf.Abs(gap), Is.LessThanOrEqualTo(TerrainSnap.TOLERANCE), $"{t.position}이 지표에 닿지 않았습니다.");
      }
    }

    /// <summary>여러 개를 옮긴 뒤 Ctrl+Z 한 번으로 전부 돌아와야 도구로서 쓸 수 있습니다.</summary>
    /// <remarks>
    /// <b>이 테스트가 고정하는 것은 그룹 관리가 아니라 <see cref="Undo.RecordObject"/> 호출의 존재입니다.</b>
    /// 변이 테스트로 확인했습니다 — <c>CollapseUndoOperations</c>를 지워도,
    /// <c>IncrementCurrentGroup</c>·<c>SetCurrentGroupName</c>까지 통째로 지워도 이 테스트는 통과합니다.
    /// 한 호출 안의 기록들은 프레임 경계가 없어 이미 같은 그룹에 들어가기 때문입니다.
    /// <c>RecordObject</c>를 지웠을 때만 실패합니다.
    /// <para>
    /// 그래도 검사할 값어치가 있습니다. 되돌릴 수 없는 일괄 이동 도구는 쓸 수 없고,
    /// <c>RecordObject</c>를 빠뜨리는 것이 에디터 도구에서 가장 흔한 누락입니다.
    /// 다만 <b>그룹 관리가 검사되고 있다고 착각하면 안 됩니다.</b>
    /// </para>
    /// </remarks>
    [Test]
    public void 스냅한_이동을_되돌릴_수_있다() {
      using var f = new Fixture();
      var cubes = new[] {
        f.Cube(new(20, -30, 20)).transform,
        f.Cube(new(60, -40, 60)).transform,
      };
      var before = new[] { cubes[0].position, cubes[1].position };

      TerrainSnap.Snap(cubes, f.Terrain);
      Undo.PerformUndo();

      for (int i = 0; i < cubes.Length; i++)
        Assert.That(cubes[i].position.y, Is.EqualTo(before[i].y).Within(EPS),
          "되돌리기가 이동을 취소하지 않았습니다. Undo.RecordObject 호출이 빠진 것입니다.");
    }
    #endregion
  }
}
