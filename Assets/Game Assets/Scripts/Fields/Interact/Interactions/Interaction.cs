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

  /// <summary><see cref="Prop"/>이 런타임에 조립하는 상호작용 정의입니다.</summary>
  /// <remarks>
  /// <b><see cref="ScriptableObject"/>를 상속하지 않습니다. 되돌리지 마십시오.</b>
  /// <para>
  /// 예전에는 상속하고 있었는데, 정작 <see cref="Prop.DefaultDestroyInteraction"/> 등이
  /// <c>new Interaction()</c>으로 생성했습니다. Unity는 <see cref="ScriptableObject"/>의
  /// <c>new</c> 생성을 금지하므로 다음 오류가 나고 <b>객체가 온전하지 않은 상태로 반환</b>됐습니다.
  /// <code>
  /// Interaction must be instantiated using ScriptableObject.CreateInstance
  /// instead of new Interaction.
  /// </code>
  /// <see cref="Prop.OnEnable"/>이 <c>useDestroy</c>/<c>useRepair</c> 플래그에 따라 호출하므로,
  /// 해당 플래그가 켜진 프롭이 씬에 있으면 매 활성화마다 발생했습니다.
  /// 즉 기본 파괴·수리 상호작용이 동작하지 않고 있었습니다. (문서 05 P0-12)
  /// </para>
  /// <see cref="CreateInstance"/>로 바꾸는 대신 상속을 뗀 이유 —
  /// 이 타입은 에셋으로 저장된 적이 없고(프로젝트 내 인스턴스 0건),
  /// <see cref="Prop"/>이 코드에서 만들어 쓰는 값 객체이므로 에셋 수명 관리가 불필요합니다.
  /// </remarks>
  [Serializable]
  public class Interaction
  {
    public const string SpriteFolder = "Sprites/Interaction/";
    // 기본 지정 스프라이트
    public Sprite BattleSprite => Resources.Load<Sprite>(SpriteFolder + "Battle");
    public Sprite ConstructSprite => Resources.Load<Sprite>(SpriteFolder + "Construct");
    public Sprite DestroySprite => Resources.Load<Sprite>(SpriteFolder + "Destroy");
    public Sprite RepairSprite => Resources.Load<Sprite>(SpriteFolder + "Repair");

    #region 개요
    public Sprite sprite;
    //? ScriptableObject.name을 가리던 new 한정자를 제거했습니다. 이제 그냥 자기 필드입니다.
    public string name;
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
