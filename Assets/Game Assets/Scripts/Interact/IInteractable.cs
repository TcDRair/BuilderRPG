using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


public interface IInteractable
{
    /// <summary>상호작용 UI 구성요소를 반환합니다.</summary>
    InteractSlot[] slots { get; }
    /// <summary>상호작용 가능 여부를 나타냅니다.</summary>
    bool interactable { get; }
    /// <summary>네임태그에 표시할 이름을 나타냅니다.</summary>
    string tagName { get; }
    /// <summary>필요에 따라 인터페이스가 부착된 게임오브젝트의 중앙 위치를 가져옵니다.</summary>
    Vector3 GetPosition();
}

[Serializable]
public class InteractSlot {
    //* 필수 구성요소
    public enum Type {
        /// <summary>일반 슬롯입니다. 별다른 추가 조치가 없습니다.</summary>
        Default,
        /// <summary>단순 정보 슬롯입니다. 시간과 비용이 없고 작은 크기의 슬롯을 사용합니다.</summary>
        Info,
        /// <summary>다른 UI를 표시하는 슬롯입니다. 시간과 비용이 없습니다.</summary>
        CallUI,
        /// <summary>플레이어가 특정 행동을 수행하게 만드는 슬롯입니다. 다른 색상의 슬롯을 사용합니다.</summary>
        StartAction,
    } public Type type;
    public string slotName;
    public Sprite sprite;
    /// <summary>슬롯을 클릭하면 여기에 할당된 작업을 실행합니다.</summary>
    public UnityAction<BaseEventData> action;
    /// <summary>슬롯에 길게 마우스오버 시 툴팁을 표시할지를 나타냅니다.</summary>
    public bool hasTooltip;

    //* 선택 구성요소
    public float time = -1f;
    public int amount = -1;
    [Serializable]
    public class Cost {
        public enum Type {
            /// <summary>아무것도 소모되지 않습니다.(기본값)</summary>
            None,
            B, C, D,
        }
        /// <summary>어떤 종류의 비용이 소모되는지 표시합니다.</summary>
        public Type type;
        /// <summary>얼마만큼의 비용이 소모되는지 표시합니다.</summary>
        public float amount;
    } public Cost cost;
    public string tooltipDescription;

}