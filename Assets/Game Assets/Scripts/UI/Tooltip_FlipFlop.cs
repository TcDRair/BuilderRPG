using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip_FlipFlop : MonoBehaviour
{
    public CanvasGroup alpha;

    public void AlphaSwitch() { alpha.alpha = 1 - alpha.alpha; }
}
