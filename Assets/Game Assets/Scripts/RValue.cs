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
    public override float Value => RVValue.Value;
    /// <summary>이벤트 및 승가산값에 따른 최종 결과를 구조체로 반환합니다.</summary>
    public override RVFloat RVValue
      => Apply?.GetInvocationList()
               .Aggregate(base.RVValue, (v, a) => (a as Applier).Invoke(v))
        ?? base.RVValue;

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
