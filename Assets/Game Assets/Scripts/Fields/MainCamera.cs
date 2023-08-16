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
  public static Ray Ray => Cam.ScreenPointToRay(Input.mousePosition);
  public static MainCamera Instance;
  public Transform T { get; private set; }
  public Transform target;
  public Rair.Field.Interact.Prop trackTarget;
  
  public Vector3 RelativePos;

  private int _targetRenderer;
  protected void Awake() {
    Cam = GetComponent<Camera>();
    T = transform;
    Instance = this;

    _targetRenderer = target.GetComponentInChildren<Renderer>().GetInstanceID();
    RenderPipelineManager.beginCameraRendering += MakeTranslucencyProps;
  }

  protected void Start() {
    T.position = target.position + RelativePos;
    T.LookAt(target);
  }

  protected void OnDestroy() {
    RenderPipelineManager.beginCameraRendering -= MakeTranslucencyProps;
  }

  private Vector3 camVel = Vector3.zero;
  public void SmoothUpdatePos(Vector3 pos) {
    transform.position = Vector3.SmoothDamp(transform.position, pos + RelativePos, ref camVel, .2f, 100f);
  }

  #region Prop Transparency
  const float MIN_ALPHA = 0.85f, FADE_SPEED = 0.02f;
  class Blender {
    public Material material;
    public URPLitVar matVar;
    public float alpha;
    public bool targeted = false;
  }
  readonly Dictionary<int, Blender> blenders = new();
  readonly List<int> blockings = new();
  const string shaderName = "Universal Render Pipeline/Lit";

  /// <summary>
  /// 카메라 기준 플레이어를 가리는 모든 URP Lit 오브젝트를 반투명하게 만듭니다.<br/>
  /// 범위를 벗어난 오브젝트는 다시 보이게 됩니다.
  /// </summary>
  protected void MakeTranslucencyProps(ScriptableRenderContext context, Camera cam) {
    //? 빌트인 렌더파이프라인이 아닐 경우 OnPreCull()이 작동하지 않음
    blockings.Clear();
    //* 카메라와 플레이어 사이에 존재하는 모든 Collider에 대해 다음을 검사합니다.
    //TODO 카메라가 플레이어를 추적하고 있지 않을 경우 일시적으로 투명화를 중단합니다.
    Vector3 line = target.position - T.position;
    //* 카메라와 플레이어 사이에 존재하는 모든 Collider를 검사하고 적용 대상 여부를 판단합니다.
    foreach (var hit in Physics.RaycastAll(T.position, line, line.magnitude - 0.2f)) {
      // 바닥 타일을 제외하기 위해 Ray를 약간 짧게 설정합니다.
      Renderer renderer;
      if (
        ( // 렌더러 존재 검사
          (renderer = hit.collider.GetComponent<Renderer>()) == null &&
          (renderer = hit.collider.GetComponentInChildren<Renderer>()) == null
        ) ||
        renderer.GetInstanceID() == _targetRenderer || // 플레이어 렌더러 검사
        renderer.enabled is false || // 렌더러 활성화 검사
        renderer.material.shader.name is not shaderName // Standard Shader 여부 검사
      ) continue;
      //* 1. 저장할 키와 값을 가져옵니다.
      int key = hit.colliderInstanceID;
      blockings.Add(key);
      //* 2. 새 오브젝트일 경우 추적을 시작합니다.
      if (!blenders.ContainsKey(key)) {
        blenders.Add(key, new() {
          material = renderer.material,
          matVar = renderer.material.GetLitVar(),
          alpha = renderer.material.color.a
        });
        // Debug.Log($"Transparent Start - {renderer.name} : {blenders[key].blend}{((blenders[key].blend == StandardShaderMethods.Mode.Transparent) ? "(" + blenders[key].color.a.ToString("P0") + ")" : "")} -> Transparent({minAlpha.ToString("P0")})");
      }
    }
    //* 이번 컬링에서 적용 대상 오브젝트에 대해 다음 과정을 수행합니다.
    foreach(var pair in blenders.ToArray()) { //? ToArray로 복제하여 순회 대상을 수정하는 오류를 방지합니다.
      //* 1. 카메라를 가리는 오브젝트는 minAlpha까지 점차 투명해집니다.
      if (blockings.Contains(pair.Key)) {
        if (pair.Value.targeted is false) // 이번 컬링에 추가된 오브젝트
          pair.Value.material.ToFadeMode();
        var color = pair.Value.material.color;
        color.a = Mathf.Clamp(color.a - FADE_SPEED, MIN_ALPHA, 1f);
        pair.Value.material.color = color; // Dict Value(Blender) is called by reference
      }
      //* 2. 카메라를 가리지 않는 오브젝트는 점차 알파를 회복하고, 완전히 회복한 오브젝트는 더 이상 추적하지 않습니다.
      else {
        var color = pair.Value.material.color;
        color.a = Mathf.Clamp(color.a + FADE_SPEED, MIN_ALPHA, pair.Value.alpha);
        pair.Value.material.color = color;
        if (color.a == pair.Value.alpha) {
          pair.Value.material.SetLitVar(pair.Value.matVar);
          blenders.Remove(pair.Key);
        }
      }
    }
  }
  #endregion
}

public struct URPLitVar {
  public bool APM_ON, STT;
  public int RQ, DB, DBA, S, ZW;
  public bool DO, SC;
}
public static class URPLitExtensions
{
  public enum Mode { Opaque, Transparent }
  
  public static void ToOpaqueMode(this Material material) {
    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
    material.renderQueue = -1;
    material.SetShaderPassEnabled("DepthOnly", true);
    material.SetShaderPassEnabled("ShadowCaster", true);
    material.SetInt("_DstBlend", (int)BlendMode.Zero);
    material.SetInt("_DstBlendAlpha", (int)BlendMode.Zero);
    material.SetInt("_Surface", 0);
    material.SetInt("_ZWrite", 1);
  }
  public static void ToFadeMode(this Material material) {
    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    material.renderQueue = (int)RenderQueue.Transparent;
    material.SetShaderPassEnabled("DepthOnly", false);
    material.SetShaderPassEnabled("ShadowCaster", false);
    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
    material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
    material.SetInt("_Surface", 1);
    material.SetInt("_ZWrite", 0);
  }

  public static URPLitVar GetLitVar(this Material material)
    => new() {
      APM_ON = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"),
      STT = material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"),
      RQ = material.renderQueue,
      DB = material.GetInt("_DstBlend"),
      DBA = material.GetInt("_DstBlendAlpha"),
      S = material.GetInt("_Surface"),
      ZW = material.GetInt("_ZWrite"),
      DO = material.GetShaderPassEnabled("DepthOnly"),
      SC = material.GetShaderPassEnabled("ShadowCaster")
    };
  public static void SetLitVar(this Material material, URPLitVar var) {
    if (var.APM_ON) material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
    else material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    if (var.STT) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
    material.renderQueue = var.RQ;
    material.SetShaderPassEnabled("DepthOnly", var.DO);
    material.SetShaderPassEnabled("ShadowCaster", var.SC);
    material.SetInt("_DstBlend", var.DB);
    material.SetInt("_DstBlendAlpha", var.DBA);
    material.SetInt("_Surface", var.S);
    material.SetInt("_ZWrite", var.ZW);
  }
}
