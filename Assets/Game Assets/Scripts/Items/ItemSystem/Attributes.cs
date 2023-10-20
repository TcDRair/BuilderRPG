using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Rair.Events;
using UnityEngine.Assertions.Must;

namespace Rair.Items
{
	/// <summary>모든 아이템에 공통적으로 존재하는 속성 정보를 담습니다.</summary>
	public class Attributes
	{
		public int ID { get; init; } = -1;
		public Item Reference { get; init; }

		#region 생성자 / 초기화
		/// <summary>가능한 경우 속성 정보를 초기화합니다.</summary>
		public static implicit operator bool(Attributes ip)
		{
			if (ip is null) return false;
			if (ip.Initialized is false) { ip.Initialize(); ip.Initialized = true; }
			return true;
		}
		/// <summary>기본값으로 초기화된 새 속성을 생성합니다.</summary>
		public Attributes(Item item)
		{
			Reference = item;
			durability.OnValueChanged += this.DestroyItem;
		}
		/// <summary>여러 속성에서 병합 가능한 요소만을 합친 새 속성을 생성합니다.</summary>
		public void Merge(IEnumerable<Attributes> items)
		{
			if (!Mergable) throw new InvalidOperationException("병합할 수 없는 아이템입니다.");
			// 자동 병합이 가능한 속성 처리
			for (int i = 0; i < Bools.Length; i++)
				Bools[i].Merge(items.Select(s => s.Bools[i]));
			for (int i = 0; i < Ints.Length; i++)
				Ints[i].Merge(items.Select(s => s.Ints[i]));
			for (int i = 0; i < Floats.Length; i++)
				Floats[i].Merge(items.Select(s => s.Floats[i]));
			// 온도 : 열용량 기준 계산
			var temSum = items.Sum(s => s.property.mass * s.property.temperature);
			property.temperature.Value = temSum / property.mass;
			// 속성 정합성을 판단합니다.
			Initialize();
		}

		/// <summary>속성 정보를 초기화하고 충돌 정보를 표시합니다.</summary>
		public void Initialize()
		{
			//! DEBUG
			Debug.Log("초기화 완료");
		}
		#endregion

		#region 기본 - 모든 아이템이 가지고 있는 명시 속성
		public string Name { get; init; } = "이름 없음";
		/// <summary>속성 정보가 올바르게 초기화되었는지를 나타냅니다.</summary>
		public bool Initialized { get; private set; } = false;
		public bool Mergable { get; init; } = false;
		/// <summary>내구도 - 아이템이 파괴되기 전까지 사용할 수 있는 정도를 나타냅니다.</summary>
		public readonly EFloat durability = new(0);
		/// <summary>가공 가능 횟수 - Recipe 재료로 사용될 수 있는 한도를 지정합니다.</summary>
		public readonly EInt recipeCount = new(0, MergeType.Min);
		#endregion

		#region 특성 - 형태, 성질 등 아이템의 정보를 나타내는 속성
		public class Property
		{
			public readonly EFloat mass = new(1, MergeType.Sum);
			public readonly EFloat volume = new(1, MergeType.Sum);
			public readonly EFloat temperature = new(0, MergeType.None);
			public float Hardness { get; init; } = 1; // 경도(고체) : 1 ~ 
			public float Viscosity { get; init; } = 1; // 점도(액체) : 0 ~ 100,000
			public float SpecificHeat { get; init; } = .5f; // 비열 : 0 ~ 1
			public float HeatCapacity => mass * SpecificHeat; // 열용량 : 0 ~
			public float Conductivity { get; init; } = 0; // 열전도율 : 0 ~
			public float HeatTransfer { get; init; } = 0; // 열전달율 : 0 ~
			public Phase Phase { get; init; }
			public float Density => mass / volume;
		}
		public Property property = new();
		#endregion

		#region 분류 - 아이템 대분류 지정 속성
		public class Category
		{
			/// <summary>
			/// 건축물인지를 나타냅니다. 생성 시점에 지정해야 합니다.<br/>
			/// 장비 등 대부분의 분류보다 우선합니다.
			/// </summary>
			public readonly EBool IsStructure = new(false);
			/// <summary>무기로 장착 가능한 형태인가를 나타냅니다. 생성 시점에 지정해야 합니다.</summary>
			public readonly EBool IsWeapon = new(false);
			/// <summary>방어구로 장착 가능한 형태인가를 나타냅니다. 생성 시점에 지정해야 합니다.</summary>
			public readonly EBool IsArmor = new(false);
			/// <summary>식용 가능 여부를 나타냅니다.</summary>
			public readonly EBool Edible = new(false);
		}
		public Category category = new();
		#endregion

		#region 용도 - 아이템 하위 분류 지정 속성
		public class Usage
		{
			/// <summary>열량 - 섭취 시 획득 열량을 나타냅니다. 섭취 여부와 무관합니다.</summary>
			public readonly EFloat calorie = new(0, MergeType.Sum);
			/// <summary>공격력 - 무기 장착 가능 여부 장착 시 기본 공격력을 나타냅니다.</summary>
			public readonly EFloat attack = new(0);
			/// <summary>방어력 - 방어구 장착 가능 여부와 장착 시 기본 방어력을 나타냅니다.</summary>
			public readonly EFloat defense = new(0);
			/// <summary>공격 속도 - 기본 공격 속도를 나타냅니다.</summary>
			public readonly EFloat attackSpeed = new(1);
			/// <summary>작업 속도 - 도구 사용 가능 여부와 장착 시 기본 작업 속도를 나타냅니다.</summary>
			public readonly EFloat workSpeed = new(0);
		}
		public Usage usage = new();
		#endregion


		//* 선택 속성 - 재질(Material)

		#region Properties
		public EBool[] Bools => new EBool[] { category.IsStructure, category.IsWeapon, category.IsArmor, category.Edible };
		public EInt[] Ints => new EInt[] { recipeCount };
		public EFloat[] Floats => new EFloat[] { durability, property.mass, property.volume, usage.calorie, usage.attack, usage.defense, usage.attackSpeed, usage.workSpeed };
		#endregion
	}

	public enum Phase { Solid, Liquid, Gas }
}