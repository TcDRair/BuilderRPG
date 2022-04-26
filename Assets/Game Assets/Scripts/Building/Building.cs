using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// using UnityEditor;

[Serializable]
/// <summary>
/// 건축물 프리팹이 가지고 있는 클래스로, 건축물에 대한 정보를 저장합니다.<br/>
/// 상호작용 등 건축물 내부 메서드는 자체 스크립트에서 구현합니다.
/// </summary>
public class Building
{
    public BuildingInfo info;
    /// <summary>건설 진행 중에 사용되는 건축 모델 배열입니다. 맨 마지막을 건설 완료 모델로 간주합니다.</summary>
    [Tooltip("건설 중일 때 순서대로 표시되는 건축 모델링들을 저장합니다. 가장 마지막 모델이 건설 완료 모델입니다.")]
    public Transform[] constructings;

    /*// 리소스 파일 경로
    internal const string prefabPath = "Assets/Game Assets/Resources/Prefabs/Buildings/";
    internal const string ResourcePath = "Prefabs/Buildings/";*/

    // 건설 중 사용되는 정보.
    /// <summary>실제로 경과한 건설 시간을 의미합니다. 건설 속도 증감에 영향을 받으며 <see cref="BuildingInfo.buildTime"/>이 최대치입니다.</summary>
    float _buildProgress = 0f;
    /// <summary>건설 이전을 0, 건설 완료를 1로 잡았을 때 현재 건설 진행도 비율을 의미합니다.</summary>
    public float buildProgress { get => _buildProgress/info.buildTime; }
    
    /// <summary>
    /// 플레이어가 이 건축물을 건설하고 있는 프레임마다 호출하여 건설 진행 상황을 업데이트합니다.<br/>
    /// 진행도에 비례하여 건축물 모델이 변화할 수 있으며, 시간이 충분히 누적되면 완성 모델을 보여줍니다.<br/>
    /// 처음 모델 변화 전까지는 프리뷰 모델을 보여줍니다.
    /// </summary>
    /// <param name="elapsedTime">
    /// 이전 호출로부터 경과한 시간을 나타냅니다. <see cref="Time.deltaTime"/>을 기본값으로 사용합니다.
    /// <para>건설 속도 증감과 같은 시스템이 존재할 경우 해당 시간에 숙련도로 인한 속도 변화를 적용한 뒤 전달하세요.</para>
    /// </param>
    /// <returns>건설이 최종 완료되면 <see langword="true"/>를 반환합니다.</returns>
    public bool ShowConstructingModel(float? elapsedTime = null) {
        Mathf.Clamp(_buildProgress += (elapsedTime ?? Time.deltaTime), 0f, info.buildTime);
        int index = (int)(buildProgress * (constructings.Length-1));
        for (int i = 0; i < constructings.Length; i++) {
            constructings[i].gameObject.SetActive(i == index);
        }
        return _buildProgress >= info.buildTime;
    }
}


[Serializable]
public class BuildingInfo {
    public string name;
    [TextArea]
    public string description;
    /// <summary>건축물의 범주입니다. 최대 세 개를 가질 수 있으며, 첫 번째 항목을 메인 그룹으로 간주합니다.</summary>
    public Group[] group;
    public enum Group {
        /// <summary>이동 속도에 영향을 주는 건물</summary>
        Road,
        /// <summary>아이템 저장이 가능한 건물</summary>
        Chest,
        /// <summary>실내 판정을 받는 건물</summary>
        Indoor,
        /// <summary>휴식할 수 있는 건물</summary>
        Shelter,
        /// <summary>작업 기능이 있는 건물</summary>
        Workbench,
        /// <summary>특별한 기능이 없는 단순 장식 건물</summary>
        Deco,
        /// <summary>주변에 열을 제공하는 건물</summary>
        HeatSource,
    }
    public ItemT[] material;
    public const InteractTargetType interactTargetType = InteractTargetType.Building;
    public InteractType[] interactTypes = new InteractType[] { InteractType.Build, InteractType.BuildCancel };

    public BuildableGrid grid;
    [Tooltip("건축물의 미리보기 이미지입니다. 건설 선택 화면에서 표시됩니다.")]
    /// <summary>건축물의 미리보기 이미지입니다.</summary>
    public Sprite sprite;
    [Tooltip("건축물의 미리보기 모델입니다.")]
    /// <summary>건축물의 미리보기 모델입니다.</summary>
    public GameObject preview;
    public Vector2Int size = new Vector2Int(1, 1);
    [Tooltip("건설에 걸리는 시간입니다. 초 단위로 입력합니다.")]
    /// <summary>건설에 걸리는 시간입니다. 초 단위로 입력합니다.</summary>
    public float buildTime = 30f;
    public int width { get => size.x; }
    public int length { get => size.y; }
}

public interface IBuildingObject {
    /// <summary>게임오브젝트에 들어있는 Building 오브젝트에 접근합니다.</summary>
    Building obj { get; }
}


[Flags]
public enum Buildable : ushort {
    None  = 0,
    Floor    = 0b_0000_0000_0000_0001, // 바닥
    Indoor   = 0b_0000_0000_0000_0010, // 실내 건축물. 내부 건축이 불가능할 경우 활성화하여 판정을 막아야 함.
    Ceiling  = 0b_0000_0000_0000_0100, // 천장. 벽 플래그 필요
    Attach_C = 0b_0000_0000_0000_1000, // 천장 부착물
    Wall_N   = 0b_0000_0000_0001_0000, // 벽
    Wall_E   = 0b_0000_0000_0010_0000,
    Wall_S   = 0b_0000_0000_0100_0000,
    Wall_W   = 0b_0000_0000_1000_0000,
    Attach_N = 0b_0000_0001_0000_0000, // 벽 부착물
    Attach_E = 0b_0000_0010_0000_0000,
    Attach_S = 0b_0000_0100_0000_0000,
    Attach_W = 0b_0000_1000_0000_0000,
    IsInside = 0b_0001_0000_0000_0000, // 건물 내부 판정 플래그. //! 삭제될 가능성 높음
    IsFalseWall = 0b_0010_0000_0000_0000, // 실제 벽이 아님을 나타내는 플래그. 실제 벽 건설 시 지워져야 한다.
    // 두 Buildable에서 IsFalseWall & IsFalseWall인 결과물이 최종 플래그에 남게 처리하면 되겠다.
    UnderConstruction = 0b_0100_0000_0000_0000, // 건설 중 플래그. 건설 완료 시 Building 스크립트에서 지울 것.
    Unbuildable = 0b_1000_0000_0000_0000, //! Building 인스턴스가 가지면 안 됨.

    // 합성 플래그
    FullStruct = 0b_0000_1111_1111_1111,
    FullFrame  = 0b_0000_0000_1111_0111, // 바닥 + 벽 + 천장 + 내부가 가득 참
    Attach     = 0b_0000_1111_0000_1000, // 부착물
    Inside     = 0b_0000_1111_0000_1010, // 내부. 부착물과 실내 구조물은 중첩할 수 없다.
    Wall       = 0b_0000_0000_1111_0000,
    FilledWall = Wall | Attach,

    FilledWall_N = Wall_N | Attach_N,
    FilledWall_E = Wall_E | Attach_E,
    FilledWall_S = Wall_S | Attach_S,
    FilledWall_W = Wall_W | Attach_W,
}