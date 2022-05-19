using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(RandomTextureGenerator))]
public class RTGEditor : Editor
{
    bool aborted = false;
    float progress;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        RandomTextureGenerator inst = (RandomTextureGenerator)target;

        GUI.enabled = !inst.isRunning && !aborted;

        //* Map Generate Settings
        inst.saveMap = GUILayout.Toggle(inst.saveMap, new GUIContent("Save Map to Asset", "체크하면 지정한 이미지 파일에 맵(Texture)을 저장합니다."));
        if (GUILayout.Button("Create New Island")) inst.TryMapGenerate();
        inst.fixSeed = GUILayout.Toggle(inst.fixSeed, new GUIContent("Fix Seed", "체크하면 아래의 시드로 맵을 생성합니다. 체크 해제 시 시드가 랜덤으로 생성됩니다."));
        if (inst.fixSeed) inst.seed = EditorGUILayout.IntField("Seed", inst.seed);
        else {
            GUI.enabled = false;
            inst.seed = EditorGUILayout.IntField("Seed", inst.seed);
            GUI.enabled = !inst.isRunning && !aborted;
        }
        inst.mapSize = (Assets.Map.Size)EditorGUILayout.EnumPopup("Map Size", inst.mapSize);
        inst.landRatio = EditorGUILayout.Slider("Land Ratio", inst.landRatio, 0.1f, 0.9f);
        inst.lakeThreshold = EditorGUILayout.Slider("Lake Threshold", inst.lakeThreshold, 0f, 0.6f);
        inst.riverCount = EditorGUILayout.IntSlider("River Amount", inst.riverCount, 0, (int)((int)inst.mapSize/8 * inst.landRatio));

        //* Generate Progress Bar
        if (inst.isRunning) {
            GUI.enabled = true;
            if (inst?.mapTexture?.progress.hasStarted ?? false) {
                progress = inst.mapTexture.progress.totalProgress * 0.8f + 0.2f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, inst.mapTexture.progress.ToString());
            }
            else if (inst?.map?.graph?.progress.hasStarted ?? false) {
                progress = inst.map.graph.progress.totalProgress * 0.15f + 0.05f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, inst.map.graph.progress.ToString());
            }
            else if (inst?.map?.progress.hasStarted ?? false) {
                progress = inst.map.progress.totalProgress * 0.05f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, inst.map.progress.ToString());
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