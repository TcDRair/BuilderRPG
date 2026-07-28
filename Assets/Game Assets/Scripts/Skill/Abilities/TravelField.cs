using System;
using UnityEngine;

using Rair.Field;

namespace Rair.Skill.AbilityStorage {
  public class Survival : Profession
  {
    //todo 전체 partial class로 묶을 것 (생각해보니 얘가 독자적으로 뭘 하지 않는다)
    public Survival() { ID = Prof.Survival; }
  }
  public class Travel : Field
  {
    //todo 얘도
    public Travel() {
      ProfID = Prof.Survival;
      ID = Fld.Travel;
    }
  }

  public class LightBreeze : Ability {
    public LightBreeze(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "산들바람 " + Level;
      Summary = "하체의 균형으로 가벼운 보폭을 유지합니다.";
      Description = "";
      Toggleable = true;
      FieldID = Fld.Travel;
      AbilID = Abil.MovementReinforcement_Alpha;
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/running");
    }

    float RunSpeedMod => Level switch { 1 => .10f, 2 => .20f, _ => .30f };
    float SPReduction => .5f;
    float Overload => Level switch { 1 => .80f, 2 => .85f, _ => .90f };

    bool Light(FieldUnit u) => u.status.load == LoadStatus.Lightweight;
    bool Over(FieldUnit u) => u.status.load == LoadStatus.Overburdened;
    float LoadRatioRelativeDecision(FieldUnit.LoadRatio lr)
      => Mathf.Min(lr.ratio / lr.heavy, 1);
    LoadStatus LoadStatusDecision(FieldUnit.LoadRatio lr)
      => lr.ratio <= lr.heavy ? LoadStatus.Lightweight : LoadStatus.Overburdened;

    //? 이 어빌리티는 진행 상태가 없어 UnitEffect를 그대로 씁니다.
    public override UnitEffect CreateEffect(FieldUnit unit) => new(this) {
      DurationText = _ => $"",
      OnApply = (FieldUnit u) => {
        u.stat.RunSpeed *= 1 + RunSpeedMod;
        u.stat.RunSPCost *= 1 - SPReduction;
        u.stat.LoadMax *= (1 - Overload);
        u.LoadStatusDecision = LoadStatusDecision;
        u.LoadRatioRelativeDecision = LoadRatioRelativeDecision;
      },
      OnRemove = u => {
        u.stat.RunSpeed /= 1 + RunSpeedMod;
        u.stat.RunSPCost /= 1 - SPReduction;
        u.stat.LoadMax /= (1 - Overload);
        u.LoadStatusDecision = u.Default_LoadStatusDecision;
        u.LoadRatioRelativeDecision = u.Default_LoadRatioRelative;
      },
      Description = new EffectText[] {
        _ => "질주 속도 " + $"{RunSpeedMod:P0} 증가".Color(Color.green),
        _ => "질주 SP 소모 " + $"{SPReduction:P0} 감소".Color(Color.green),
        u => "최대 하중 " + $"{Overload:P0} 감소".Color(Color.red) + $"\n하중 상태가 {"경량".Color(Color.white, Light(u))}/{"과적".Color(Color.white, Over(u))}으로 제한됨".Ignore(),
        _ => "하체의 균형으로 가벼운 보폭을 유지합니다.".Flavor()
      }
    };

    public override void Invoke(FieldUnit unit) { }
  }


  public class RunnersHigh : Ability {
    /// <summary>이 어빌리티가 유닛마다 들고 있어야 하는 진행 상태입니다.</summary>
    /// <remarks>
    /// 이전에는 <c>trigger</c>/<c>elapsed</c>가 어빌리티의 인스턴스 필드였습니다.
    /// 어빌리티는 공유될 예정이므로 그대로 두면 유닛끼리 서로의 타이머를 덮어씁니다.
    /// </remarks>
    sealed class State : UnitEffect {
      public State(Ability ability) : base(ability) { }
      /// <summary>직전 프레임에 질주 중이었는지</summary>
      public bool running = false;
      /// <summary>스택 증감 타이머 (초)</summary>
      public float elapsed = 0;
    }

    public RunnersHigh(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "러너스 하이 " + Level;
      Summary = "달리는 기쁨을 받아들입니다.";
      Description = ""; //todo ???
      FieldID = Fld.Travel;
      AbilID = Abil.MovementReinforcement_Beta;
      Toggleable = true;
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/running");
    }

    float FatigueMod => Level switch { 1 => 3, 2 => 3, _ => 2 };
    float FatigueStackMod => Level switch { 1 => .02f, 2 => .04f, _ => .06f };
    float SPMod => Level switch { 1 => .01f, 2 => .02f, _ => .04f };
    int MaxStackOf => Level switch { 1 => 100, 2 => 50, _ => 25 };

    /// <summary>활성화 중 적용되는 질주 피로 배율</summary>
    float Fatigue(UnitEffect e) => 1 + FatigueMod - e.Stack * FatigueStackMod;
    /// <summary>활성화 중 적용되는 질주 SP 배율</summary>
    float SP(UnitEffect e) => 1 - SPMod * e.Stack;

    public override UnitEffect CreateEffect(FieldUnit unit) {
      var state = new State(this) {
        MaxStack = MaxStackOf,
        DurationText = _ => $"{"피로".Highlight()} 전까지 지속",
      };

      //? 적용기는 붙일 때와 뗄 때 같은 델리게이트여야 하므로 지역 변수로 잡아 둡니다.
      RAFloat.Applier fatigue = f => f * Fatigue(state);
      RAFloat.Applier spCost = f => f * SP(state);

      state.OnApply = u => {
        u.info.Fatigue_Run.Apply += fatigue;
        u.stat.RunSPCost.Apply += spCost;
      };
      state.OnRemove = u => {
        u.info.Fatigue_Run.Apply -= fatigue;
        u.stat.RunSPCost.Apply -= spCost;
      };
      state.OnTick = u => FatigueStack(u, state);
      state.Description = new EffectText[] {
        _ => "질주 시 피로 " + $"{Fatigue(state)-1:P0} 증가".Color(Color.red) + $"\n(지속 질주로 {FatigueMod+1-Fatigue(state):P0} 감소)".Color(Color.gray),
        _ => "질주 시 SP 소모 " + $"{1-SP(state):P0} 감소".Color(Color.green),
        _ => "즐기는 자에게 한계란 없는 법".Flavor()
      };

      return state;
    }

    void FatigueStack(FieldUnit unit, State s) {
      s.elapsed += Time.deltaTime;
      //* 중첩 연산
      if (unit.status.Running) {
        if (s.running) { // 계속 달리는 중
          if (s.elapsed >= 1) {
            s.Stack = Mathf.Clamp(s.Stack + 1, 0, MaxStackOf);
            s.elapsed = 0;
          }
        } else { // 스택을 잃거나 없는 상태에서 질주 시작
          s.running = true;
          s.elapsed = 0;
        }
      } else {
        if (s.running) { // 직전까지 달림
          s.running = false;
          s.elapsed = 0;
        } else { // 달리지 않음
          if (s.elapsed >= 1) {
            s.Stack = Mathf.Clamp(s.Stack - (int)(MaxStackOf * 0.2f), 0, MaxStackOf);
            s.elapsed = 0;
          }
        }
      }
    }

    public override void Invoke(FieldUnit unit) { }
  }


  public class IronStep : Ability {
    /// <summary>유닛마다의 진행 상태. 주 효과와 회복 효과가 같은 인스턴스를 공유합니다.</summary>
    sealed class State : UnitEffect {
      public State(Ability ability) : base(ability) { }
      /// <summary>이 어빌리티로 소모해 되돌려받아야 할 HP 총량 (구 <c>rcr</c>)</summary>
      public float restricted = 0;
      /// <summary>현재 SP가 바닥나 HP를 대신 소모하는 중인지 (구 <c>hp_trigger</c>)</summary>
      public bool consuming = false;
    }

    float HpResMod => Level switch { 1 => 1.25f, 2 => 1.75f, _ => 2.5f };
    float WalkSpdMod => Level switch { 1 => .50f, 2 => .25f, _ => 0 };
    const float MIN_HP_RATIO = .10f;

    public IronStep(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "무쇠걸음 " + Level;
      Summary = "다리의 힘을 한계까지 끌어냅니다.";
      Description = ""; //todo ???
      FieldID = Fld.Travel;
      AbilID = Abil.MovementReinforcement_Gamma;
      Toggleable = true;
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/hiking");
    }

    public override UnitEffect CreateEffect(FieldUnit unit) {
      var state = new State(this) {
        ID = new(ID, 0),
        DurationText = _ => "HP가 소진되기 전까지 지속",
      };

      state.Description = new EffectText[] {
        u => "SP 소진 시 HP를 소모하여 보행 가능" + $"\n(현재 {state.restricted/u.stat.HPMax.Value:P0} / 최대 {1-MIN_HP_RATIO:P0})".Ignore(),
        _ => $"중량 {"보행속도 감소".Highlight()} " + $"{1-WalkSpdMod:P0} 완화".Color(Color.green),
        _ => "진정한 힘은 통제된 동작에서 피어납니다.".Flavor()
      };
      state.OnApply = u => {
        u.MovementDecision = unit => MovementDecision(unit, state);
        u.stat.SPRegen.Nullify = true; // SP Regen Disabled
        u.info.WalkSpeedM_Heavy *= WalkSpdMod;
      };
      state.OnTick = u => HPRestriction(u, state);
      state.OnRemove = u => {
        u.MovementDecision = u.Default_MovementDecision;
        u.stat.SPRegen.Nullify = false; // SP Regen Enabled
        u.info.WalkSpeedM_Heavy /= WalkSpdMod;
        u.ApplyEffect(CreateRestoration(state));
      };

      return state;
    }

    void HPRestriction(FieldUnit u, State s) {
      if (u.stat.HPRatio <= MIN_HP_RATIO) {
        ToggleOff(u);
        return;
      }

      float amt = 0;
      if (s.consuming && u.status.Moving)
        amt = u.status.speed * HpResMod * Time.deltaTime;
      s.restricted += amt;
      u.stat.HPRCR += amt;
    }

    /// <summary>주 효과가 걷힌 뒤 소모분을 되돌려주는 부수 효과입니다.</summary>
    /// <remarks>
    /// 주 효과의 <see cref="State"/>를 그대로 참조합니다.
    /// 이전에는 접근할 때마다 새 <see cref="UnitEffect"/>를 만드는 프로퍼티였고,
    /// 자기 자신을 제거할 때도 새 인스턴스를 만들어 넘겼습니다.
    /// ID로만 대조되어 동작하긴 했으나 의도를 읽기 어려웠습니다.
    /// </remarks>
    UnitEffect CreateRestoration(State state) {
      var restoration = new UnitEffect(this) {
        ID = new(ID, 1),
        Visible = false,
        Name = "무쇠걸음: 피로 회복",
        DurationText = _ => "소모분 완전 회복 시 해제",
        Description = new EffectText[] {
          _ => "제한된 HP 자연 회복중" + $"\n잔여 회복량: {state.restricted:F0}".Ignore(),
        },
      };
      restoration.OnTick = unit => {
        var amt = unit.stat.HPRegen.Value * Time.deltaTime;
        if (state.restricted <= amt) {
          unit.stat.HPRCR -= state.restricted;
          state.restricted = 0;
          unit.RemoveEffect(restoration.ID);
        } else {
          state.restricted -= amt;
          unit.stat.HPRCR -= amt;
        }
      };
      return restoration;
    }

    MovementStatus MovementDecision(FieldUnit u, State s) {
      if (s.consuming = u.stat.SPRatio <= 0) // Consume HP
        return u.status.load switch {
          LoadStatus.Overburdened => MovementStatus.Idle,
          _ => u.stat.HPRatio > MIN_HP_RATIO ? MovementStatus.Walkable : MovementStatus.Idle
        };
      else return u.status.load switch { // Consume SP
          LoadStatus.Lightweight => MovementStatus.Runnable,
          LoadStatus.Standard => MovementStatus.Runnable,
          LoadStatus.Heavyweight => MovementStatus.Walkable,
          _ => MovementStatus.Idle
        };
    }

    public override void Invoke(FieldUnit unit) { }
  }
}
