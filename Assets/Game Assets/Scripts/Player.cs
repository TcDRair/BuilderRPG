using UnityEngine;
using UnityEngine.AI;
using TMPro;

using static MainSetting;

/// <summary>게임 플레이어가 조작하는 개체에 부착되어 동작하는 기능들이 포함됩니다.</summary>
public class Player : MonoBehaviour
{
  public NavMeshAgent agent;
  // public TextMeshProUGUI text;

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
  public bool IsTargetReached() { return agent.remainingDistance <= agent.stoppingDistance; }

  public static Player Instance;

  void Awake() { Instance = this; }

  void Start() {
    agent.updatePosition = false;
  }

  RaycastHit hit;
  void Update() {
    if (Input.GetKeyDown(KeyCode.F11)) Debug.Log(State.Current.Player);

    //* 이동 제어
    if (State.Current.CanMove()) {
      if (Input.GetMouseButton(1) && Physics.Raycast(MainCamera.Ray, out hit, 100, floorMask)) {
        //TODO 이동할 수 없는 조건을 체크합니다
        agent.SetDestination(hit.point);
        State.Current.DoMove();
      }
      else if (slot?.StartAction() ?? false) slot = null;
    }
    else { //? 이동이 불가능한 상태일 때에는 이동 명령을 취소합니다. 목표 타겟이 있다면 목표에서 제거됩니다.
      slot = null;
      agent.ResetPath();
    }

    //* 다른 제어
    //TODO 애니메이션 제어
    switch(State.Current.Player) {
      case State.PState.Idle: break;
      case State.PState.Action_Active: {
        if (_b is not null && _b.ShowConstructingModel()) { animator.SetTrigger("Build End"); _b = null; }
        break;
      }
    }

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
  const float VELOCITY_MODULAR = 0.25f, ANGULAR_MODULAR = 0.25f;
  void LateUpdate() {
    // 애니메이션 제어
    animator.SetFloat("Forward", agent.velocity.GetHorizontalMagnitude() * VELOCITY_MODULAR);
    animator.SetFloat("Turn", transform.rotation.GetAngularSpeed(previousRotation) * ANGULAR_MODULAR);
    previousRotation = transform.rotation;
  }

  void OnAnimatorMove() {
    transform.position = agent.nextPosition;
  }
}
