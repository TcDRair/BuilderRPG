using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Rair.EditorTools
{
  /// <summary>
  /// 프로젝트 전체에서 해석되지 않는 GUID 참조(끊긴 참조)를 수집합니다.
  /// </summary>
  /// <remarks>
  /// 2026-07 GUID 유실 사고 때 일회용 스크립트로 하던 일을 상시 도구로 옮긴 것입니다.
  /// 그때는 판정 과정을 재실행할 수 없어 근거가 커밋 메시지에만 남았습니다. (보완 기록 P3-7)
  /// <br/>
  /// 복구 작업용이자 <b>조기 경보용</b>입니다. 같은 사고가 나면
  /// 여기서 먼저 드러나야 합니다.
  /// </remarks>
  public static class BrokenReferenceAudit
  {
    /// <summary>
    /// 스캔 대상에서 제외하는 경로.
    /// </summary>
    /// <remarks>
    /// 임포트한 벤더 에셋은 우리가 유지보수하지 않고, 데모 씬 하나가 45MB에 달해
    /// 스캔 시간을 지배합니다. 이 폴더들의 끊긴 참조는 재임포트로 해결할 문제이지
    /// 복구 대상이 아닙니다.
    /// </remarks>
    static readonly string[] ExcludedPrefixes = {
      "Assets/Imported Assets/",
      "Assets/Samples/",
      "Assets/TextMesh Pro/",
      "Assets/Dagger's-AssetCleaner/",
    };

    /// <summary>YAML로 직렬화되어 GUID 참조를 담을 수 있는 확장자.</summary>
    static readonly HashSet<string> ScannedExtensions = new() {
      ".prefab", ".unity", ".asset", ".mat", ".controller", ".anim", ".overrideController",
      ".physicMaterial", ".physicsMaterial2D", ".spriteatlas", ".terrainlayer",
      ".playable", ".signal", ".guiskin", ".fontsettings", ".renderTexture",
      ".cubemap", ".flare", ".brush", ".mixer", ".preset", ".lighting", ".shadervariants"
    };

    //? "field: {fileID: N, guid: ..., type: N}" 형태. 시퀀스 항목("- target: {...}")도 포함합니다.
    //? 타임아웃은 안전장치입니다. 길이 제한을 뚫는 입력이 와도 에디터가 멈추지 않아야 합니다.
    static readonly Regex ReferencePattern = new(
      @"(?<field>[\w\.]+)\s*:\s*\{fileID:\s*(?<fileID>-?\d+),\s*guid:\s*(?<guid>[0-9a-fA-F]{32}),\s*type:\s*(?<type>\d+)\}",
      RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    static readonly Regex DocumentPattern = new(@"^--- !u!(?<class>\d+) &(?<id>-?\d+)", RegexOptions.Compiled);
    static readonly Regex NamePattern = new(@"^  m_Name:\s*(?<name>.*)$", RegexOptions.Compiled);
    static readonly Regex OwnerPattern = new(@"^  m_GameObject:\s*\{fileID:\s*(?<id>-?\d+)\}", RegexOptions.Compiled);

    const int GAMEOBJECT_CLASS = 1;
    /// <summary>정상적인 GUID 참조 줄의 상한. 이보다 긴 줄은 직렬화된 데이터 덩어리입니다.</summary>
    const int MAX_LINE = 4096;

    public sealed class BrokenReference
    {
      public string assetPath;
      public string ownerName;   // 참조를 들고 있는 GameObject 이름 (해석 실패 시 "?")
      public string field;
      public long fileID;
      public string guid;
      public int type;
    }

    /// <summary>스캔 전체에 허용하는 시간. 넘으면 중단하고 부분 결과를 반환합니다.</summary>
    public static TimeSpan Budget { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>마지막 스캔이 예산 초과로 중단되었는지.</summary>
    public static bool LastRunAborted { get; private set; }

    /// <summary>끊긴 참조를 전부 수집합니다.</summary>
    public static List<BrokenReference> Collect() {
      var results = new List<BrokenReference>();
      var resolved = new Dictionary<string, bool>();
      var clock = System.Diagnostics.Stopwatch.StartNew();
      LastRunAborted = false;

      foreach (var path in EnumerateScannableFiles()) {
        //! 어떤 입력이 와도 에디터를 붙잡아 두지 않아야 합니다.
        //! 실제로 이 도구가 TMP 폰트 에셋에서 정규식 백트래킹으로 에디터를 두 번 멈춘 적이 있습니다.
        if (clock.Elapsed > Budget) {
          LastRunAborted = true;
          Debug.LogWarning($"[Audit] {Budget.TotalSeconds:F0}초 예산을 초과해 중단했습니다. 결과가 불완전합니다. (마지막 파일: {path.relative})");
          break;
        }

        IEnumerable<string> lines;
        try { lines = File.ReadLines(path.absolute); }
        catch { continue; } // 바이너리 직렬화 등 읽을 수 없는 것은 건너뜁니다.

        var names = new Dictionary<long, string>();   // GameObject fileID → 이름
        var owners = new Dictionary<long, long>();    // 컴포넌트 fileID → GameObject fileID
        var hits = new List<(int docId, BrokenReference re)>();

        long currentDoc = 0;
        int currentClass = 0;

        foreach (var line in lines) {
          //! 정규식을 아무 줄에나 돌리면 안 됩니다.
          //! TMP 폰트 에셋은 아틀라스 전체를 한 줄(3,300만 자)로 직렬화하는데,
          //! 참조 정규식이 매칭에 실패하면 시작 위치마다 재시도해 사실상 멈춥니다.
          //! 정상적인 YAML 한 줄이 가질 수 없는 길이는 먼저 걷어냅니다.
          if (line.Length > MAX_LINE) continue;

          var doc = DocumentPattern.Match(line);
          if (doc.Success) {
            currentDoc = long.Parse(doc.Groups["id"].Value);
            currentClass = int.Parse(doc.Groups["class"].Value);
            continue;
          }

          if (currentClass == GAMEOBJECT_CLASS) {
            var n = NamePattern.Match(line);
            if (n.Success) names[currentDoc] = n.Groups["name"].Value.Trim();
          } else {
            var o = OwnerPattern.Match(line);
            if (o.Success) owners[currentDoc] = long.Parse(o.Groups["id"].Value);
          }

          //? 참조가 없는 줄이 대부분이므로, 값싼 문자열 검사로 정규식 호출 자체를 줄입니다.
          if (line.IndexOf("guid:", StringComparison.Ordinal) < 0) continue;

          MatchCollection matches;
          try { matches = ReferencePattern.Matches(line); }
          catch (RegexMatchTimeoutException) {
            //? 길이 제한을 뚫는 입력. 그 줄만 버리고 계속합니다.
            Debug.LogWarning($"[Audit] 정규식 타임아웃으로 한 줄을 건너뜁니다: {path.relative}");
            continue;
          }

          foreach (Match m in matches) {
            var guid = m.Groups["guid"].Value.ToLowerInvariant();
            if (IsBuiltin(guid)) continue;

            if (!resolved.TryGetValue(guid, out var exists)) {
              exists = !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid));
              resolved[guid] = exists;
            }
            if (exists) continue;

            hits.Add(((int)currentDoc, new BrokenReference {
              assetPath = path.relative,
              field = m.Groups["field"].Value,
              fileID = long.Parse(m.Groups["fileID"].Value),
              guid = guid,
              type = int.Parse(m.Groups["type"].Value)
            }));
          }
        }

        //* 이름 해석은 파일을 다 읽은 뒤에 합니다. 참조가 GameObject 정의보다 먼저 나올 수 있습니다.
        foreach (var (docId, re) in hits) {
          re.ownerName = ResolveName(docId, names, owners);
          results.Add(re);
        }
      }

      return results;
    }

    static string ResolveName(long docId, Dictionary<long, string> names, Dictionary<long, long> owners) {
      if (names.TryGetValue(docId, out var direct) && !string.IsNullOrEmpty(direct)) return direct;
      if (owners.TryGetValue(docId, out var owner) && names.TryGetValue(owner, out var viaOwner) && !string.IsNullOrEmpty(viaOwner))
        return viaOwner;
      return "?";
    }

    /// <summary>Unity 내장 리소스 GUID는 AssetDatabase로 해석되지 않지만 끊긴 것이 아닙니다.</summary>
    static bool IsBuiltin(string guid) => guid.StartsWith("0000000000000000");

    static IEnumerable<(string absolute, string relative)> EnumerateScannableFiles() {
      var root = Directory.GetParent(Application.dataPath).FullName;

      foreach (var relative in AssetDatabase.GetAllAssetPaths()) {
        if (!relative.StartsWith("Assets/")) continue;
        if (ExcludedPrefixes.Any(p => relative.StartsWith(p))) continue;
        if (!ScannedExtensions.Contains(Path.GetExtension(relative).ToLowerInvariant())) continue;

        var absolute = Path.Combine(root, relative);
        if (File.Exists(absolute)) yield return (absolute, relative);
      }

      //? ProjectSettings는 AssetDatabase가 열거하지 않지만 같은 형식이고, 실제로 끊긴 참조가 있었습니다.
      var settings = Path.Combine(root, "ProjectSettings");
      if (!Directory.Exists(settings)) yield break;
      foreach (var absolute in Directory.GetFiles(settings, "*.asset", SearchOption.TopDirectoryOnly))
        yield return (absolute, "ProjectSettings/" + Path.GetFileName(absolute));
    }

    [MenuItem("Tools/Rair/Audit/끊긴 참조 수집")]
    public static void RunAndLog() {
      var broken = Collect();
      if (broken.Count == 0) {
        Debug.Log("[Audit] 끊긴 참조가 없습니다.");
        return;
      }

      var files = broken.Select(b => b.assetPath).Distinct().Count();
      var guids = broken.Select(b => b.guid).Distinct().Count();
      Debug.Log($"[Audit] 끊긴 참조 {broken.Count}건 / 파일 {files}개 / 고유 GUID {guids}개\n" +
                "자세한 분류는 Tools/Rair/Audit/복구 목록 생성 을 실행하십시오.");
    }
  }
}
