using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MainCamera))]
public class MainCameraEditor : Editor {
  public override void OnInspectorGUI() {
    base.OnInspectorGUI();
    if (GUILayout.Button("Set Target")) {
      var cam = target as MainCamera;
      cam.transform.position = cam.target.position + cam.RelativePos;
    }
  }
}
