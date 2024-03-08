using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class UIExtensions
{
	#region CanvasGroup
	public static void Enable(this CanvasGroup cg)
	{
		cg.alpha = 1f;
		cg.interactable = true;
		cg.blocksRaycasts = true;
	}
	public static void Disable(this CanvasGroup cg)
	{
		cg.alpha = 0f;
		cg.interactable = false;
		cg.blocksRaycasts = false;
	}
	public static void Toggle(this CanvasGroup cg)
	{
		if (cg.alpha == 0f) cg.Enable();
		else cg.Disable();
	}
  #endregion
}
