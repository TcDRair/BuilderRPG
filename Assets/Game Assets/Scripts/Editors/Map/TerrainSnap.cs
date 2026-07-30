using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using Rair.Field.Maps;

namespace Rair.EditorTools
{
  /// <summary>씬 오브젝트를 지형 표면에 맞추고, 지표 아래 묻힌 것을 찾습니다.</summary>
  /// <remarks>
  /// 문서 05 P0-13의 <b>근본 원인</b>에 대한 대응입니다.
  /// <para>
  /// 결함 자체는 지형 높이를 10에서 96으로 올리자 같은 좌표의 지표면이 최대 43유닛 올라가
  /// 씬 배치물이 지하에 묻힌 것이었습니다. 당시에는 <c>MapData</c>의 R 채널로 지표 높이를 계산해
  /// 오브젝트 7개를 손으로 올렸는데, 그 방식에는 두 가지 문제가 있습니다.
  /// <list type="bullet">
  ///   <item><description><b>일정한 값을 더하는 것은 답이 아닙니다.</b> 어긋난 양은 지점마다 다릅니다</description></item>
  ///   <item><description><b>다 찾았는지 알 수 없습니다.</b> 눈으로 훑는 방식이라 남은 것이 있어도 모릅니다</description></item>
  /// </list>
  /// 높이를 인스펙터로 노출한 이상 그 값은 앞으로도 바뀝니다.
  /// 바뀔 때마다 손으로 맞춰야 한다면 노출한 이점이 반감되므로 도구로 만들었습니다.
  /// </para>
  /// <b>묻힘만 보고하고 뜬 것은 보고하지 않습니다.</b>
  /// 지표 위에 있는 것은 대부분 정상입니다(카메라·조명·공중 배치물).
  /// 반면 지표 아래로 들어간 것은 그 자체로 거의 항상 결함입니다.
  /// 판정이 애매한 쪽을 목록에 섞으면 목록을 보지 않게 됩니다.
  /// <para>
  /// 이 도구는 <see cref="RandomTextureGenerator"/> 없이도 동작합니다.
  /// 씬의 <see cref="Terrain"/>을 직접 찾으므로 생성기가 없는 씬에서도 쓸 수 있고,
  /// 생성기는 생성된 프롭을 검사 대상에서 빼는 데에만 씁니다.
  /// </para>
  /// </remarks>
  public static class TerrainSnap
  {
    /// <summary>이 이하로 어긋난 것은 맞닿은 것으로 봅니다(월드 유닛).</summary>
    public const float TOLERANCE = .05f;

    const string UNDO_LABEL = "지표면 스냅";
    const string MENU = "Tools/Rair/지형/";

    #region 계산 — 테스트가 직접 호출합니다
    /// <summary>주어진 월드 좌표에서 지표면의 월드 y입니다.</summary>
    /// <remarks>
    /// <see cref="Terrain.SampleHeight"/>는 <b>터레인 원점 기준 상대 높이</b>를 돌려줍니다.
    /// 이 프로젝트는 해수면을 월드 y=0에 두려고 터레인을 <c>-totalHeight/2</c>로 내려 두었으므로
    /// (P0-11) 그 오프셋을 더하지 않으면 결과가 절반 높이만큼 어긋납니다.
    /// </remarks>
    public static float SurfaceHeight(Terrain terrain, Vector3 worldPos)
      => terrain.transform.position.y + terrain.SampleHeight(worldPos);

    /// <summary>월드 좌표가 터레인의 XZ 범위 안에 있는지 여부입니다.</summary>
    /// <remarks>
    /// 범위 밖에서도 <see cref="Terrain.SampleHeight"/>는 값을 돌려주지만
    /// 가장자리 값으로 고정된 것이라 의미가 없습니다. 그래서 먼저 걸러냅니다.
    /// </remarks>
    public static bool Contains(Terrain terrain, Vector3 worldPos) {
      var origin = terrain.transform.position;
      var size = terrain.terrainData.size;
      return worldPos.x >= origin.x && worldPos.x <= origin.x + size.x
          && worldPos.z >= origin.z && worldPos.z <= origin.z + size.z;
    }

    /// <summary>오브젝트가 지표에 닿아야 하는 지점의 월드 y입니다.</summary>
    /// <param name="fromBounds">
    /// 렌더러·콜라이더의 밑면을 쓴 경우 <c>true</c>, 피벗으로 대체한 경우 <c>false</c>입니다.
    /// </param>
    /// <remarks>
    /// 피벗이 아니라 <b>밑면</b>을 기준으로 삼습니다. 피벗 위치는 모델링 쪽 사정이라
    /// 중심에 있기도 하고 발밑에 있기도 한데, "땅에 닿는다"는 어느 쪽이든 밑면의 이야기입니다.
    /// <para>
    /// 카메라·빈 오브젝트처럼 형상이 없는 것은 밑면이 없으므로 피벗을 씁니다.
    /// 이 경우 스냅 결과는 피벗이 지표에 놓이는 것이라, 뜻대로인지 호출한 쪽이 판단해야 합니다.
    /// </para>
    /// </remarks>
    public static float GetBottom(Transform target, out bool fromBounds) {
      var bounds = GetFormBounds(target);
      fromBounds = bounds.HasValue;
      return fromBounds ? bounds.Value.min.y : target.position.y;
    }

    /// <summary>자신과 자손의 형상을 감싸는 월드 AABB입니다. 형상이 없으면 <c>null</c>입니다.</summary>
    static Bounds? GetFormBounds(Transform target) {
      Bounds? result = null;

      //? 파티클·트레일·라인은 경계가 런타임 상태에 따라 변하므로 형상으로 보지 않습니다.
      foreach (var r in target.GetComponentsInChildren<Renderer>(false)) {
        if (r is ParticleSystemRenderer or TrailRenderer or LineRenderer) continue;
        if (r.bounds.size == Vector3.zero) continue;
        result = result.HasValue ? Encapsulated(result.Value, r.bounds) : r.bounds;
      }
      if (result.HasValue) return result;

      //? 렌더러가 없는 충돌체만의 오브젝트(트리거 볼륨 등)도 배치물입니다.
      foreach (var c in target.GetComponentsInChildren<Collider>(false)) {
        if (c.bounds.size == Vector3.zero) continue;
        result = result.HasValue ? Encapsulated(result.Value, c.bounds) : c.bounds;
      }
      return result;
    }

    static Bounds Encapsulated(Bounds a, Bounds b) { a.Encapsulate(b); return a; }

    /// <summary>오브젝트 밑면과 지표면 사이의 간격입니다. 양수는 떠 있음, 음수는 묻힘입니다.</summary>
    /// <returns>터레인 범위 밖이면 <c>false</c>이고 <paramref name="gap"/>은 정의되지 않습니다.</returns>
    public static bool TryGetGap(Transform target, Terrain terrain, out float gap) {
      gap = 0;
      if (terrain == null || !Contains(terrain, target.position)) return false;
      gap = GetBottom(target, out _) - SurfaceHeight(terrain, target.position);
      return true;
    }
    #endregion

    #region 대상 찾기
    /// <summary>월드 좌표를 담는 터레인, 없으면 XZ 거리가 가장 가까운 터레인입니다.</summary>
    public static Terrain FindTerrain(Vector3 worldPos) {
      var terrains = Terrain.activeTerrains;
      if (terrains == null || terrains.Length == 0) return null;

      return terrains.FirstOrDefault(t => Contains(t, worldPos))
          ?? terrains.OrderBy(t => Vector2.Distance(
               new(worldPos.x, worldPos.z),
               new(t.transform.position.x + t.terrainData.size.x / 2,
                   t.transform.position.z + t.terrainData.size.z / 2))).First();
    }

    /// <summary>검사 대상이 되는 씬 최상위 오브젝트들입니다.</summary>
    /// <remarks>
    /// 최상위만 봅니다. 배치 단위는 최상위 오브젝트이고,
    /// 자손까지 훑으면 메시 하나하나가 따로 보고되어 목록이 쓸 수 없게 됩니다.
    /// <para>
    /// 세 부류를 제외합니다.
    /// <list type="bullet">
    ///   <item><description><b>지형 자신</b> — 자기 표면에 스냅한다는 것은 의미가 없습니다</description></item>
    ///   <item><description><b>생성된 프롭</b> — 생성기가 이미 지표에 놓고, 다시 생성하면 덮어씁니다</description></item>
    ///   <item><description><b>UI</b> — <see cref="RectTransform"/>의 좌표는 월드 공간이 아닙니다</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    static IEnumerable<Transform> AuditTargets() {
      var propParent = FindPropParent();

      foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) {
        var tr = root.transform;
        if (tr is RectTransform) continue;
        if (root.GetComponentInChildren<Terrain>(true) != null) continue;
        if (propParent != null && (tr == propParent || tr.IsChildOf(propParent))) continue;
        yield return tr;
      }
    }

    static Transform FindPropParent() {
      //? ?? 를 쓰면 안 됩니다. 파괴된 UnityEngine.Object는 null 병합 연산자를 통과합니다.
      //? Unity가 재정의한 == 만이 그 상태를 null로 봅니다.
      var generator = RandomTextureGenerator.Instance;
      if (generator == null) generator = Object.FindAnyObjectByType<RandomTextureGenerator>();
      return generator == null ? null : generator.terrainVariables.propParent;
    }
    #endregion

    #region 동작 — 메뉴와 테스트가 공유합니다
    /// <summary><see cref="Snap"/>의 결과 집계입니다.</summary>
    public readonly struct SnapReport
    {
      /// <summary>실제로 옮긴 개수입니다.</summary>
      public readonly int Moved;
      /// <summary>이미 맞닿아 있거나 터레인 범위 밖이라 건너뛴 개수입니다.</summary>
      public readonly int Skipped;
      /// <summary>형상이 없어 피벗을 기준으로 삼은 개수입니다.</summary>
      public readonly int Pivots;

      public SnapReport(int moved, int skipped, int pivots) {
        (Moved, Skipped, Pivots) = (moved, skipped, pivots);
      }
    }

    /// <summary>주어진 오브젝트들의 밑면을 지표면에 맞춥니다.</summary>
    /// <param name="on">
    /// 맞출 대상 지형입니다. <c>null</c>이면 오브젝트 위치마다 <see cref="FindTerrain"/>으로 찾습니다.
    /// </param>
    /// <remarks>
    /// <see cref="Selection"/>이 아니라 인수를 받습니다.
    /// 메뉴는 선택 목록을 넘기고, 테스트는 자기가 만든 오브젝트를 넘깁니다.
    /// 선택 상태에 의존하면 테스트가 에디터 UI 타이밍에 묶입니다.
    /// <para>
    /// <b>Ctrl+Z 한 번으로 전부 되돌아가는 것은 그룹 관리 덕분이 아닙니다.</b>
    /// 한 호출 안의 <see cref="Undo.RecordObject"/>들은 프레임 경계가 없어 이미 같은 그룹에 들어갑니다.
    /// (<see cref="Undo.CollapseUndoOperations"/>를 지워도, 그룹 관리를 통째로 지워도 동작이 같습니다.
    ///  테스트 변이로 확인했습니다.)
    /// 여기서 그룹을 다루는 이유는 다른 두 가지입니다.
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="Undo.IncrementCurrentGroup"/> — 직전에 하던 편집이 같은 그룹에 섞여
    ///     되돌리기 한 번에 스냅과 무관한 변경까지 취소되는 것을 막습니다
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="Undo.SetCurrentGroupName"/> — 되돌리기 이력에 "지표면 스냅"으로 남습니다.
    ///     그러지 않으면 "Move"나 "Inspector"로 남아 무엇을 되돌리는지 알 수 없습니다
    ///   </description></item>
    /// </list>
    /// 둘 다 에디터 UI에서만 확인할 수 있어 테스트로 고정되지 않습니다.
    /// </para>
    /// </remarks>
    public static SnapReport Snap(IEnumerable<Transform> targets, Terrain on = null) {
      //? 먼저 그룹을 넘겨야 합니다. 그러지 않으면 직전에 하던 작업이 같은 그룹에 들어가
      //? 되돌리기 한 번에 스냅과 무관한 변경까지 취소됩니다.
      Undo.IncrementCurrentGroup();
      Undo.SetCurrentGroupName(UNDO_LABEL);
      int group = Undo.GetCurrentGroup();

      int moved = 0, skipped = 0, pivots = 0;
      foreach (var target in targets) {
        var terrain = on != null ? on : FindTerrain(target.position);
        if (terrain == null || !Contains(terrain, target.position)) { skipped++; continue; }

        float bottom = GetBottom(target, out bool fromBounds);
        if (!fromBounds) pivots++;

        float delta = SurfaceHeight(terrain, target.position) - bottom;
        if (Mathf.Abs(delta) <= TOLERANCE) { skipped++; continue; }

        Undo.RecordObject(target, UNDO_LABEL);
        target.position += Vector3.up * delta;
        moved++;
      }

      //? RecordObject의 변경 내역은 프레임 경계에서 정리됩니다.
      //? 이어서 바로 묶으려면 직접 밀어내야 합니다.
      Undo.FlushUndoRecordObjects();
      Undo.CollapseUndoOperations(group);
      return new(moved, skipped, pivots);
    }

    /// <summary>씬에서 지표 아래로 묻힌 배치물을 찾습니다. 깊은 것부터 정렬됩니다.</summary>
    /// <param name="outside">터레인 범위 밖이라 판정하지 않은 개수입니다.</param>
    /// <param name="on"><inheritdoc cref="Snap" path="/param[@name='on']"/></param>
    public static List<(Transform target, float depth)> FindBuried(out int outside, Terrain on = null) {
      var buried = new List<(Transform, float)>();
      outside = 0;

      foreach (var target in AuditTargets()) {
        var terrain = on != null ? on : FindTerrain(target.position);
        if (terrain == null) break;
        if (!TryGetGap(target, terrain, out float gap)) { outside++; continue; }
        if (gap >= -TOLERANCE) continue;
        buried.Add((target, -gap));
      }

      buried.Sort((a, b) => b.Item2.CompareTo(a.Item2));
      return buried;
    }
    #endregion

    #region 메뉴
    [MenuItem(MENU + "선택 오브젝트를 지표면에 스냅", true)]
    static bool CanSnapSelection() => Selection.transforms.Length > 0;

    [MenuItem(MENU + "선택 오브젝트를 지표면에 스냅")]
    static void SnapSelectionMenu() {
      //? Selection.transforms는 최상위만 돌려줍니다.
      //? 부모와 자식이 함께 선택된 경우 부모만 옮겨야 이중 이동이 없습니다.
      var targets = Selection.transforms;
      if (targets.Length == 0) return;
      if (Terrain.activeTerrains.Length == 0) {
        Debug.LogWarning("[Snap] 씬에 활성 터레인이 없습니다.");
        return;
      }

      var report = Snap(targets);
      var detail = report.Pivots > 0 ? $" (형상이 없어 피벗을 쓴 것 {report.Pivots}개)" : "";
      Debug.Log($"[Snap] {report.Moved}개를 지표면에 맞췄습니다. " +
                $"이미 맞았거나 범위 밖인 것 {report.Skipped}개.{detail}");
    }

    [MenuItem(MENU + "묻힌 오브젝트 검사")]
    static void AuditBuriedMenu() {
      if (Terrain.activeTerrains.Length == 0) {
        Debug.LogWarning("[Snap] 씬에 활성 터레인이 없습니다.");
        return;
      }

      var buried = FindBuried(out int outside);
      if (buried.Count == 0) {
        Debug.Log($"[Snap] 지표 아래로 묻힌 오브젝트가 없습니다. (범위 밖 {outside}개는 검사하지 않았습니다.)");
        return;
      }

      //? 로그에 문맥 오브젝트를 넘기면 콘솔 항목을 클릭해 씬에서 찾아갈 수 있습니다.
      foreach (var (target, depth) in buried)
        Debug.LogWarning($"[Snap] {target.name} — 지표 아래 {depth:F2}유닛", target);

      //? 검사 결과를 그대로 선택 상태로 만들어 스냅 메뉴로 이어지게 합니다.
      Selection.objects = buried.Select(b => (Object)b.target.gameObject).ToArray();

      Debug.Log($"[Snap] 묻힌 오브젝트 {buried.Count}개를 선택했습니다. " +
                $"범위 밖 {outside}개는 검사하지 않았습니다.\n" +
                $"바로 고치려면 {MENU}선택 오브젝트를 지표면에 스냅 을 실행하십시오.");
    }
    #endregion
  }
}
