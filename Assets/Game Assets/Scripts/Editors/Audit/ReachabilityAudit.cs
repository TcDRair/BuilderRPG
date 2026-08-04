using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Rair.EditorTools
{
  /// <summary>
  /// 씬·빌드 설정에서 출발해 참조를 따라가며, 어디서도 닿지 않는 에셋을 찾습니다.
  /// </summary>
  /// <remarks>
  /// 2026-07 GUID 유실 사고 때 쓰고 버린 "도달 가능성 BFS"를 상시 도구로 되살린 것입니다.
  /// (보완 기록 P3-7의 나머지 절반)
  /// <br/>
  /// <b>결과는 삭제 목록이 아니라 조사 대상 목록입니다.</b>
  /// Unity에서 "참조되지 않음"과 "쓰이지 않음"은 다릅니다.
  /// <see cref="Resources"/> 폴더는 경로 문자열로 로드되고, 에디터 도구·셰이더 변형·
  /// 어드레서블 등은 정적 참조 없이도 쓰입니다. 그래서 이 도구는 그런 것들을
  /// 애초에 도달 가능한 뿌리로 취급합니다.
  /// </remarks>
  public static class ReachabilityAudit
  {
    const string OUTPUT = "UNREACHABLE-ASSETS.md";

    /// <summary>참조가 없어도 도달 가능한 것으로 보는 경로.</summary>
    /// <remarks>
    /// 여기 들어간 것은 "쓰이는 게 확실해서"가 아니라
    /// <b>"정적 참조로는 판정할 수 없어서"</b>입니다.
    /// </remarks>
    static readonly (string prefix, string reason)[] ImplicitRoots = {
      ("Assets/Resources/",           "Resources.Load 경로 로드"),
      ("Assets/Editor/",              "에디터 도구"),
      ("Assets/Imported Assets/",     "벤더 에셋 — 우리가 관리하지 않음"),
      ("Assets/Samples/",             "패키지 샘플"),
      ("Assets/TextMesh Pro/",        "패키지 리소스"),
      ("Assets/Dagger's-AssetCleaner/", "서드파티 도구"),
    };

    /// <summary>에셋이 아니거나 판정 대상이 아닌 확장자.</summary>
    /// <remarks>
    /// <c>.cs</c>가 여기 있는 이유 — <b>스크립트의 사용 여부는 컴파일이 정합니다.</b>
    /// 에셋 참조로 보면 씬·프리팹에 컴포넌트로 붙은 MonoBehaviour만 "도달"이고,
    /// 확장 메서드·정적 클래스·데이터 타입은 전부 미참조로 잡힙니다.
    /// 실제로 이 항목을 빼기 전에는 123건 중 92건이 MonoScript였습니다.
    /// 죽은 코드를 찾으려면 이 도구가 아니라 컴파일러 경고나 IDE 분석을 쓰십시오.
    /// </remarks>
    static readonly HashSet<string> Ignored = new() {
      ".meta", ".asmdef", ".asmref", ".md", ".txt", ".json", ".xml", ".cs"
    };

    public sealed class Unreachable
    {
      public string path;
      public string type;
      public long bytes;
    }

    /// <summary>어디서도 참조되지 않는 에셋을 수집합니다.</summary>
    public static List<Unreachable> Collect() {
      var roots = CollectRoots();

      //? GetDependencies(recursive: true)가 BFS를 대신해 줍니다.
      //? 직접 큐를 돌리는 것보다 빠르고, 씬/프리팹 내부 참조까지 Unity가 풀어 줍니다.
      var reachable = new HashSet<string>(AssetDatabase.GetDependencies(roots.ToArray(), true));
      foreach (var r in roots) reachable.Add(r);

      var results = new List<Unreachable>();
      foreach (var path in AssetDatabase.GetAllAssetPaths()) {
        if (!path.StartsWith("Assets/")) continue;
        if (AssetDatabase.IsValidFolder(path)) continue;
        if (Ignored.Contains(Path.GetExtension(path).ToLowerInvariant())) continue;
        if (IsImplicitRoot(path)) continue;
        if (reachable.Contains(path)) continue;

        var full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
        results.Add(new Unreachable {
          path = path,
          type = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name ?? "(unknown)",
          bytes = File.Exists(full) ? new FileInfo(full).Length : 0
        });
      }

      return results.OrderByDescending(r => r.bytes).ToList();
    }

    static bool IsImplicitRoot(string path)
      => ImplicitRoots.Any(r => path.StartsWith(r.prefix, StringComparison.Ordinal))
      //? 어느 위치에 있든 Resources 폴더 안이면 경로 로드 대상입니다.
      || path.Contains("/Resources/", StringComparison.Ordinal);

    /// <summary>탐색의 출발점. 빌드 씬과 암묵적 뿌리들입니다.</summary>
    static List<string> CollectRoots() {
      var roots = new List<string>();

      foreach (var scene in EditorBuildSettings.scenes)
        if (scene.enabled && !string.IsNullOrEmpty(scene.path)) roots.Add(scene.path);

      //? 빌드 설정이 비어 있으면 판정이 통째로 무의미해지므로 프로젝트의 모든 씬을 뿌리로 씁니다.
      if (roots.Count == 0)
        roots.AddRange(AssetDatabase.FindAssets("t:Scene")
          .Select(AssetDatabase.GUIDToAssetPath)
          .Where(p => p.StartsWith("Assets/")));

      foreach (var path in AssetDatabase.GetAllAssetPaths())
        if (path.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(path) && IsImplicitRoot(path))
          roots.Add(path);

      roots.AddRange(ProjectSettingsReferences());
      return roots;
    }

    /// <summary>
    /// <c>ProjectSettings</c>가 직접 참조하는 에셋들입니다.
    /// </summary>
    /// <remarks>
    /// <c>AssetDatabase.GetDependencies</c>는 <c>Assets/</c> 밖에서 출발하지 못합니다.
    /// 그래서 렌더 파이프라인 에셋·볼륨 프로파일처럼
    /// <b>그래픽스/품질 설정에서만 참조되는 것들이 미참조로 잡힙니다.</b>
    /// 실제로 URP 관련 에셋 4종이 그렇게 오탐이었습니다.
    /// </remarks>
    static IEnumerable<string> ProjectSettingsReferences() {
      var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ProjectSettings");
      if (!Directory.Exists(dir)) yield break;

      var pattern = new System.Text.RegularExpressions.Regex(
        @"guid:\s*([0-9a-f]{32})", System.Text.RegularExpressions.RegexOptions.Compiled);

      foreach (var file in Directory.GetFiles(dir, "*.asset", SearchOption.TopDirectoryOnly)) {
        string text;
        try { text = File.ReadAllText(file); } catch { continue; }

        foreach (System.Text.RegularExpressions.Match m in pattern.Matches(text)) {
          var path = AssetDatabase.GUIDToAssetPath(m.Groups[1].Value);
          if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/")) yield return path;
        }
      }
    }

    [MenuItem("Tools/Rair/Audit/미참조 에셋 목록 생성")]
    public static void Generate() {
      var found = Collect();
      var root = Directory.GetParent(Application.dataPath).FullName;
      File.WriteAllText(Path.Combine(root, OUTPUT), Compose(found), new UTF8Encoding(false));

      AssetDatabase.Refresh();
      Debug.Log($"[Audit] {OUTPUT} 생성 완료 — 미참조 후보 {found.Count}건 / {found.Sum(f => f.bytes) / 1048576f:F1} MB");
    }

    static string Compose(List<Unreachable> found) {
      var sb = new StringBuilder();
      sb.AppendLine("# 미참조 에셋 후보");
      sb.AppendLine();
      sb.AppendLine("> **이 파일은 자동 생성됩니다.** `Tools/Rair/Audit/미참조 에셋 목록 생성`");
      sb.AppendLine($"> 마지막 생성 {DateTime.Now:yyyy-MM-dd}");
      sb.AppendLine();
      sb.AppendLine("## 읽는 법");
      sb.AppendLine();
      sb.AppendLine("**삭제 목록이 아닙니다.** 빌드 씬에서 출발해 참조를 따라갔을 때 닿지 않은 것들입니다.");
      sb.AppendLine("Unity에서 \"참조되지 않음\"과 \"쓰이지 않음\"은 다릅니다 —");
      sb.AppendLine("경로 문자열 로드, 에디터 전용 사용, 셰이더 변형 등은 정적 참조로 드러나지 않습니다.");
      sb.AppendLine();
      sb.AppendLine("판정할 수 없는 것들은 아예 뿌리로 취급해 목록에서 뺐습니다.");
      sb.AppendLine();
      sb.AppendLine("| 제외 경로 | 이유 |");
      sb.AppendLine("|---|---|");
      foreach (var (prefix, reason) in ImplicitRoots) sb.AppendLine($"| `{prefix}` | {reason} |");
      sb.AppendLine("| `**/Resources/**` | `Resources.Load` 경로 로드 |");
      sb.AppendLine("| `*.cs` | 사용 여부를 컴파일이 정함 — 에셋 참조로 판정할 수 없음 |");
      sb.AppendLine();
      sb.AppendLine("`ProjectSettings/*.asset`이 참조하는 것(렌더 파이프라인 에셋 등)도 뿌리에 넣었습니다.");
      sb.AppendLine("`GetDependencies`가 `Assets/` 밖에서 출발하지 못해 생기던 오탐입니다.");
      sb.AppendLine();

      sb.AppendLine($"## 후보 ({found.Count}건 · {found.Sum(f => f.bytes) / 1048576f:F1} MB)");
      sb.AppendLine();
      if (found.Count == 0) { sb.AppendLine("없습니다."); return sb.ToString(); }

      sb.AppendLine("용량 내림차순입니다.");
      sb.AppendLine();
      sb.AppendLine("| 크기 | 종류 | 경로 |");
      sb.AppendLine("|---:|---|---|");
      foreach (var f in found)
        sb.AppendLine($"| {f.bytes / 1024f:N0} KB | {f.type} | `{f.path}` |");

      return sb.ToString();
    }
  }
}
