using System.Collections;

using UnityEngine.AI;

public static class NavMeshAgentExtensions
{
  public static bool WannaMoving(this NavMeshAgent agent)
    => agent.remainingDistance > agent.stoppingDistance;
  public static bool IsArrived(this NavMeshAgent agent)
    => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
}
