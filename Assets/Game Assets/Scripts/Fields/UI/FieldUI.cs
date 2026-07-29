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
    protected void Awake() => Instance = this;
    private Player P => Player.Instance;

    [SerializeField] protected FieldInteractionMenu interactionMenu;
    [SerializeField] protected RectTransform EffectArea;
    [SerializeField] protected GameObject AbilityUI;
    [SerializeField] protected Image HP, SP, HPRes, SPRes, Fatigue;
    [SerializeField] protected TextMeshProUGUI FatigueLog, Load;

    const float ABIL_UI_GAP = 90;

    protected void Update() {
      #region HP/SP/Fatigue
      //? ref로 잡아야 합니다. UnitStat/UnitStatus는 struct라 그냥 대입하면 복사본이 되고,
      //? 그 복사본에 쓰기를 하면 원본에 반영되지 않습니다. (문서 05 P1-4)
      //? 값 멤버(RFloat)는 스냅샷이 되고 참조 멤버(RAFloat)는 공유되어 시맨틱이 엇갈립니다.
      ref var s = ref P.stat;
      ref var st = ref P.status;
      HP.fillAmount = s.HPRatio;
      HPRes.fillAmount = s.HPRCR.Value / s.HPMax.Value;
      SP.fillAmount = s.SPRatio;
      SPRes.fillAmount = s.SPRCR.Value / s.SPMax.Value;
      Fatigue.fillAmount = s.Fatigue.Value / s.FatigueMax.Value;
      FatigueLog.text = $"Fatigue: {s.Fatigue.Value:F1} / {s.FatigueMax.Value:F0}\n{st.fatiguePerMinute:F1} / min";
      Load.text = $"Load: {$"{s.Load.Value:F1}".Color(Color.red, s.Load.Value >= s.LoadMax.Value)} / {s.LoadMax.Value:F0}\nStatus: {st.load}";
      #endregion

      #region Ability
      var effects = P.Effects;
      //* Effect O / UI X : Add UI (if Visible)
      foreach (var eff in effects) if (!UIList.Any(ui => ui.EffectID == eff.ID) && eff.Visible) {
        var ui = Instantiate(AbilityUI, EffectArea).GetComponent<FieldEffectUI>();
        ui.Init(eff, P);
        UIList.Add(ui);
      }
      //* Effect X / UI O : Remove UI
      var idList = UIList.ToDictionary(ui => ui.EffectID);
      foreach (var id in idList) if (!effects.Any(e => e.ID == id.Key)) {
        Destroy(id.Value.gameObject);
        UIList.Remove(id.Value);
      }
      //* Update UI Position
      for (int i = 0; i < UIList.Count; i++)
        UIList[i].Rect.anchoredPosition = new(ABIL_UI_GAP * i, 0);

      #endregion
    }
    private readonly List<FieldEffectUI> UIList = new();

    public void UISpaceClicked() {
      if (Input.GetMouseButtonUp(0)&&
        interactionMenu.Current != null &&
        interactionMenu.inputInterval <= 0)
        FieldInteractionMenu.Instance.HideInteractions();
    }
  }
}
