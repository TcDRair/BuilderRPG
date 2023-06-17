using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

using static MainSetting;

/// <summary>게임 플레이어가 조작하는 개체에 부착되어 동작하는 기능들이 포함됩니다.</summary>
public class PlayerFreeMove : MonoBehaviour
{
  public Animator animator;
  public Rigidbody rigidBody;
  [Range(10, 50)] public float speed;
  [Range(.2f, 1)] public float rotationSpeed;
  [Range(1,   5)] public int jumpPower;

  const float DRAG_M = .4f, ANG_M = 1e-5f, JUMP_M = .6f; // multipliers
  bool onGround;
  readonly RigidbodyConstraints freeY = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ,
    freeze = RigidbodyConstraints.FreezeRotation
  ;
  void Update() {
    rigidBody.AddForce(-rigidBody.velocity * .2f);
    rigidBody.AddForce(Physics.gravity * 1.4f);
    //! Physics Engine goes sth wrong...
    //? move forward or backward anytime. (for ease of control)
    if (Input.GetKey(KeyCode.W)) rigidBody.AddForce(transform.forward * DRAG_M * speed, ForceMode.VelocityChange);
    if (Input.GetKey(KeyCode.S)) rigidBody.AddForce(-transform.forward * DRAG_M * speed, ForceMode.VelocityChange);
    //? rotate and jump only when on ground.
    if (onGround) {
      rigidBody.constraints = freeY;
      if (Input.GetKey(KeyCode.A)) rigidBody.AddTorque(Vector3.down * ANG_M * rotationSpeed, ForceMode.VelocityChange);
      if (Input.GetKey(KeyCode.D)) rigidBody.AddTorque(Vector3.up * ANG_M * rotationSpeed, ForceMode.VelocityChange);
      if (Input.GetKeyDown(KeyCode.Space)) {
        rigidBody.AddForce(Vector3.up * rigidBody.mass * Physics.gravity.magnitude * JUMP_M * jumpPower, ForceMode.Impulse);
      }
    }
    else {
      rigidBody.constraints = freeze;
    }
  }
  void Start() { StartCoroutine(JumpCheck()); }

  const float VELOCITY_MODULAR = .125f, ANGULAR_MODULAR = 0.125f;
  void LateUpdate() {
    // 애니메이션 제어
    animator.SetFloat("Forward", rigidBody.velocity.GetHorizontalMagnitude() * VELOCITY_MODULAR);
    animator.SetFloat("Turn", rigidBody.angularVelocity.y * ANGULAR_MODULAR);
    animator.SetBool("OnGround", onGround);
  }

  bool grounding;
  void OnCollisionStay(Collision collision) {
    if (
      collision.collider.CompareTag("Terrain") &&
      Mathf.Abs(collision.relativeVelocity.normalized.y) > .5f // 
    ) { grounding = true; onGround = true; }
  }
  void OnCollisionExit(Collision collision) {
    if (collision.collider.CompareTag("Terrain")) { grounding = false; }
  }

  float timer;
  IEnumerator JumpCheck() {
    for(;;) {
      if (!grounding && onGround) {
        timer = .3f;
        yield return new WaitUntil(() => grounding || (timer -= Time.deltaTime) <= 0);
        if (!grounding) onGround = false;
      }

      yield return null;
    }
  }
}
