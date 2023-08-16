using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Rair.Field.Interact
{
  [Serializable]
  public class Interaction : ScriptableObject
  {
    public Sprite BattleSprite, ConstructSprite;
    #region 공통 속성
    public enum Type { Action, Task, Special }
    public Sprite sprite;
    public new string name;
    public string tooltip;
    public int level;
    public Type type;
    public UnityEvent onSelected = new();
    #endregion

    //TODO 요구 기술
    /// <summary>지정되지 않았을 경우 애니메이션의 길이로 대체합니다.</summary>
    public float duration = 0;
    [HideInInspector] public AnimationClip animation;
    public enum SpecialType { Battle, Construct }
    public SpecialType specialType;
    public Sprite SpecialSprite => specialType switch
    {
      SpecialType.Battle => BattleSprite,
			SpecialType.Construct => ConstructSprite,
			_ => null
    };
    //TODO Enum => Color + Sprite


    public override string ToString() => $"[{level}] {name}";
  }
}