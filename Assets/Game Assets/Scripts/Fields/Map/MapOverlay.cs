using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Maps;
using UnityEditor;

namespace Rair.Field.MapASDF {
  public class MapOverlay : MonoBehaviour
  {
    public RandomTextureGenerator generator;
    public Player player;

    public GameObject Blue, Red;

    Transform parent;
    protected void Awake() { parent = transform; }

    Values.GridMap grid;
    RectInt bounds;

    protected void Start() { InitGrid(); }
    public void InitGrid() {
      var h = Resources.Load<Texture2D>("Sprites/Map/heightMoisture");
      int size = h.width - 1; //? Heightmap size is 513x513
      grid = new(size / 4, h.GetPixels(0, 0, size, size).Select(p => p.r > 0), 4);
      bounds = new RectInt(0, 0, size/4, size/4);
    }
    protected void Update() { if (Input.GetKeyDown(KeyCode.G)) GenerateGrid(); }

    const int RANGE = 3; // 7x7 tiles
    public void GenerateGrid() {
      parent.RemoveAllChildren();
      // generate grid within player's range(6 tiles)
      // 1. get player's position
      var realPos = player.tr.position;
      (int x, int y) center = ((int)(realPos.x/grid.scale), (int)(realPos.z/grid.scale));
      // generate grid
      for (int x = -RANGE; x <= RANGE; x++) {
        for (int y = -RANGE; y <= RANGE; y++) {
          Vector2Int pos = new(center.x + x, center.y + y);
          if (!bounds.Contains(pos)) continue; // skip if out of bounds
          var g = grid.Grids[pos.x + pos.y * grid.size];
          var cell = Instantiate((g > Values.Occupy.None) ? Red : Blue, parent).transform;
          cell.position = new Vector3(pos.x*grid.scale, 0, pos.y*grid.scale);
          cell.localScale = new Vector3(grid.scale, 1, grid.scale);
        }
      }
    }
  }
}