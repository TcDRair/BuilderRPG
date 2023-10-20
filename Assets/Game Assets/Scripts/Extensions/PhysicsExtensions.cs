using System.Collections;

using UnityEngine;
using UnityEngine.UI;

public static class PhysicsExtensions
{
  #region Raycast
  public static Ray MousePointRay(this Camera camera)
  {
    return camera.ScreenPointToRay(Input.mousePosition);
    /*
    if (camera.orthographic)
    {
      var a1 = camera.ScreenToWorldPoint(Input.mousePosition);
      var a2 = camera.transform.forward * 100;
      return new Ray(a1, a1 + a2);
    }
    else // Perspective
    {
      return camera.ScreenPointToRay(Input.mousePosition);
    }*/
  }

  public static bool ClickRaycast(this Camera camera, out RaycastHit hit, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
  {
    bool raycast = Physics.Raycast(camera.MousePointRay(), out hit, maxDistance, layerMask, queryTriggerInteraction);
    return Input.GetMouseButton(1) && raycast;
  }
  #endregion
}