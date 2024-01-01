using System.Collections.Generic;

using UnityEngine;

using Rair.Field;
using System;
using Unity.VisualScripting;
namespace Rair.Skill
{
  public delegate void EffectAction(FieldUnit unit);
  public delegate string EffectText(FieldUnit unit);
  public class UnitEffect
  {
    #region Logic
    public int Duration = -1;
    public int Stack = -1, MaxStack = -1;
    public EffectAction OnApply = _ => { };
    public EffectAction OnTick = _ => { };
    public EffectAction OnRemove = _ => { };
    public EffectAction OnEnd = _ => { };
    #endregion

    #region UI
    public string Name = "";
    public Sprite Icon;
    // new() : blank string
    public EffectText DurationText;
    public EffectText MaxStackText;
    public EffectText[] Description = new EffectText[0];
    #endregion
  }

  public struct RichText
  {
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
      return $"<mark=#{boxColor.ToHexString()}><color=#{textColor.ToHexString()}>{text}</color></mark>";
    }
  }
}