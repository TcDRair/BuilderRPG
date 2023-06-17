using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Data {
  public interface IConstantData { int ID { get; } }
  public static class ConstantData
  {
    public readonly static Dictionary<int, MasteryData> MasteryData = new();
    public readonly static Dictionary<int, MasteryCategoryData> MasteryCategoryData = new();
  }

  public interface IVariableData {
    int ID { get; }
    /// <summary>
    /// 인게임 가변 데이터를 Json 문자열로 직렬화하는 함수입니다.<br/>
    /// 클래스 내부 구조와 Json 구조가 일부 다를 때를 위해 사용합니다.
    /// </summary>
    string ToJson();
  }
  public static class VariableData
  {
    public readonly static Dictionary<int, Mastery> Mastery = new();
    public readonly static Dictionary<int, MasteryCategory> MasteryCategory = new();
  }
}
