using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Side_Menu : MonoBehaviour
{
    public static UI_Side_Menu Instance;
    void Awake() { Instance = this; }
    
    void Start() {
        menu.Disable();
        icon.Enable();
    }


    public CanvasGroup menu, icon;
    public void OpenSideMenu() {
        menu.Enable();
        icon.Disable();
        State.Current.Set(State.MState.Menu_Side);
    }
    public void CloseSideMenu() {
        menu.Disable();
        icon.Enable();
        State.Current.Set(State.MState.Idle);
    }
}
