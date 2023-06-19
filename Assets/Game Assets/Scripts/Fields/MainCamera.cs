using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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

public class MainCamera : MonoBehaviour
{
  public static Camera Cam;
  public static MainCamera Instance;
  public Transform T { get; private set; }
  public Transform target;
  public IInteractable trackTarget;
  
  public Vector3 RelativePos;

  public static Ray Ray => Cam.ScreenPointToRay(Input.mousePosition);
  protected void Awake() {
    Cam = GetComponent<Camera>();
    T = transform;
    Instance = this;
  }

  protected void Start() {
    T.position = target.position + RelativePos;
    T.LookAt(target);
  }

  private Vector3 camVel = Vector3.zero;
  public void SmoothUpdatePos(Vector3 pos) {
    transform.position = Vector3.SmoothDamp(transform.position, pos + RelativePos, ref camVel, .2f, 100f);
  }


  const float MIN_ALPHA = 0.4f, FADE_SPEED = 0.02f;
  class Blender {
    public Material material;
    public StandardShaderMethods.Mode blend;
    public Color color;
    public bool recovered = false;
  }
  readonly Dictionary<int, Blender> blenders = new();
  readonly List<int> blockings = new();

  /// <summary>
  /// 카메라 기준 플레이어를 가리는 모든 Unity Standard Shader 적용 오브젝트를 반투명하게 만듭니다.<br/>
  /// 범위를 벗어난 오브젝트는 다시 보이게 됩니다.
  /// </summary>
  protected void OnPreCull() {
    blockings.Clear();
    // 카메라와 플레이어 사이에 존재하는 모든 Collider에 대해 다음을 검사합니다.
    //TODO 카메라가 플레이어를 추적하고 있지 않을 경우 일시적으로 투명화를 중단합니다.
    Vector3 line = target.position - T.position;
    foreach (var hit in Physics.RaycastAll(T.position, line, line.magnitude - 0.6f)) {
      // 바닥 타일과 플레이어 모델을 제외하기 위해 0.3f 정도 짧게 설정합니다.
      //* 0. 렌더러가 유효하지 않거나 자식 오브젝트에서도 렌더러가 없을 경우 검사하지 않습니다. (후자 예시 : 건물 오브젝트)
      Renderer renderer;
      if (
        (renderer = hit.collider.GetComponent<Renderer>()) == null &&
        (renderer = hit.collider.GetComponentInChildren<Renderer>()) != null &&
        renderer.enabled is false &&
        renderer.material.shader.name is not "Standard"
      ) continue;
      //* 1. 저장할 키와 값을 가져옵니다.
      int key = hit.colliderInstanceID;
      blockings.Add(key);
      //* 2. 새 오브젝트일 경우 추적을 시작합니다.
      if (!blenders.ContainsKey(key)) {
        blenders.Add(key, new() {
          material = renderer.material,
          blend = renderer.material.CheckRenderMode(),
          color = renderer.material.color
        });
        // Debug.Log($"Transparent Start - {renderer.name} : {blenders[key].blend}{((blenders[key].blend == StandardShaderMethods.Mode.Transparent) ? "(" + blenders[key].color.a.ToString("P0") + ")" : "")} -> Transparent({minAlpha.ToString("P0")})");
      }
    }
    // 이번 컬링에서 투명성을 판단할 오브젝트들을 검사합니다.
    foreach(var pair in blenders.ToArray()) { // ToArray로 복제한 뒤 Dictionary를 제어합니다.
      // 카메라를 가리는 오브젝트는 minAlpha까지 점차 투명해집니다.
      if (blockings.Contains(pair.Key)) { //? 해당 시점에 카메라를 가리는 오브젝트일 경우
        if (!pair.Value.material.IsRenderMode(StandardShaderMethods.Mode.Transparent)) {
          pair.Value.material.ChangeRenderMode(StandardShaderMethods.Mode.Transparent);
        }
        var color = pair.Value.material.color;
        color.a = Mathf.Clamp(color.a - FADE_SPEED, MIN_ALPHA, 1f);
        pair.Value.material.color = color; // Dict Value(Blender) is called by reference
      }
      // 카메라를 가리지 않는 오브젝트는 점차 알파를 회복하고, 완전히 회복한 오브젝트는 더 이상 추적하지 않습니다.
      else {
        var color = pair.Value.material.color;
        color.a = Mathf.Clamp(color.a + FADE_SPEED, MIN_ALPHA, pair.Value.color.a);
        pair.Value.material.color = color;
        if (color.a == pair.Value.color.a) {
          if (pair.Value.blend != StandardShaderMethods.Mode.Transparent) {
            pair.Value.material.ChangeRenderMode(pair.Value.blend);
            pair.Value.material.color = pair.Value.color;
          }
          blenders.Remove(pair.Key);
        }
      }
    }
  }
}

// https://answers.unity.com/questions/1004666/change-material-rendering-mode-in-runtime.html 에서 코드 참조.
public static class StandardShaderMethods
{
  public enum Mode
  {
    Opaque,
    Cutout,
    Fade,
    Transparent
  }

  public static bool IsRenderMode(this Material standardShaderMaterial, Mode mode) => mode switch {
    Mode.Opaque => (
      standardShaderMaterial.GetInt("_SrcBlend") == (int)BlendMode.One &&
      standardShaderMaterial.GetInt("_DstBlend") == (int)BlendMode.Zero &&
      standardShaderMaterial.IsKeywordEnabled("_ALPHATEST_ON") == false
    ),
    Mode.Cutout => (
      standardShaderMaterial.GetInt("_SrcBlend") == (int)BlendMode.One &&
      standardShaderMaterial.GetInt("_DstBlend") == (int)BlendMode.Zero &&
      standardShaderMaterial.IsKeywordEnabled("_ALPHATEST_ON") == true
    ),
    Mode.Fade => (
      standardShaderMaterial.GetInt("_SrcBlend") == (int)BlendMode.SrcAlpha &&
      standardShaderMaterial.GetInt("_DstBlend") == (int)BlendMode.OneMinusSrcAlpha
    ),
    Mode.Transparent => (
      standardShaderMaterial.GetInt("_SrcBlend") == (int)BlendMode.One &&
      standardShaderMaterial.GetInt("_DstBlend") == (int)BlendMode.OneMinusSrcAlpha
    ),
    _ => false
  };

  public static Mode CheckRenderMode(this Material standardShaderMaterial) {
    if (standardShaderMaterial.GetInt("_SrcBlend") == (int)BlendMode.One) {
      if (standardShaderMaterial.GetInt("_DstBlend") == (int)BlendMode.Zero) {
        if (standardShaderMaterial.IsKeywordEnabled("_ALPHATEST_ON")) return Mode.Cutout;
        return Mode.Opaque;
      }
      return Mode.Transparent;
    }
    return Mode.Fade;
  }

  public static void ChangeRenderMode(this Material standardShaderMaterial, Mode mode) {
    switch (mode)
    {
      case Mode.Opaque:
        standardShaderMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
        standardShaderMaterial.SetInt("_DstBlend", (int)BlendMode.Zero);
        standardShaderMaterial.SetInt("_ZWrite", 1);
        standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
        standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
        standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        standardShaderMaterial.renderQueue = -1;
        break;
      case Mode.Cutout:
        standardShaderMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
        standardShaderMaterial.SetInt("_DstBlend", (int)BlendMode.Zero);
        standardShaderMaterial.SetInt("_ZWrite", 1);
        standardShaderMaterial.EnableKeyword("_ALPHATEST_ON");
        standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
        standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        standardShaderMaterial.renderQueue = 2450;
        break;
      case Mode.Fade:
        standardShaderMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        standardShaderMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        standardShaderMaterial.SetInt("_ZWrite", 0);
        standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
        standardShaderMaterial.EnableKeyword("_ALPHABLEND_ON");
        standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        standardShaderMaterial.renderQueue = 3000;
        break;
      case Mode.Transparent:
        standardShaderMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
        standardShaderMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        standardShaderMaterial.SetInt("_ZWrite", 0);
        standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
        standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
        standardShaderMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        standardShaderMaterial.renderQueue = 3000;
        break;
    }
  }
}