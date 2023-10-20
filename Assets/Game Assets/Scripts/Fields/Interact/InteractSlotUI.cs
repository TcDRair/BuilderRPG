using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Rair.Field.Interact
{
	public class InteractSlotUI : MonoBehaviour
	{
		// Cell의 목적 : 해당 셀에 할당되는 버튼의 스프라이트, 작동 시간, 툴팁, 연결 메서드를 관리
		[SerializeField] protected Image image, progress;
		[SerializeField] protected Text cellName, duration, amount;
		[SerializeField] protected RectTransform cellNameRect;
		[SerializeField] protected CanvasGroup add, cancel;
		[SerializeField] protected Button detail;

		[HideInInspector] public Prop prop;
		[HideInInspector] public bool active = false;
		[HideInInspector] public RectTransform rect;
		[HideInInspector] public Interaction interaction;

		public void Init(Prop prop, Interaction interaction)
		{
			this.prop = prop;
			rect = GetComponent<RectTransform>();
			this.interaction = interaction;
			cellName.text = interaction.name;
			duration.text = interaction.duration.ToColonNotation();
			image.sprite = interaction.sprite;

			//TODO Detail Button
		}

    protected void Update()
    {
      //TODO Add Button <-> Cancel Button
			// 이왕이면 애니메이션(컴포넌트 회전)도 해주면 좋겠어~
    }

    public void TaskUpdate(Task task)
    {
      if (task.interaction == interaction)
			{
				float remain = interaction.duration - task.elapsedTime;
				duration.text = remain.ToColonNotation();
				progress.fillAmount = remain / interaction.duration;
			}
    }

		public void AddTask()
		{
			if (Player.Instance.moveTask.i == interaction) return;
			prop.TryAddTask(Player.Instance, interaction);
		}
	}
}