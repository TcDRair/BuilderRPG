using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class EditorSceneShortcutUtil {
  static EditorSceneShortcutUtil() { }
  [MenuItem("Edit/Play from Loading Scene %1")]
  public static void PlayFromPrelaunchScene() {
    if (EditorApplication.isPlaying is false) {
      EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
      EditorSceneManager.OpenScene("Assets/Scenes/Load Scene.unity");
      EditorApplication.isPlaying = true;
    }
  }
}
