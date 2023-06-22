using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

namespace Rair.Field.Maps {
  public class MapOverlay : MonoBehaviour
  {
    public RandomTextureGenerator generator;
    public Player player;

    public GameObject Blue, Red;

    Transform parent;
    protected void Awake() { parent = transform; }

    Values.OccupyGrid grid;

    protected void Start() { InitGrid(); }
    public void InitGrid() {
      var h = Resources.Load<Texture2D>("Sprites/Map/heightMoisture");
      grid = new(h.width, h.GetPixels32().Select(p => (Values.Occupy)p.b), 4);
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
          if (!grid.bounds.Contains(pos)) continue; // skip if out of bounds
          var g = grid.Grids[pos.x + pos.y * grid.size];
          var cell = Instantiate((g > Values.Occupy.None) ? Red : Blue, parent).transform;
          cell.position = new Vector3(pos.x*grid.scale, 0, pos.y*grid.scale);
          cell.localScale = new Vector3(grid.scale, 1, grid.scale);
        }
      }
    }
  }
}