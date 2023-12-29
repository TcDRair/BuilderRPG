using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static MainSetting;

using Rair.Field.Interact;
using Rair.Skill;
using TMPro;
using System.Linq;

namespace Rair.Field
{
  public class FieldUI : MonoBehaviour
  {
    public static FieldUI Instance;

    [SerializeField] protected FieldInteractionMenu interactionMenu;
    [SerializeField] protected RectTransform PlayerUI;
    [SerializeField] protected GameObject AbilityUI;
    [SerializeField] protected Image HP, SP, HPRes, SPRes, Fatigue;
    [SerializeField] protected TextMeshProUGUI Load, Shift, FatigueLog;

    protected void Awake() {
      Instance = this;
    }

    protected void Update() {
      var s = Player.Instance.stat;
      HP.fillAmount = s.HP.Value / s.HPMax.Value;
      HPRes.fillAmount = s.HPRCR.Value / s.HPMax.Value;
      SP.fillAmount = s.SP.Value / s.SPMax.Value;
      SPRes.fillAmount = s.SPRCR.Value / s.SPMax.Value;
      Fatigue.fillAmount = s.Fatigue.Value / s.Fatigue.Max.Value;
      FatigueLog.text = $"Fatigue: {s.Fatigue.Value:F1} / {s.Fatigue.Max.Value:F0}\n{Player.Instance.FatigueTick:F1} / min";
      float load = s.Load.Value, loadMax = s.LoadMax.Value;
      Load.text = $"Load : {(load > loadMax ? $"<color=red>{load}</color>" : load)} / {loadMax:F0}";
      Shift.text = "Shift : " + (Input.GetKey(KeyCode.LeftShift) ? "On" : "Off");
    }

    private readonly List<FieldAbilityUI> abilities = new();
    public void ShowAbility(Ability ability) {
      var ui = Instantiate(AbilityUI, PlayerUI).GetComponent<FieldAbilityUI>();
      ui.Init(ability);
      abilities.Add(ui);
    }
    public void HideAbility(Ability ability) {
      var ui = abilities.Find(x => x.ability == ability);
      abilities.Remove(ui);
      Destroy(ui.gameObject);
    }

    public void UISpaceClicked() {
      if (Input.GetMouseButtonUp(0)) {
        if (interactionMenu.Current != null && interactionMenu.inputInterval <= 0) {
          FieldInteractionMenu.Instance.HideInteractions();
        }
      }
    }
  }
}