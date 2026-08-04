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
  /// <summary>어빌리티는 "무엇을 하는가"의 정의입니다.</summary>
  /// <remarks>
  /// <b>!! 여기에 가변 상태를 두지 마십시오. !!</b>
  /// <para>
  /// <see cref="Abilities"/>는 <c>Dictionary&lt;Abil, Ability&gt;</c>로 어빌리티를
  /// <b>공유</b>하는 설계입니다. 인스턴스 필드에 진행 상태를 두면
  /// 여러 유닛이 같은 값을 덮어씁니다.
  /// </para>
  /// <para>
  /// "이 유닛에게 이 효과가 걸린 동안의 진행도"는 <see cref="UnitEffect"/>가 가집니다.
  /// 타이머·래치·누적값이 필요하면 <see cref="UnitEffect"/>를 상속한 클래스를 만들어
  /// <see cref="CreateEffect"/>에서 그 인스턴스를 돌려주십시오.
  /// <see cref="FieldUnit"/>이 효과를 유닛별로 들고 있으므로 그것만으로 격리됩니다.
  /// </para>
  /// 레벨·계수처럼 생성 후 변하지 않는 값은 여기 있어도 됩니다.
  /// </remarks>
  public abstract class Ability {
    public string Name { get; protected set; }
    public string Summary { get; protected set; }
    public int Level { get; protected set; } = -1;
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

    /// <summary>이 어빌리티의 효과를 <b>유닛마다 새로</b> 만듭니다.</summary>
    /// <remarks>
    /// 인스턴스를 재사용하지 마십시오. 반환한 효과가 곧 그 유닛의 상태 저장소입니다.
    /// </remarks>
    public abstract UnitEffect CreateEffect(FieldUnit unit);

    /// <summary>이 어빌리티가 만드는 주 효과의 식별자입니다.</summary>
    public UnitEffect.IDSet EffectID => new(ID);

    public virtual void ToggleOn(FieldUnit unit) => unit.ApplyEffect(CreateEffect(unit));
    public virtual void ToggleOff(FieldUnit unit) => unit.RemoveEffect(EffectID);

    //todo 실행 간 안정성이 없어 세이브 데이터에 쓸 수 없습니다. 명시적 ID 필요. (보완 기록 P1-6)
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