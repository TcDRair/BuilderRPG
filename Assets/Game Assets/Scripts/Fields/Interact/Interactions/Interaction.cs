using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Rair.Field.Interact
{
  public class IATask
  {
    public Interaction interaction;
    public float elapsedTime = 0;
    public int current = 0;
    public Prop prop;
    public FieldUnit unit;
    public IATask(FieldUnit unit, Prop prop, Interaction interaction)
    {
      this.unit = unit;
      this.prop = prop;
      this.interaction = interaction;
    }
  }

  [Serializable]
  public class Interaction : ScriptableObject
  {
    public const string SpriteFolder = "Sprites/Interaction/";
    // 기본 지정 스프라이트
    public Sprite BattleSprite => Resources.Load<Sprite>(SpriteFolder + "Battle");
    public Sprite ConstructSprite => Resources.Load<Sprite>(SpriteFolder + "Construct");
    public Sprite DestroySprite => Resources.Load<Sprite>(SpriteFolder + "Destroy");
    public Sprite RepairSprite => Resources.Load<Sprite>(SpriteFolder + "Repair");

    #region 개요
    public Sprite sprite;
    public new string name;
    public string tooltip;
    public int level = -1;
    /// <summary>상호작용이 가능한 최대 거리</summary>
    public float maxDistance = -1;
    #endregion
    #region Functions
    public Func<bool> onCondition;
    public readonly List<IAProgress> progress = new();
    #endregion
    //TODO 요구 기술
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

  public class IAProgress
  {
    public enum Type { Auto, Manual, Special }
    public readonly Type type;
    public readonly float duration;
    public readonly FieldAnimatorState anim;
    public delegate void Act(IATask task);
    public readonly Act onStart, onGoing, onCancelled, onEnd;
    public readonly bool cancellable;
    /// <summary>
    /// 상호작용 프로세스를 생성합니다.
    /// </summary>
    /// <param name="type">프로세스가 자동으로 진행되는지를 지정합니다.</param>
    /// <param name="duration">프로세스 진행 시간을 지정합니다.</param>
    /// <param name="anim">진행 중 재생할 애니메이션을 지정합니다.</param>
    /// <param name="onStart">시작 시 작업을 지정합니다.</param>
    /// <param name="onGoing">진행 중 작업을 지정합니다.</param>
    /// <param name="onCancelled">취소 시 작업을 지정합니다.</param>
    /// <param name="onEnd">완료 시 작업을 지정합니다.</param>
    /// <param name="cancellable">취소 가능 여부를 지정합니다.</param>
    public IAProgress(Type type, float duration, FieldAnimatorState anim = FieldAnimatorState.None, Act onStart = null, Act onGoing = null, Act onCancelled = null, Act onEnd = null, bool cancellable = true) {
      this.type = type;
      this.duration = duration;
      this.anim = anim;
      this.onStart = onStart;
      this.onGoing = onGoing;
      this.onCancelled = onCancelled;
      this.onEnd = onEnd;
      this.cancellable = cancellable;
    }
  }
}
