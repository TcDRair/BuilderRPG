using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

using Assets.Maps;
using Assets.Util;

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
    public override void OnInspectorGUI() {
      var inst = (RandomTextureGenerator)target;

      #region Map Generator
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
          randomConfig = false;
          inst.fixSeed = true;
          inst.seed = 2103673821;// 1167937052;
          inst.mapSize = Size.s4;
          inst.landRatio = 0.3864051f;// 0.4142049f;
          inst.riverCount = 6;// 11;
        }

        GUI.enabled = !useSample;
        GUILayout.BeginHorizontal();
          inst.fixSeed = EditorGUILayout.Toggle("Fix Seed", inst.fixSeed);
          GUI.enabled = !inst.fixSeed;
          inst.seed = EditorGUILayout.IntField(inst.seed);
          GUI.enabled = !useSample;
        GUILayout.EndHorizontal();

        inst.mapSize = (Size)EditorGUILayout.EnumPopup("Map Size", inst.mapSize);
        randomConfig = EditorGUILayout.Toggle("Random Config", randomConfig);
          EditorGUI.indentLevel++;
          GUI.enabled = !randomConfig && !useSample;
            inst.landRatio = EditorGUILayout.Slider("Land Ratio", inst.landRatio, 0.25f, 0.75f);
            inst.riverCount = EditorGUILayout.IntSlider("River Amount", inst.riverCount, 0, (int)((int)inst.mapSize/16 * inst.landRatio));
          GUI.enabled = true;
          EditorGUI.indentLevel--;
        // EditorGUILayout.ObjectField(inst.terrainVariables.props);
        EditorGUI.indentLevel--;
      //* Progress Indiators
      EditorGUILayout.LabelField("Progress", bold);
        EditorGUI.indentLevel++;
        if (MapGenerationRunner.IsRunning(inst)) {
          var m = inst.MapInst;
          var g = MapGenerationRunner.Terrain(inst);
          (var p, int step) =
            m.Timer.Finished is not true
              ? ((IProgressTimerProvider)m, 1)
            : m.Graph.Timer.Finished is not true
              ? (m.Graph, 2)
            : m.MapTexture.Timer.Finished is not true
              ? (m.MapTexture, 3)
              : (g, 4);
          EditorGUI.ProgressBar(
            Indented,
            progress = p.Timer.CurrentRatio,
            $"{p.Timer} [{step}/4]"
          );
          if (GUI.Button(Indented, "Cancel Generating")) {
            MapGenerationRunner.Cancel(inst);
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
            MapGenerationRunner.Cancel(inst);
            MapGenerationRunner.Reset(inst);
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
              inst.riverCount = Random.Range(0, (int)((int)inst.mapSize/16 * inst.landRatio));
            }
            initial = false;
            MapGenerationRunner.Begin(inst, this);
          }
        }
        EditorGUI.indentLevel--;
      #endregion
    }
    Rect Indented => EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
  }
}
