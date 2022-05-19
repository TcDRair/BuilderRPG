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
        State.current.Set(State.Main.Menu_Side);
    }
    public void CloseSideMenu() {
        menu.Disable();
        icon.Enable();
        State.current.Set(State.Main.Idle);
    }
}
