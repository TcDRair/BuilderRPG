using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildSelector_Info : MonoBehaviour
{
    public static BuildSelector_Info Instance;
    void Awake() { Instance = this; }

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
        time.text = "건설 소요 시간 : " + info.buildTime.ToString("F0") + " 초";
        // 스프라이트 표시
        sprite.sprite = info.sprite ?? defaultSprite;
        // 그리드 표시 <- 가장 복잡. 스크립트 작성 필요
        int width = info.width, length = info.length;
        Vector2 cellSize = new Vector2(grid.sizeDelta.x/(width+2), grid.sizeDelta.y/(length+2));
        for (int i=-1; i<=width; i++) {
            for (int j=-1; j<=length; j++) {
                Buildable buildable = info.grid[i, j];
                RectTransform cell;
                if (buildable == Buildable.None) { // 해당 공간을 차지하지 않으므로 회색 셀로 표시
                    cell = Instantiate(gridCellGray, grid).GetComponent<RectTransform>();
                }
                else if (i == -1 || i == width || j == -1 || j == length) { // 경계이면서 공간을 차지하므로 노랑 셀로 표시
                    cell = Instantiate(gridCellYellow, grid).GetComponent<RectTransform>();
                }
                else { // 중앙이면서 공간을 차지하므로 초록 셀로 표시
                    cell = Instantiate(gridCellGreen, grid).GetComponent<RectTransform>();
                }
                cell.name = $"Cell {i},{j}";
                cell.sizeDelta = cellSize;
                // cell.localPosition = new Vector3((i+0.5f - width/2f) * cellSize.x, (j+0.5f - height/2f) * cellSize.y, 0);
                cell.localPosition = new Vector3((i-0.5f - width) * cellSize.x, (j-0.5f - length) * cellSize.y, 0);
            }
        }
        // 그룹 정보 표시 -> 듀랑고 아이템 카테고리처럼 스프라이트로 표시
        for (int k=0; k<info.group.Length; k++) {
            var group = info.group[k];
            RectTransform rect;
            switch (group) {
                case BuildingInfo.Group.Road:
                    rect = Instantiate(group_Road, groupDescription).GetComponent<RectTransform>();
                    break;
                case BuildingInfo.Group.Shelter:
                    rect = Instantiate(group_Shelter, groupDescription).GetComponent<RectTransform>();
                    break;
                case BuildingInfo.Group.Indoor:
                    rect = Instantiate(group_Indoor, groupDescription).GetComponent<RectTransform>();
                    break;
                case BuildingInfo.Group.Workbench:
                    rect = Instantiate(group_Workbench, groupDescription).GetComponent<RectTransform>();
                    break;
                default : rect = Instantiate(group_UNKNOWN, groupDescription).GetComponent<RectTransform>(); break;
            }
            rect.localPosition = new Vector3(0, -k * rect.sizeDelta.y, 0);
        }
        // 건설 재료 정보 표시
        //TODO : 아이템 UI 프리팹, 또는 Item.GetUI()와 같이 각 아이템 내부에서 UI 오브젝트를 구성해서 가져올 수 있도록 구현할 것
        // 최종 Info 화면 크기 조정
        content.sizeDelta = new Vector2(content.sizeDelta.x, 440f + materialInfo.sizeDelta.y);

        // 건설 버튼 활성화
        isButtonActive = true;
        buildButton.color = Color.white;
        MapGenerator.Instance.currentBuilding = building;
    }

    /// <summary>현재 표시중인 건축물 정보란을 초기 상태로 청소합니다.</summary>
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
        State.current.Set(State.Main.Menu_BuildPreview);
    }
}