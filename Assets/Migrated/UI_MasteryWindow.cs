using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Data;
namespace Guild.UI {
  public class UI_MasteryWindow : MonoBehaviour
  {
    [SerializeField] protected ScrollRect CategoryRect;
    [SerializeField] protected RectTransform CategoryListContent, CategoryContent;
    [SerializeField] protected GameObject CategoryButtonPrefab, MasteryPrefab, ArrowLinePrefab;
    [SerializeField] protected Text CategoryName;
    [SerializeField] protected Vector2Int Interval;
    private float size;
    public void Start() => Init();
    public void Init() {
      var height = CategoryButtonPrefab.GetComponent<RectTransform>().rect.height;
      int i = 0;
      foreach (var mc in VariableData.MasteryCategory.Values) {
        var rect = Instantiate(CategoryButtonPrefab, CategoryListContent).GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, -height * i++);
        rect.GetComponent<UI_MasteryCatUI>().Init(mc, OnCategoryClicked);
      }
      size = MasteryPrefab.GetComponent<RectTransform>().rect.width;

      OnCategoryClicked(VariableData.MasteryCategory.First().Key);
    }

    private Vector2 GridPos(int cat, int mst) => new Vector2(
      (size + Interval.x) * ConstantData.MasteryData[mst].Tier - size,
      size - (size + Interval.y) * ConstantData.MasteryCategoryData[cat].Row[mst]
    );

    public void OnCategoryClicked(int id) {
      foreach (Transform child in CategoryContent) Destroy(child.gameObject);
      CategoryContent.anchoredPosition = Vector2.zero;

      var category = ConstantData.MasteryCategoryData[id];
      var masteries = category.Masteries.Select(id => VariableData.Mastery[id]);

      CategoryName.text = category.Name;
      foreach (var m in masteries) {
        var rect = Instantiate(MasteryPrefab, CategoryContent).GetComponent<RectTransform>();
        rect.anchoredPosition = GridPos(id, m.ID);
        rect.GetComponent<UI_MasteryFrame>().Init(m, OnMasteryClicked);
      }

      float maxX = masteries.Max(m => m.Info.Tier) * (size + Interval.x) + Interval.x;
      float maxY = category.Row.Max(p => p.Value) * (size + Interval.y) + Interval.y;
      CategoryRect.horizontal = CategoryRect.viewport.rect.width < maxX;
      CategoryRect.vertical = CategoryRect.viewport.rect.height < maxY;
      CategoryContent.sizeDelta = new Vector2(maxX, maxY);

      foreach (var m1 in masteries) {
        var to = GridPos(id, m1.ID);
        foreach (var m2 in m1.Info.Prerequisites) {
          var from = GridPos(id, m2.id);
          var arrow = Instantiate(ArrowLinePrefab, CategoryContent).GetComponent<UI_MasteryArrow>();
          arrow.Init(from, to, size);
        }
      }
    }

    private void OnMasteryClicked(int id) {
      
    }
  }
}

public static class QueueExtension {
  public static bool Dequeue<T>(this Queue<T> queue, out T result) {
    if (queue.Count > 0) {
      result = queue.Dequeue();
      return true;
    }
    result = default;
    return false;
  }
}