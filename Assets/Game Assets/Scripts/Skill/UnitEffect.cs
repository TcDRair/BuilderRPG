using System.Collections.Generic;

using UnityEngine;

using Rair.Field;
using System;
namespace Rair.Skill
{
  public delegate void EffectAction(FieldUnit unit);
  public delegate string EffectText(FieldUnit unit);
  public class UnitEffect
  {
    #region Constructors
    public UnitEffect(Ability ability) {
      ID = new(ability.ID);
      Name = ability.Name;
      Icon = ability.Icon;
    }

    /// <summary>식별자와 표시 정보를 스스로 채우는 파생 클래스를 위한 생성자입니다.</summary>
    protected UnitEffect() { }
    #endregion
    public struct IDSet {
      public int Main, Variant;
      public IDSet(int main, int variant = -1) => (Main, Variant) = (main, variant);
      public readonly override bool Equals(object obj)
        => obj is IDSet set && set.Main == Main && set.Variant == Variant;
      public readonly override int GetHashCode() => Main.GetHashCode() ^ Variant.GetHashCode();
      public static bool operator ==(IDSet a, IDSet b) => a.Equals(b);
      public static bool operator !=(IDSet a, IDSet b) => !a.Equals(b);
    }

    #region Logic
    public IDSet ID;
    public int Duration = -1;
    /// <summary>-1은 "스택 개념 없음"을 뜻합니다.</summary>
    public int Stack = -1, MaxStack = -1;
    public EffectAction OnApply = _ => { };
    public EffectAction OnTick = _ => { };
    public EffectAction OnRemove = _ => { };
    public EffectAction OnEnd = null;
    #endregion

    #region Lifecycle
    /// <summary>이 효과가 유닛에게 걸리기 시작할 때 한 번 호출됩니다.</summary>
    /// <remarks>
    /// <see cref="OnApply"/>를 직접 부르지 말고 이쪽을 쓰십시오.
    /// 스택을 쓰는 효과의 <see cref="Stack"/>을 계산에 넣기 전에 0으로 맞춥니다.
    /// 초기값 -1은 "스택 없음"을 뜻하는 UI용 감시값이라, 그대로 두면
    /// 첫 틱까지 약 1초 동안 계수 계산에 -1이 섞여 들어갑니다.
    /// </remarks>
    public void Begin(FieldUnit unit) {
      if (MaxStack >= 0 && Stack < 0) Stack = 0;
      OnApply(unit);
    }

    /// <summary>이 효과가 유닛에게서 완전히 걷힐 때 한 번 호출됩니다.</summary>
    public void End(FieldUnit unit) => OnRemove(unit);

    /// <summary>
    /// 같은 <see cref="ID"/>의 효과가 이미 걸린 상태에서 다시 적용될 때의 규칙입니다.
    /// </summary>
    /// <remarks>
    /// <b>기본은 갱신입니다.</b> <see cref="Duration"/>만 대조해 긴 쪽을 남깁니다.
    /// 가장 단순하고, 다중 적용에서 의도하지 않은 동작이 생길 여지가 가장 적습니다.
    /// <br/>
    /// 중첩이 필요한 효과는 이 메서드를 재정의해 <see cref="Stack"/>을 직접 다루십시오.
    /// 파생 클래스가 자체 상태를 함께 갱신할 수 있도록 <c>virtual</c>로 열어 둡니다.
    /// </remarks>
    /// <param name="incoming">새로 적용하려던 효과. 버려지므로 필요한 값만 옮겨 담으십시오.</param>
    public virtual void Refresh(UnitEffect incoming) {
      //? -1은 무기한을 뜻하므로 어느 쪽이든 -1이면 무기한이 남습니다.
      Duration = (Duration < 0 || incoming.Duration < 0)
        ? -1
        : Mathf.Max(Duration, incoming.Duration);
    }

    /// <summary>중첩을 일부만 걷어냅니다.</summary>
    /// <returns>효과가 완전히 만료되어 제거되어야 하면 <c>true</c>.</returns>
    /// <remarks>
    /// 중첩 제거로 만료되는 효과와 그렇지 않은 효과가 모두 있을 수 있어
    /// 판단을 효과 쪽에 둡니다. 재정의해서 바꿀 수 있습니다.
    /// </remarks>
    public virtual bool Consume(int amount, FieldUnit unit) {
      //? 스택 개념이 없는 효과에 부분 제거를 요청하면 통째로 걷어냅니다.
      if (MaxStack < 0) { End(unit); return true; }

      Stack = Mathf.Max(0, Stack - amount);
      if (Stack > 0) return false;

      End(unit);
      return true;
    }
    #endregion

    #region UI
    public string Name = "";
    public bool Visible = true;
    public Sprite Icon;
    public EffectText DurationText;
    public EffectText MaxStackText;
    public EffectText[] Description = new EffectText[0];
    #endregion

    public override bool Equals(object obj)
      => obj is UnitEffect e
        && e.ID == ID;
    public override int GetHashCode() => ID.GetHashCode();
  }

  public struct RichText {
    public string text;
    public Color boxColor;
    public Color textColor;
    public RichText(string text, Color boxColor = default, Color textColor = default) {
      this.text = text;
      this.boxColor = boxColor;
      this.boxColor.a = Mathf.Min(.25f, boxColor.a);
      this.textColor = textColor;
      m_instantiated = true;
    }
    private readonly bool m_instantiated;

    public readonly override string ToString() {
      if (!m_instantiated) return "";
      return $"<mark=#{ColorUtility.ToHtmlStringRGBA(boxColor)}><color=#{ColorUtility.ToHtmlStringRGBA(textColor)}>{text}</color></mark>";
    }
  }
}
