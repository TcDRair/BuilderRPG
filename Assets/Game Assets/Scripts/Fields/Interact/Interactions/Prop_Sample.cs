using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Rair.Field.Interact
{
	public class Prop_Sample : Prop
	{
		private readonly Queue<Interaction> tasks = new();
		public void CancelTasks() => tasks.Clear();
		public void AddTask(Interaction interaction)
		{
			if (interaction.type == Interaction.Type.Task) tasks.Enqueue(interaction);
		}
		public bool HasTask() => tasks.Count > 0;

		protected float elapsedTime = 0;
		protected void Awake()
		{
			#region Repair
			Interaction repair = new()
			{
				name = "수리",
				tooltip = "구조물의 내구도를 최대치까지 회복합니다.",
				type = Interaction.Type.Action,
				sprite = DefaultRepairProp.sprite,
				duration = level,
				animation = DefaultRepairProp.animation,
				useCondition = true,
			};
			repair.onTriggered.AddListener(() =>
			{
				float m = Durability.maximum,
					c = Durability.current,
					d = Random.value > .8f
						? m - Random.value * (m - c) * .05f
						: m;
				Durability = (d, d);
			});
			repair.onCancelled.AddListener(p =>
			{
				float m = Durability.maximum, c = Durability.current;
				var d = m - Random.value * (m - c) * .05f;
				Durability = (d, c + (d - c) * p);
			});
			repair.onCondition = () => Durability.current < Durability.maximum;
			slots.Add(repair);
			#endregion
		}
		protected void Update()
		{
			if (tasks.TryPeek(out var i))
			{
				elapsedTime += Time.deltaTime;
				if (elapsedTime > i.duration)
				{
					elapsedTime = 0;
					i.onTriggered.Invoke();
					tasks.Dequeue();
				}
			}
		}

	}
}