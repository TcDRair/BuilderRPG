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
  public class FieldAbilityUI : MonoBehaviour
  {
    [SerializeField] protected Image Icon;
    [SerializeField] protected TextMeshProUGUI Name, Stack;
    [SerializeField] protected CanvasGroup Description;
    [SerializeField] protected GameObject TextBox;

    public Ability ability;
    public void Init(Ability ability)
    {
      this.ability = ability;
      // check ability.toggleable?
      Icon.sprite = ability.Icon;
      Name.text = ability.Name;
      foreach (var line in ability.Effect)
      {
        var box = Instantiate(TextBox, Description.transform).transform.GetComponent<TextMeshProUGUI>();
        var boxColor = line.boxColor;
        boxColor.a = Mathf.Min(.25f, boxColor.a);
        var text = $"<mark=#{boxColor.ToHexString()}>{line.text}</color>";
        box.text = text;
      }
    }

    protected void Update() {
      Stack.text = (ability.Stack != -1) ? ability.Stack.ToString() : "";
    }

    public void Toggle()
      => Description.alpha = Description.alpha == 0 ? 1 : 0;
  }
}