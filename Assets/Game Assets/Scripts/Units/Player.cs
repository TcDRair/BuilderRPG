using UnityEngine;
using UnityEngine.AI;
using TMPro;

using static MainSetting;

/// <summary>게임 플레이어가 조작하는 개체에 부착되어 동작하는 기능들이 포함됩니다.</summary>
public class Player : MonoBehaviour
{
  public NavMeshAgent agent;
  // public TextMeshProUGUI text;

  /// <summary>Transform</summary>
  public Transform tr;
  public MainCamera cam;
  public Animator animator;
  public Rigidbody rigidBody;

  InteractSlot slot = null;
  Building _b = null;
  public void StartBuild(Building building) {
    _b = building;
    animator.SetTrigger("Build");
  }
  public void SetTarget(InteractSlot target) {
    if (ReferenceEquals(slot, target)) return; // 이미 타겟이 설정되어 있어 동작 중이므로 무시해도 괜찮습니다.
    slot = target;
    GoToTarget();
  }
  public void GoToTarget() {
    UI.Instance.ClearInteractions();
    agent.SetDestination(slot.body.GetPosition());
  }
  public bool IsTargetReached => agent.remainingDistance <= agent.stoppingDistance;
  public bool Immovable => false;

  public static Player Instance;

  void Awake() { Instance = this; }

  void Start() {
    agent.updatePosition = false;
  }

  RaycastHit hit;
  void Update() {
    // if (Input.GetKeyDown(KeyCode.F11)) Debug.Log Player State

    //* 이동 제어
    //** 1. 이동 조건 미충족 시
    if (Immovable) {
      slot = null;
      agent.ResetPath();
    }
    //** 2. 이동 조작 입력 시
    else if (Input.GetMouseButton(1) && Physics.Raycast(MainCamera.Ray, out hit, 100, floorMask)) {
      agent.SetDestination(hit.point);
    }

    //** X. 카메라 조정
    cam.UpdatePos(tr.position);

    //* 다른 제어
    
    //TODO 애니메이션 제어
    //? if (_b is not null && _b.ShowConstructingModel()) { animator.SetTrigger("Build End"); _b = null; }

    /*if (Input.GetMouseButtonDown(0)) {
      var idx = TMP_TextUtilities.FindIntersectingWord(text, Input.mousePosition, null);
      if (idx > 0) {
        var word = text.textInfo.wordInfo[idx].GetWord();
        if (Keywords.TryGetKeyword(word, out var keyword)) Debug.Log($"{keyword.Name} : Keyword\n{keyword.Description}");
        else Debug.Log(word);
      }
    }*/
  }

  private Quaternion previousRotation;
  const float VELOCITY_MODULAR = .25f, ANGULAR_MODULAR = .25f;
  void LateUpdate() {
    // 애니메이션 제어
    animator.SetFloat("Forward", agent.velocity.GetHorizontalMagnitude() * VELOCITY_MODULAR);
    animator.SetFloat("Turn", tr.rotation.GetAngularSpeed(previousRotation) * ANGULAR_MODULAR);
    previousRotation = tr.rotation;
  }

  void OnAnimatorMove() {
    tr.position = agent.nextPosition;
  }
}
