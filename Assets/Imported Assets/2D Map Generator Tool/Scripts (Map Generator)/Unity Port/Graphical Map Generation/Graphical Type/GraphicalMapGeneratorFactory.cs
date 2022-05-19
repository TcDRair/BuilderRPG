using System;

namespace MapGeneratorTool.UnityPort
{
    public class GraphicalMapGeneratorToolFactory
    {
        public IGraphicalMapGeneratorTool GetGraphicalMapGeneratorTool(GraphicalGenerationType generationType, ISpaceOrientation spaceOrientation)
        {
            switch (generationType)
            {
                case GraphicalGenerationType.Sprites:
                    return new SpriteMapGeneratorTool(spaceOrientation);
                case GraphicalGenerationType.TileMap:
                    return new TileMapGeneratorTool(spaceOrientation);
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
