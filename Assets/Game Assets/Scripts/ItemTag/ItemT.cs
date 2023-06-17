using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ItemT
{
    public string name, description;

    public Sprite sprite;

    /// <summary>모든 아이템에 공통적으로 존재하는 속성 정보를 담습니다.</summary>
    [Serializable]
    public class Attribute {
        //* 특수 속성 - 다른 속성과 독립적으로 작동
        /// <summary>
        /// 구조물 - 월드 맵에 배치 및 건축이 가능한 건축물임을 나타냅니다.<br/>
        /// 대부분의 속성보다 우선순위가 높으며 일반적인 아이템과는 달리 건축물 정보 UI를 가질 예정입니다.
        /// </summary>
        public bool structure = false;

        //* 필수 속성
        /// <summary>내구성 - 아이템이 파괴되기 전까지 사용할 수 있는 정도를 나타냅니다.</summary>
        public float durability;

        /// <summary>가공 가능 횟수 - Recipe 재료로 사용될 수 있는 한도를 지정합니다.</summary>
        public int recipeCount;

        //* 선택 속성 - 용도(Usage)
        /// <summary>열량 - 섭취 가능 여부와 섭취 시 획득 열량을 나타냅니다.</summary>
        public float calorie = 0f;
        /// <summary>공격력 - 무기 장착 가능 여부 장착 시 기본 공격력을 나타냅니다.</summary>
        public float attack { get => 0f; } //TODO 적용 공식을 정의해야 합니다.
        /// <summary>방어력 - 방어구 장착 가능 여부와 장착 시 기본 방어력을 나타냅니다.</summary>
        public float defense { get => 0f; } //TODO 적용 공식을 정의해야 합니다.
        /// <summary>공격 속도 - 기본 공격 속도를 나타냅니다.</summary>
        public float attackSpeed { get => 0.5f/weight; }
        /// <summary>작업 속도 - 도구 사용 가능 여부와 장착 시 기본 작업 속도를 나타냅니다.</summary>
        public float workSpeed { get; } //TODO 적용 공식을 정의해야 합니다.

        //* 선택 속성 - 형태(Shape) 및 성질(Property)
        /// <summary>크기 - 실제 부피나 면적을 나타냅니다. 물 1L의 부피(1000cm³)를 1로 간주합니다.</summary>
        public float size = 1f;
        /// <summary>밀도 - 실제 밀도를 나타냅니다. 물의 밀도(1g/cm³)를 1로 간주합니다.</summary>
        public float density = 1f;
        public float weight { get => size*density; }
        //* 선택 속성 - 재질(Material)
    } public Attribute attribute;
    /// <summary>조건에 따라 아이템에 붙는 특성 정보를 담습니다.</summary>
    [Serializable]
    public class Property {

    } public Property property;
}



//TODO 클래스 구성이 대강 완성되면 스크립트 파일을 분리해야 합니다.
/// <summary>
/// Item 객체를 조합하고 결과물을 반환하는 기능을 가지는 클래스입니다.
/// </summary>
public abstract class Recipe
{
    /// <summary>레시피의 사용 가능 여부를 나타냅니다.</summary>
    public enum UsableType {
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
    public abstract UsableType CanUseRecipe(Player player, ItemT[] items);
    /// <summary>레시피를 가동하고 결과물을 반환합니다.</summary>
    public abstract ItemT[] RunRecipe(ItemT[] items);
    
}
