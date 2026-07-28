using System.Linq;
using UnityEngine;

namespace Rair
{
  public interface IRefFloat { float Value { get; } }

  /// <summary>Range-included Float</summary>
  public struct RFloat {
    private float _value;
    public float Value {
      readonly get => _value;
      set => _value = Mathf.Clamp(value, Min, Max);
    }
    public float Min { get; set; }
    public float Max { get; set; }

    public RFloat(float value, float min = -SOFT_CAP, float max = SOFT_CAP) {
      Min = min;
      Max = max;
      _value = Mathf.Clamp(value, min, max);
    }

    public const float SOFT_CAP = 999_999_999;

    #region Operators
    public static implicit operator float(RFloat r) => r.Value;
    public static RFloat operator +(RFloat r, float f) => new(r.Value + f, r.Min, r.Max);
    public static RFloat operator -(RFloat r, float f) => new(r.Value - f, r.Min, r.Max);
    #endregion

    public readonly override string ToString() => Value.ToString();
  }

  /// <summary>struct version for <see cref="RMFloat"/></summary>
  public struct RVFloat {
    public bool Nullify { get; set; }
    public RFloat baseValue;
    public RVFloat(float value, float min = -RFloat.SOFT_CAP, float max = RFloat.SOFT_CAP, float adder = 0, float multiplier = 1, bool nullify = false) {
      baseValue = new(value, min, max);
      Adder = adder;
      Multiplier = multiplier;
      Nullify = nullify;
    }

    public readonly float Value => Nullify ? 0 : (baseValue.Value + Adder) * Multiplier;
    public float Adder { get; private set; }
    public float Multiplier { get; private set; }

    #region Operators
    // public static implicit operator float(RVFloat r) => r;
    public static RVFloat operator +(RVFloat r, float f) {
      r.Adder += f;
      return r;
    }
    public static RVFloat operator -(RVFloat r, float f) {
      r.Adder -= f;
      return r;
    }
    /// <summary>
    /// Changes multiplier value. It does not multiply the result itself.<br/>
    /// (ex: RV(100) * 1.5 == 150 is not determined)
    /// </summary>
    public static RVFloat operator *(RVFloat r, float f) {
      r.Multiplier += (f - 1);
      return r;
    }
    /// <summary>
    /// Changes multiplier change. It does not divide the result itself.<br/>
    /// (ex: RV(100) / 2 == 50 is not determined)
    /// </summary>
    public static RVFloat operator /(RVFloat r, float f)    {
      r.Multiplier -= (f - 1);
      return r;
    }
    #endregion

    const bool DETAIL = true;
    public readonly override string ToString() => DETAIL
      ? $"{Value:F0}{(Nullify ? "(Nullified)" : "")} = ({baseValue:F0}+{Adder:F0}) x {Multiplier:F2}"
      : $"{Value:F0}{(Nullify ? "(Nullified)" : "")}";
  }

  /// <summary>Range-included Modifier Float</summary>
  public class RMFloat : IRefFloat {
    protected float value, min, max, add, mul;
    /// <summary>특정 참조값을 최소값으로 설정하길 원할 경우 사용합니다</summary>
    public IRefFloat refMin;
    /// <summary>특정 참조값을 최대값으로 설정하길 원할 경우 사용합니다</summary>
    public IRefFloat refMax;
    /// <returns>true일 경우 항상 0을 반환합니다</returns>
    public bool Nullify;

    public delegate float Modifier(float f);
    /// <summary>실제 최소값을 변동시키길 원할 경우 사용합니다</summary>
    public Modifier MinModifier;
    /// <summary>실제 최대값을 변동시키길 원할 경우 사용합니다</summary>
    public Modifier MaxModifier;
    /// <summary>범위와 승가산 값을 가질 수 있는 <see cref="float"/>를 생성합니다</summary>
    /// <param name="value">초기값을 입력합니다</param>
    /// <param name="min">이 값이 가질 수 있는 최소값입니다.</param>
    /// <param name="max">이 값이 가질 수 있는 최대값입니다.</param>
    /// <param name="adder">값에 추가되는 초기 가산값을 지정합니다.</param>
    /// <param name="multiplier">전체 값을 증감시키는 초기 승산값을 지정합니다.</param>
    /// <param name="refMin">다른 참조 변수를 최소값으로 지정합니다. <paramref name="min"/>의 값은 무시됩니다.</param>
    /// <param name="refMax">다른 참조 변수를 최대값으로 지정합니다. <paramref name="max"/>의 값은 무시됩니다.</param>
    /// <param name="minMod">실제 적용되는 최소값을 해당 함수로 변동시킵니다. <paramref name="refMin"/>로 참조한 값을 조정하여 적용하고자 할 경우 유용합니다.</param>
    /// <param name="maxMod">실제 적용되는 최대값을 해당 함수로 변동시킵니다. <paramref name="refMax"/>로 참조한 값을 조정하여 적용하고자 할 경우 유용합니다.</param>
    public RMFloat(float value, float min = -RFloat.SOFT_CAP, float max = RFloat.SOFT_CAP, float adder = 0, float multiplier = 1, IRefFloat refMin = null, IRefFloat refMax = null, Modifier minMod = null, Modifier maxMod = null) {
      this.value = value;
      this.min = min;
      this.max = max;
      add = adder;
      mul = multiplier;
      this.refMin = refMin;
      this.refMax = refMax;
      MinModifier = minMod ?? (v => v);
      MaxModifier = maxMod ?? (v => v);
    }

    public float Min => MinModifier(refMin?.Value ?? min);
    public float Max => MaxModifier(refMax?.Value ?? max);
    /// <summary>승가산값에 따른 연산 결과값을 반환합니다.</summary>
    public virtual float Value => Nullify ? 0 : Mathf.Clamp((value + add) * mul, Min, Max);
    /// <summary>승가산값에 따른 최종 결과를 구조체로 반환합니다.</summary>
    public virtual RVFloat RVValue => new(value, Min, Max, add, mul, Nullify);

    public override string ToString()
      => $"{Value}{(Nullify ? "(Nullified)" : "")} = {value} [{Min:F0}~{Max:F0}]";
    public static RMFloat operator +(RMFloat r, float f) {
      r.value = Mathf.Clamp(r.value + f, r.Min, r.Max);
      return r;
    }
    public static RMFloat operator -(RMFloat r, float f) {
      r.value = Mathf.Clamp(r.value - f, r.Min, r.Max);
      return r;
    }
    public static RMFloat operator *(RMFloat r, float f) {
      r.mul += (f - 1);
      return r;
    }
    public static RMFloat operator /(RMFloat r, float f) {
      r.mul -= (f - 1);
      return r;
    }
  }

  /// <summary><see cref="RMFloat"> with an <see cref="Applier"/></summary>
  public sealed class RAFloat : RMFloat
  {
    /// <summary>승가산값을 누적해 가는 적용기입니다.</summary>
    /// <remarks>
    /// <b>계약</b> — 구현은 다음 두 가지를 지켜야 합니다.
    /// <list type="number">
    ///   <item>
    ///     <b>받은 값에서 파생시킬 것.</b> <c>previous</c>에 연산자를 적용해 반환하십시오.
    ///     <c>new RVFloat(...)</c>로 새로 만들어 반환하면 앞선 적용기의 기여가 사라집니다.
    ///   </item>
    ///   <item>
    ///     <b>부수효과가 없을 것.</b> 검증을 위해 한 프레임에 두 번 호출될 수 있습니다.
    ///   </item>
    /// </list>
    /// 이를 지키면 등록 순서가 결과를 바꾸지 않습니다.
    /// <c>+</c>는 <c>Adder</c>에, <c>*</c>는 <c>Multiplier</c>에 각각 누적되는
    /// 독립적인 연산이기 때문입니다.
    /// <br/>
    /// 에디터와 개발 빌드에서는 적용기가 둘 이상일 때 순서 무관 여부를 실제로 검사합니다.
    /// (<see cref="VerifyOrderIndependence"/>)
    /// </remarks>
    public delegate RVFloat Applier(RVFloat previous);
    public event Applier Apply;
    /// <summary>범위와 승가산값, 변동 이벤트를 가질 수 있는 <see cref="float"/>를 생성합니다</summary>
    /// <param name="value">초기값을 입력합니다</param>
    /// <param name="min">이 값이 가질 수 있는 최소값입니다.</param>
    /// <param name="max">이 값이 가질 수 있는 최대값입니다.</param>
    /// <param name="adder">값에 추가되는 초기 가산값을 지정합니다.</param>
    /// <param name="multiplier">전체 값을 증감시키는 초기 승산값을 지정합니다.</param>
    /// <param name="refMin">다른 참조 변수를 최소값으로 지정합니다. <paramref name="min"/>의 값은 무시됩니다.</param>
    /// <param name="refMax">다른 참조 변수를 최대값으로 지정합니다. <paramref name="max"/>의 값은 무시됩니다.</param>
    /// <param name="minMod">실제 적용되는 최소값을 해당 함수로 변동시킵니다. <paramref name="refMin"/>로 참조한 값을 조정하여 적용하고자 할 경우 유용합니다.</param>
    /// <param name="maxMod">실제 적용되는 최대값을 해당 함수로 변동시킵니다. <paramref name="refMax"/>로 참조한 값을 조정하여 적용하고자 할 경우 유용합니다.</param>
    public RAFloat(float value, float min = -RFloat.SOFT_CAP, float max = RFloat.SOFT_CAP, float adder = 0, float multiplier = 1, IRefFloat refMin = null, IRefFloat refMax = null, Modifier minMod = null, Modifier maxMod = null) : base(value, min, max, adder, multiplier, refMin, refMax, minMod, maxMod) { }

    /// <summary>이벤트 및 승가산값에 따른 연산 결과값을 반환합니다.</summary>
    /// <remarks>
    /// 적용기 체인을 <b>모두 거친 뒤</b> 선언 범위로 클램프합니다.
    /// 이전에는 클램프하지 않아 <see cref="RMFloat"/>와 같은 식에 다르게 반응했습니다.
    /// </remarks>
    public override float Value => Nullify ? 0 : Mathf.Clamp(RVValue.Value, Min, Max);

    /// <summary>이벤트 및 승가산값에 따른 최종 결과를 구조체로 반환합니다.</summary>
    /// <remarks>
    /// 클램프 이전의 원본입니다. 범위를 넘어선 정도를 알아야 하는 곳에서 씁니다.
    /// 최종 수치가 필요하면 <see cref="Value"/>를 쓰십시오.
    /// </remarks>
    public override RVFloat RVValue {
      get {
        if (Apply == null) return base.RVValue;

        var appliers = Apply.GetInvocationList();
        var result = Fold(appliers, base.RVValue, forward: true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        //? 적용기가 하나뿐이면 순서라는 것이 없습니다.
        if (appliers.Length > 1) VerifyOrderIndependence(appliers, result);
#endif
        return result;
      }
    }

    RVFloat Fold(System.Delegate[] appliers, RVFloat seed, bool forward) {
      var v = seed;
      for (int i = 0; i < appliers.Length; i++)
        v = ((Applier)appliers[forward ? i : appliers.Length - 1 - i]).Invoke(v);
      return v;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool orderWarned = false;
    /// <summary>
    /// 적용기 체인을 역순으로도 접어 보고 결과가 같은지 확인합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Applier"/>의 계약이 실제로 지켜지는지 검사합니다.
    /// 계약을 어기는 대표적인 경우는 두 가지입니다.
    /// <list type="bullet">
    ///   <item>적용기가 <c>new RVFloat(...)</c>를 반환해 앞선 기여를 버리는 경우</item>
    ///   <item>적용기가 누적된 <c>Value</c>를 읽고 그에 따라 분기하는 경우</item>
    /// </list>
    /// 둘 다 컴파일러가 막을 수 없어서, 실제로 순서를 바꿔 보는 방식으로 잡습니다.
    /// 부수효과가 없다는 계약이 여기에 필요합니다.
    /// </remarks>
    void VerifyOrderIndependence(System.Delegate[] appliers, RVFloat forward) {
      if (orderWarned) return;

      var backward = Fold(appliers, base.RVValue, forward: false);
      if (Mathf.Approximately(forward.Value, backward.Value)) return;

      orderWarned = true;
      Debug.LogError(
        $"[RAFloat] 적용기 등록 순서가 결과를 바꿉니다: 정순 {forward.Value} / 역순 {backward.Value}\n" +
        $"적용기 {appliers.Length}개 — {string.Join(", ", appliers.Select(d => d.Method.Name))}\n" +
        "Applier 계약을 확인하십시오. 받은 값에서 파생시키지 않고 새 RVFloat를 만들었거나, " +
        "누적된 Value를 읽어 분기했을 가능성이 큽니다.");
    }
#endif

    public static RAFloat operator +(RAFloat r, float f) {
      r.value = Mathf.Clamp(r.value + f, r.Min, r.Max);
      return r;
    }
    public static RAFloat operator -(RAFloat r, float f) {
      r.value = Mathf.Clamp(r.value - f, r.Min, r.Max);
      return r;
    }
    public static RAFloat operator *(RAFloat r, float f) {
      r.mul += (f - 1);
      return r;
    }
    public static RAFloat operator /(RAFloat r, float f) {
      r.mul -= (f - 1);
      return r;
    }

    public override string ToString() => RVValue.ToString();
    //todo public string Log() { }
  }
}
