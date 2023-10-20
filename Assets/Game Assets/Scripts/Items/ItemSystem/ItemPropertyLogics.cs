using System;
using System.Collections;
using UnityEngine;

using Rair.Events;
namespace Rair.Items
{
	public static class ItemPropertyLogics
	{
		public static void DestroyItem(this Attributes p, float _, float dur)
		{
			if (dur <= 0) p.Reference.name = "파괴된 " + p.Reference.name;
		}
	}
}