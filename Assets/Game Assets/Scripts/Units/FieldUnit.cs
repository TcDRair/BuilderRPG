using System.Collections.Generic;
using System;

using UnityEngine;
using UnityEngine.AI;

using Rair.Field.Interact;
using Rair.Skill;
using Rair.Skill.AbilityStorage;
using System.Linq;

namespace Rair.Field
{
  public abstract partial class FieldUnit : MonoBehaviour
  {
    public Transform tr;
    public Animator animator;
    public NavMeshAgent agent;
    public Rigidbody rigidBody;

    public UnitStat stat;
    public UnitInfo info;
    public UnitStatus status;

    //todo Decision 관련 함수를 묶을 것

    #region Tick Events
    public delegate RFloat ValueTick(FieldUnit unit);
    public event ValueTick HPVTick, HPRCRVTick, SPVTick, LoadVTick;
    #endregion

    #region Movement
    /// <summary>
    /// 상황에 따라 유닛이 달리거나 걸을 수 있는지 판단합니다. 기본적으로 <see cref="Default_MovementDecision"/>가 할당됩니다.
    /// </summary>
    public Func<FieldUnit, MovementStatus> MovementDecision { get; set; }

    protected bool regenTrigger = false;
    /// <summary>
    /// 유닛의 이동 가능성을 판단하는 기본 함수입니다.
    /// </summary>
    public virtual MovementStatus Default_MovementDecision(FieldUnit u) {
      u.regenTrigger = (u.regenTrigger && u.stat.SPRatio < .25f) || (!u.regenTrigger && u.stat.SPRatio <= .01f);

      return u.status.load switch {
        LoadStatus.Lightweight => u.regenTrigger ? MovementStatus.Walkable : MovementStatus.Runnable,
        LoadStatus.Standard => u.regenTrigger ? MovementStatus.Walkable : MovementStatus.Runnable,
        LoadStatus.Heavyweight => u.regenTrigger ? MovementStatus.Idle : MovementStatus.Walkable,
        _ => MovementStatus.Idle
      };
    }
    #endregion

    #region LEGACY
    /// <summary>유닛이 이동하면 취소되는 코루틴을 할당합니다</summary>
    public (Interaction i, Coroutine c) moveTask_Approaching;
    /// <summary>유닛이 이동하면 취소되는 상호작용 작업을 할당합니다</summary>
    public (Prop p, IATask t) moveTask_Interacting;
    #endregion
    public virtual bool RunIntent => true;

    #region Load
    public struct LoadRatio { public float ratio, light, standard, heavy; }
    public LoadRatio Load { get; private set; }
    public Func<LoadRatio, LoadStatus> LoadStatusDecision;
    public LoadStatus Default_LoadStatusDecision(LoadRatio lr) {
      if (lr.ratio <= lr.light)
        return LoadStatus.Lightweight;
      else if (lr.ratio <= lr.standard)
        return LoadStatus.Standard;
      else if (lr.ratio <= lr.heavy)
        return LoadStatus.Heavyweight;
      else
        return LoadStatus.Overburdened;
    }
    public Func<LoadRatio, float> LoadRatioRelativeDecision;
    public float Default_LoadRatioRelative(LoadRatio lv) {
      if (lv.ratio <= lv.light)
        return lv.ratio / lv.light;
      else if (lv.ratio <= lv.standard)
        return (lv.ratio - lv.light) / (lv.standard - lv.light);
      else if (lv.ratio <= lv.heavy)
        return (lv.ratio - lv.standard) / (lv.heavy - lv.standard);
      else
        return 1;
    }
    /// <summary>하중 구간 내에서의 비율을 반환합니다.</summary>
    public float LoadRatioRelative => LoadRatioRelativeDecision(Load);
    #endregion

    #region Effect
    /// <summary>실시간 동작하는 효과들을 할당합니다</summary>
    readonly Dictionary<UnitEffect.IDSet, UnitEffect> effects = new();
    /// <summary>현재 적용중인 효과를 반환합니다. 여기의 요소를 변경시켜도 실제 효과에는 영향을 주지 않습니다.</summary>
    public List<UnitEffect> Effects => effects.Values.ToList();
    public void ApplyEffect(UnitEffect effect) {
      if (effects.TryGetValue(effect.ID, out var e)) {
        //todo 효과 중첩 or 갱신 or 덮어쓰기.
        //todo Effect 내에서 수행할 것?
      } else {
        effects.Add(effect.ID, effect);
        effect.OnApply(this);
      }
    }
    public void RemoveEffect(UnitEffect effect, int stack = -1) {
      if (!effects.TryGetValue(effect.ID, out var e)) return;
      if (stack < 0){
        e.OnRemove(this);
        effects.Remove(e.ID);
      } else {
        //todo 효과 중첩 제거. Effect 내에서 수행할 것
        //todo 중첩 제거 시 만료되는 것도 있고 아닌 것도 있기 때문
      }
    }
    #endregion

    #region Unity Events + Tick
    protected virtual void Awake() {

    }

    protected virtual void Start() {
      MovementDecision = Default_MovementDecision;
      LoadStatusDecision = Default_LoadStatusDecision;
      LoadRatioRelativeDecision = Default_LoadRatioRelative;
      stat.HPRegen.Apply += Default_HPRegen;
      stat.SPRegen.Apply += Default_SPRegen;
      stat.SPCost.Apply += Default_SPCost;
      stat.RunSpeed.Apply += Default_RunSpeed;
      stat.WalkSpeed.Apply += Default_WalkSpeed;
    }

    protected virtual void Update() {
      UpdateStatus();
      Tick();
    }

    protected void UpdateStatus() {
      var movement = agent.WannaMoving() ? (RunIntent ? MovementStatus.Runnable : MovementStatus.Walkable) : MovementStatus.Immovable;
      status.movement = (movement & (status.movable = MovementDecision(this))).MaxBit();
      status.speed = agent.velocity.magnitude;

      status.fatiguePerMinute = status.movement switch {
        MovementStatus.Sit => info.Fatigue_Sit.Value,
        MovementStatus.Walk => info.Fatigue_Walk.Value,
        MovementStatus.Run => info.Fatigue_Run.Value,
        _ => info.Fatigue_Idle.Value
      };

      Load = new() {
        ratio = stat.Load.Value / stat.LoadMax.Value,
        light = info.LoadLimit_Lightweight.Value,
        standard = info.LoadLimit_Standard.Value,
        heavy = info.LoadLimit_Heavyweight.Value
      };
      status.load = LoadStatusDecision(Load);
    }

    protected void Tick() {
      //* 이동 상태 갱신
      agent.speed = status.movement switch {
        MovementStatus.Run => stat.RunSpeed.Value,
        MovementStatus.Walk => stat.WalkSpeed.Value,
        _ => 0
      };

      //* 회복/감소 연산
      stat.HP += (stat.HPRegen.Value - stat.HPCost.Value) * Time.deltaTime;
      stat.SP += (stat.SPRegen.Value - stat.SPCost.Value) * Time.deltaTime;

      //* 피로 연산
      stat.Fatigue += status.fatiguePerMinute * Time.deltaTime / 60; // 분당 피로

      //* 효과 적용
      foreach (var e in effects.Values.ToList())
        e.OnTick(this);
    }

    #endregion

    /// <summary>
    /// 지정 위치로 이동을 시도하고 진행중인 작업을 처리합니다.
    /// </summary>
    /// <param name="point">이동할 위치입니다.</param>
    protected void Move(Vector3 point) {
      if (moveTask_Approaching.c is not null) {
        StopCoroutine(moveTask_Approaching.c);
        moveTask_Approaching = default;
      } else if (moveTask_Interacting != default) {
        moveTask_Interacting.p.PauseCurrentTask();
        moveTask_Interacting = default;
        animator.Play(FieldAnimatorState.Idle.ToString());
      }
      agent.SetDestination(point);
    }
    public void PlayAnim(FieldAnimatorState state) => animator.SetTrigger(state.ToString());
  }

  public enum FieldAnimatorState
  {
    /// <summary>애니메이터에 영향을 주지 않음</summary>
    None,

    Idle,
    Walk,
    Run,
    Sit,

    Attack,

    Mine,
    Construct,
    Destroy,
    Repair,
  }
}
