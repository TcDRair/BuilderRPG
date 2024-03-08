using System;
using UnityEngine;

namespace Rair.Field.Maps
{
[CreateAssetMenu(fileName = "TPData", menuName = "ScriptableObjects/TerrainPropData", order = 1)]
public class TerrainPropData : ScriptableObject {
  public Prop[] props;
}

[Serializable]
public struct Prop {
  public string name;
  public GameObject[] prefabs;
  [Serializable]
  public class Condition {
    //? Unity 인스펙터에서 바이옴 이름이 적절한 위치에 보이기 위해 사용
    [InspectorName("바이옴")] public Assets.Maps.Biome biome;
    [Range(0, .25f)] public float scale;
    [Range(0, .2f)] public float density;
  }
  [EnumAsElementName(nameof(Condition.biome))] public Condition[] conditions;
}
}
