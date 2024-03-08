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
    public Texture2D mapData;

    public GameObject Blue, Red;

    Transform parent;
    protected void Awake() { parent = transform; }

    Values.OccupyGrid grid;

    protected void Start() { InitGrid(); }
    public void InitGrid() {
      grid = new(generator.mapVariables.MapTerrain, mapData.width, mapData.GetPixels().Select(p => (Values.Occupy)p.a), 4);
    }
    RectInt area;
    float[,] data;
    protected void Update() {
      if (Input.GetKeyDown(KeyCode.G)) GenerateGrid();
      if (Input.GetKeyDown(KeyCode.V)) {
        var g = grid.GetAreaHeight(MainCamera.Ray, new(-1, -1, 3, 3));
        area = g.area;
        data = grid.FlattenArea(g.area, g.min);
      }
      if (Input.GetKeyDown(KeyCode.B)) grid.RetrieveArea(area, data);
    }

    const int RANGE = 3; // 7x7 tiles
    public void GenerateGrid() {
      parent.RemoveAllChildren();
      // generate grid within player's range(6 tiles)
      // 1. get player's position
      var realPos = player.tr.position;
      (int x, int y) center = ((int)(realPos.x/grid.Scale), (int)(realPos.z/grid.Scale));
      // generate grid
      for (int x = -RANGE; x <= RANGE; x++) {
        for (int y = -RANGE; y <= RANGE; y++) {
          Vector2Int pos = new(center.x + x, center.y + y);
          if (!grid.bounds.Contains(pos)) continue; // skip if out of bounds
          var g = grid.Grid[pos.x + pos.y * grid.Size];
          var cell = Instantiate((g > Values.Occupy.None) ? Red : Blue, parent).transform;
          cell.localPosition = new Vector3(pos.x*grid.Scale, 0, pos.y*grid.Scale);
          cell.localScale = new Vector3(grid.Scale, 1, grid.Scale);
        }
      }
    }
  }
}
