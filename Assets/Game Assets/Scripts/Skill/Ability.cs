using System.Collections.Generic;

using UnityEngine;

using Rair.Field;
using Rair.Skill.AbilityStorage;

namespace Rair.Skill
{
  //todo : Ability는 몰라도 Professions와 Fields는 struct로 해도 되지 않을까?
  public abstract class Profession
  {
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public Prof ID { get; protected set; }
  }
  public abstract class Field
  {
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public Prof ProfID { get; protected set; }
    public Fld ID { get; protected set; }
  }
  public abstract class Ability {
    public string Name { get; protected set; }
    public string Summary { get; protected set; }
    public int Level { get; protected set; } = -1;
    public UnitEffect Effect { get; protected set; }
    /// <summary>
    /// 능력의 상세 효과 텍스트<br/>
    /// 레벨, 스택 등 상황에 따라 효과가 달라질 경우 모두 표시합니다.
    /// </summary>
    public string Description { get; protected set; }
    public string Flavor { get; protected set; }
    public Fld FieldID { get; protected set; }
    public Abil AbilID { get; protected set; }
    public Sprite Icon { get; protected set; }
    
    public bool Toggleable { get; protected set; } = false;

    public abstract void Invoke(FieldUnit unit);
    public abstract void ToggleOn(FieldUnit unit);
    public abstract void ToggleOff(FieldUnit unit);

    public int ID => (FieldID, AbilID).GetHashCode();
  }

  public class Abilities
  {
    private readonly Dictionary<Prof, Profession> professions = new();
    private readonly Dictionary<Fld, Field> fields = new();
    private readonly Dictionary<Abil, Ability> abilities = new();
    private Abilities() { }
    private static Abilities _inst;
    public static Abilities Instance => _inst ??= new();

    public bool Initialized { get; private set; } = false;

    public void Initialize()
    {
      if (Initialized) return;
      //TODO 데이터 로드
      //TODO 로드 매니저 호출
      Initialized = true;
    }
  }
}