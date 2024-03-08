using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Rair.Items
{
	[Serializable]
	public class Item/* : ISerializable*/
	{
		public string name, description;

		public Sprite sprite;
		public Properties prop;
		// 값(Value)은 조정될 수 있으나 이벤트 함수는 자체 생성하지 않음
		public Attributes attr;
		// ID/Level만 보유하며 실제 구현은 외부 스크립트에서 이루어짐
		public List<ItemTag> tags;

		public Item(string name)
		{
			this.name = name;
			attr = new(this);
		}

		//DEBUG
		public override string ToString() => $"{name}[{(attr.category.IsWeapon ? "무기" : "기타")}] : ATK {attr.usage.attack}";
		public static Item GetItem(int id) => new("DEBUG");
	}
}
