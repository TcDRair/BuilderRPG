using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildSelector_Info : MonoBehaviour
{
  public static BuildSelector_Info Instance;
  public void Awake() { Instance = this; }

  #region 인스펙터 메뉴
  public Text buildingName, description, time;
  public Image sprite;
  [Tooltip("건물 데이터에 미리보기 이미지가 없을 경우 표시할 기본 스프라이트")]
  public Sprite defaultSprite;
  public RectTransform content, grid, groupDescription, materialInfo;
  public GameObject gridCellGreen, gridCellYellow, gridCellGray;
  public GameObject group_UNKNOWN, group_Road, group_Shelter, group_Indoor, group_Workbench;

  public Image buildButton;
  #endregion

  bool isButtonActive = false;

  /// <summary>넘겨준 건축물의 데이터를 정보란에 표시합니다.</summary>
  public void ShowBuildSelectorInfo(Building building) {
    BuildingInfo info = building.info;
    // 이름 표시
    buildingName.text = info.name;
    // 설명문 표시
    description.text = info.description;
    // 시간 표시
    time.text = "건설 소요 시간 : " + info.buildTime.ToTimeString();
    // 스프라이트 표시
    sprite.sprite = info.sprite ?? defaultSprite;
    // 그리드 표시 <- 가장 복잡. 스크립트 작성 필요
    int width = info.grid.Size.x, length = info.grid.Size.y;
    Vector2 cellSize = new(grid.sizeDelta.x/(width+2), grid.sizeDelta.y/(length+2));
    for (int i=-1; i<=width; i++) {
      for (int j=-1; j<=length; j++) {
        // 아래 셀에는 영향을 주지 않습니다.
        if ((i == -1 && j == -1) || (i == -1 && j == length) || (i == width && j == -1) || (i == width && j == length)) continue;
        GameObject gridColor;
        // 아래 셀에는 각각의 영향을 파악합니다.
        if (i == -1) gridColor = ((info.grid[i+1, j, true] & Buildable.Wall_West) != Buildable.None) ? gridCellYellow : gridCellGray;
        else if (i == width) gridColor = ((info.grid[i-1, j, true] & Buildable.Wall_East) != Buildable.None) ? gridCellYellow : gridCellGray;
        else if (j == -1) gridColor = ((info.grid[i, j+1, true] & Buildable.Wall_South) != Buildable.None) ? gridCellYellow : gridCellGray;
        else if (j == length) gridColor = ((info.grid[i, j-1, true] & Buildable.Wall_North) != Buildable.None) ? gridCellYellow : gridCellGray;
        // 나머지 셀은 실제 구조를 확인합니다.
        else {
          gridColor = info.grid[i, j] switch {
            // 해당 공간을 차지하지 않으므로 회색 셀로 표시
            (Buildable.None, Buildable.None) => gridCellGray,
            // 해당 공간을 실제로 차지하므로 녹색 셀로 표시
            (             _, Buildable.None) => gridCellGreen,
            /*// 해당 공간의 특정 구조를 요구하므로 노란색 셀로 표시
            //TODO 인접 셀 탐색으로 변경해야 합니다. 이유 : 차지하는 공간 없이 요구조건만 존재하는 경우는 없으므로
            (Buildable.None,              _) => gridCellYellow,*/
            // 그리드 셀이 올바르게 정의되지 않았습니다.
            _ => throw new Exception("그리드 셀이 올바르게 정의되지 않았습니다.")
          };
        }
        RectTransform cell = Instantiate(gridColor, grid).GetComponent<RectTransform>();
        cell.name = $"Cell {i},{j}";
        cell.sizeDelta = cellSize;
        // cell.localPosition = new((i+0.5f - width/2f) * cellSize.x, (j+0.5f - height/2f) * cellSize.y, 0);
        cell.localPosition = new((i-0.5f - width) * cellSize.x, (j-0.5f - length) * cellSize.y, 0);
      }
    }
    // 그룹 정보 표시 -> 듀랑고 아이템 카테고리처럼 스프라이트로 표시
    for (int k=0; k<info.group.Length; k++) {
      var group = info.group[k];
      var groupIndicator = group switch {
        BuildingInfo.Group.Road => group_Road,
        BuildingInfo.Group.Shelter => group_Shelter,
        BuildingInfo.Group.Indoor => group_Indoor,
        BuildingInfo.Group.Workbench => group_Workbench,
        _ => group_UNKNOWN
      };
      RectTransform rect = Instantiate(groupIndicator, groupDescription).GetComponent<RectTransform>();
      rect.localPosition = new(0, -k * rect.sizeDelta.y, 0);
    }
    // 건설 재료 정보 표시
    //TODO : 아이템 UI 프리팹, 또는 Item.GetUI()와 같이 각 아이템 내부에서 UI 오브젝트를 구성해서 가져올 수 있도록 구현할 것
    // 최종 Info 화면 크기 조정
    content.sizeDelta = new(content.sizeDelta.x, 440f + materialInfo.sizeDelta.y);

    // 건설 버튼 활성화
    isButtonActive = true;
    buildButton.color = Color.white;
    MapGenerator.Instance.currentBuilding = building;
  }

  /// <summary>현재 표시중인 건축물 정보 UI를 초기 상태로 청소합니다.</summary>
  public void CleanBuildSelectorInfo() {
    // 이름 제거
    buildingName.text = "";
    // 설명문 제거
    description.text = "";
    // 시간 제거
    time.text = "";
    // 스프라이트 제거
    sprite.sprite = defaultSprite;
    // 그리드 제거
    grid.transform.RemoveAllChildren();
    // 그룹 정보 제거
    groupDescription.RemoveAllChildren();
    // 건설 재료 정보 제거
    materialInfo.RemoveAllChildren();

    // 건설 버튼 비활성화
    isButtonActive = false;
    buildButton.color = Color.gray;
  }



  /// <summary>건설 버튼을 눌렀을 때 호출. 건물 선택 모드 UI를 닫고 건물 프리뷰 모드 UI를 시작</summary>
  public void BuildButtonPressed() {
    if (!isButtonActive) return;
    State.Current.Set(State.MState.Mode_BuildPreview);
  }
}