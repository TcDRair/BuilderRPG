using UnityEngine;

namespace Rair.Field
{
  public abstract partial class FieldUnit {

    public virtual RVEFloat.Applier Movement_SPRegen => (RVFloat spRegen) => {
      var (lr, ll, la, lh) = LoadVariables();

      spRegen.Nullify = // SP Regen Disable when moving
        status.moving && (lr > la || stat.SP.Value <= 0);

      if (lr < ll) spRegen *= 1 + info.SPRegen_Light.Value * (lr / ll);
      else if (lr < la) spRegen *= 1;
      else if (lr < lh) spRegen *= 1 + info.SPRegen_Heavy.Value;
      else spRegen.Nullify = true; // SP Regen Disabled

      return spRegen;
    };
    public virtual RVEFloat.Applier Movement_SPCost => (RVFloat spCost) => {
      var (lr, ll, la, lh) = LoadVariables();
      float wC = stat.WalkSPCost.Value, rC = stat.RunSPCost.Value;

      if (status.running) {
        if (lr <= ll) spCost += rC * (0.8f + info.RunSPCost_Light.Value * (lr / ll));
        else spCost += rC;
      } else if (status.moving) {
        if (lr > la) spCost += wC * (.1f + info.WalkSPCost_Heavy.Value * ((lr - la) / (lh - la)));
      } // else : No move no cost

      return spCost;
    };
    public virtual RVEFloat.Applier Movement_RunSpeed => (RVFloat runSpeed) => {
      var (lr, ll, la, lh) = LoadVariables();

      runSpeed.Nullify = false;
      if (lr <= ll) runSpeed *= (1 + info.RunSpeed_Light.Value * (lr / ll));
      else if (lr <= la) runSpeed *= 1;
      else runSpeed.Nullify = true;
      return runSpeed;
    };
    public virtual RVEFloat.Applier Movement_WalkSpeed => (RVFloat walkSpeed) => {
      var (lr, _, la, lh) = LoadVariables();

      if (lr <= la) return walkSpeed;

      if (lr <= lh) walkSpeed *= (1 + info.WalkSpeed_Heavy.Value * ((lr - la) / (lh - la)));
      else walkSpeed.Nullify = true;

      return walkSpeed;
    };

  }
  public struct UnitInfo
  {
    public RVFloat Load_Light, Load_Average, Load_Heavy;
    public RVFloat SPRegen_Light, SPRegen_Heavy;
    public RVFloat RunSpeed_Light, WalkSpeed_Heavy;
    public RVFloat RunSPCost_Light, WalkSPCost_Heavy;
    public RVFloat Fatigue_Run, Fatigue_Walk, Fatigue_Idle, Fatigue_Sit;

    public UnitInfo(bool player = false) {
      Load_Light = new(.75f, .01f, .75f); // 0% ~ 75%
      Load_Average = new(1, .75f, 1.5f); // 75% ~ 150%
      Load_Heavy = new(1.5f, 1.5f, 10); // 150% ~ 1000%

      SPRegen_Light = new(.15f, 0, 1); // +15%
      SPRegen_Heavy = new(-.2f, -1, 0); // -20%
      RunSpeed_Light = new(.1f, 0, 1); // +10%
      WalkSpeed_Heavy = new(-.75f, -1, 0); // -75%
      RunSPCost_Light = new(.15f, 0, 1); // +15%
      WalkSPCost_Heavy = new(.9f, 0, 1); // +90%

      Fatigue_Run = new(3, 0);
      Fatigue_Walk = new(1, 0);
      Fatigue_Idle = new(0);
      Fatigue_Sit = new(-1, max: 0);
    }

    public readonly override string ToString() =>
      "Info : \n" +
      $"Load Boundary: {Load_Light} / {Load_Average} / {Load_Heavy}";
  }
  public struct UnitStat
  {
    public RVEFloat HPMax;
    public RVEFloat HPRegen;
    public RVEFloat HPCost;
    public RVMFloat HPRCR; // HP Restriction
    public RVMFloat HP;

    public RVEFloat SPMax;
    public RVEFloat SPRegen;
    public RVEFloat SPCost;
    public RVMFloat SPRCR; // SP Restriction
    public RVMFloat SP;

    public RVEFloat WalkSpeed, RunSpeed;
    public RVEFloat WalkSPCost, RunSPCost;

    public RVEFloat LoadMax;
    public RFloat Load;

    public RVEFloat FatigueMax;
    public RVMFloat Fatigue;

    public UnitStat(float hpMax, float hpRegen, float spMax, float spRegen, float walkSpeed, float runSpeed, float walkSPCost, float runSPCost, float loadMax) {
      HPMax = new(hpMax, 1);
      HPRegen = new(hpRegen, 0);
      HPCost = new(0, 0);
      RVMFloat hpRcr = new(0, 0, HPMax, v => v - 1);
      HPRCR = hpRcr;
      HP = new(hpMax, 0, HPMax, v => v - hpRcr.Value);

      SPMax = new(spMax, 1);
      SPRegen = new(spRegen);
      SPCost = new(0, 0);
      RVMFloat spRcr = new(0, 0, SPMax, v => v - 1);
      SPRCR = spRcr;
      SP = new(spMax, 0, SPMax, v => v - spRcr.Value);

      WalkSpeed = new(walkSpeed, 0, 5);
      RunSpeed = new(runSpeed, 0, 20);
      WalkSPCost = new(walkSPCost, 0);
      RunSPCost = new(runSPCost, 0);

      LoadMax = new(loadMax, 0);
      Load = new(0, 0);

      FatigueMax = new(300, 0);
      Fatigue = new(0, 0, FatigueMax);

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
  public struct UnitStatus
  {
    public float speed;
    public bool moving, running, sitting;

    public float fatigueTick;
  }
}