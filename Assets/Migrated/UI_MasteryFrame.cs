using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Guild.UI {
  public class UI_MasteryFrame : MonoBehaviour
  {
    public Image Icon, EXP;
    public Text Level;
    public Button Frame;

    public void Init(Mastery mastery, System.Action<int> onClick) {
      EXP.fillAmount = mastery.Ratio;
      Icon.sprite = mastery.Info.Icon;
      Level.text = $"Lv. {mastery.Level} / {mastery.Info.MaxLevel}";
      Frame.onClick.AddListener(() => onClick?.Invoke(mastery.ID));
    }
  }
}
