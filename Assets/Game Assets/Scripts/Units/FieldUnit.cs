using System.Collections;

using Rair.Field.Interact;

using UnityEngine;
using UnityEngine.AI;

namespace Rair.Field
{
  public class FieldUnit : MonoBehaviour
  {
    public Transform tr;
    public Animator animator;
    public NavMeshAgent agent;
    public Rigidbody rigidBody;

    /// <summary>이동 조작이 없으면 계속 수행되는 작업을 할당합니다</summary>
    public (Interaction i, Coroutine c) moveTask;

    public void PlayAnim(FieldAnimatorState state) => animator.SetTrigger(state.ToString());
  }
  public enum FieldAnimatorState
  {
    Idle,
    Walk,
    Run,
    Sit,
    

    Mine,
    Build,

  }
}