using UnityEngine;
using UnityEngine.UI;

namespace Rair.Field.Maps
{
  /// <summary>맵 생성이 읽고 쓰는 씬 참조들입니다.</summary>
  /// <remarks>
  /// <b>씬에 직렬화되므로 런타임 어셈블리에 있어야 합니다.</b>
  /// 에디터 전용 어셈블리로 옮기면 플레이 모드를 오갈 때 값이 사라집니다.
  /// 자세한 경위는 <see cref="RandomTextureGenerator"/> 주석 참조.
  /// </remarks>
  [System.Serializable]
  public struct MapVar {
    [InspectorName("Island Map")] public Image image;
    public Sprite MapSprite, MapDataSprite;
    public Material mapMaterial;
    public Terrain MapTerrain;
  }

  /// <summary>지형 생성 설정입니다.</summary>
  /// <inheritdoc cref="MapVar"/>
  [System.Serializable]
  public struct TerrainVar {
    public Transform propParent;
    public TerrainPropData data;
    public SmoothVar smoothenBorder, smoothenBiomes;

    /// <summary>지형 전체 높이(월드 유닛). 0이면 <see cref="DEFAULT_TOTAL_HEIGHT"/>를 씁니다.</summary>
    /// <remarks>
    /// 고도가 <c>[-1,1] → [0,1]</c>로 매핑되므로 <b>해수면이 정확히 이 값의 절반</b>이고,
    /// 육지가 쓸 수 있는 기복은 나머지 절반뿐입니다.
    /// <para>
    /// 맵 가로 폭에 비해 이 값이 작으면 지형이 평면처럼 보입니다.
    /// 이전 값 10은 512 폭 맵에서 육지 기복이 5유닛이라 눈으로 평면과 구분되지 않았습니다.
    /// </para>
    /// 눈으로 보면서 정해야 하는 값이라 상수 대신 인스펙터로 노출했습니다.
    /// </remarks>
    [Range(0, 256)] public int totalHeight;

    public const int DEFAULT_TOTAL_HEIGHT = 96;

    /// <summary>실제로 적용할 지형 높이입니다. 미설정(0) 시 기본값으로 대체합니다.</summary>
    public int TotalHeight => totalHeight > 0 ? totalHeight : DEFAULT_TOTAL_HEIGHT;

    [System.Serializable]
    public struct SmoothVar {
      public bool active;
      [Range(0, .2f)] public float randomize;
      [Range(1, 5)] public int range;
      [Range(1, 3)] public int iterations;
    }
  }
}
