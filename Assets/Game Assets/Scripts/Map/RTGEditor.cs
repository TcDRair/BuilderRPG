using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Assets.Map;

#if UNITY_EDITOR
[CustomEditor(typeof(RandomTextureGenerator))]
public class RTGEditor : Editor
{
    bool aborted = false, randomConfig = false, usePreset = false;
    float progress;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        RandomTextureGenerator inst = (RandomTextureGenerator)target;

        GUI.enabled = !inst.isRunning && !aborted;

        //* Map Generate Settings
        GUILayout.BeginHorizontal();
        inst.saveMap = GUILayout.Toggle(inst.saveMap, new GUIContent("Save Map to Asset", "체크하면 지정한 이미지 파일에 맵(Texture)을 저장합니다."));
        usePreset = GUILayout.Toggle(usePreset, new GUIContent("Use Preset", "체크하면 아래 설정들이 사전에 지정된 값들로 초기화됩니다. 항상 동일한 샘플 맵을 생성합니다."));
        GUILayout.EndHorizontal();

        //* Start Button
        if (GUILayout.Button("Create New Island")) {
            if (randomConfig) {
                inst.landRatio = Random.Range(.25f, .75f);
                inst.lakeThreshold = Random.Range(0, .7f);
                inst.riverCount = Random.Range(0, (int)((int)inst.mapSize/16 * inst.landRatio) + 1);
            }
            inst.TryMapGenerate();
        }
        if (usePreset) {
            GUI.enabled = false;
            randomConfig = false;
            inst.randomSeed = true;
            inst.seed = 920955062;
            inst.mapSize = Size.s4;
            inst.landRatio = .5f;
            inst.lakeThreshold = .5f;
            inst.riverCount = 5;
        }
        GUI.enabled = !usePreset;

        //* Seed
        GUILayout.BeginHorizontal();
        inst.randomSeed = GUILayout.Toggle(inst.randomSeed, new GUIContent("Fix Seed", "체크하면 오른쪽의 시드를 고정시킨 상태로 맵을 생성합니다. 체크 해제 시 시드가 랜덤으로 생성됩니다."));
        GUI.enabled = !inst.randomSeed && !usePreset;
        inst.seed = EditorGUILayout.IntField(inst.seed);
        GUILayout.EndHorizontal();

        //* Config
        GUI.enabled = !usePreset;
        inst.mapSize = (Size)EditorGUILayout.EnumPopup("Map Size", inst.mapSize);
        randomConfig = GUILayout.Toggle(randomConfig, new GUIContent("Random Config", "체크하면 아래의 설정들이 랜덤으로 생성됩니다. 체크 해제 시 아래의 설정들이 사용됩니다."));
        GUI.enabled = !randomConfig && !usePreset;
        inst.landRatio = EditorGUILayout.Slider("Land Ratio", inst.landRatio, 0.25f, 0.75f);
        inst.lakeThreshold = EditorGUILayout.Slider("Lake Threshold", inst.lakeThreshold, 0f, 0.7f);
        inst.riverCount = EditorGUILayout.IntSlider("River Amount", inst.riverCount, 0, (int)((int)inst.mapSize/16 * inst.landRatio));

        GUI.enabled = !aborted;
        //* Progress Bar
        if (inst.isRunning) {
            if (inst?.MapTexture?.progress.HasStarted ?? false) {
                progress = inst.MapTexture.progress.TotalProgress * 0.8f + 0.2f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, inst.MapTexture.progress.ToString());
            }
            else if (inst?.Map?.Graph?.progress.HasStarted ?? false) {
                progress = inst.Map.Graph.progress.TotalProgress * 0.15f + 0.05f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, inst.Map.Graph.progress.ToString());
            }
            else if (inst?.Map?.progress.HasStarted ?? false) {
                progress = inst.Map.progress.TotalProgress * 0.05f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, inst.Map.progress.ToString());
            }

            if (GUILayout.Button(new GUIContent("Cancel Generating", "설정을 변경하고 싶거나 너무 오래 걸릴 때 눌러 생성을 취소합니다."))) {
                inst.CancelGenerate();
                aborted = true;
            }
            Repaint();
        }
        else if (aborted) {
            var temp = GUI.contentColor;
            GUI.color = Color.yellow;
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, "Cancelled. Restart Generator to proceed.");
            GUI.color = temp;
        }
        else EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), 1f, "Finished");

    }
}
#endif