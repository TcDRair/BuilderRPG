using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Rair.Field.Interact
{
  public class Task
  {
    public Interaction interaction;
    public float elapsedTime = 0;
    public Task(Interaction interaction) => this.interaction = interaction;
  }

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
    public float maxDistance = -1;
    public UnityEvent onTriggered = new();
    public bool cancelable = false;
    public UnityEvent<float> onCancelled = new();
    public bool useCondition = false;
    public Func<bool> onCondition;
    #endregion
    //TODO 요구 기술
    /// <summary>지정되지 않았을 경우 애니메이션의 길이로 대체합니다.</summary>
    public float duration = 0;
    [HideInInspector] public AnimationClip animation;
    public enum SpecialType { 전투, 건설, 파괴, 철거, 증축 }
    public SpecialType specialType;
    public Sprite SpecialSprite => specialType switch
    {
      SpecialType.전투 => BattleSprite,
			SpecialType.건설 => ConstructSprite,
			_ => null
    };
    //TODO Enum => Color + Sprite

    public override string ToString() => $"{(level > 0 ? $"[{level}]" : "")} {name}";
  }
}