using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;

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
    public readonly int size, scale;
    public Occupy[] Grid { get; private set; }
    public readonly RectInt bounds;
    
    public OccupyGrid(int originalSize, IEnumerable<Occupy> data, int scale = 1) {
      this.scale = scale;
      size = originalSize / scale;
      bounds = new(0, 0, size, size);
      if (scale == 1) Grid = data.ToArray();
      else Grid = Scale(data, scale);
    }

    private Occupy[] Scale(IEnumerable<Occupy> data, int scale) {
      int width = size * scale;
      var newGrids = new Occupy[size * size];
      for (int i = 0; i < size * size; i++) {
        RectInt rect = new(i % size * scale, i / size * scale, scale, scale);
        foreach (var p in rect.allPositionsWithin) newGrids[i] &= data.ElementAt(p.x + p.y * width);
      }
      return newGrids;
    }
    Vector3 basis;
    Vector2 worldScale;
    public void SetWorldPivot(Terrain terrain) {
      basis = terrain.transform.position; // offset
      basis.y = 0;
      worldScale = terrain.terrainData.size.XZ();
      Debug.Log($"[OccupyGrid] {basis} {worldScale}");
    }
    public Vector3 GetWorldPos(Vector2Int pos, float randomRatio = 0, float yOffset = 0) {
      var offsetPos = pos + Random.insideUnitCircle * randomRatio;
      return basis + (offsetPos * worldScale / size).XZToX0Z() + Vector3.up * yOffset;
    }
    public Occupy this[Vector3 worldPos] =>
      this[Vector2Int.FloorToInt((worldPos.XZ() - basis.XZ()) / worldScale * size)];
    public Occupy this[Vector2Int pos, bool unscaled = false] =>
      unscaled
        ? this[pos / scale, false]
      : bounds.Contains(pos)
        ? Grid[pos.x + pos.y * size]
        : Occupy.FULL;
  }
}