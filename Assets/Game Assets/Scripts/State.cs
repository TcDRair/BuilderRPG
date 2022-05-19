using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State
{
    static State Instance = new State();
    private State() {} // 생성 방지
    public static State current { get => Instance; }

    #region Enum
    public enum Main {
        Idle,
        //* interactables
        Interactable_Select,
        Interactable_Action,

        //* menu
        Menu_Side,
        Menu_Inventory,
        Menu_BuildSelect,
        Menu_BuildPreview,
        //* others
    }
    public enum Camera {
        Track_Player,
        Track_Interactable,
        Track_PlayerFurther,
        Inactive,
    }
    public enum Player {
        Idle,
        Inactive,
        /// <summary>이전 상태 지속. 모든 행동 가능</summary>
        _,
        /// <summary>행동 상태. 다른 행동 불가능</summary>
        Action_Active,
        /// <summary>이전 행동 상태 지속. 이동 제외 모든 행동 가능.</summary>
        Action_Inactive,
        Battle,
    }
    public enum UI {
        Idle,
        Side,
        BuildSelect,
        BuildPreview,
        Inventory,
        Interactable_Select,
        Battle,
    }
    #endregion

    #region variables
    Main _main = Main.Idle, _main_prev = Main.Idle;
    public Main main { get => _main; }
    Camera _camera = Camera.Track_Player;
    public Camera camera { get => _camera; }
    Player _player_prev = Player.Idle;
    Player _player = Player.Idle;
    /// <summary>플레이어의 현재 상태를 나타냅니다. 이 변수에서 <see cref="Player._"/>는 나오지 않습니다.</summary>
    public Player player {
        get => (_player == Player._) ? _player_prev : _player;
    }
    UI _ui = UI.Idle;
    public UI ui { get => _ui; }
    #endregion

    /// <summary>전체 상태를 조정합니다.</summary>
    public void Set(Main main) {
        //* 변화가 생기면 이전 상태 업데이트
        if (_main == main) return;
        else { _main_prev = _main; _main = main; }


        if (_player != Player._) _player_prev = _player;
        switch (main) {
            case Main.Idle:
                _camera = Camera.Track_Player;
                _player = Player.Idle;
                _ui = UI.Idle;
                
                break;
            case Main.Interactable_Select:
                _camera = Camera.Track_Interactable;
                _player = Player.Action_Inactive;
                _ui = UI.Interactable_Select;
                break;
            case Main.Interactable_Action:
                _camera = Camera.Track_Interactable;
                _player = Player.Action_Active;
                _ui = UI.Interactable_Select;
                break;
            case Main.Menu_BuildPreview:
                _camera = Camera.Track_Player;
                _player = Player.Idle;
                _ui = UI.BuildPreview;
                break;
            case Main.Menu_BuildSelect:
                _camera = Camera.Track_Player;
                _player = Player.Action_Inactive;
                _ui = UI.BuildSelect;
                break;
            case Main.Menu_Side:
                _camera = Camera.Track_Player;
                _player = Player.Idle;
                _ui = UI.Side;
                break;
            case Main.Menu_Inventory:
                _camera = Camera.Track_Player;
                _player = Player.Action_Inactive;
                _ui = UI.Inventory;
                break;
            default: Debug.Log($"정의되지 않은 상태 지정 시도 : {main}"); break;
        }
    }

    /// <summary>호출한 시점에 플레이어가 이동할 수 있는지 확인합니다.</summary>
    public bool CanMove() {
        switch (player) {
            case Player.Idle:
            case Player.Action_Active: return true;
            case Player.Inactive:
            case Player.Action_Inactive: return false;
            default : Debug.Log($"정의되지 않은 상태 확인 시도 : {player}"); return false;
        }
    }
    /// <summary>호출한 시점에 플레이어가 이동했을 때의 상태 변화를 기술합니다.</summary>
    public void DoMove() {
        switch (player) {
            case Player.Action_Active : Set(Main.Idle); break;
        }
    }

    public bool isCameraTrackingPlayer { get => camera == Camera.Track_Player; }
    
}
