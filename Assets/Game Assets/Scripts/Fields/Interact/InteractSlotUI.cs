using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;

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
			this.interaction = interaction;
			rect = GetComponent<RectTransform>();
			Clear();
			//TODO Detail Button
		}

    protected void LateUpdate()
    {
			//TODO Add Button <-> Cancel Button
			// 이왕이면 UI 애니메이션도
			if (prop == null) FieldInteractionMenu.Instance.HideInteractions();
    }

    public void TaskUpdate(IATask task)
    {
      if (task.interaction == interaction)
			{
				float dur = interaction.progress[task.current].duration;
				float remain = dur - task.elapsedTime;
				duration.text = remain.ToColonNotation();
				progress.fillAmount = remain / dur;
			}
    }
		public void Clear()
		{
			cellName.text = interaction.name;
			var dur = interaction.progress.Sum(p => p.duration);
			duration.text = dur.ToColonNotation();
			image.sprite = interaction.sprite;
		}

		public void AddTaskForPlayer()
		{
			//TODO waitingTask에 있으면 추가 안 되던데 동일 조건 파악하자
			//TODO 중복 가능한 작업(수확 등)과 아닌 것(파괴, 수리 등)을 구분하자
			if (interaction == Player.Instance.moveTask_Approaching.i) return;
			prop.TryAddTask(Player.Instance, interaction);
		}
	}
}