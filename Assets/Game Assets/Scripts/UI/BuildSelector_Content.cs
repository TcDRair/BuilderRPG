using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class BuildSelector_Content : MonoBehaviour
{
    public static BuildSelector_Content Instance;
    void Awake() { Instance = this; }

    const float margin = 20f, size = 200f;
    public GameObject buildSelectorCell;
    public RectTransform content;
    public void InitBuildSelectorContents(Building[] buildings) {
        int h = -1;
        for (int i=0; i<buildings.Length; i++) {
            Building building = buildings[i];
            int w = i%5; h = i/5;
            Transform cell = Instantiate(buildSelectorCell, content).transform;
            cell.localPosition = new Vector3(size * w + (2*w+1) * margin, -size * h - (2*h+1) * margin, 0);
            cell.GetComponent<Image>().sprite = building.info.sprite;
            var entry = new EventTrigger.Entry() { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(new UnityAction<BaseEventData>((_) => BuildSelector_Info.Instance.ShowBuildSelectorInfo(building)));
            //* delegate에 i와 같이 런타임에 변화하는 값을 전달하면 안 된다.
            //* 인덱스나 매개 변수를 직접 할당하면 다른 호출 후 delegate가 발현될 때 변경된 값을 참조하는 일이 생길 수 있다.
            cell.GetComponent<EventTrigger>().triggers.Add(entry);
        }
        content.sizeDelta = new Vector2(0, (h+1) * size + (2*h+2) * margin);
    }
}