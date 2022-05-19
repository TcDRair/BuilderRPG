using MapGeneratorTool.DataModels;
using UnityEngine;

namespace MapGeneratorTool.UnityPort
{
    public interface ISpaceOrientation
    {
        Vector3 GetPositionVector(Vector2Float vector);
        Vector3 GetSpriteRotationVector();
        Vector3 GetObjectRotationVector();

        UnityEngine.Tilemaps.Tilemap.Orientation GetTilemapOrientation();
        GridLayout.CellSwizzle GetGridOrientation();
    }
}
