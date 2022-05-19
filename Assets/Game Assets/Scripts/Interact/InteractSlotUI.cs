using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class InteractSlotUI : MonoBehaviour
{
    // Cell의 목적 : 해당 셀에 할당되는 버튼의 스프라이트, 작동 시간, 툴팁, 연결 메서드를 관리
    public Image image;
    public Text cellName, duration, amount;
    public RectTransform cellNameRect;
    public EventTrigger trigger;
}
