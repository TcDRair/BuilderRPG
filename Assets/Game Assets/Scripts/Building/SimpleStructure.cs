using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별도의 상호작용이 없는 단순한 구조물 프리팹을 위한 클래스입니다.<br/>
/// 이 건물은 모든 건물이 가지는 기본 상호작용만 가능합니다.
/// </summary>
public class SimpleStructure : MonoBehaviour, IBuildingObject
{
    [SerializeField]
    Building building;
    public Building obj => building;
}