using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


public interface IInteractable
{
  /// <summary>상호작용 UI 구성요소를 반환합니다. 건물의 상태에 따라 변동할 수 있게 설정해야 합니다.</summary>
  InteractSlot[] Slots { get; }
  /// <summary>상호작용 가능 여부를 나타냅니다.</summary>
  bool Interactable { get; }
  /// <summary>네임태그에 표시할 이름을 나타냅니다.</summary>
  string TagName { get; }
  /// <summary>필요에 따라 인터페이스가 부착된 게임오브젝트의 중앙 위치를 가져옵니다.</summary>
  Vector3 GetPosition();
}

[Serializable]
public class InteractSlot {
  //* 필수 구성요소
  /// <summary>슬롯의 역할에 따른 구분</summary>
  public enum Type {
    /// <summary>일반 슬롯입니다. 특별한 기능이 없습니다.</summary>
    Default,
    /// <summary>작은 크기의 슬롯을 사용합니다. 정보나 잘 사용되지 않는 기능을 담당합니다.</summary>
    Small,
    /// <summary>일반 UI를 표시하는 슬롯입니다. 일반적으로 시간과 비용이 표시되지 않습니다.</summary>
    UI,
    /// <summary>플레이어가 특정 행동을 수행하게 만드는 슬롯입니다. <see cref="Default"/>와 다른 색상의 슬롯을 사용합니다.</summary>
    StartAction,
  } public Type type;
  public string slotName;
  public Sprite sprite;

  //* 필수 구성요소 (기본값 보유)
  /// <summary>슬롯을 클릭하면 여기에 할당된 작업을 실행합니다.</summary>
  public UnityAction action;
  /// <summary>슬롯 위에 마우스를 올려두었을 때 툴팁이 표시되는지를 나타냅니다.</summary>
  public bool hasTooltip = false;
  /// <summary>행동을 수행하기 위해 가까이 다가가야 하는지를 나타냅니다.</summary>
  public bool shouldApproach = false;

  //* 선택 구성요소
  public State.MState actionState = State.MState.Interactable_Action;
  public float time = -1f;
  public int amount = -1;
  public string tooltipDescription;

  //* 자동 구성요소
  public int ID => slotName.GetHashCode();
  public InteractSlot(IInteractable body) => this.body = body;
  public readonly IInteractable body;
  /// <summary>호출 시점에서 지정된 상호작용을 시도합니다.</summary>
  /// <returns>즉시 시작할 수 있다면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
  public bool StartAction() {
    if (shouldApproach) {
      Player.Instance.SetTarget(this); // 타겟 설정 및 이동 시작. 나머지는 Player에서 담당.
      if (Player.Instance.IsTargetReached()) {
        State.Current.Set(actionState);
        action.Invoke();
        return true;
      }
      return false;
    }
    State.Current.Set(actionState);
    action.Invoke();
    return true;
  }
}