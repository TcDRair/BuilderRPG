using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rair.Field {
public static class GridExtensions
{
  private static RectInt GetArea(this Values.OccupyGrid grid, Ray ray, RectInt relativeArea) {
    if (relativeArea == default) relativeArea = new(0, 0, 1, 1);
    if (Physics.Raycast(ray, out var hit, 100, 1 << grid.Terrain.gameObject.layer)) {
      relativeArea.position = grid.GetGridPos(hit.point);
      return relativeArea;
    }
    else throw new System.Exception("Raycast failed. Grid position not found.");
  }

  public static Rect GetGridArea(this Values.OccupyGrid grid, Ray ray, RectInt relativeArea = default)
    => grid.GetWorldArea(grid.GetArea(ray, relativeArea));
  public static (RectInt area, float min, float max) GetAreaHeight(this Values.OccupyGrid grid, Ray ray, RectInt relativeArea = default) {
    var area = grid.GetArea(ray, relativeArea);
    int scale = grid.TerrainScale;
    RectInt scaled = new(area.position * scale, area.size * scale);

    var heights = grid.Terrain.terrainData.GetHeights(scaled.x, scaled.y, scaled.width, scaled.height).GetValues();
    /*var min = (heights.MinItem(h => h.value).pos + scaled.position) / scale;
    var max = (heights.MaxItem(h => h.value).pos + scaled.position) / scale;
    return (area, grid.GetWorldHeight(min), grid.GetWorldHeight(max));*/
    return (area, heights.Min(h => h.value), heights.Max(h => h.value));
  }

  public static float[,] FlattenArea(this Values.OccupyGrid grid, RectInt area, float height) {
    int scale = grid.TerrainScale;
    RectInt scaled = new(area.position * scale, area.size * scale);

    var heights = grid.Terrain.terrainData.GetHeights(scaled.x, scaled.y, scaled.width, scaled.height);
    float[,] newHeights = new float[scaled.width, scaled.height];
    for (int x = 0; x < scaled.width; x++) {
      for (int y = 0; y < scaled.height; y++) {
        newHeights[x, y] = height;
      }
    }
    grid.Terrain.terrainData.SetHeights(scaled.x, scaled.y, newHeights);
    //TODO : Raycast vs GetHeights 퍼포먼스 비교

    return heights;
  }
  public static void RetrieveArea(this Values.OccupyGrid grid, RectInt area, float[,] heights) {
    int scale = grid.TerrainScale;
    RectInt scaled = new(area.position * scale, area.size * scale);

    grid.Terrain.terrainData.SetHeights(scaled.x, scaled.y, heights);
  }
}
}