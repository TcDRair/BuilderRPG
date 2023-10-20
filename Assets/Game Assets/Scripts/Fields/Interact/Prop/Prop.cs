using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditorInternal;

namespace Rair.Field.Interact
{
	public abstract class Prop : MonoBehaviour
	{ // 상호작용 가능한 대상
		#region Properties shown in other scripts
		public int Level => level;
		public virtual bool Active => slots.Count() > 0;
		public virtual string Name => propName;
		public Transform Tr => _Tr = _Tr != null ? _Tr : transform;
		private Transform _Tr;
		#endregion

		#region Properties editable in Inspector
		[SerializeField] protected string propName;
		[SerializeField] protected int level = -1;
		[SerializeField] protected bool useDefaultInteractions, useDestroy, useRepair, useDismantle;

		public readonly List<Interaction> slots = new();
		public readonly Queue<Task> tasks = new();
		public readonly List<UnityEvent> triggers = new();
		public readonly List<UnityEvent<float>> cancellers = new();
		#endregion

		#region Properties editable in derived script
		/// <summary>추가 UI를 표시할 필요가 있는지를 나타냅니다</summary>
		public virtual bool HasInfo => false;
		public (float current, float maximum) Durability { get; protected set; }
		#endregion

		protected virtual void Update()
		{
			UpdateTasks();
		}

		#region Tasks
		public void CancelTasks() => tasks.Clear();
		public void TryAddTask(FieldUnit u, Interaction i)
		{
			u.moveTask = new(i, StartCoroutine(CheckMovingNeeded(u, i)));
		}
		private IEnumerator CheckMovingNeeded(FieldUnit u, Interaction i) {
			var dist = Vector3.Distance(u.transform.position, Tr.position);
			if (i.maxDistance > 0 && dist > i.maxDistance)
			{
				u.agent.SetDestination(Tr.position);
				yield return new WaitWhile(() => Vector3.Distance(u.transform.position, Tr.position) > i.maxDistance);
				u.agent.ResetPath();
			}
			AddTask(i);
			u.moveTask = default;
		}
		protected virtual void AddTask(Interaction i)
		{
			if (i.type is Interaction.Type.Special) return;
			tasks.Enqueue(new(i));
		}

		public bool HasTask() => tasks.Count > 0;
		protected void UpdateTasks()
		{
			if (tasks.TryPeek(out var task) && (task.elapsedTime += Time.deltaTime) >= task.interaction.duration)
			{
				task.interaction.onTriggered.Invoke();
				tasks.Dequeue();
			}
		}
		#endregion

		#region Default Interactions
		public Interaction DefaultDestroyProp
		{
			get
			{
				Interaction p = new()
				{
					name = "파괴",
					tooltip = "이 대상을 파괴합니다.",
					type = Interaction.Type.Action,
					duration = 5,
					maxDistance = 4,
					useCondition = false
				};
				// p.onTriggered.AddListener(() => Destroy(gameObject));
				p.sprite = p.DestroySprite;
				return p;
			}
		}
		public Interaction DefaultRepairProp
		{
			get
			{
				Interaction p = new()
				{
					name = "수리",
					tooltip = "구조물의 내구도를 최대치까지 수리합니다.",
					type = Interaction.Type.Action,
					duration = level * .5f,
					maxDistance = 4,
				};
				// p.onTriggered += () => Debug.Log("수리 완료");
				return p;
			}
		}
		#endregion
	}
}


/*
#if UNITY_EDITOR
[CustomEditor(typeof(Prop), true)]
public class PropEditor : Editor
{
	const int LINE_HEIGHT = 20, INDENT = 15;
	ReorderableList rL;

	protected void OnEnable()
	{
		var prop = target as Prop;
		var slots = prop.slots;
		rL = new(slots, typeof(Interaction))
		{
			drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Slots"),
			drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				if (index >= slots.Count) return;
				var i = slots[index];
				#region Interaction

				#region 공통 속성
				//TODO rect로 layout 함수 변경
				EditorGUI.LabelField(SetArea(ref rect), $"{index}번 슬롯 : [{i.type}] {i.name}", EditorStyles.boldLabel);
				EditorGUI.LabelField(SetArea(ref rect), "기본 속성", EditorStyles.boldLabel);
				BeginArea(rect); // Horizontal
				i.sprite = EditorGUI.ObjectField(SetArea(ref rect, 100, 100, false), i.sprite, typeof(Sprite), false) as Sprite;
				BeginArea(rect); // Vertical
				HorizontalField(ref rect, "이름 :", 40, () => i.name = EditorGUI.TextField(SetArea(ref rect), i.name));
				EditorStyles.textField.wordWrap = true;
				HorizontalField(ref rect, "설명: ", 40, () => i.tooltip = EditorGUI.TextArea(SetArea(ref rect, 3 * LINE_HEIGHT), i.tooltip));
				HorizontalField(ref rect, "레벨: ", 40, () => i.level = EditorGUI.IntSlider(SetArea(ref rect), i.level, -1, 60));
				EndArea(ref rect);
				EndArea(ref rect);
				#endregion

				EditorGUI.LabelField(SetArea(ref rect), "세부 속성", EditorStyles.boldLabel);
				rect.x += INDENT; rect.width -= INDENT;
				HorizontalField(ref rect, "이 상호작용은", 85, () => i.type = (Interaction.Type)EditorGUI.EnumPopup(SetArea(ref rect, LINE_HEIGHT, 100, false), i.type), "유형입니다.");
				switch (i.type)
				{
					case Interaction.Type.Action:
						HorizontalField(ref rect, "애니메이션", 85, () => i.animation = EditorGUI.ObjectField(SetArea(ref rect), i.animation, typeof(AnimationClip), false) as AnimationClip);
						HorizontalField(ref rect, "지속시간(초)", 85, () => i.duration = EditorGUI.FloatField(SetArea(ref rect), i.duration));
						break;
					case Interaction.Type.Task:
						HorizontalField(ref rect, "대기시간(초)", 85, () => i.duration = EditorGUI.FloatField(SetArea(ref rect), i.duration));
						break;
					case Interaction.Type.Special:
						HorizontalField(ref rect, "특수 유형", 85, () => i.specialType = (Interaction.SpecialType)EditorGUI.EnumPopup(SetArea(ref rect, width: 100), i.specialType));
						EditorGUI.BeginDisabledGroup(true);
						rect.x += INDENT; rect.width -= INDENT;
						HorizontalField(ref rect, "고유 색상", 70, () => EditorGUI.ColorField(SetArea(ref rect), i.specialType switch
						{
							Interaction.SpecialType.전투 => Color.red,
							Interaction.SpecialType.건설 => Color.yellow,
							_ => Color.white
						}));
						HorizontalField(ref rect, "고유 이미지", 70, () => EditorGUI.ObjectField(SetArea(ref rect, 60, 120), i.SpecialSprite, typeof(Sprite), false));
						rect.x -= INDENT; rect.width += INDENT;
						EditorGUI.EndDisabledGroup();
						break;
				}
				rect.x -= INDENT; rect.width += INDENT;
				//? UnityEvent 객체는 리스트 가급적 제일 마지막에 둘 것. (rect 크기가 가변적이기 때문)
				EditorGUI.LabelField(SetArea(ref rect), "이벤트", EditorStyles.boldLabel);
				SerializedProperty trigger = GetEventProperty(index, EventType.Trigger),
					canceller = GetEventProperty(index, EventType.Cancel)
					condition = GetEventProperty(index, EventType.Condition);
				EditorGUI.PropertyField(rect, trigger, new GUIContent($"기본 수행 작업"));
				rect.y += EditorGUI.GetPropertyHeight(trigger);
				if (i.cancelable = EditorGUI.Toggle(SetArea(ref rect), "상호작용 취소 가능", i.cancelable))
				{
					EditorGUI.PropertyField(rect, canceller, new GUIContent($"상호작용 취소 시 수행할 작업"));
					rect.y += EditorGUI.GetPropertyHeight(canceller);
				}
				if (i.useCondition = !EditorGUI.Toggle(SetArea(ref rect), "슬롯 항상 표시", !i.useCondition))
				{
					EditorGUI.PropertyField(rect, condition, new GUIContent($"{index}번 슬롯이 표시될 조건"));
					rect.y += EditorGUI.GetPropertyHeight(condition);
					EditorGUI.HelpBox(SetArea(ref rect), "슬롯이 표시되는 조건은 아직 지원하지 않습니다.", MessageType.Warning);
				}

				serializedObject.ApplyModifiedProperties();
				#endregion
			},
			elementHeightCallback = (index) =>
			{
				var i = slots[index];
				float height = i.type switch
				{
					Interaction.Type.Task => LINE_HEIGHT * 13,
					Interaction.Type.Action => LINE_HEIGHT * 14,
					Interaction.Type.Special => LINE_HEIGHT * 17,
					_ => 0
				};
				height += EditorGUI.GetPropertyHeight(GetEventProperty(index, EventType.Trigger));
				height += i.cancelable ? EditorGUI.GetPropertyHeight(GetEventProperty(index, EventType.Cancel)) : 0;
				height += i.useCondition ? EditorGUI.GetPropertyHeight(GetEventProperty(index, EventType.Condition)) LINE_HEIGHT : 0;
				return height;
			},
			onAddCallback = (ReorderableList list) =>
			{
				slots.Add(CreateInstance<Interaction>());
				prop.triggers.Add(default);
				prop.cancellers.Add(default);
				// prop.conditions.Add(default);
			},
			onRemoveCallback = (ReorderableList list) =>
			{
				if (slots.Count > list.index) slots.RemoveAt(list.index);
				if (prop.triggers.Count > list.index) prop.triggers.RemoveAt(list.index);
				if (prop.cancellers.Count > list.index) prop.cancellers.RemoveAt(list.index);
				// if (prop.conditions.Count > list.index) prop.conditions.RemoveAt(list.index);
			}
		};
	}

	public override void OnInspectorGUI()
	{
		var prop = target as Prop;
		SerializedProperty name = serializedObject.FindProperty("propName"),
			level = serializedObject.FindProperty("level"),
			uDI = serializedObject.FindProperty("useDefaultInteractions"),
			uDt = serializedObject.FindProperty("useDestroy"),
			uRp = serializedObject.FindProperty("useRepair"),
			uDm = serializedObject.FindProperty("useDismantle");
		HorizontalFieldLayout("이름", 80, () => name.stringValue = EditorGUILayout.TextField(name.stringValue, GUILayout.Height(LINE_HEIGHT)));
		HorizontalFieldLayout("레벨", 80, () => level.intValue = EditorGUILayout.IntSlider(level.intValue, -1, 60));
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField(new GUIContent("기본 상호작용 추가", "공통적으로 사용되는 기본 슬롯을 추가합니다.\n기능은 같으나 다른 설정을 원할 경우 해당 항목을 체크 해제하고 수동으로 추가하십시오."), GUILayout.Width(120));
		uDI.boolValue = EditorGUILayout.Toggle(uDI.boolValue);
		EditorGUILayout.EndHorizontal();
		if (uDI.boolValue)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.BeginHorizontal();
			uDt.boolValue = EditorGUILayout.Toggle(uDt.boolValue, GUILayout.Width(25));
			EditorGUILayout.LabelField("파괴", GUILayout.Width(40));
			uRp.boolValue = EditorGUILayout.Toggle(uRp.boolValue, GUILayout.Width(25));
			EditorGUILayout.LabelField("수리", GUILayout.Width(40));
			uDm.boolValue = EditorGUILayout.Toggle(uDm.boolValue, GUILayout.Width(25));
			EditorGUILayout.LabelField("철거", GUILayout.Width(40));
			EditorGUILayout.EndHorizontal();
			EditorGUI.indentLevel--;
		}
		#region Interaction Slots
		EditorGUILayout.LabelField("상호작용 슬롯 설정", EditorStyles.boldLabel);
		var slots = prop.slots;
		var events = prop.triggers;
		if (GUILayout.Button(new GUIContent("Clear Slots")))
		{
			slots.Clear();
			events.Clear();
		}
		rL.DoLayoutList();
		#endregion

		if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
	}

	#region Helper Methods
	private void HorizontalFieldLayout(string label, float width, Action action, string label2 = "")
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField(label, GUILayout.Width(width));
		action.Invoke();
		if (label2.Length > 0) EditorGUILayout.LabelField(label2);
		EditorGUILayout.EndHorizontal();
	}
	private void HorizontalField(ref Rect rect, string label, float width, Action action, string label2 = "")
	{
		BeginArea(rect);
		EditorGUI.LabelField(SetArea(ref rect, LINE_HEIGHT, width, false), label);
		action.Invoke();
		if (label2.Length > 0) EditorGUI.LabelField(SetArea(ref rect, LINE_HEIGHT, width), label2);
		EndArea(ref rect);
	}
	private Rect SetArea(ref Rect rect, float height = LINE_HEIGHT, float width = int.MaxValue, bool vertical = true)
	{
		Rect area = new(rect.position, new(Mathf.Min(width, rect.width), height));
		if (vertical) { rect.y += height; }
		else { rect.x += width; rect.width -= width; }
		return area;
	}
	readonly Stack<Rect> stack = new();
	private void BeginArea(Rect rect)
	{
		stack.Push(new(rect.x, 0, rect.width, rect.height)); // remember x only
	}
	private void EndArea(ref Rect rect)
	{
		if (stack.TryPop(out var r) && r.y == 0) { rect.x = r.x; rect.width = r.width; }
		else Debug.LogError("BeginHorizontal() and EndHorizontal() must be called in pairs.");
	}
	private SerializedProperty GetEventProperty(int index, EventType type)
	{
		string name = type switch
		{
			EventType.Trigger => nameof(Prop.triggers),
			EventType.Cancel => nameof(Prop.cancellers),
			// EventType.Condition => nameof(Prop.conditions),
			_ => throw new ArgumentException("Invalid EventType")
		};
		return serializedObject.FindProperty(name).GetArrayElementAtIndex(index);
	}
	#endregion
	private enum EventType { Trigger, Cancel, Condition }
}
#endif
*/