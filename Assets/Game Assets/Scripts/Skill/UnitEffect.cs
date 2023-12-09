using System.Collections.Generic;

using UnityEngine;

using Rair.Field;
namespace Rair.Skill
{
  public abstract class UnitEffect
  {
    public int ID { get; protected set; }
    public int Duration { get; protected set; } = -1;
    public bool Sustained { get; protected set; } = false;
    public bool Stackable { get; protected set; } = false;

    public abstract void OnApply(FieldUnit unit);
    public abstract void OnRemove(FieldUnit unit);
    public abstract void OnEnd(FieldUnit unit);
  }
}