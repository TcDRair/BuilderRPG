using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditorInternal;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

namespace Rair.Field.Interact
{
	public abstract class Prop : MonoBehaviour
	{ // 상호작용 가능한 대상
		#region Properties shown in other scripts
		public int Level => level;
		public virtual bool Active => slots.Count() > 0;
		public virtual string Name => name;
		#endregion

		#region Properties editable in Inspector
		[SerializeField] protected new string name;
		[SerializeField, Range(-1, 60)] protected int level = -1;

		[HideInInspector] public List<Interaction> slots = new();
		[HideInInspector] public List<UnityEvent> events = new();
		#endregion

		#region Properties editable in derived script
		/// <summary>추가 UI를 표시할 필요가 있는지를 나타냅니다</summary>
		public virtual bool HasInfo => false;
		#endregion

		protected void OnEnable()
		{
			for (int i = 0; i < slots.Count; i++) slots[i].onSelected = events[i];
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(Prop), true)]
	public class PropEditor : Editor
	{
		const int LINE_HEIGHT = 20, INDENT = 15;
		ReorderableList rL;

		protected void OnEnable()
		{
			var slots = (target as Prop).slots;
			var events = (target as Prop).events;
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
								Interaction.SpecialType.Battle => Color.red,
								Interaction.SpecialType.Construct => Color.yellow,
								_ => Color.white
							}));
							HorizontalField(ref rect, "고유 이미지", 70, () => EditorGUI.ObjectField(SetArea(ref rect, 60, 120), i.SpecialSprite, typeof(Sprite), false));
							rect.x -= INDENT; rect.width += INDENT;
							EditorGUI.EndDisabledGroup();
							break;
					}
					rect.x -= INDENT; rect.width += INDENT;
					//? UnityEvent 객체는 리스트 제일 마지막에 둘 것. (rect 크기가 가변적이기 때문)
					EditorGUI.PropertyField(rect, GetEventProperty(index), new GUIContent($"{index}번 슬롯 상호작용 시 수행할 작업"));
					serializedObject.ApplyModifiedProperties();
					#endregion
				},
				elementHeightCallback = (index) => slots[index].type switch
				{
					Interaction.Type.Task => LINE_HEIGHT * 10,
					Interaction.Type.Action => LINE_HEIGHT * 11,
					Interaction.Type.Special => LINE_HEIGHT * 14,
					_ => 0
				} + EditorGUI.GetPropertyHeight(GetEventProperty(index)),
				onAddCallback = (ReorderableList list) =>
				{
					slots.Add(CreateInstance<Interaction>());
					events.Add(new());
				},
				onRemoveCallback = (ReorderableList list) =>
				{
					if (slots.Count > list.index) slots.RemoveAt(list.index);
					if (events.Count > list.index) events.RemoveAt(list.index);
				}
			};
		}

		public override void OnInspectorGUI()
		{
			var prop = target as Prop;
			base.OnInspectorGUI();
			serializedObject.Update();

			#region Interaction Slots
			var slots = prop.slots;
			var events = prop.events;
			if (GUILayout.Button(new GUIContent("Clear Slots")))
			{
				slots.Clear();
				events.Clear();
			}
			rL.DoLayoutList();
			#endregion
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
		private SerializedProperty GetEventProperty(int index)
			=> serializedObject.FindProperty(nameof(Prop.events)).GetArrayElementAtIndex(index);
		#endregion
	}
#endif
}