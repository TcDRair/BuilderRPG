using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractSlotSprites : MonoBehaviour
{
    public static InteractSlotSprites Instance;
    void Awake() { Instance = this; }
    public Sprite buildInfo, buildFillMaterials, build, buildCancel, buildDestroy;
}

