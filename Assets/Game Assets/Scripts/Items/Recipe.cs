


namespace Rair.Items
{
	/// <summary>
	/// Item 객체를 조합하고 결과물을 반환하는 기능을 가지는 클래스입니다.
	/// </summary>
	public abstract class Recipe
	{
		/// <summary>레시피의 사용 가능 여부를 나타냅니다.</summary>
		public enum UsableType
		{
			/// <summary>레시피를 사용할 수 있습니다.</summary>
			Valid,
			/// <summary>아이템 수량이 부족합니다.</summary>
			InsufficientItem,
			/// <summary>아이템이 조건을 만족하지 않습니다.</summary>
			InappropriateItem,
			/// <summary>플레이어가 레시피 사용 조건을 만족하지 못했습니다.</summary>
			InvalidPlayer,
		}

		/// <summary>레시피를 이용할 조건이 갖추어졌는지 확인합니다.</summary>
		public abstract UsableType CanUseRecipe(Field.Player player, Item[] items);
		/// <summary>레시피를 가동하고 결과물을 반환합니다.</summary>
		public abstract Item[] RunRecipe(Item[] items);

	}
}