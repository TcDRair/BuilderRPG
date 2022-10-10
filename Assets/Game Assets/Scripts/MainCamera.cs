using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MainCamera : MonoBehaviour
{
  public static Camera cam;
  public static MainCamera Instance;


  public IInteractable trackTarget;
  
  public Vector3 quaterViewPos;

  public static Ray ray {
    get => cam.ScreenPointToRay(Input.mousePosition);
  }

  void Awake() {
    cam = GetComponent<Camera>();
    Instance = this;
  }

  /// <summary>직전 프레임, 혹은 마지막 검사 시 저장된 목표 지점입니다.</summary>
  Vector3 targetPos;
  // Update is called once per frame
  void Update() {
    switch(State.Current.Camera) {
      case State.CState.Track_Player: targetPos = Player.Instance.transform.position + quaterViewPos; break;
      case State.CState.Track_Interactable:
        targetPos = trackTarget.GetPosition() + quaterViewPos;
        //! 임시
        if (Input.GetKeyDown(KeyCode.Escape)) {
          trackTarget = null;
          UI.Instance.ClearInteractions();
        }
        break;
      //TODO case TrackMode.PlayerInBattle: break;
    }
    transform.position = Vector3.Lerp(transform.position, targetPos, 0.05f);
  }


  const float minAlpha = 0.4f, fadeSpeed = 0.02f;
  class Blender {
    public Material material;
    public StandardShaderMethods.Mode blend;
    public Color color;
    public bool recovered = false;
  }
  Dictionary<int, Blender> blenders = new Dictionary<int, Blender>();
  List<int> blockings = new List<int>();

  /// <summary>
  /// 카메라와 플레이어 사이에 다른 오브젝트가 존재할 때 머티리얼을 반투명하게 만듭니다.<br/>
  /// 범위를 벗어난 오브젝트는 다시 보이게 됩니다.
  /// </summary>
  void OnPreCull() {
    blockings.Clear();
    // 카메라와 플레이어 사이에 존재하는 모든 Collider에 대해 다음을 검사합니다.
    //* 카메라가 플레이어를 추적하고 있지 않을 경우 일시적으로 투명화를 중단합니다.
    Vector3 line = Player.Instance.transform.position - transform.position;
    if (State.Current.IsCameraTrackingPlayer) foreach (var hit in Physics.RaycastAll(transform.position, line, line.magnitude - 0.3f)) {
      // 바닥 타일을 제외하기 위해 0.3f 정도 짧게 설정합니다.
      // 저장할 키와 값을 가져옵니다.
      int key = hit.colliderInstanceID;
      Renderer renderer = hit.collider.GetComponent<Renderer>();
      // 렌더러가 유효하지 않으면 검사하지 않습니다.
      if (renderer == null || !renderer.enabled) {
        // 다만 이 경우 활성화된 자식 오브젝트에 렌더러가 있는지도 검사합니다. 건물 오브젝트가 이 경우에 해당합니다.
        if ((renderer = hit.collider.GetComponentInChildren<Renderer>()) == null || !renderer.enabled) continue;
      }
      // 렌더러가 유효하면 검사합니다.
      blockings.Add(key);
      // 새 오브젝트가 추가되면 추적을 시작합니다.
      if (!blenders.ContainsKey(key)) {
        blenders.Add(key, new Blender() {
          material = renderer.material,
          blend = renderer.material.CheckRenderMode(),
          color = renderer.material.color
        });
        // Debug.Log($"Transparent Start - {renderer.name} : {blenders[key].blend}{((blenders[key].blend == StandardShaderMethods.Mode.Transparent) ? "(" + blenders[key].color.a.ToString("P0") + ")" : "")} -> Transparent({minAlpha.ToString("P0")})");
      }
    }
    // 이번 컬링에서 투명성을 판단할 오브젝트들을 검사합니다.
    foreach(var pair in blenders.ToArray()) {
      // 카메라를 가리는 오브젝트는 minAlpha까지 점차 투명해집니다.
      if (blockings.Contains(pair.Key)) {
        if (!pair.Value.material.IsRenderMode(StandardShaderMethods.Mode.Transparent)) {
          pair.Value.material.ChangeRenderMode(StandardShaderMethods.Mode.Transparent);
        }
        var color = pair.Value.material.color;
        color.a = Mathf.Clamp(color.a - fadeSpeed, minAlpha, 1f);
        pair.Value.material.color = color;
      }
      // 카메라를 가리지 않는 오브젝트는 점차 알파를 회복하고, 완전히 회복한 오브젝트는 더 이상 추적하지 않습니다.
      else {
        var color = pair.Value.material.color;
        color.a = Mathf.Clamp(color.a + fadeSpeed, minAlpha, pair.Value.color.a);
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

  public static bool IsRenderMode(this Material standardShaderMaterial, Mode mode) {
    switch (mode)
    {
      case Mode.Opaque:
        return (
          standardShaderMaterial.GetInt("_SrcBlend") == (int)BlendMode.One &&
          standardShaderMaterial.GetInt("_DstBlend") == (int)BlendMode.Zero &&
          standardShaderMaterial.IsKeywordEnabled("_ALPHATEST_ON") == false
        );
      case Mode.Cutout:
        return (
          standardShaderMaterial.GetInt("_SrcBlend") == (int)BlendMode.One &&
          standardShaderMaterial.GetInt("_DstBlend") == (int)BlendMode.Zero &&
          standardShaderMaterial.IsKeywordEnabled("_ALPHATEST_ON") == true
        );
      case Mode.Fade:
        return (
          standardShaderMaterial.GetInt("_SrcBlend") == (int)BlendMode.SrcAlpha &&
          standardShaderMaterial.GetInt("_DstBlend") == (int)BlendMode.OneMinusSrcAlpha
        );
      case Mode.Transparent:
        return (
          standardShaderMaterial.GetInt("_SrcBlend") == (int)BlendMode.One &&
          standardShaderMaterial.GetInt("_DstBlend") == (int)BlendMode.OneMinusSrcAlpha
        );
      default : return false;
    }
  }

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
 