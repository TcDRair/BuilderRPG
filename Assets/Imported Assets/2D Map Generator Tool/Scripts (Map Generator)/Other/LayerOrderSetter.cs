using UnityEngine;
using MapGeneratorTool.UnityPort;

public class LayerOrderSetter : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    private MapGeneratorTool.UnityPort.MapGeneratorTool MapGeneratorToolTool;

    void Start()
    {
        MapGeneratorToolTool = FindObjectOfType<MapGeneratorTool.UnityPort.MapGeneratorTool>();
        spriteRenderer.sortingOrder = (int)(MapGeneratorToolTool.height - transform.position.z);
    }
}
