using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using static MainSetting;

using Rair.Field.Interact;
namespace Rair.Field
{
	public class FieldUI : MonoBehaviour
	{
		public static FieldUI Instance;

		protected void Awake()
		{
			Instance = this;
		}
	}
}