using UnityEngine;
using UnityEditor;

using Assets.Maps;
using Assets.Util;

#if UNITY_EDITOR
namespace Rair.Field.Maps
{
  [CustomEditor(typeof(RandomTextureGenerator))]
  public class RTGEditor : Editor
  {
    bool aborted = false, randomConfig = false, useSample = false;
    bool initial = true; // Has not generated yet
    float progress;

    GUIStyle bold;
    protected void OnEnable() {
      bold = new() {
        fontStyle = FontStyle.Bold,
        normal = new() { textColor = Color.white }
      };
    }
    public override void OnInspectorGUI()
    {
      RandomTextureGenerator inst = (RandomTextureGenerator)target;

      //* Inspector properties
      EditorGUILayout.LabelField("Properties", bold);
        EditorGUI.indentLevel++;
        base.OnInspectorGUI();
        EditorGUI.indentLevel--;
      //* Inspector configurations
      EditorGUILayout.LabelField("Configurations", bold);
        EditorGUI.indentLevel++;

        useSample = EditorGUILayout.Toggle("Use Sample", useSample);
        if (useSample) {
          GUI.enabled = randomConfig = inst.fixedSeed = false;
          inst.seed = 1167937052;
          inst.mapSize = Size.s4;
          inst.landRatio = 0.4142049f;
          inst.lakeThreshold = 0.08866274f;
          inst.riverCount = 11;
        }

        GUI.enabled = !useSample;
        GUILayout.BeginHorizontal();
          inst.fixedSeed = EditorGUILayout.Toggle("Fix Seed", inst.fixedSeed);
          GUI.enabled = !inst.fixedSeed;
          inst.seed = EditorGUILayout.IntField(inst.seed);
          GUI.enabled = !useSample;
        GUILayout.EndHorizontal();

        inst.mapSize = (Size)EditorGUILayout.EnumPopup("Map Size", inst.mapSize);
        randomConfig = EditorGUILayout.Toggle("Random Config", randomConfig);
          EditorGUI.indentLevel++;
          GUI.enabled = !randomConfig && !useSample;
            inst.landRatio = EditorGUILayout.Slider("Land Ratio", inst.landRatio, 0.25f, 0.75f);
            inst.lakeThreshold = EditorGUILayout.Slider("Lake Threshold", inst.lakeThreshold, 0f, 0.7f);
            inst.riverCount = EditorGUILayout.IntSlider("River Amount", inst.riverCount, 0, (int)((int)inst.mapSize/16 * inst.landRatio));
          GUI.enabled = true;
          EditorGUI.indentLevel--;
        
        EditorGUI.indentLevel--;
      //* Progress Indiators
      EditorGUILayout.LabelField("Progress", bold);
        EditorGUI.indentLevel++;
        if (inst.isRunning) {
          var m = inst.Map;
          (var p, int step) =
            m.Timer.Finished is not true
              ? ((IProgressTimerProvider)m, 1)
            : m.Graph.Timer.Finished is not true
              ? (m.Graph, 2)
            : inst.MapTexture.Timer.Finished is not true
              ? (inst.MapTexture, 3)
              : (inst.terrainGenerator, 4);
          EditorGUI.ProgressBar(
            Indented,
            progress = p.Timer.CurrentRatio,
            $"{p.Timer} [{step}/4]"
          );
          if (GUI.Button(Indented, "Cancel Generating")) {
            inst.CancelGenerate();
            aborted = true;
          }
          Repaint();
        } else if (aborted) {
          var temp = GUI.contentColor;
          GUI.contentColor = Color.yellow;
            EditorGUI.ProgressBar(
              Indented,
              progress,
              "Cancelled. Restart Generator to proceed."
            );
          GUI.contentColor = temp;
          if (GUI.Button(Indented, "Restart Generator")) {
            inst.CancelGenerate();
            inst.Reset();
            aborted = false;
          }
        } else {
          EditorGUI.ProgressBar(
            Indented,
            initial ? 0 : 1,
            initial ? "Ready" : "Finished"
          );
          if (GUI.Button(Indented, "Create New Island")) {
            if (randomConfig) {
              inst.landRatio = Random.Range(.25f, .75f);
              inst.lakeThreshold = Random.Range(0, .7f);
              inst.riverCount = Random.Range(0, (int)((int)inst.mapSize/16 * inst.landRatio));
            }
            initial = false;
            inst.TryMapGenerate(this);
          }
        }
        EditorGUI.indentLevel--;
      //end
    }
    Rect Indented => EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
  }
}
#endif