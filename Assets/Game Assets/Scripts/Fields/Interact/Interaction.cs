using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UltEvents;

namespace Rair.Field.Interact
{
  [Serializable]
  public class Interaction : ScriptableObject
  {
    // 기본 지정 스프라이트
    public Sprite BattleSprite, ConstructSprite, DestroySprite, RepairSprite;

    #region 공통 속성
    public enum Type { Action, Task, Special }
    public Sprite sprite;
    public new string name;
    public string tooltip;
    public int level = -1;
    public Type type;
    public UnityEvent onTriggered;
    public bool cancelable = false;
    public UnityEvent<float> onCancelled;
    public bool useCondition = false;
    public Func<bool> onCondition;
    #endregion
    //TODO 요구 기술
    /// <summary>지정되지 않았을 경우 애니메이션의 길이로 대체합니다.</summary>
    public float duration = 0;
    [HideInInspector] public AnimationClip animation;
    public enum SpecialType { Battle, Construct, Destroy, Dismantle }
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