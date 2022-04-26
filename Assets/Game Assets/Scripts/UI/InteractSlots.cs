using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractSlots : MonoBehaviour
{
    [System.Serializable]
    /// <summary>상호작용을 위해 할당할 기본 프리팹을 지정합니다. 사용되지 않는 오브젝트는 할당되지 않습니다.</summary>
    public class CellPrefabs {
        public GameObject buildingInfo;
        public GameObject natureInfo;
        public GameObject creatureInfo;
        public GameObject build;
        public GameObject buildCancel;
        public GameObject destroy;
        public GameObject toItem;
        public GameObject chest;
        public GameObject attack;
        public GameObject harvest;
    }
    [Tooltip("상호작용을 위해 각 슬롯에 넣을 프리팹을 지정합니다. 사용되지 않는 오브젝트는 할당되지 않습니다.")]
    public CellPrefabs cellPrefabs;
}

public enum InteractTargetType {
    Building,
    Nature,
    Creature,
}

public enum InteractType
{
    /// <summary>클릭 시 구조물 건설을 진행</summary>
    Build,
    /// <summary>클릭 시 구조물 건설을 취소</summary>
    BuildCancel,
    /// <summary>클릭 시 구조물/자연물 파괴를 진행</summary>
    Destroy,
    /// <summary>클릭 시 구조물 정보를 보여줌</summary>
    BuildingInfo,
    /// <summary>클릭 시 자연물 정보를 보여줌</summary>
    NatureInfo,
    /// <summary>클릭 시 구조물을 아이템화하여 인벤토리에 보관을 시도</summary>
    ToItem,
    /// <summary>클릭 시 창고 기능이 있는 구조물을 엶</summary>
    Chest,
    /// <summary>클릭 시 대상 공격 모드에 돌입</summary>
    Attack,
    /// <summary>클릭 시 대상 자연물을 수집, 수확 또는 도축</summary>
    Harvest,
}
