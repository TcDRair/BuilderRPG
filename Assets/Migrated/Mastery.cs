using System;
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

using Data;
public class Mastery : IVariableData {
  public int ID { get; private set; }
  public float TotalEXP { get; private set; }
  public Mastery(int id, float exp) { ID = id; TotalEXP = exp; }
  public MasteryData Info => ConstantData.MasteryData[ID];
  public string Description => Info.Description(Level);
  public int Level => (int)Info.Level(TotalEXP);
  public float Ratio => Info.Ratio(TotalEXP);
  public string ToJson() => $"{{\"id\":{ID},\"exp\":{TotalEXP}}}";
}

/// <summary>
/// Creating Mastery info from text json. This doesn't directly contains Mastery function.<br/>
/// Descriptions with several pattern are formatted with colored or variable values.
/// </summary>
public class MasteryData : IConstantData {
  #region Privates
  readonly string m_iconPath;
  const string ICON_PATH = "Sprites/Icon/Mastery/";
  private const string CFR = @"([+-]?\d+(?:\.\d*)?|[+-]?\.\d+)", // Captured Float Regex
    levelRegex = @"\[lv(\d):" + CFR + "," + CFR + @"\]", // [lv{level}:{a},{b}]
    levelPercentRegex = @"\[lvp:" + CFR + "," + CFR + @"\]"; // [lvp:{a},{b}]
  private static readonly Regex m_level = new Regex(levelRegex),
    m_levelPercent = new Regex(levelPercentRegex),
    m_all = new Regex(levelRegex + "|" + levelPercentRegex); //TODO Append Regex if needed
  private readonly float fA, fB;
  private readonly DescLine[] m_descs;
  #endregion

  #region Publics
  public int ID { get; private set; }
  public int Tier { get; private set; }
  public Condition[] Prerequisites { get; private set; }
  public int[] Dependencies { get; private set; } = new int[0];
  public string Name { get; private set; }
  public int MaxLevel { get; private set; }
  public Sprite Icon => Resources.Load<Sprite>(m_iconPath);

  public const int MAX_TIER = 9;
  #endregion

  #region Func
  public float Level(float exp) => Mathf.Max(0, Mathf.Log(exp * Mathf.Log(fA) / fB + 1, fA));
  public float EXP(int level) => Mathf.Pow(fA, level) * fB / Mathf.Log(fA);
  public float Ratio(float exp) => Mathf.Pow(Level(exp)%1, fA) / fA;
  public string Description(int level) => level < 0 || level > MaxLevel
    ? "[Level is out of range]"
    : $"선행 마스터리 : {(Prerequisites.Length > 0 ? string.Join(", ", Prerequisites.Select(p => $"{ConstantData.MasteryData[p.id].Name} Lv.{p.level}")) : "없음")}\n"
    + string.Join("\n", m_descs.Select(unit => {
      var name = $"<b>{unit.Name}</b>{(unit.Toggle ? "[토글형]" : "")}";
      return unit.Level < 0 ? unit.Text(level)
      : unit.Level <= level ? $"<color=cyan>o {name} : {unit.Text(level)}</color>" + $" <i>{unit.Flavor}</i>"
      : !unit.Hidden ? $"<color=#808080>x {name} : {unit.Text(level)}</color>" + $" <i>{unit.Flavor}</i>"
      : $"<color=#808080>x {name} : {unit.Level}레벨에 해금됩니다.</color>";
    }));
  public void SetDependencies(IEnumerable<MasteryData> data) { Dependencies = data.Where(md => md.Prerequisites.Any(p => p.id == ID)).Select(md => md.ID).ToArray(); }
  #endregion

  public MasteryData(int id, int tier, string name, string icon, Condition[] prerequisites, DescParam[] descriptions, int maxLevel, float ratio, float basic) {
    ID = id;
    Tier = tier;
    Name = name;
    m_iconPath = ICON_PATH + icon;
    MaxLevel = maxLevel;
    fA = ratio; fB = basic;
    Prerequisites = prerequisites;
    //? Parse each description lines
    m_descs = descriptions.Select(d => {
      var desc = new DescLine(d);
      int prevIdx = 0;
      //? For each matched pattern
      foreach (Match m in m_all.Matches(d.desc)) {
        string prev = d.desc.Substring(prevIdx, m.Index - prevIdx);
        if (m_level.Match(m.Value) is var l && l.Success) {
          int precision = int.Parse(l.Groups[1].Value);
          float v1 = float.Parse(l.Groups[2].Value), v2 = float.Parse(l.Groups[3].Value);
          desc += level => prev + ((level < d.level) ? "--" : (level * v1 + v2).ToString($"F{precision}"));
        } else if (m_levelPercent.Match(m.Value) is var lp && lp.Success) {
          float v1 = float.Parse(lp.Groups[1].Value), v2 = float.Parse(lp.Groups[2].Value);
          desc += level => prev + ((level < d.level) ? "--%" : $"{(level * v1 + v2)*100:F1}%");
        }
        //TODO Add more Regex if needed
        prevIdx = m.Index + m.Length;
      }
      if (d.desc.Length > prevIdx) desc += _ => d.desc.Substring(prevIdx);
      return desc;
    }).ToArray();
  }

  public class Condition { public int id; public int level = -1; }
  public class DescParam { public int level; public string name = ""; public bool toggle = false; public bool hidden = false; public string desc; public string flavor = ""; }
  private class DescLine {
    public DescLine(DescParam param) {
      Level = param.level;
      Name = param.name;
      Flavor = param.flavor;
      Toggle = param.toggle;
      Hidden = param.hidden;
    }
    public int Level { get; private set; }
    public string Name { get; private set; }
    public string Flavor { get; private set; }
    public bool Toggle { get; private set; }
    public bool Hidden { get; private set; }
    public string Text(int level) => string.Join("", m_desc.Select(f => f(level)));
    private readonly List<Func<int, string>> m_desc = new List<Func<int, string>>();
    public static DescLine operator +(DescLine d, Func<int, string> f) { d.m_desc.Add(f); return d; }
  }
}

public class MasteryCategory : IVariableData {
  public int ID { get; private set; }
  public float TotalEXP { get; private set; }
  public MasteryCategory(int id, float exp) { ID = id; TotalEXP = exp; }
  public MasteryCategoryData Info => ConstantData.MasteryCategoryData[ID];
  public int Level => (int)Info.Level(TotalEXP);
  public float Ratio => Info.Ratio(TotalEXP);
  public string ToJson() => $"{{\"id\":{ID},\"exp\":{TotalEXP}}}";
}

public class MasteryCategoryData : IConstantData {
  public int ID { get; private set; }
  public string Name { get; private set; }
  public string Icon { get; private set; }
  public int[] Masteries { get; private set; }
  public Dictionary<int, int> Row { get; private set; }
  public int MaxLevel { get; private set; }
  private readonly float fA, fB;
  public float Level(float exp) => Mathf.Log(exp * Mathf.Log(fA) / fB + 1, fA);
  public float EXP(int level) => Mathf.Pow(fA, level) * fB / Mathf.Log(fA);
  public float Ratio(float exp) => Mathf.Pow(Level(exp)%1, fA) / fA;
  public MasteryCategoryData(int id, string name, string icon, int[] masteries, int[] row, int maxLevel, float ratio, float basic) {
    ID = id;
    Name = name;
    Icon = icon;
    Masteries = masteries;
    Row = new Dictionary<int, int>();
    for (int i = 0; i < masteries.Length; i++) Row[masteries[i]] = row[i];
    MaxLevel = maxLevel;
    fA = ratio;
    fB = basic;
  }
}