using UnityEngine;
using UnityEngine.AI;

using static MainSetting;

/// <summary>게임 플레이어가 조작하는 개체에 부착되어 동작하는 기능들이 포함됩니다.</summary>
public class Player : MonoBehaviour
{
    public NavMeshAgent agent;

    Animator animator;
    new Rigidbody rigidbody;
    Building targetBuilding;

    public static MonoBehaviour Instance;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody>();

        agent.updatePosition = false;
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.F11)) Debug.Log(State.current.player);

        RaycastHit hit;
        //* 이동 제어
        if (State.current.CanMove()) {
            if (Input.GetMouseButton(1) && Physics.Raycast(MainCamera.ray, out hit, 100, floorMask)) {
                //TODO 이동할 수 없는 조건을 체크합니다 -> 추후 업데이트로 추가
                agent.SetDestination(hit.point);
                State.current.DoMove();
            }
        }
        else {
            agent.ResetPath();
        }

        //TODO Player는 현재 Action중일 때 Inactive / Battle 상태로 전환되지 않는 이상 행동을 지속합니다.

        //* 다른 제어
        //TODO 애니메이션 제어
        switch(State.current.player) {
            case State.Player.Idle: break;
        }
    }

    private Quaternion previousRotation;
    const float velocityModular = 0.25f, angularModular = 0.25f;
    void LateUpdate() {
        // 애니메이션 제어
        animator.SetFloat("Forward", agent.velocity.GetHorizontalMagnitude() * velocityModular);
        animator.SetFloat("Turn", transform.rotation.GetAngularSpeed(previousRotation) * angularModular);
        previousRotation = transform.rotation;
    }

    void OnAnimatorMove() {
        transform.position = agent.nextPosition;
    }
}
