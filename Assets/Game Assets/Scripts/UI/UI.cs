using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using static MainSetting;

public class UI : MonoBehaviour
{
  public static UI Instance;

  //* 캔버스 그룹은 UI 내에서만 직접 조작합니다. 외부에서는 UI.State로 간접 조작할 수 있도록 합니다.
  [SerializeField]
  CanvasGroup buildingModeMenu, buildingSelectionUI, nameTagUI, interactionSelectUI;

  public Text buildMode, buildInfo, interactableObjectName;
  public GameObject nameTagPrefab;
  public InteractSlotUI[] interactSlots, interactSmallSlots;

  void Awake() { Instance = this; }

  void Start() {
    buildingModeMenu.Disable();
    buildingSelectionUI.Disable();
    BuildSelector_Info.Instance.CleanBuildSelectorInfo();
    buildInfo.text = "";
    ClearInteractions();
  }

  void Update() {
    //* 모든 UI 상태에서, 또는  특정 상태를 제외한 나머지 상태에서 수행하는 동작을 지정합니다.
    buildingModeMenu.Disable(); // Build_Preview 상태에서만 활성화됩니다.
    nameTagUI.Disable();

    switch (State.Current.UI) {
      case State.UState.Idle: {
        // 일반 상태에서는 기본 UI 메뉴 호출과 네임태그를 통한 상태 전환이 가능합니다.
        if (Input.GetKeyDown(KeyCode.B)) {
          buildingSelectionUI.Enable();
          State.Current.Set(State.MState.Menu_FullScreen);
        }
        ShowInteractableNameTags(); // 플레이어에 인접한 상호작용 개체의 이름표를 표시합니다.
        break;
      }
      case State.UState.FullScreen: {
        // 건설 선택 상태에서는 선택 UI를 닫을 수 있습니다.
        if (Input.GetKeyDown(KeyCode.B)) {
          buildingSelectionUI.Disable();
          BuildSelector_Info.Instance.CleanBuildSelectorInfo();
          State.Current.Set(State.MState.Idle);
        }
        break;
      }
      case State.UState.BuildPreview: {
        // 건설 프리뷰 상태에서는 관련 정보를 표시합니다.
        buildingSelectionUI.Disable();
        buildingModeMenu.Enable();
        buildMode.text = "Current Building : " + MapGenerator.Instance.currentBuilding.info.name;
        break;
      }
      case State.UState.Interactable_Select: {
        // 상호작용 선택 상태에서는
        
        break;
      }
    }
  }

  #region Interact UI (NameTag ~ Slots)
  void ClearNameTags() { nameTagUI.transform.RemoveAllChildren(); }

  readonly Dictionary<int, RectTransform> nameTagIDs = new();
  readonly List<int> hits = new();
  /// <summary>
  /// 호출되는 시점에 플레이어 일정 거리 내에 존재하는 모든 상호작용 가능한 오브젝트들에 네임태그 UI를 표시합니다.<br/>
  /// 네임태그는 클릭 시 상호작용 메뉴를 표시하는 기능을 가집니다.
  /// </summary>
  void ShowInteractableNameTags() {
    nameTagUI.Enable();

    Vector3 playerPos = Player.Instance.transform.position;
    hits.Clear();
    //* 일정 거리 내의 모든 상호작용 개체의 이름표를 표시합니다.
    foreach (RaycastHit hit in Physics.SphereCastAll(playerPos, 10f, Vector3.up, 0f, interactableMask)) {
      IInteractable interactable = hit.transform.GetComponent<IInteractable>();
      if (interactable != null) {
        Vector3 newPosition = MainCamera.cam.WorldToScreenPoint(interactable.GetPosition());
        int id = hit.colliderInstanceID;
        if (nameTagIDs.ContainsKey(id)) nameTagIDs[id].position = newPosition;
        else nameTagIDs.Add(id, CreateNameTag(interactable, newPosition));
        hits.Add(id);
      }
    }
    foreach (var pair in nameTagIDs.ToArray()) if (!hits.Contains(pair.Key)) {
      Destroy(pair.Value.gameObject);
      nameTagIDs.Remove(pair.Key);
    }
  }

  /// <summary>주어진 좌표에 해당 상호작용 개체의 네임태그를 생성합니다.</summary>
  /// <param name="pos">가리키는 개체의 캔버스상의 위치입니다. <see cref="Camera.WorldToScreenPoint(Vector3)"/>로 얻을 수 있습니다.</param>
  RectTransform CreateNameTag(IInteractable interactable, Vector3 pos) {
    RectTransform nameTag = Instantiate(nameTagPrefab, nameTagUI.transform).GetComponent<RectTransform>();
    Text text = nameTag.GetChild(0).GetComponent<Text>();
    text.text = interactable.TagName;
    nameTag.sizeDelta = new Vector2(text.preferredWidth + 40f, nameTag.sizeDelta.y);
    nameTag.position = pos;
    nameTag.GetComponent<Button>().onClick.AddListener(() => { ShowInteractions(interactable); });
    // ↑ 프리팹에서 OnPointerClick 항목이 미리 추가되어 있어야 합니다.

    return nameTag;
  }

  /// <summary>NameTag 클릭 시 호출하여 상호작용 선택 슬롯을 전개합니다.</summary>
  public void ShowInteractions(IInteractable interactable) {
    //* 카메라에 개체 추적을 설정하고 추적 대상을 전달합니다.
    MainCamera.Instance.trackTarget = interactable; 
    //* UI와 상태를 설정합니다.
    State.Current.Set(State.MState.Interactable_Select);
    interactionSelectUI.Enable();
    interactableObjectName.text = interactable.TagName;

    //* 상호작용이 가능한 경우에만 셀을 표시합니다.
    if (interactable.Interactable) for (int i=0; i<interactSlots.Length; i++) {
      //* 모든 상호작용 요소를 그리고 남은 셀은 모두 비활성화합니다.
      if (i >= interactable.Slots.Length) {
        interactSlots[i].gameObject.SetActive(false);
        interactSmallSlots[i].gameObject.SetActive(false);
        continue;
      }
      //* 슬롯 필수 구성요소 표시
      InteractSlot slot = interactable.Slots[i];
      InteractSlotUI slotUI;
      if (slot.type == InteractSlot.Type.Small) {
        slotUI = interactSmallSlots[i];
        slotUI.gameObject.SetActive(true);
        interactSlots[i].gameObject.SetActive(false);
      }
      else {
        slotUI = interactSlots[i];
        slotUI.gameObject.SetActive(true);
        interactSmallSlots[i].gameObject.SetActive(false);
      }
      slotUI.image.sprite = slot.sprite;
      slotUI.cellName.text = slot.slotName; //TODO 리치 텍스트 지원
      slotUI.cellNameRect.sizeDelta = new Vector2(slotUI.cellName.preferredWidth + 40f, slotUI.cellNameRect.sizeDelta.y);
      //* 이벤트 추가
      slotUI.button.onClick.AddListener(() => slot.StartAction());
      //* 슬롯 선택 구성요소 표시
      slotUI.amount.text = slot.amount == -1 ? "" : slot.amount.ToString();
      slotUI.duration.text = slot.time == -1 ? "" : slot.time.ToTimeString();

      if (slot.hasTooltip) {
        /*
        entryOver.callback.RemoveAllListeners();
        TODO 언젠가 추가
        slotUI.trigger.triggers.Add(entryOver);
        */
      }
    }
    //* 상호작용이 불가능하면 모든 셀을 보이지 않게 처리합니다. 개체의 이름은 항상 보입니다.
    else interactionSelectUI.Disable();
  }
  
  /// <summary>상호작용 선택을 해제합니다. 아무것도 선택하지 않고 나갈 때만 호출해야 합니다.</summary>
  public void ClearInteractions() {
    State.Current.Set(State.MState.Idle);

    interactionSelectUI.Disable();
    interactableObjectName.text = "";
  }

  #endregion

  #region Building UI (Selector ~ Preview)

  public void ShowBuildMessage(MapGenerator.BuildableInfo info) {
    buildInfo.text = info switch {
      MapGenerator.BuildableInfo.Unbuildable => "해당 위치에 건설할 수 없습니다.",
      MapGenerator.BuildableInfo.NotQualified => "이 건물을 건설할 자격이 부족합니다.",
      MapGenerator.BuildableInfo.OutOfBounds => "지도 밖으로 건설할 수 없습니다.",
      MapGenerator.BuildableInfo.NotEnoughMaterial => "건설 재료가 부족합니다.",
      MapGenerator.BuildableInfo.NotEnoughMoney => "건설 재화가 부족합니다.",
      MapGenerator.BuildableInfo.PlayerOverlapped => "건설하려는 공간에 플레이어가 있습니다.",
      _ => "건설이 가능합니다."
    };
  }
  
  public void ShowBuildingInfo(Building building) {
    var info = building.info;

    //TODO Show Info UI(small), Set State To Idle After Info UI Closed
  }

  #endregion
}
