using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public static UI ui;
    //* Building
    public CanvasGroup buildingModeMenu, buildingSelectionUI;
    public Text buildMode, buildInfo;

    public GameObject nameTagPrefab, interactPrefab;

    public static bool buildSelect = false, buildPreview = false;

    void Awake() { ui = this;}

    void Start() {
        buildingModeMenu.Disable();
        buildingSelectionUI.Disable();
        BuildSelector_Info.Instance.CleanBuildSelectorInfo();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.B)) {
            buildSelect = !buildSelect;
            if (buildSelect) OpenBuildSelector();
            else CloseBuildSelector();
        }

        if (buildPreview) {
            buildingModeMenu.Enable();
            buildMode.text = "Build Mode\nCurrent Building : " + MapGenerator.currentBuilding.info.name;
        }
        else {
            buildingModeMenu.Disable();
            buildMode.text = "";
            buildInfo.text = "";
        }
    }

    #region Building UI
    Dictionary<int, Transform> tags;
    public void ShowBuildingNameTag(GameObject building) {
        Transform nameTag;
        if (!tags.TryGetValue(building.GetInstanceID(), out nameTag)) {
            nameTag = Instantiate<GameObject>(nameTagPrefab, transform).transform;
            tags.Add(building.GetInstanceID(), nameTag);
        }
        // nameTag.localPosition = Camera.main.WorldToViewportPoint(building.transform.position);
        Building build = building.GetComponent<IBuildingObject>().obj;
        nameTag.GetComponentInChildren<Text>().text = build.info.name;
    }

    public static void ShowBuildingInteraction(Building building) {

    }

    public void ShowBuildMessage(MapGenerator.BuildableInfo info) {
        switch (info) {
            case MapGenerator.BuildableInfo.Unbuildable : buildInfo.text = "해당 위치에 건설할 수 없습니다."; break;
            case MapGenerator.BuildableInfo.NotQualified : buildInfo.text = "이 건물을 건설하실 수 없습니다."; break;
            case MapGenerator.BuildableInfo.OutOfBounds : buildInfo.text = "지도 밖으로 건설할 수 없습니다."; break;
            case MapGenerator.BuildableInfo.NotEnoughMaterial : buildInfo.text = "건설 재료가 부족합니다."; break;
            case MapGenerator.BuildableInfo.NotEnoughMoney : buildInfo.text = "건설 재화가 부족합니다."; break;
            case MapGenerator.BuildableInfo.PlayerOverlapped : buildInfo.text = "플레이어가 그 자리에 있습니다."; break;
            default : buildInfo.text = "건설이 가능합니다."; break;
        }
    }
    #endregion

    #region UI 메뉴 호출
    public void OpenBuildSelector() {
        buildingSelectionUI.Enable();
    }
    public void CloseBuildSelector() {
        buildingSelectionUI.Disable();
        BuildSelector_Info.Instance.CleanBuildSelectorInfo();
    }
    #endregion
}

public static class CanvasGroupMethods {
    public static void Enable(this CanvasGroup cg) {
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }
    public static void Disable(this CanvasGroup cg) {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
}