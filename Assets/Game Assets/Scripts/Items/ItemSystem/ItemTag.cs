using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rair.Items
{
	public enum TagType { Requirement, Category, Event }
	public class ItemTag
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public int ID { get; set; }
		public TagType Type { get; set; }
		public Action<Item> OnAdded { get; set; }

		internal ItemTag() { }

		public override bool Equals(object obj)
			=> obj is ItemTag t && t.ID == ID;
		public override int GetHashCode()
			=> ID.GetHashCode();
	}

	public static partial class Tags
	{
		public static ItemTag _DBG = new()
		{
			Name = "DebugTag",
			ID = 0
		},
		#region Category
		Edible = new()
		{
			Name = "식용",
			ID = 1,
			Type = TagType.Category
		},
		Tool = new()
		{
			Name = "도구",
			ID = 2,
			Type = TagType.Category
		},

		#endregion


		__NULL1;
	}
}