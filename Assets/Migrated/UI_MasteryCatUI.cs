using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Guild.UI {
  public class UI_MasteryCatUI : MonoBehaviour
  {
    [SerializeField] protected Image image, progress;
    [SerializeField] protected Text title, level;
    [SerializeField] protected Button button;

    public void Init(MasteryCategory mc, System.Action<int> onClick) {
      image.sprite = Resources.Load<Sprite>(mc.Info.Icon);
      title.text = mc.Info.Name;
      level.text = $"{mc.Level} / {mc.Info.MaxLevel}";
      progress.fillAmount = mc.Ratio;
      button.onClick.AddListener(() => onClick?.Invoke(mc.ID));
    }
  }
}