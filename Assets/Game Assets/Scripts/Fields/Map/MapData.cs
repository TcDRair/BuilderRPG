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
    public Occupy[] Grids { get; private set; }
    public readonly RectInt bounds;
    
    public OccupyGrid(int originalSize, IEnumerable<Occupy> data, int scale = 1) {
      size = originalSize / scale;
      bounds = new(0, 0, size, size);
      this.scale = scale;
      if (scale > 1) Grids = Scale(data, scale);
      else Grids = data.ToArray();
    }

    private Occupy[] Scale(IEnumerable<Occupy> data, int scale) {
      int width = size * scale;
      var newGrids = new Occupy[size * size];
      for (int i = 0; i < size * size; i++) {
        RectInt rect = new(i % size * scale, i / size * scale, scale, scale);
        foreach (var p in rect.allPositionsWithin) newGrids[i] &= data.ElementAt(p.x + p.y * width);
        if (newGrids[i] != Occupy.FULL) newGrids[i] = Occupy.None;
      }
      return newGrids;
    }
    Vector3 basis, worldScale;
    public void SetWorldPivot(Terrain terrain) {
      basis = terrain.transform.position; // offset
      worldScale = terrain.terrainData.size;
    }

    public Occupy this[Vector3 worldPos] { get {
      Vector2Int p = Vector2Int.RoundToInt((worldPos.XZ() - basis.XZ()) / worldScale.XZ() * size);
      return this[p];
    }}
    public Occupy this[Vector2Int pos] { get {
      if (bounds.Contains(pos)) return Grids[pos.x + pos.y * size];
      else return Occupy.FULL;
    }}
  }
}