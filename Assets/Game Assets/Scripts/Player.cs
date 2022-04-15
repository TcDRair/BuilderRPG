using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


/// <summary>게임 플레이어가 조작하는 개체에 부착되어 동작하는 기능들이 포함됩니다.</summary>
public class Player : MonoBehaviour
{
    public NavMeshAgent agent;
    public Camera playerCamera;
    public Vector3 quaterViewPos;

    Animator animator;
    new Rigidbody rigidbody;

    bool OnGround;

    public static MonoBehaviour instance;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // 우클릭 -> 이동
        if (Input.GetMouseButton(1)) {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                agent.SetDestination(hit.point);
            }
        }

        // 스페이스바 -> 슬라이딩

        // 좌클릭 -> 프롭 선택 (없을 경우 아무 행동 없음)
    }



    private Quaternion previousRotation;
    const float velocityModular = 0.25f, angularModular = 0.25f;
    void LateUpdate() {
        // 애니메이션 제어
        animator.SetFloat("Forward", agent.velocity.GetHorizontalMagnitude() * velocityModular);
        animator.SetFloat("Turn", transform.rotation.GetAngularSpeed(previousRotation) * angularModular);
        previousRotation = transform.rotation;
        

        // 카메라 위치 조정
        playerCamera.transform.position = transform.position + quaterViewPos;
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