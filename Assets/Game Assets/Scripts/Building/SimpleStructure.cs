using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별도의 상호작용이 없는 단순한 구조물 프리팹을 위한 클래스입니다.<br/>
/// 이 건물은 모든 건물이 가지는 기본 상호작용만 가능합니다.
/// </summary>
public class SimpleStructure : BuildingMono
{
  public override bool Interactable => true; // 장식 건물은 항상 상호작용이 가능합니다.
  public override InteractSlot[] Slots => building.state switch {
    Building.Progress.NeedMaterials => new[] { building.DefaultBuildingInfo, building.DefaultFillMaterials, building.DefaultCancelBuild },
    Building.Progress.Constructing => new[] { building.DefaultBuildingInfo, building.DefaultBuild, building.DefaultCancelBuild },
    Building.Progress.Complete => new[] { building.DefaultBuildingInfo, building.DefaultDestroy },
    _ => new InteractSlot[0]
  };
}