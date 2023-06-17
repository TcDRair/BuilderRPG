using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FreeMoveCamera : MonoBehaviour
{
  public Camera cam;
  public Transform target;

  Transform tr;
  void Start() {
    tr = transform;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
  }

  [HideInInspector] public bool lockX = false, lockY = true;
  [HideInInspector] public Vector2 rotateBorder = new(0, 180);

  Vector2 rotate;
  [Range(1, 10)]
  public float sensitivity;
  void LateUpdate() {
    // Rotate camera with mouse, inside rotate border.
    var deltaRotate = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * (sensitivity / 3);
    rotate.Set(
      lockX ? Mathf.Clamp(rotate.x + deltaRotate.x, -rotateBorder.x, rotateBorder.x) : rotate.x + deltaRotate.x,
      lockY ? Mathf.Clamp(rotate.y + deltaRotate.y, -rotateBorder.y, rotateBorder.y) : rotate.y + deltaRotate.y
    );
    tr.rotation = Quaternion.Euler(-rotate.y, rotate.x, 0);
    tr.position = tr.localRotation * Vector3.back * 2.5f + Vector3.up * 2 + target.position;
  }
}

[CustomEditor(typeof(FreeMoveCamera))]
class FMCEditor : Editor {
  public override void OnInspectorGUI() {
    base.OnInspectorGUI();
    var fmc = target as FreeMoveCamera;
    EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Lock Rotation", GUILayout.Width(126));
      GUI.enabled = fmc.lockX = GUILayout.Toggle(fmc.lockX, "", GUILayout.Width(15));
      float x = EditorGUILayout.FloatField(fmc.rotateBorder.x, GUILayout.ExpandWidth(true));
      GUI.enabled = true;
      GUI.enabled = fmc.lockY = GUILayout.Toggle(fmc.lockY, "", GUILayout.Width(15));
      float y = EditorGUILayout.FloatField(fmc.rotateBorder.y, GUILayout.ExpandWidth(true));
      fmc.rotateBorder.Set(Mathf.Clamp(x, 0, 180), Mathf.Clamp(y, 0, 90));
    EditorGUILayout.EndHorizontal();
    GUI.enabled = true;
  }
}