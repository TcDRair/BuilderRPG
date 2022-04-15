using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(MapGenScript))]
public class MapGenEditor : Editor {

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapGenScript myScript = (MapGenScript)target;
        if (GUILayout.Button("Set Map Variables")) { myScript.SetWidthHeight(); }
        if (GUILayout.Button("Build Map from Texture")) { myScript.MapBuild(); }
        if (GUILayout.Button("Remove Map")) { myScript.MapDestroy(); }
        if (GUILayout.Button("Create Texture from Map")) { myScript.MapTextureCreate(); }
    }
}