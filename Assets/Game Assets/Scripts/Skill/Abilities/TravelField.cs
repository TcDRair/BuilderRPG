using System;
using UnityEngine;

using Rair.Field;

namespace Rair.Skill.AbilityStorage {
  public class Survival : Profession
  {
    public Survival() { ID = Prof.Survival; }
  }
  public class Travel : Field
  {
    public Travel() {
      ProfID = Prof.Survival;
      ID = Fld.Travel;
    }
  }
  public class LowerBodyReinforcementAlpha : Ability
  {
    //TODO 구현부와 데이터부를 분리할 것. 구현부만 Instantiate될 수 있게
    public LowerBodyReinforcementAlpha(int level) {
      Level = Mathf.Clamp(level, 1, 3);
      Name = "굳건한 행군 " + Level;
      Description = "다리의 힘을 한계까지 끌어냅니다.";
      Effect = new RichText[] {
        new($"SP {"재생 불가".Rich()}", Color.red),
        new($"이동할 때마다 {"HP 제한".Rich()}", Color.red),
        new($"무거운 짐의 {"보행속도 감소".Rich()} 완화", Color.clear),
      };
      FieldID = Fld.Travel;
      ID = Abil.LowerBodyReinforcementAlpha;
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/hiking"); // 임시
      Toggleable = true;
    }
    float HpResMod => Level switch { 1 => .25f, 2 => .30f, _ => .35f };
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
    public override void ToggleOn(FieldUnit unit)
    {
      unit.effects[ID] = HPRestriction;
      unit.Movement_Available = MoveInt(unit);
      unit.stat.SPRegen.value.Nullify = true; // SP Regen Disabled
      unit.info.WalkSpeed_Heavy *= WalkSpdMod;
    }
    public override void ToggleOff(FieldUnit unit)
    {
      unit.effects.Remove(ID);
      unit.Movement_Available = unit.MoveInt;
      unit.stat.SPRegen.value.Nullify = false; // SP Regen Enabled
      unit.info.WalkSpeed_Heavy /= WalkSpdMod;

      unit.stat.HPRCR.Value -= rcr;
    }
  }
  public class LowerBodyReinforcementBeta : Ability
  {
    public LowerBodyReinforcementBeta()
    {
      Name = "하체 보강 β : 지구력";
      Description = "다리의 민첩성을 한계까지 끌어올립니다.";
      FieldID = Fld.Travel;
      ID = Abil.LowerBodyReinforcementBeta;
      Toggleable = true;
      Effect = new RichText[] {
        new($"스태미나 소진 시 생명력 대신 소모", Color.clear),
        new($"{"하중에 의한 이동 속도 감소".Rich()} 완화", Color.clear),
        new($"스태미나 {"회복 불가".Rich()}", Color.red),
        new($"소모된 생명력 {"회복 제한".Rich()}", Color.red)
      };
      Icon = Resources.Load<Sprite>("Sprites/Flaticon/hiking"); // 임시
    } 
    public override void Invoke(FieldUnit unit) { }
    public override void ToggleOn(FieldUnit unit)
    {
      //TODO unit.ApplyEffect(sthbuff);
    }
    public override void ToggleOff(FieldUnit unit)
    {
      //TODO unit.RemoveEffect(sthbuff);
    }
  }
}