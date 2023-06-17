using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor (typeof(MapGenerator))]
public class MapGenEditor : Editor {

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapGenerator myScript = (MapGenerator)target;
        if (GUILayout.Button("Set Map Variables")) myScript.SetVariables(myScript.mapTexture);
        if (GUILayout.Button("Build Map from Texture")) myScript.GenerateMap();
        if (GUILayout.Button("Remove Map")) myScript.DestroyMap();
        if (GUILayout.Button("Create Texture from Map")) myScript.SaveMap();
    }
}
#endif