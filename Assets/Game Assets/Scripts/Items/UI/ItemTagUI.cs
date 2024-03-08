using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rair.Items
{
	public class ItemTagUI : MonoBehaviour
	{
		[SerializeField] protected Text text;
		[SerializeField] protected CanvasGroup group;
		ItemTag itemTag;
		RectTransform rect, desc;
		float width = 0, groupHeight;
		int toggle = -1;

		public void Set(ItemTag tag)
		{
			itemTag = tag;
			text.text = tag.Name;
		}

		public void OnClicked() => toggle = (toggle < 0) ? 1 : -1;

		protected void Awake()
		{
			rect = GetComponent<RectTransform>();
			desc = group.GetComponent<RectTransform>();
			groupHeight = desc.localPosition.y;
		}
		protected void Update()
		{
			if (width != text.preferredWidth)
			{
				width = text.preferredWidth;
				rect.sizeDelta = new Vector2(width + 40, rect.sizeDelta.y);
			}
			float alpha = group.alpha + toggle * .1f;
			float height = groupHeight + toggle * 5 * (alpha - 1);
			group.alpha = alpha;
			var p = desc.localPosition;
			desc.localPosition = new Vector3(p.x, height, p.z);
		}
	}
}
