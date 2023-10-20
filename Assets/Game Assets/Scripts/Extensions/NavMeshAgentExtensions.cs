using System.Collections;

using UnityEngine.AI;

public static class NavMeshAgentExtensions
{
  public static bool IsMoving(this NavMeshAgent agent)
  {
    return agent.velocity.magnitude > 0.1f;
  }
  public static bool IsArrived(this NavMeshAgent agent)
    => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
}
