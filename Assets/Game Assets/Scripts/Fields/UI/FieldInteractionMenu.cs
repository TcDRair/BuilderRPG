using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static MainSetting;

using Rair.Field.Interact;

namespace Rair.Field
{
	public class FieldInteractionMenu : MonoBehaviour
	{
		public static FieldInteractionMenu Instance;
		[SerializeField] protected RectTransform buttonArea, taskArea;
		[SerializeField] protected CanvasGroup interactionUI;
		[SerializeField] protected GameObject slotUI, taskUI;
		[SerializeField] protected Animator animator;
		[SerializeField] protected Text titleText;
		private readonly Dictionary<Interaction, InteractSlotUI> slots = new();
		private readonly Dictionary<IATask, InteractTaskUI> tasks = new();

		private float _slotInterval, _taskInterval;
		[HideInInspector] public float inputInterval = 0;
		private bool enable = false;
		public Prop Current { get; private set; }
		const float GAP = 5, INPUT_TOLERANCE = .5f;

		#region Initialize
		protected void OnEnable() => Instance = this;
		protected void Awake()
		{
			_slotInterval = slotUI.GetComponent<RectTransform>().rect.height + GAP;
			_taskInterval = taskUI.GetComponent<RectTransform>().rect.height + GAP;
		}
		#endregion

		public void ShowPropMenu(Prop prop)
		{
      #region 추적 대상 확인
      if (!enable) // 메뉴 활성화
			{
				enable = true;
				animator.SetTrigger("Open"); // alpha 및 blocksRaycasts 조정 포함
			}
			else if (ReferenceEquals(prop, Current)) return; // 이미 활성화된 메뉴
			else // 메뉴 변경
			{
				buttonArea.RemoveAllChildren();
				taskArea.RemoveAllChildren();
			}
			Current = prop;
			#endregion
			titleText.text = prop.Name;
			ShowInteractionSlots();
			inputInterval = INPUT_TOLERANCE;
		}
		private void ShowInteractionSlots()
		{
			float y1 = 0, y2 = 0;
      #region 슬롯 갱신
      foreach (var i in Current.slots)
			{
				if (!slots.TryGetValue(i, out var ui))
				{ // UI가 없는 슬롯은 UI 생성
					ui = Instantiate(slotUI, buttonArea).GetComponent<InteractSlotUI>();
					ui.Init(Current, i);
					slots.Add(i, ui);
				}
				// 슬롯 위치 조정(또는 유지)
				ui.rect.anchoredPosition = new(0, y1);
				y1 -= _slotInterval;
			}
			buttonArea.sizeDelta = new(0, -y1);
			foreach (var kp in slots.ToList())
			{
				if (Current.slots.Contains(kp.Key)) continue;
				// 슬롯이 사라진 경우 제거
				Destroy(kp.Value.gameObject);
				slots.Remove(kp.Key);
			}
      #endregion

      #region 대기 작업 갱신
			var waitingTasks = Current.waitingTasks;
      foreach (var t in waitingTasks)
			{
				if (!tasks.TryGetValue(t, out var ui))
				{ // UI가 없는 대기 작업은 UI 생성
          ui = Instantiate(taskUI, taskArea).GetComponent<InteractTaskUI>();
					ui.Init(Current, t);
          tasks[t] = ui;
        }
				// 대기 작업 위치 조정(또는 유지)
				ui.rect.anchoredPosition = new(0, y2);
				y2 -= _taskInterval;
			}
			taskArea.sizeDelta = new(100, -y2);
			foreach(var kp in tasks.ToList())
			{
				if (waitingTasks.Contains(kp.Key)) continue;
				// 대기 작업이 사라진 경우 제거
				Destroy(kp.Value.gameObject);
				tasks.Remove(kp.Key);
			}
      #endregion

      #region 진행 작업 갱신
			if (Current.CurrentTask is var task && task != default)
			{
				slots[task.interaction].TaskUpdate(task);
			}
			else foreach (var s in slots.Values) s.Clear();
      #endregion
    }

    protected void LateUpdate()
		{
			if (enable && Current != null)
			{
				ShowInteractionSlots();
			}
			inputInterval -= Time.deltaTime;
		}

		public void HideInteractions()
		{
			if (enable)
			{
				enable = false;
				animator.SetTrigger("Close"); // alpha 및 blocksRaycasts 조정 포함
				buttonArea.RemoveAllChildren();
				taskArea.RemoveAllChildren();
				slots.Clear();
				tasks.Clear();
			}
		}
	}
}