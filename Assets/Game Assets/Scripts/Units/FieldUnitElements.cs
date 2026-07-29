using System;

using UnityEngine;

namespace Rair.Field
{
  public abstract partial class FieldUnit {

    public virtual RVFloat Default_HPRegen(RVFloat hpRegen) {
      hpRegen += info.HPRegen_Default.Value;
      return hpRegen;
    }
    public virtual RVFloat Default_SPRegen(RVFloat spRegen) {
      spRegen += info.SPRegen_Default.Value * status.movement switch {
        MovementStatus.Run => 0,
        MovementStatus.Walk => status.load <= LoadStatus.Standard ? 1 : 0,
        _ => 1
      };
      spRegen *= status.load switch {
        LoadStatus.Lightweight => 1 + info.SPRegenM_Light.Value * (1 - LoadRatioRelative),
        LoadStatus.Standard => 1,
        _ => 1 + info.SPRegenM_Heavy.Value
      };
      return spRegen;
    }
    public virtual RVFloat Default_SPCost(RVFloat spCost) {
      float wC = stat.WalkSPCost.Value, rC = stat.RunSPCost.Value;
      float rCL = info.RunSPCostM_Light.Value, wCH = info.WalkSPCostM_Heavy.Value;
      bool light = status.load <= LoadStatus.Lightweight, heavy = status.load >= LoadStatus.Heavyweight;
      var s = spCost;
      spCost += status.load switch {
        LoadStatus.Lightweight => status.movement switch {
          MovementStatus.Run => rC * (.95f - rCL * (1 - LoadRatioRelative)),
          _ => 0
        },
        LoadStatus.Standard => status.movement switch {
          MovementStatus.Run => rC,
          _ => 0
        },
        _ => status.movement switch {
          MovementStatus.Run => rC, // If available
          MovementStatus.Walk => wC * (.1f + wCH * LoadRatioRelative),
          _ => 0
        }
      };
      return spCost;
    }
    public virtual RVFloat Default_RunSpeed(RVFloat runSpeed) {
      runSpeed.Nullify = false;
      switch (status.load) {
        case LoadStatus.Lightweight:
          runSpeed *= 1 + info.RunSpeedM_Light.Value * (1 - LoadRatioRelative);
          break;
        case LoadStatus.Standard: break; // do nothing
        default:
          runSpeed.Nullify = true;
          break;
      }
      return runSpeed;
    }
    public virtual RVFloat Default_WalkSpeed(RVFloat walkSpeed) {
      switch (status.load) {
        case LoadStatus.Heavyweight:
          walkSpeed *= 1 + info.WalkSpeedM_Heavy.Value * LoadRatioRelative;
          break;
        case LoadStatus.Overburdened:
          walkSpeed.Nullify = true;
          break;
      }

      return walkSpeed;
    }
  }
  public struct UnitInfo {
    public RVFloat LoadLimit_Lightweight, LoadLimit_Standard, LoadLimit_Heavyweight;
    public RVFloat HPRegen_Default, SPRegen_Default;
    //todo 언젠가 배율 수치와 배가 수치를 분리하자
    public RVFloat SPRegenM_Light, SPRegenM_Heavy;
    public RVFloat RunSpeedM_Light, WalkSpeedM_Heavy;
    //todo 질주 / 보행뿐만 아니라 "유보"도 구현할 것
    public RVFloat RunSPCostM_Light, WalkSPCostM_Heavy;
    public RAFloat Fatigue_Run, Fatigue_Walk, Fatigue_Idle, Fatigue_Sit;

    public UnitInfo(bool player = false) {
      HPRegen_Default = new(1, 0); // 1/s
      SPRegen_Default = new(5, 0); // 5/s
      Fatigue_Run = new(3, 0);
      Fatigue_Walk = new(1, 0);
      Fatigue_Idle = new(0);
      Fatigue_Sit = new(-1, max: 0);

      LoadLimit_Lightweight = new(.75f, .01f, .75f); // 0% ~ 75%
      LoadLimit_Standard = new(1, .75f, 1.5f); // 75% ~ 150%
      LoadLimit_Heavyweight = new(1.5f, 1.5f, 10); // 150% ~ 1000%

      SPRegenM_Light = new(.15f, 0, 1); // +15%
      SPRegenM_Heavy = new(-.2f, -1, 0); // -20%
      RunSpeedM_Light = new(.1f, 0, 1); // +10%
      WalkSpeedM_Heavy = new(-.75f, -1, 0); // -75%
      RunSPCostM_Light = new(.15f, 0, 1); // +15%
      WalkSPCostM_Heavy = new(.9f, 0, 1); // +90%
    }

    public readonly override string ToString() =>
      "Unit Info : \n" +
      $"Default Regen: HP {HPRegen_Default}, SP {SPRegen_Default}\n" +
      $"Fatigue : Run {Fatigue_Run}, Walk {Fatigue_Walk}, Idle {Fatigue_Idle}, Sit {Fatigue_Sit}\n" +
      $"Load Boundary: {LoadLimit_Lightweight} / {LoadLimit_Standard} / {LoadLimit_Heavyweight}\n" +
      $"SP Regen Multiplier: Light {SPRegenM_Light}, Heavy {SPRegenM_Heavy}\n" +
      $"Run Speed Multiplier: Light {RunSpeedM_Light}, Heavy {WalkSpeedM_Heavy}\n" +
      $"SP Cost Multiplier: Light {RunSPCostM_Light}, Heavy {WalkSPCostM_Heavy}\n";
  }
  /// <summary>유닛의 능력치입니다.</summary>
  /// <remarks>
  /// <b>이 struct를 복사한 뒤 쓰지 마십시오.</b> (문서 05 P1-4)
  /// <para>
  /// 값 타입 멤버(<see cref="RFloat"/> <c>Load</c>)와 참조 타입 멤버(<see cref="RAFloat"/> <c>HP</c>)가
  /// 섞여 있어 복사 시맨틱이 일관되지 않습니다.
  /// 복사하면 <b>참조 멤버는 원본과 공유되고 값 멤버는 스냅샷이 됩니다.</b>
  /// </para>
  /// <code>
  /// var s = unit.stat;   // 복사본
  /// s.Load += 10;        // 원본에 반영되지 않음
  /// s.HP += 10;          // 원본에도 반영됨 (참조 공유)
  /// </code>
  /// 지역 변수로 잡아야 하면 <c>ref var s = ref unit.stat;</c>를 쓰십시오.
  /// <para>
  /// 클래스 전환은 보류했습니다. 현재 복사 지점이 전부 읽기 전용이라 실제 결함이 없고,
  /// <see cref="UnitStatus"/>는 어디서도 대입되지 않아 struct의 0 초기화에 의존하므로
  /// 클래스로 바꾸면 무해한 기본값이 <c>null</c> 참조 오류로 바뀝니다.
  /// </para>
  /// </remarks>
  public struct UnitStat {
    public RAFloat HPMax;
    public RAFloat HPRegen;
    public RAFloat HPCost;
    public RMFloat HPRCR; // HP Restriction
    public RMFloat HP;
    public readonly float HPRatio => HP.Value / HPMax.Value;

    public RAFloat SPMax;
    public RAFloat SPRegen;
    public RAFloat SPCost;
    public RMFloat SPRCR; // SP Restriction
    public RMFloat SP;
    public readonly float SPRatio => SP.Value / SPMax.Value;

    public RAFloat WalkSpeed, RunSpeed;
    public RAFloat WalkSPCost, RunSPCost;

    public RAFloat LoadMax;
    public RFloat Load; //todo : 아이템에 의한 결과값 데이터로 전환

    public RAFloat FatigueMax;
    public RMFloat Fatigue;

    public UnitStat(float hpMax, float spMax, float walkSpeed, float runSpeed, float walkSPCost, float runSPCost, float loadMax) {
      HPMax = new(hpMax, 1);
      HPRegen = new(0, 0);
      HPCost = new(0, 0);
      RMFloat hpRcr = new(0, 0, refMax: HPMax, maxMod: v => v - 1);
      HPRCR = hpRcr;
      HP = new(hpMax, 0, refMax: HPMax, maxMod: v => v - hpRcr.Value);

      SPMax = new(spMax, 1);
      SPRegen = new(0, 0);
      SPCost = new(0, 0);
      RMFloat spRcr = new(0, 0, refMax: SPMax, maxMod: v => v - 1);
      SPRCR = spRcr;
      SP = new(spMax, 0, refMax: SPMax, maxMod: v => v - spRcr.Value);

      WalkSpeed = new(walkSpeed, 0, 5);
      RunSpeed = new(runSpeed, 0, 20);
      WalkSPCost = new(walkSPCost, 0);
      RunSPCost = new(runSPCost, 0);

      LoadMax = new(loadMax, 0);
      Load = new(0, 0);

      FatigueMax = new(300, 0);
      Fatigue = new(0, 0, refMax: FatigueMax);

      Debug.Log(this);
    }

    public readonly override string ToString() =>
      "Stat : \n" +
      $"HP Max: {HPMax}\n" +
      $"HP Restriction: {HPRCR}\n" +
      $"HP Regen: {HPRegen}\n" +
      $"HP Cost: {HPCost}\n" +
      $"HP : {HP}\n" +
      $"SP Max: {SPMax}\n" +
      $"SP Restriction: {SPRCR}\n" +
      $"SP Regen: {SPRegen}\n" +
      $"SP Cost: {SPCost}\n" +
      $"SP : {SP}\n" +
      $"Walk Speed: {WalkSpeed}\n" +
      $"Run Speed: {RunSpeed}\n" +
      $"Load Max: {LoadMax}\n" +
      $"Load : {Load}\n";
  }

  [Flags] public enum MovementStatus {
    Sit  = 1 << 0,
    Idle = 1 << 1,
    Walk = 1 << 2,
    Run  = 1 << 3,

    Immovable = Sit | Idle,
    Walkable = Sit | Idle | Walk,
    Runnable = Walkable | Run,
    Move = Walk | Run
  }
  public enum LoadStatus { Lightweight, Standard, Heavyweight, Overburdened }
  public struct UnitStatus {
    public float speed;
    public MovementStatus movement, movable;
    public readonly bool Moving => movement >= MovementStatus.Walk;
    public readonly bool Running => movement >= MovementStatus.Run;

    public LoadStatus load;

    public float fatiguePerMinute;

    public override string ToString() =>
      "Status : \n" +
      $"Current Speed: {speed}\n" +
      $"Movement: {movement} / {movable}\n" +
      $"Load Status: {load}\n" +
      $"Fatigue Per Minute: {fatiguePerMinute}\n";
  }
}
