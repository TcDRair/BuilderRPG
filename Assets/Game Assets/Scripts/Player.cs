using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


/// <summary>게임 플레이어가 조작하는 개체에 부착되어 동작하는 기능들이 포함됩니다.</summary>
public class Player : MonoBehaviour
{
    public static Transform trans;
    public NavMeshAgent agent;

    Animator animator;
    new Rigidbody rigidbody;

    bool OnGround;
    bool build;
    Building building;

    public static MonoBehaviour Instance;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        trans = transform;
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody>();

        agent.updatePosition = false;
    }

    // Update is called once per frame
    void Update()
    {
        // 우클릭 -> 이동
        if (Input.GetMouseButton(1)) {
            RaycastHit hit;
            if (Physics.Raycast(MainCamera.ray, out hit)) {
                agent.SetDestination(hit.point);
            }
        }

        // 스페이스바 -> 슬라이딩

        // 좌클릭 -> 프롭 선택 (없을 경우 아무 행동 없음)
        if (Input.GetMouseButtonDown(0) && !UI.buildPreview) {
            RaycastHit hit;
            if (Physics.Raycast(MainCamera.ray, out hit)) {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Building")) {
                    building = hit.collider.transform.parent.GetComponent<IBuildingObject>().obj;
                    // 건설 중이면 건설 UI 생성
                    if (building.buildProgress < 1f) {
                        build = true;
                        agent.SetDestination(hit.point);
                        animator.SetTrigger("Build");
                    }
                    // 완성 건물이면 건물 UI 생성
                    else {
                        //TODO 1 : NavMesh에 완성 건물 추가. 이건 별도의 스크립트를 마련하는 편이 좋아보임.
                        //TODO 2 : 건설 도중 움직이면 건설 중 취소 <- if문을 여럿 뜯어고쳐야 할 듯.
                        //TODO 3 : UI의 buildSelect와 buildPreview 변수를 하나의 enum으로 통합.
                        //TODO     -> 어차피 화면 전체를 가리는 UI는 중복해서 적용하면 안 되니까.
                    }
                }
            }
        }

        if (build && building.ShowConstructingModel()) {
            build = false;
            building = null;
            animator.SetTrigger("Build End");
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


    void OnColliderEnter(Collider other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Building")) {
            
        }
    }
    void OnColliderStay(Collider other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Building")) {
            UI.ui.ShowBuildingNameTag(other.gameObject);
        }
    }
    void OnColliderExit(Collider other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Building")) {
            
        }
    }
}


public static class Vector3QuaternionMethods {
    public static float GetHorizontalMagnitude(this Vector3 velocity) {
        velocity.y = 0;
        return velocity.magnitude;
    }

    public static float GetAngularSpeed(this Quaternion currentRotation, Quaternion previousRotation) {
        Vector3 currRV = currentRotation.eulerAngles;
        Vector3 prevRV = previousRotation.eulerAngles;
        return (currRV.y - prevRV.y)%360;
    }
}