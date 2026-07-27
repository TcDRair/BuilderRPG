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
    public int Stack = -1, MaxStack = -1;
    public EffectAction OnApply = _ => { };
    public EffectAction OnTick = _ => { };
    public EffectAction OnRemove = _ => { };
    public EffectAction OnEnd = null;
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
