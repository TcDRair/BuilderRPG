using System.Collections.Generic;
using System;

using UnityEngine;
using UnityEngine.AI;

using Rair.Field.Interact;
using Rair.Skill;
using Rair.Skill.AbilityStorage;

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

    #region Tick Events
    public delegate RFloat ValueTick(FieldUnit unit);
    public event ValueTick HPVTick, HPRCRVTick, SPVTick, LoadVTick;
    #endregion

    #region Stat Appliers
    /// <summary>
    /// 상황에 따라 유닛이 달리거나 걸을 수 있는지 판단합니다.
    /// </summary>
    /// <returns>0: 보행 불가, 1: 보행 가능, 2: 주행 가능</returns>
    public Func<int> Movement_Available { get; set; }
    public virtual int MoveInt() {
      var (lr, _, la, lh) = LoadVariables();
      float spRatio = stat.SP.Value / stat.SPMax.Value;
      if (spRatio <= .01f) move_trigger = true; // regenerating phase start

      if (move_trigger) { // regenerating phase
        if (spRatio >= .25f) move_trigger = false; // regenerating phase end
        if (lr <= la) return 1;
        else return 0;
      }
      return (lr <= la)
        ? 2
        : (lr <= lh ? 1 : 0);
    }
    protected bool move_trigger = false;
    #endregion

    #region LEGACY
    /// <summary>유닛이 이동하면 취소되는 코루틴을 할당합니다</summary>
    public (Interaction i, Coroutine c) moveTask_Approaching;
    /// <summary>유닛이 이동하면 취소되는 상호작용 작업을 할당합니다</summary>
    public (Prop p, IATask t) moveTask_Interacting;
    #endregion
    public virtual bool RunIntent => true;

    #region Load
    public (float ratio, float light, float average, float heavy) LoadVariables()
  => (stat.Load.Value / stat.LoadMax.Value, info.Load_Light.Value, info.Load_Average.Value, info.Load_Heavy.Value);
    #endregion

    #region Effect
    /// <summary>실시간 동작하는 효과들을 할당합니다</summary>
    readonly Dictionary<int, UnitEffect> effects = new(), hiddenEffects = new();
    public void ApplyEffect(int id, UnitEffect effect) {
      if (effects.TryGetValue(id, out var e)) {
        //todo 효과 중첩 or 갱신 or 덮어쓰기.
        //todo Effect 내에서 수행할 것
      } else {
        effects.Add(id, effect);
        effect.OnApply(this);
      }
    }
    public void RemoveEffect(int id, int stack = -1) {
      if (stack < 0) {
        effects.Remove(id);
      }
      else if (effects.TryGetValue(id, out var e)) {
        //todo 효과 중첩 제거. Effect 내에서 수행할 것
      }
    }
    #endregion

    #region Unity Events + Tick
    protected virtual void Awake() {

    }

    protected virtual void Start() {
      Movement_Available = MoveInt;
      stat.SPRegen.Apply += Movement_SPRegen;
      stat.SPCost.Apply += Movement_SPCost;
      stat.RunSpeed.Apply += Movement_RunSpeed;
      stat.WalkSpeed.Apply += Movement_WalkSpeed;
    }

    protected virtual void Update() {
      Tick();
    }

    protected void Tick() {
      //* 이동 상태 갱신
      int move = Movement_Available();
      status.speed = agent.velocity.magnitude;
      status.moving = status.speed > .05f && move >= 1;
      status.running = status.moving && RunIntent && move == 2;

      agent.speed = RunIntent && move == 2
        ? stat.RunSpeed.Value
        : (move >= 1 ? stat.WalkSpeed.Value : 0);

      //* 회복/감소 연산
      stat.HP.Value += (stat.HPRegen.Value - stat.HPCost.Value) * Time.deltaTime;
      if (status.running)
        stat.SP.Value -= stat.SPCost.Value * Time.deltaTime;
      else
        stat.SP.Value += stat.SPRegen.Value * Time.deltaTime;

      //* 피로 연산
      status.fatigueTick =
        status.running ? info.Fatigue_Run.Value :
        status.moving ? info.Fatigue_Walk.Value :
        status.sitting ? info.Fatigue_Sit.Value :
        info.Fatigue_Idle.Value;
      stat.Fatigue.Value += status.fatigueTick * Time.deltaTime / 60; // 분당 피로

      //* 효과 적용
      foreach (var e in hiddenEffects.Values)
        e.OnTick(this);
      foreach (var e in effects.Values)
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