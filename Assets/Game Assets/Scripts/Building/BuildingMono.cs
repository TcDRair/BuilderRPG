using UnityEngine;
using UnityEngine.AI;

/// <summary>건축물 프리팹이 가지는 표준 스크립트 구조입니다.</summary>
public abstract class BuildingMono : MonoBehaviour, IBuildingObject, IInteractable {
  [SerializeField]
  protected Building building;
  public Building Obj => building;
  public void Awake() { building.Init(gameObject); }
  
  public virtual string TagName => building.info.name;
  public abstract bool Interactable { get; }
  public abstract InteractSlot[] Slots { get; }

  public Vector3 GetPosition() => building.CurrentModel.bounds.center;
}