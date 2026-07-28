using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Rair.Field.Values
{
  [System.Flags]
  public enum Occupy : short {
    None = 0,
    Floor = 1 << 0,
    Ceiling = 1 << 1,
    WallN = 1 << 2,
    WallE = 1 << 3,
    WallS = 1 << 4,
    WallW = 1 << 5,
    Inside = 1 << 6,
    Other = 1 << 7,
    FULL = byte.MaxValue //? 1 << 8 - 1
  }
  
  public class OccupyGrid {
    public int Size { get; private set; }
    public int Scale { get; private set; }
    public Terrain Terrain { get; private set; }
    public Occupy[] Grid { get; private set; }
    public readonly RectInt bounds;
    public int TerrainScale => Terrain.terrainData.heightmapResolution / Size; //! Need to be checked
    
    public OccupyGrid(Terrain terrain, int originalSize, IEnumerable<Occupy> data, int scale = 1) {
      Terrain = terrain;
      Scale = scale;
      Size = originalSize / scale;
      bounds = new(0, 0, Size, Size);
      if (scale == 1) Grid = data.ToArray();
      else Grid = ApplyScale(data, scale);
      SetWorldPivot(terrain);
    }
    private void SetWorldPivot(Terrain terrain) {
      basis = terrain.transform.position; // offset
      worldScale = terrain.terrainData.size.XZ();
    }

    private Occupy[] ApplyScale(IEnumerable<Occupy> data, int scale) {
      int width = Size * scale;
      var newGrids = new Occupy[Size * Size];
      for (int i = 0; i < Size * Size; i++) {
        RectInt rect = new(i % Size * scale, i / Size * scale, scale, scale);
        //? 축소 셀은 포함된 원본 셀들의 점유 플래그를 모두 합집합으로 가진다.
        //? (하나라도 막혀 있으면 막힌 것으로 본다. 범위 밖을 FULL로 취급하는 것과 같은 보수적 기준)
        foreach (var p in rect.allPositionsWithin) newGrids[i] |= data.ElementAt(p.x + p.y * width);
      }
      return newGrids;
    }
    Vector3 basis;
    Vector2 worldScale;
    #region Get Methods
    public Vector2Int GetGridPos(Vector3 pos) {
      var gridPos = Vector2Int.FloorToInt((pos.XZ() - basis.XZ()) / worldScale * Size);
      return bounds.Contains(gridPos) ? gridPos : Vector2Int.zero;
    }
    public Vector3 GetWorldPos(Vector2 pos, float randomRange = 0) {
      var offsetPos = pos + Random.insideUnitCircle * randomRange;
      return basis + (offsetPos * worldScale / Size).XZToX0Z();
    }
    public Rect GetWorldArea(Vector2Int pos) {
      var worldPos = GetWorldPos(pos);
      return new(worldPos, worldScale.XZToX0Z());
    }
    public Rect GetWorldArea(RectInt rect) {
      var worldPos = GetWorldPos(rect.min);
      return new(worldPos, rect.size * worldScale);
    }
    public float GetWorldHeight(Vector2 pos, bool relative = true)
      => relative
        ? Terrain.SampleHeight(GetWorldPos(pos + basis.XZ())) + basis.y
        : Terrain.SampleHeight(GetWorldPos(pos)) + basis.y;
    #endregion

    public Occupy this[Vector3 worldPos] =>
      this[Vector2Int.FloorToInt((worldPos.XZ() - basis.XZ()) / worldScale * Size)];
    public Occupy this[Vector2Int pos, bool unscaled = false] =>
      unscaled
        ? this[pos / Scale, false]
      : bounds.Contains(pos)
        ? Grid[pos.x + pos.y * Size]
        : Occupy.FULL;
  }
}
