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
  public abstract class Ability
  {
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public int Level { get; protected set; } = -1;
    public struct RichText // 임시
    {
      public string text;
      public Color boxColor;
      public RichText(string text, Color boxColor)
      {
        this.text = text;
        this.boxColor = boxColor;
      }
    }
    public RichText[] Effect { get; protected set; }
    public string Flavor { get; protected set; }
    public Fld FieldID { get; protected set; }
    public Abil ID { get; protected set; }
    public Sprite Icon { get; protected set; }
    
    public bool Toggleable { get; protected set; } = false;

    public abstract void Invoke(FieldUnit unit);
    public abstract void ToggleOn(FieldUnit unit);
    public abstract void ToggleOff(FieldUnit unit);

    public static void Init()
    {

    }
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