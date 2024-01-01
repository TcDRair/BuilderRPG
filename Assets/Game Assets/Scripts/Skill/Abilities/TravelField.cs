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

  public class LightBreeze : Ability
  {
    public LightBreeze(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "산들바람" + Level;
      Summary = "하체의 균형으로 가벼운 보폭을 유지합니다.";
      /*EffectText = new RichText[] {
      new("", Color.clear),
      };*/
      FieldID = Fld.Travel;
      AbilID = Abil.MovementReinforcement_Alpha;
      Icon = Resources.Load<Sprite>("");
      Toggleable = false;
    }

    public override void Invoke(FieldUnit unit) { }
    public override void ToggleOn(FieldUnit unit) { }
    public override void ToggleOff(FieldUnit unit) { }
  }


  public class RunnersHigh : Ability
  {
    public RunnersHigh(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "러너스 하이 " + Level;
      Summary = "달리는 기쁨을 받아들입니다.";
      FieldID = Fld.Travel;
      AbilID = Abil.MovementReinforcement_Beta;
      Toggleable = true;
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/running");

      Effect = new() {
        Name = Name,
        MaxStack = MaxStack,
        OnTick = FatigueStack,
        Icon = Icon,
        DurationText = _ => $"{"피로".Interested()} 전까지 지속",
        Description = new EffectText[] {
          _ => "질주 시 피로 " + $"{Fatigue-1:P0} 증가".Colored(Color.red) + $"\n(지속 질주로 {FatigueMod+1-Fatigue:P0} 감소)".Colored(Color.gray),
          _ => "질주 시 SP 소모 " + $"{1-SP:P0} 감소".Colored(Color.green),
          _ => "<i>\"즐기는 자에게 한계란 없는 법.\"</i>".Colored(Color.gray)
        }
      };
    }
    float FatigueMod => Level switch { 1 => 3, 2 => 3, _ => 2 };
    float FatigueStackMod => Level switch { 1 => .02f, 2 => .04f, _ => .06f };
    float SPMod => Level switch { 1 => .01f, 2 => .02f, _ => .04f };
    int MaxStack => Level switch { 1 => 100, 2 => 50, _ => 25 };

    public override void Invoke(FieldUnit unit) { }
    public override void ToggleOn(FieldUnit unit)
      => unit.ApplyEffect(ID, Effect);
    public override void ToggleOff(FieldUnit unit) {
      Reset();
      unit.RemoveEffect(ID);
    }

    bool trigger = false;
    float elapsed = 0, prevFat = 1, prevSP = 1;
    void Reset() {
      Effect.Stack = 0;
      trigger = false;
      elapsed = 0;
      prevFat = 1;
      prevSP = 1;
    }

    void FatigueStack(FieldUnit unit) {
      elapsed += Time.deltaTime;
      //* 중첩 연산
      if (unit.status.running) {
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
      //* 피로 연산
      unit.info.Fatigue_Run /= prevFat;
      unit.info.Fatigue_Run *= Fatigue;
      unit.stat.RunSPCost.value /= prevSP;
      unit.stat.RunSPCost.value *= SP;

      prevFat = Fatigue;
      prevSP = SP;
    }
    /// <summary>활성화 중 적용되는 질주 피로 배율</summary>
    float Fatigue => 1 + FatigueMod - Effect.Stack * FatigueStackMod;
    /// <summary>활성화 중 적용되는 질주 SP 배율</summary>
    float SP => 1 - SPMod * Effect.Stack;
  }

  public class IronStep : Ability
  {
    //TODO 구현부와 데이터부를 분리할 것. 구현부만 Instantiate될 수 있게
    public IronStep(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "무쇠걸음 " + Level;
      Summary = "다리의 힘을 한계까지 끌어냅니다.";
      /*DescriptionText = new RichText[] {
        new($"SP {"재생 불가".Interested()}", Color.red),
        new($"이동 거리 비례 {"HP 제한".Interested()}", Color.red),
        new($"중량 {"보행속도 감소".Interested()} 완화", Color.clear),
        new($"<i>진정한 힘은 통제된 동작에서 피어납니다.</i>", Color.clear)
      };*/
      FieldID = Fld.Travel;
      AbilID = Abil.MovementReinforcement_Gamma;
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/hiking"); // 임시
      Toggleable = true;
    }
    float HpResMod => Level switch { 1 => 1.25f, 2 => 1.75f, _ => 2.5f };
    float WalkSpdMod => Level switch { 1 => .50f, 2 => .25f, _ => 0 };

    float rcr = 0; // Restricted amount of HP Recovery
    bool hp_trigger = false;
    void HPRestriction(FieldUnit u) {
      float amt = 0;
      if (hp_trigger && u.status.moving)
        amt = u.status.speed * HpResMod * Time.deltaTime;
      rcr += amt;
      u.stat.HPRCR.Value += amt;
    }
    Func<int> MoveInt(FieldUnit u) => () => {
      var (lr, _, la, lh) = u.LoadVariables();
      float spRatio = u.stat.SP.Value / u.stat.SPMax.Value,
      hpRatio = u.stat.HP.Value / u.stat.HPMax.Value;
      if (hp_trigger = spRatio <= 0) { // Consume HP
        if (lr <= lh && hpRatio > .01f) return 1;
        else return 0;
      } else { // Consume SP
        if (lr <= la) return 2;
        else if (lr <= lh) return 1;
        else return 0;
      }
    };
    public override void Invoke(FieldUnit unit) { }
    public override void ToggleOn(FieldUnit unit) {
      // unit.ApplyEffect(ID, HPRestriction);
      unit.Movement_Available = MoveInt(unit);
      unit.stat.SPRegen.value.Nullify = true; // SP Regen Disabled
      unit.info.WalkSpeed_Heavy *= WalkSpdMod;
    }
    public override void ToggleOff(FieldUnit unit) {
      // unit.effects.Remove(AbilID);
      unit.Movement_Available = unit.MoveInt;
      unit.stat.SPRegen.value.Nullify = false; // SP Regen Enabled
      unit.info.WalkSpeed_Heavy /= WalkSpdMod;

      unit.stat.HPRCR.Value -= rcr;
    }
  }
}