using System.Collections;
using System.Collections.Generic;

using Rair.Skill;

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TMPro.EditorUtilities;
using Unity.VisualScripting;

namespace Rair.Field
{
  public class FieldEffectUI : MonoBehaviour
  {
    [SerializeField] protected Image Icon;
    [SerializeField] protected TextMeshProUGUI Name, Duration, Stack, MaxStack;
    [SerializeField] protected CanvasGroup Description;
    [SerializeField] protected GameObject TextBox;

    UnitEffect effect;
    FieldUnit unit;

    readonly List<TextMeshProUGUI> boxes = new();
    public void Init(UnitEffect effect, FieldUnit unit)
    {
      this.effect = effect;
      this.unit = unit;
      // check ability.toggleable?
      Name.text = effect.Name;
      Icon.sprite = effect.Icon;
      for (int _ = 0; _ < effect.Description.Length; _++)
      {
        var box = Instantiate(TextBox, Description.transform).transform.GetComponent<TextMeshProUGUI>();
        boxes.Add(box);
      }
    }

    protected void Update() {
      MaxStack.text = (effect.MaxStack == -1) ? ""
        : effect.MaxStackText?.Invoke(unit).ToString() ?? $"√÷¥Î ¡ﬂ√∏ {effect.MaxStack}";
      Stack.text = (effect.Stack == -1) ? "" : $"{effect.Stack}";
      Duration.text = effect.DurationText?.Invoke(unit).ToString() ?? "";
      for (int i = 0; i < boxes.Count; i++)
        boxes[i].text = $"{effect.Description[i](unit)}";
    }

    public void Toggle()
      => Description.alpha = Description.alpha == 0 ? 1 : 0;
  }
}