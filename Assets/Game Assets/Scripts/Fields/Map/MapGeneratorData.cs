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

    [System.Serializable]
    public struct SmoothVar {
      public bool active;
      [Range(0, .2f)] public float randomize;
      [Range(1, 5)] public int range;
      [Range(1, 3)] public int iterations;
    }
  }
}
