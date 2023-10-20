using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static MainSetting;

using Rair.Field.Interact;
namespace Rair.Field
{
	public class FieldUI : MonoBehaviour
	{
		public static FieldUI Instance;

		[SerializeField] protected FieldInteractionMenu interactionMenu;

		protected void Awake()
		{
			Instance = this;
		}

		public void UISpaceClicked()
		{
			if (Input.GetMouseButtonUp(0)) {
				if (interactionMenu.Current != null && interactionMenu.inputInterval <= 0)
				{
					FieldInteractionMenu.Instance.HideInteractions();
				}
			}
		}
  }
}