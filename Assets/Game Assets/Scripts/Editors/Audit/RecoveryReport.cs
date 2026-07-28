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
  /// <see cref="BrokenReferenceAudit"/>의 결과를 원인 단위로 묶어 <c>RECOVERY-TODO.md</c>를 생성합니다.
  /// </summary>
  /// <remarks>
  /// 기존 목록은 참조 <b>발생 지점</b>을 한 행씩 나열해서, 프리팹 하나가 유실되면
  /// 그 안의 모든 오버라이드가 별도 행으로 잡혔습니다(`Small Wooden Shelter.prefab` 127행).
  /// 실제로 손봐야 할 것은 원인 GUID 몇 건입니다. (문서 05 P3-6)
  /// <br/>
  /// 또한 <b>복구할 것과 삭제할 것을 나눕니다.</b> 유실된 <c>m_Script</c>는
  /// 스크립트 파일이 지워진 결과이므로 되살릴 대상이 아니라 참조를 걷어낼 대상입니다.
  /// </remarks>
  public static class RecoveryReport
  {
    const string OUTPUT = "RECOVERY-TODO.md";

    /// <summary>이 표식 아래 내용은 재생성 시 그대로 보존됩니다. 수동 판정 결과를 적는 곳입니다.</summary>
    const string MANUAL_MARKER = "<!-- MANUAL: 이 아래는 자동 생성이 덮어쓰지 않습니다 -->";

    enum Kind { Cleanup, Recover }

    sealed class Cause
    {
      public string guid;
      public Kind kind;
      public string inferredType;
      public List<BrokenReferenceAudit.BrokenReference> refs = new();
      public IEnumerable<string> Files => refs.Select(r => r.assetPath).Distinct().OrderBy(p => p);
    }

    [MenuItem("Tools/Rair/Audit/복구 목록 생성")]
    public static void Generate() {
      var broken = BrokenReferenceAudit.Collect();
      var causes = GroupByCause(broken);

      var root = Directory.GetParent(Application.dataPath).FullName;
      var outputPath = Path.Combine(root, OUTPUT);

      var manual = ExtractManualSection(outputPath);
      File.WriteAllText(outputPath, Compose(broken, causes, manual), new UTF8Encoding(false));

      AssetDatabase.Refresh();
      Debug.Log($"[Audit] {OUTPUT} 생성 완료 — 끊긴 참조 {broken.Count}건 / 원인 GUID {causes.Count}개");
    }

    static List<Cause> GroupByCause(List<BrokenReferenceAudit.BrokenReference> broken)
      => broken.GroupBy(b => b.guid).Select(g => {
        var cause = new Cause { guid = g.Key, refs = g.ToList() };
        var fields = cause.refs.Select(r => r.field).Distinct().ToList();

        //? m_Script 유실은 "에셋이 사라진 것"이 아니라 "스크립트가 삭제된 것"입니다.
        //? 되살릴 대상이 아니라 참조를 걷어낼 대상이므로 따로 셉니다.
        cause.kind = fields.Contains("m_Script") ? Kind.Cleanup : Kind.Recover;
        cause.inferredType = InferType(fields);
        return cause;
      }).OrderByDescending(c => c.refs.Count).ToList();

    static string InferType(List<string> fields) {
      if (fields.Contains("m_Script")) return "MonoScript";
      if (fields.Contains("m_SourcePrefab") || fields.Contains("m_CorrespondingSourceObject")) return "Prefab";
      if (fields.Contains("m_Sprite") || fields.Contains("sprite")) return "Sprite";
      if (fields.Contains("m_Texture")) return "Texture";
      if (fields.Contains("m_Mesh")) return "Mesh";
      if (fields.Contains("m_Controller")) return "AnimatorController";
      if (fields.Any(f => f.EndsWith("Shader"))) return "Shader";
      return fields.Count == 1 ? fields[0] : "(필드 혼재)";
    }

    static string Compose(
      List<BrokenReferenceAudit.BrokenReference> broken, List<Cause> causes, string manual) {

      var sb = new StringBuilder();
      var cleanup = causes.Where(c => c.kind == Kind.Cleanup).ToList();
      var recover = causes.Where(c => c.kind == Kind.Recover).ToList();

      sb.AppendLine("# 남은 끊긴 참조 작업 목록");
      sb.AppendLine();
      sb.AppendLine("> **이 파일은 자동 생성됩니다.** `Tools/Rair/Audit/복구 목록 생성`");
      sb.AppendLine($"> 마지막 생성 {DateTime.Now:yyyy-MM-dd}");
      sb.AppendLine();

      sb.AppendLine("## 요약");
      sb.AppendLine();
      sb.AppendLine("| 구분 | 원인 GUID | 참조 발생 | 영향 파일 |");
      sb.AppendLine("|---|---|---|---|");
      sb.AppendLine($"| **복구 대상** (에셋 유실) | {recover.Count} | {recover.Sum(c => c.refs.Count)} | {FileCount(recover)} |");
      sb.AppendLine($"| **정리 대상** (스크립트 삭제분) | {cleanup.Count} | {cleanup.Sum(c => c.refs.Count)} | {FileCount(cleanup)} |");
      sb.AppendLine($"| 합계 | {causes.Count} | {broken.Count} | {broken.Select(b => b.assetPath).Distinct().Count()} |");
      sb.AppendLine();
      sb.AppendLine("참조 발생 수가 원인 수보다 훨씬 큰 것은 정상입니다. ");
      sb.AppendLine("프리팹 하나가 유실되면 그 안의 오버라이드마다 참조가 하나씩 잡히기 때문입니다.");
      sb.AppendLine("**작업 단위는 원인 GUID입니다.**");
      sb.AppendLine();

      AppendSection(sb, "복구 대상 — 유실된 에셋", recover,
        "에셋 파일을 되찾거나 대체본을 연결해야 합니다.");
      AppendSection(sb, "정리 대상 — 유실된 스크립트", cleanup,
        "스크립트 파일이 삭제된 결과입니다. 되살리는 것이 아니라 참조를 걷어내는 쪽이 맞습니다.");

      sb.AppendLine("---");
      sb.AppendLine();
      sb.AppendLine(MANUAL_MARKER);
      sb.AppendLine();
      sb.Append(manual);

      return sb.ToString();
    }

    static int FileCount(List<Cause> causes)
      => causes.SelectMany(c => c.Files).Distinct().Count();

    static void AppendSection(StringBuilder sb, string title, List<Cause> causes, string note) {
      sb.AppendLine($"## {title} ({causes.Count}건)");
      sb.AppendLine();
      if (causes.Count == 0) { sb.AppendLine("없습니다."); sb.AppendLine(); return; }

      sb.AppendLine(note);
      sb.AppendLine();
      sb.AppendLine("| GUID | 추정 종류 | 참조 | 영향 파일 |");
      sb.AppendLine("|---|---|---|---|");

      foreach (var c in causes) {
        var files = c.Files.ToList();
        var shown = files.Count <= 3
          ? string.Join("<br/>", files.Select(Short))
          : string.Join("<br/>", files.Take(3).Select(Short)) + $"<br/>… 외 {files.Count - 3}개";
        sb.AppendLine($"| `{c.guid}` | {c.inferredType} | {c.refs.Count} | {shown} |");
      }
      sb.AppendLine();
    }

    static string Short(string path) {
      const string prefix = "Assets/Game Assets/Resources/";
      return path.StartsWith(prefix) ? "…/" + path.Substring(prefix.Length) : path;
    }

    /// <summary>기존 파일의 수동 작성 구간을 살려 둡니다.</summary>
    static string ExtractManualSection(string path) {
      const string fallback =
        "## 확인된 원인 (수동 판정)\n\n" +
        "- `Small Wooden Shelter.prefab` — `BuildableGrid.cs` (커밋 `0d8ef958`에서 삭제)\n" +
        "- `Small Wooden Shelter.prefab` — `Scripts/Fields/Building` 폴더 (사고 이전 삭제)\n" +
        "- `Map Generator.prefab` — `MapGenScript.cs` (커밋 `e3a6185`에서 삭제)\n";

      if (!File.Exists(path)) return fallback;

      var text = File.ReadAllText(path);
      var index = text.IndexOf(MANUAL_MARKER, StringComparison.Ordinal);
      if (index < 0) return fallback;

      return text.Substring(index + MANUAL_MARKER.Length).TrimStart('\r', '\n');
    }
  }
}
