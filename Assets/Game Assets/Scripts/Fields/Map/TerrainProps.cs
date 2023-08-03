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
  public struct Condition {
    public Assets.Maps.Biome biome;
    [Range(0, .5f)] public float scale;
    [Range(0, .5f)] public float density;
  }
  public Condition[] conditions;
}
}