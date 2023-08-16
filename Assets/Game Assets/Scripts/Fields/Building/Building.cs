using UnityEngine;
using UnityEngine.AI;

// using UnityEditor;

[System.Serializable]
/// <summary>
/// 건축물 프리팹이 가지고 있는 클래스로, 건축물에 대한 정보를 저장합니다.<br/>
/// 상호작용 등 건축물 내부 메서드는 자체 스크립트에서 구현합니다.
/// </summary>
public class Building
{
  GameObject _g;
  BuildingMono _m;
  public void Init(GameObject gameObject) {
    _g = gameObject;
    _m = _g.GetComponent<BuildingMono>();
  }

  public BuildingInfo info;
  /// <summary>건설 진행 중에 사용되는 건축 모델 배열입니다. 맨 마지막을 건설 완료 모델로 간주합니다.</summary>
  [Tooltip("건설 진행도에 따라 표시되는 건축 모델링들을 지정합니다. 각 요소에 메쉬가 지정되어 있어야 합니다.")]
  public Transform[] constructings;

  /*// 리소스 파일 경로
  internal const string prefabPath = "Assets/Game Assets/Resources/Prefabs/Buildings/";
  internal const string ResourcePath = "Prefabs/Buildings/";*/

  [HideInInspector] public Progress state;
  // 건설 중 사용되는 정보.
  public enum Progress {
    /// <summary>건설 자재를 기다리고 있습니다.</summary>
    NeedMaterials,
    /// <summary>건설이 완료되기를 기다리고 있습니다.</summary>
    Constructing,
    /// <summary>건설이 완료되었습니다.</summary>
    Complete
    //TODO : Broken(파손 -> 재건축)
  }
  /// <summary>실제로 경과한 건설 시간을 의미합니다. 건설 속도 증감에 영향을 받으며 <see cref="BuildingInfo.buildTime"/>이 최대치입니다.</summary>
  float _buildProgress = 0f;
  /// <summary>건설 이전을 0, 건설 완료를 1로 잡았을 때 현재 건설 진행도 비율을 의미합니다.</summary>
  public float BuildProgress => _buildProgress/info.buildTime;

  /// <summary>맵에서 건축물의 셀 위치를 저장합니다.</summary>
  [HideInInspector] public (int x, int y) position;
  
  /// <summary>현재 활성화된 건축물 모델의 <see cref="Renderer"/>를 반환합니다.</summary>
  public Renderer CurrentModel {
    get {
      //* ??, ??= 연산자는 오버로드되지 않아 Component 객체에는 사용할 수 없습니다.
      if (_cM == null || _cM.gameObject.activeInHierarchy) {
        _cM = _g.GetComponent<Renderer>();
        if (_cM == null) _cM = _g.GetComponentInChildren<Renderer>(false);
      }
      return _cM;
    }
  }
  Renderer _cM;

  int previousIndex = 0, currentIndex = 0;
  /// <summary>
  /// 플레이어가 이 건축물을 건설하고 있는 프레임마다 호출하여 건설 진행 상황을 업데이트합니다.<br/>
  /// 진행도에 비례하여 저장된 모델의 수에 따라 건축물의 외형이 변화합니다.<br/>
  /// 건설이 완료되면 NavMesh에 해당 건물을 추가합니다.
  /// </summary>
  /// <param name="elapsedTime">
  /// 이전 호출로부터 경과한 시간을 나타냅니다. <see cref="Time.deltaTime"/>을 기본값으로 사용합니다.
  /// <para>건설 속도 증감과 같은 시스템이 존재할 경우 해당 시간에 숙련도로 인한 속도 변화를 적용한 뒤 전달하세요.</para>
  /// </param>
  /// <returns>건설이 최종 완료되면 <see langword="true"/>를 반환합니다.</returns>
  public bool ShowConstructingModel(float? elapsedTime = null) {
    _buildProgress = Mathf.Clamp(_buildProgress + (elapsedTime ?? Time.deltaTime), 0f, info.buildTime);
    currentIndex = (int)(BuildProgress * (constructings.Length-1));
    if (previousIndex != currentIndex) {
      for (int i = 0; i < constructings.Length; i++) constructings[i].gameObject.SetActive(i == currentIndex);
    }
    previousIndex = currentIndex;
    bool buildComplete = BuildProgress >= 1f;
    if (buildComplete) {
      state = Progress.Complete;
    }
    return buildComplete;
  }

  public void SavePosition((int x, int y) pos) { position = pos; }

  #region Interact Slots
  /*
  //* 건축물 기본 지원 슬롯. SimpleStructure.cs에서 사용법 참고
  InteractSlot _DBI, _DFM, _DB, _DCB, _DD; // Lazy initialization
  /// <summary>이 건물의 간략한 정보를 보여주는 슬롯입니다.</summary>
  public InteractSlot DefaultBuildingInfo => _DBI ??= new InteractSlot(_m) {
    type = InteractSlot.Type.Small,
    sprite = InteractSlotSprites.Instance.buildInfo,
    action = new(() => { UI.Instance.ShowBuildingInfo(this); }),
    slotName = "건물 정보",
  };
  /// <summary>이 건물의 자재를 채워넣는 슬롯입니다. 건설 시작 전 유효합니다.</summary>
  public InteractSlot DefaultFillMaterials =>_DFM ??= new InteractSlot(_m) {
    type = InteractSlot.Type.UI,
    sprite = InteractSlotSprites.Instance.buildFillMaterials,
    action = new(() => {
      //TODO 자재 보충 UI 메서드 추가
      state = Progress.Constructing;
    }),
    slotName = "자재 넣기",
    shouldApproach = true,
  };
  /// <summary>이 건물의 건설을 시작 또는 재개하는 슬롯입니다. 완공 전 유효합니다.</summary>
  public InteractSlot DefaultBuild => _DB ??= new InteractSlot(_m) {
    type = InteractSlot.Type.StartAction,
    sprite = InteractSlotSprites.Instance.build,
    action = new(() => {
      Player.Instance.StartBuild(this);
    }),
    slotName = "건설",
    shouldApproach = true,
  };
  /// <summary>이 건물의 건설을 취소하는 슬롯입니다. 완공 전 유효합니다.</summary>
  public InteractSlot DefaultCancelBuild => _DCB ??= new InteractSlot(_m) {
    type = InteractSlot.Type.StartAction,
    sprite = InteractSlotSprites.Instance.buildCancel,
    action = new(() => {
      //TODO 자재를 반환하는 메서드 추가. Destroy에서도 호출할 수 있으므로 buildProgress를 사용할 것.
      //* 취소 확인 문구가 있어도 괜찮으려나
      //TODO 매 프레임 변경되는 점은 Update() switch로, 그렇지 않은 것은 ChangeState()로 구현
      UI.Instance.ClearInteractions();
      MapGenerator.Instance.CleanBuilding(this);
      Object.Destroy(_g);
    }),
    slotName = "건설 취소",
    shouldApproach = true,
  };
  /// <summary>이 건물을 철거하는 슬롯입니다. 완공된 이후 유효합니다.</summary>
  public InteractSlot DefaultDestroy => _DD ??= new InteractSlot(_m) {
    type = InteractSlot.Type.Small,
    sprite = InteractSlotSprites.Instance.buildDestroy,
    action = new(() => {
      //TODO 철거 UI + 동작 메서드 추가
      Object.Destroy(_g); //? 임시
    }),
    slotName = "건물 철거",
    shouldApproach = true,
  };
  */
  #endregion
}

[System.Serializable]
public class BuildingInfo {
  public string name;
  [TextArea]
  public string description;
  /// <summary>건축물의 범주입니다. 최대 세 개를 가질 수 있으며, 첫 번째 항목을 메인 그룹으로 간주합니다.</summary>
  public Group[] group;
  public enum Group {
    /// <summary>이동 속도에 영향을 주는 건물</summary>
    Road,
    /// <summary>아이템 저장이 가능한 건물</summary>
    Chest,
    /// <summary>실내 판정을 받는 건물</summary>
    Indoor,
    /// <summary>휴식할 수 있는 건물</summary>
    Shelter,
    /// <summary>작업 기능이 있는 건물</summary>
    Workbench,
    /// <summary>특별한 기능이 없는 단순 장식 건물</summary>
    Deco,
    /// <summary>주변에 열을 제공하는 건물</summary>
    HeatSource,
  }
  //TODO public ItemT[] material;

  /// <summary>이 건물의 건설 구조와 필요조건을 나타냅니다.</summary>
  [Tooltip("이 건물의 구조를 나타냅니다. 건축물 프리팹에 들어 있어야 합니다.")]
  public BuildableGrid grid;

  [Tooltip("건축물의 미리보기 이미지입니다. 건설 선택 화면에서 표시됩니다.")]
  /// <summary>건축물의 미리보기 이미지입니다.</summary>
  public Sprite sprite;
  [Tooltip("건축물의 미리보기 모델입니다.")]
  /// <summary>건축물의 미리보기 모델입니다.</summary>
  public GameObject preview;
  [Tooltip("건설에 걸리는 시간입니다. 초 단위로 입력합니다.")]
  /// <summary>건설에 걸리는 시간입니다. 초 단위로 입력합니다.</summary>
  [Range(1, 600)]
  public float buildTime = 30f;
}
/// <summary>이 인터페이스를 상속받은 <see cref="GameObject"/>는 <see cref="Building"/> 객체를 제공합니다.</summary>
public interface IBuildingObject {
  Building Obj { get; }
}
