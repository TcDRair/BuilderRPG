using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Guild.UI {
  public class UI_MasteryArrow : MonoBehaviour
  {
    [System.Serializable] protected class ArrowVar { public Sprite sprite; public Vector2 offset; }
    [SerializeField] protected ArrowVar Up, Straight, Down;
    [SerializeField] protected RectTransform Rect;
    [SerializeField] protected Image Arrow;

    public void Init(Vector2 from, Vector2 to, float size = 0f) { //? size : parent rect width
      var rectOffset = new Vector2(size, -size/2);
      if (from.y > to.y) {
        Arrow.sprite = Down.sprite;
        Rect.pivot = new Vector2(0, 1);
        Rect.anchoredPosition = from + Down.offset + rectOffset;
        Rect.sizeDelta = new Vector2(to.x - from.x - size, from.y - to.y + 30);
      } else if (from.y < to.y) {
        Arrow.sprite = Up.sprite;
        Rect.pivot = new Vector2(0, 0);
        Rect.anchoredPosition = from + Up.offset + rectOffset;
        Rect.sizeDelta = new Vector2(to.x - from.x - size, to.y - from.y + 30);
      } else {
        Arrow.sprite = Straight.sprite;
        Rect.anchoredPosition = from + rectOffset;
        Rect.sizeDelta = new Vector2(to.x - from.x - size, Rect.sizeDelta.y);
      }
    }
  }
}
