using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;

namespace Rair.Field.Values
{
  [System.Flags]
  public enum Occupy {
    None = 0,
    Floor = 1 << 0,
    Wall = 1 << 1,
    Ceiling = 1 << 2,
    Furniture = 1 << 3,
    Light = 1 << 4,


    FULL = int.MaxValue //? 1 << 32 - 1
  }
  
  public class GridMap {
    public readonly int size, scale;
    public Occupy[] Grids { get; private set; }
    
    public GridMap(int size, IEnumerable<bool> walkable, int scale = 1) {
      this.size = size;
      this.scale = scale;
      if (scale > 1) Grids = Scale(walkable, scale);
      else Grids = walkable.Select(w => w ? Occupy.Floor : Occupy.Wall).ToArray();
    }

    private Occupy[] Scale(IEnumerable<bool> walkable, int scale) {
      int width = size * scale; //! assume walkable's width is multiple of Size.x
      var newGrids = new Occupy[size * size];
      for (int i = 0; i < size * size; i++) {
        RectInt rect = new(i % size * scale, i / size * scale, scale, scale);
        foreach (var p in rect.allPositionsWithin) if (!walkable.ElementAt(p.x + p.y * width)) {
          newGrids[i] = Occupy.FULL;
          break;
        }
        if (newGrids[i] != Occupy.FULL) newGrids[i] = Occupy.None;
      }
      return newGrids;
    }
  }
}