using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rair.Field.Interact
{
  public class InteractTaskUI : MonoBehaviour
  {
		[SerializeField] private Image image;

    [HideInInspector] public Prop prop;
    [HideInInspector] public RectTransform rect;
    [HideInInspector] public Task task;
		public void Init(Prop prop, Task task)
		{
			this.prop = prop;
			rect = GetComponent<RectTransform>();
			this.task = task;
			image.sprite = task.interaction.sprite;
		}

		public void Cancel()
			=> prop.CancelTasks();
  }
}