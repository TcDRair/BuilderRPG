using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별도의 상호작용이 없는 단순한 구조물 프리팹을 위한 클래스입니다.<br/>
/// 이 건물은 모든 건물이 가지는 기본 상호작용만 가능합니다.
/// </summary>
public class SimpleStructure : MonoBehaviour, IBuildingObject, IInteractable
{
    [SerializeField]
    Building building;
    public Building bldg => building;
    void Awake() { building.SetGameObject(this.gameObject); }
    public bool interactable => true; // 장식 건물은 항상 상호작용이 가능합니다.
    public string tagName => building.info.name;
    List<InteractSlot> _slots = new List<InteractSlot>();
    public InteractSlot[] slots {
        //* 호출될 때마다 아래의 조건을 검사하여 슬롯을 반환합니다.
        get {
            _slots.Clear();
            // 건물의 건설 진행도에 따라 슬롯을 할당합니다. 자재 투입 + 건설 취소 / 건설 + 건설 취소 / 파괴
            switch (building.state) {
                case Building.State.NeedMaterials: _slots.Add(building.defaultFillMaterials); _slots.Add(building.defaultCancelBuild); break;
                case Building.State.Constructing: _slots.Add(building.defaultBuild); _slots.Add(building.defaultCancelBuild); break;
                case Building.State.Complete: _slots.Add(building.defaultDestroy); break;
            }
            // 건물 정보 슬롯은 항상 포함됩니다.
            _slots.Add(building.defaultBuildingInfo);
            // 다른 기능이 없는 기본 장식 건축물이므로 다른 슬롯은 없습니다.
            return _slots.ToArray();
        }
    }

    Renderer ren = null;
    public Vector3 GetPosition() {
        if (ren == null) {
            ren ??= GetComponent<Renderer>();
            if (ren == null) ren = GetComponentInChildren<Renderer>(true);
        }
        return ren.bounds.center;
    }
}