using MapGeneratorTool.DataModels;
using UnityEngine;

namespace MapGeneratorTool.UnityPort
{
    public interface IGraphicalMapGeneratorTool
    {
        void Render(Transform parentTransform, TilesMap map);
    }
}
