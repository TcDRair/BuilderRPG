using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

public class NavMeshManager : MonoBehaviour
{
  public static NavMeshManager Instance;
  public void Awake() { Instance = this; }

  public NavMeshSurface surface;
}

#if UNITY_EDITOR

[CustomEditor(typeof(NavMeshManager))]
public class NavMeshManagerEditor : Editor
{
  public override void OnInspectorGUI() {
    base.OnInspectorGUI();
    if (GUILayout.Button("Build NavMesh")) {
      NavMeshManager manager = (NavMeshManager)target;
      manager.surface.BuildNavMesh();
    }
  }
}
#endif