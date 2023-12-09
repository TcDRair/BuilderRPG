using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Rair
{
  public interface IRefFloat
  {
    float Value { get; }
  }

  /// <summary>
  /// Range-included Float
  /// </summary>
  public struct RFloat
  {
    private float _value;
    public float Min { get; set; }
    public float Max { get; set; }
    public float Value {
      readonly get => _value;
      set => _value = Mathf.Clamp(value, Min, Max);
    }

    public RFloat(float value, float min = -SOFT_CAP, float max = SOFT_CAP) {
      Min = min;
      Max = max;
      _value = Mathf.Clamp(value, min, max);
    }

    #region Constants
    public const float SOFT_CAP = 999_999_999;
    /// <summary>Between 0 and 1 starting with 0: new(0, 0, 1)</summary>
    public static RFloat RatioZero = new(0, 0, 1);
    /// <summary>Between 0 and 1 starting with 1: new(0, 0, 1)</summary>
    public static RFloat RatioOne = new(1, 0, 1);
    /// <summary>Between 0 and 1 starting with 1/2: new(0.5, 0, 1)</summary>
    public static RFloat RatioHalf = new(.5f, 0, 1);
    /// <summary>Above 0 starting with 0: new(0, 0, +∞)</summary>
    public static RFloat PositiveZero = new(0, 0);
    /// <summary>Above 0 starting with 1: new(1, 0, +∞)</summary>
    public static RFloat PositiveOne = new(1, 0);
    /// <summary>Full Range starting with 0: new(0, -∞, +∞)</summary>
    public static RFloat FullZero = new(0, -float.MinValue);
    /// <summary>Full Range starting with 1: new(1, -∞, +∞)</summary>
    public static RFloat FullOne = new(1, -float.MinValue);
    #endregion

    #region Operators
    public static implicit operator float(RFloat r) => r.Value;
    public static RFloat operator +(RFloat r, float f) => new(r.Value + f, r.Min, r.Max);
    public static RFloat operator -(RFloat r, float f) => new(r.Value - f, r.Min, r.Max);
    public static RFloat operator *(RFloat r, float f) => new(r.Value * f, r.Min, r.Max);
    public static RFloat operator /(RFloat r, float f) => new(r.Value / f, r.Min, r.Max);
    #endregion

    public readonly override string ToString() => Value.ToString();
  }

  /// <summary>
  /// Range-included float value with Adder and Multiplier<br/>
  /// Applier exists for this type.
  /// </summary>
  public struct RVFloat
  {
    public bool Nullify { get; set; }

    public RFloat @base;
    float adder, multiplier;

    #region Public Getters
    public readonly float Value => Nullify ? 0 : (@base.Value * multiplier + adder);
    public readonly float Base => @base.Value;
    public readonly float Adder => adder;
    public readonly float Multiplier => multiplier;
    #endregion

    public RVFloat(float value, float min = -RFloat.SOFT_CAP, float max = RFloat.SOFT_CAP) {
      @base = new(value, min, max);
      adder = 0;
      multiplier = 1;
      Nullify = false;
    }

    #region Operators
    // public static implicit operator float(RVFloat r) => r;
    public static RVFloat operator +(RVFloat r, float f) {
      r.adder += f;
      return r;
    }
    public static RVFloat operator -(RVFloat r, float f) {
      r.adder -= f;
      return r;
    }
    /// <summary>
    /// Changes multiplier value. It does not multiply the result itself.<br/>
    /// (ex: RV(100) * 1.5 == 150 is not determined)
    /// </summary>
    public static RVFloat operator *(RVFloat r, float f) {
      r.multiplier += (f - 1);
      return r;
    }
    /// <summary>
    /// Changes multiplier change. It does not divide the result itself.<br/>
    /// (ex: RV(100) / 2 == 50 is not determined)
    /// </summary>
    public static RVFloat operator /(RVFloat r, float f) {
      r.multiplier -= (f - 1);
      return r;
    }
    #endregion

    const bool DETAIL = true;
    public readonly override string ToString() => DETAIL
      ? $"{Value:F0}({@base:F0}×{multiplier:F2}+{adder:F0})"
      : $"{Value:F0}";
  }

  /// <summary>
  /// Range-included float value with referencable max<br/>
  /// </summary>
  public class RVMFloat : IRefFloat
  {
    float value;
    public float Value {
      get => value;
      set => this.value = Mathf.Clamp(value, Min, maxModifier(Max.Value));
    }
    public float Min { get; set; } //todo value setter 구현 (clamp)
    public IRefFloat Max { get; set; }
    public Func<float, float> maxModifier;

    public RVMFloat(float value, float min, IRefFloat max, Func<float, float> modifier = null) {
      this.value = value;
      Min = min;
      Max = max;
      maxModifier = modifier ?? (v => v);
    }

    const bool DETAIL = true;
    public override string ToString() => DETAIL
      ? $"{Value:F0}({Min:F0}~{Max.Value:F0})"
      : $"{Value:F0}";
  }

  /// <summary>
  /// Range-included float value with Adder and Multiplier, and Applier which can change the value without changing the original value.<br/>
  /// </summary>
  public class RVEFloat : IRefFloat {
    public RVEFloat(float value, float min = -RFloat.SOFT_CAP, float max = RFloat.SOFT_CAP)
      => this.value = new(value, min, max);
    public RVFloat value;

    public delegate RVFloat Applier(RVFloat rve);
    /// <summary>Changes the value without changing the original value.</summary>
    public event Applier Apply;

    public bool Nullify {
      get => value.Nullify;
      set => this.value.Nullify = value;
    }
    public float Value {
      get {
        var result = value;
        if (Nullify) return 0;
        if (Apply != null)
          foreach (var a in Apply.GetInvocationList())
            result = (a as Applier).Invoke(result);
        return result.Value;
      }
    }
    public float BaseValue {
      get => value.Value;
      set => this.value.@base.Value = value;
    }

    public override string ToString() => value.ToString();
    /// <summary>
    /// 값의 변화 과정을 로그로 출력합니다.
    /// </summary>
    public string Log() {
      string log = "RVEFloat Log:\n" +
      $"Base Value : {value}\n" +
      $"Applier Results:\n";
      var v = value;
      foreach (var a in Apply.GetInvocationList()) {
        var c = (a as Applier).Invoke(v);
        log += $"  {v.Value} → {c.Value}\n";
        v = c;
      }
      log += $"Final Value : {Value}";
      if (Value != v.Value)
        log += $"\n  (Result incorrect with: {Value}";
      return log;
    }
  }
}