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

  public class A_LightBreeze : Ability {
    public A_LightBreeze(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "산들바람 " + Level;
      Summary = "하체의 균형으로 가벼운 보폭을 유지합니다.";
      Description = "";
      Toggleable = true;
      FieldID = Fld.Travel;
      AbilID = Abil.MovementReinforcement_Alpha;
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/running");

      Effect = new(this) {
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

    public override void Invoke(FieldUnit unit) { }
    public override void ToggleOn(FieldUnit unit)
      => unit.ApplyEffect(Effect);
    public override void ToggleOff(FieldUnit unit)
      => unit.RemoveEffect(Effect);
  }

  public class A_RunnersHigh : Ability {
    public A_RunnersHigh(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "러너스 하이 " + Level;
      Summary = "달리는 기쁨을 받아들입니다.";
      Description = ""; //todo ???
      FieldID = Fld.Travel;
      AbilID = Abil.MovementReinforcement_Beta;
      Toggleable = true;
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/running");

      Effect = new(this) {
        MaxStack = MaxStack,
        OnApply = u => {
          u.info.Fatigue_Run.Apply += ApplyFatigue;
          u.stat.RunSPCost.Apply += ApplySP;
        },
        OnRemove = u => {
          u.info.Fatigue_Run.Apply -= ApplyFatigue;
          u.stat.RunSPCost.Apply -= ApplySP;
        },
        OnTick = FatigueStack,
        DurationText = _ => $"{"피로".Highlight()} 전까지 지속",
        Description = new EffectText[] {
          _ => "질주 시 피로 " + $"{Fatigue-1:P0} 증가".Color(Color.red) + $"\n(지속 질주로 {FatigueMod+1-Fatigue:P0} 감소)".Color(Color.gray),
          _ => "질주 시 SP 소모 " + $"{1-SP:P0} 감소".Color(Color.green),
          _ => "즐기는 자에게 한계란 없는 법".Flavor()
        }
      };
    }
    float FatigueMod => Level switch { 1 => 3, 2 => 3, _ => 2 };
    float FatigueStackMod => Level switch { 1 => .02f, 2 => .04f, _ => .06f };
    float SPMod => Level switch { 1 => .01f, 2 => .02f, _ => .04f };
    int MaxStack => Level switch { 1 => 100, 2 => 50, _ => 25 };

    public override void Invoke(FieldUnit unit) { }
    public override void ToggleOn(FieldUnit unit)
      => unit.ApplyEffect(Effect);
    public override void ToggleOff(FieldUnit unit) {
      Reset();
      unit.RemoveEffect(Effect);
    }

    bool trigger = false;
    float elapsed = 0;
    void Reset() {
      Effect.Stack = 0;
      trigger = false;
      elapsed = 0;
    }

    RVFloat ApplyFatigue(RVFloat f) => f *= Fatigue;
    RVFloat ApplySP(RVFloat f) => f *= SP;
    void FatigueStack(FieldUnit unit) {
      elapsed += Time.deltaTime;
      //* 중첩 연산
      if (unit.status.Running) {
        if (trigger) { // 계속 달리는 중
          if (elapsed >= 1) {
            Effect.Stack = Mathf.Clamp(Effect.Stack + 1, 0, MaxStack);
            elapsed = 0;
          }
        } else { // 스택을 잃거나 없는 상태에서 질주 시작
          trigger = true;
          elapsed = 0;
        }
      } else {
        if (trigger) { // 직전까지 달림
          trigger = false;
          elapsed = 0;
        } else { // 달리지 않음
          if (elapsed >= 1) {
            Effect.Stack = Mathf.Clamp(Effect.Stack - (int)(MaxStack * 0.2f), 0, MaxStack);
            elapsed = 0;
          }
        }
      }
    }
    /// <summary>활성화 중 적용되는 질주 피로 배율</summary>
    float Fatigue => 1 + FatigueMod - Effect.Stack * FatigueStackMod;
    /// <summary>활성화 중 적용되는 질주 SP 배율</summary>
    float SP => 1 - SPMod * Effect.Stack;
  }

  public class A_IronStep : Ability {
    float rcr = 0;
    bool hp_trigger = false;
    float HpResMod => Level switch { 1 => 1.25f, 2 => 1.75f, _ => 2.5f };
    float WalkSpdMod => Level switch { 1 => .50f, 2 => .25f, _ => 0 };
    const float MIN_HP_RATIO = .10f;

    public A_IronStep(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "무쇠걸음 " + Level;
      Summary = "다리의 힘을 한계까지 끌어냅니다.";
      Description = ""; //todo ???
      FieldID = Fld.Travel;
      AbilID = Abil.MovementReinforcement_Gamma;
      Toggleable = true;
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/hiking");

      Effect = new(this) {
        ID =  new(ID, 0),
        DurationText = (unit) => "HP가 소진되기 전까지 지속",
        // MaxStackText = (unit) => "",
        Description = new EffectText[] {
          u => "SP 소진 시 HP를 소모하여 보행 가능" + $"\n(현재 {rcr/u.stat.HPMax.Value:P0} / 최대 {1-MIN_HP_RATIO:P0})".Ignore(),
          _ => $"중량 {"보행속도 감소".Highlight()} " + $"{1-WalkSpdMod:P0} 완화".Color(Color.green),
          _ => "진정한 힘은 통제된 동작에서 피어납니다.".Flavor()
        },

        OnApply = unit => {
          unit.MovementDecision = MovementDecision;
          unit.stat.SPRegen.Nullify = true; // SP Regen Disabled
          unit.info.WalkSpeedM_Heavy *= WalkSpdMod;
        },
        OnTick = HPRestriction,
        OnRemove = unit => {
          unit.MovementDecision = unit.Default_MovementDecision;
          unit.stat.SPRegen.Nullify = false; // SP Regen Enabled
          unit.info.WalkSpeedM_Heavy /= WalkSpdMod;
          unit.ApplyEffect(HPRestoration);
        }
      };
    }
    void HPRestriction(FieldUnit u) {
      if (u.stat.HPRatio <= MIN_HP_RATIO) {
        ToggleOff(u);
        return;
      }

      float amt = 0;
      if (hp_trigger && u.status.Moving)
        amt = u.status.speed * HpResMod * Time.deltaTime;
      rcr += amt;
      u.stat.HPRCR += amt;
    }
    UnitEffect HPRestoration => new(this) {
      ID = new(ID, 1),
      Visible = false,
      Name = "무쇠걸음: 피로 회복",
      DurationText = _ => "소모분 완전 회복 시 해제",
      Description = new EffectText[] {
        _ => "제한된 HP 자연 회복중" + $"\n잔여 회복량: {rcr:F0}".Ignore(),
      },
      OnTick = unit => {
        var amt = unit.stat.HPRegen.Value * Time.deltaTime;
        if (rcr <= amt) {
          unit.stat.HPRCR -= rcr;
          rcr = 0;
          unit.RemoveEffect(HPRestoration);
        } else {
          rcr -= amt;
          unit.stat.HPRCR -= amt;
        }
      },
    };

    MovementStatus MovementDecision(FieldUnit u) {
      if (hp_trigger = u.stat.SPRatio <= 0) // Consume HP
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
    public override void ToggleOn(FieldUnit unit)
      => unit.ApplyEffect(Effect);
    public override void ToggleOff(FieldUnit unit)
      => unit.RemoveEffect(Effect);
  }
}