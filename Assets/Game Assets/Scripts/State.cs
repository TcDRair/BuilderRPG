using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State
{
  readonly static State Instance = new();
  private State() {} // 생성 방지
  public static State Current { get => Instance; }

  #region Enum
  /// <summary>전반적인 상태를 나타냅니다. 중첩되지 않게 정의되어야 합니다.</summary>
  public enum MState {
    /// <summary>일반 상태</summary>
    Idle,
    //* interactables
    /// <summary>상호작용 객체 선택 상태</summary>
    Interactable_Select,
    /// <summary>상호작용 활동 상태</summary>
    Interactable_Action,
    /// <summary>건설 활동 상태</summary>
    Build_Action,

    //* menu
    /// <summary>사이드메뉴 활성화 상태(TODO : 다른 상황과 중첩 가능. 이를 고려해야)</summary>
    Menu_Side,
    /// <summary>전체 화면 메뉴 활성화 상태</summary>
    Menu_FullScreen,
    /// <summary>단독 메뉴 활성화 상태. 해당 메뉴가 닫히기 전까지 상태가 변화하지 않습니다.</summary>
    Menu_Solo,
    //* modes
    /// <summary>건설 프리뷰 모드 상태.</summary>
    Mode_BuildPreview,
    /// <summary>전투 상태.</summary>
    Mode_Battle,
  }
  public enum CState {
    Track_Player,
    Track_Interactable,
    Track_PlayerFurther,
    Inactive,
  }
  public enum PState {
    /// <summary>일반 상태. 이동, 정지 등 특별한 상태가 아닐 경우 해당. 모든 행동 가능.</summary>
    Idle,
    /// <summary>비활성화 상태. 조작이 비활성화되어 UI 제어만 가능한 경우 해당. 모든 행동 불가능.</summary>
    Inactive,
    /// <summary>행동 상태. 상호작용 등 특수한 행동을 수행하고 있는 경우 해당. 이동 시 해제.</summary>
    Action_Active,
    /// <summary>행동 상태. 특수 행동 + UI 제어 시 해당. UI에서 빠져나오기 전까지 이동 제한.</summary>
    Action_Inactive,
    /// <summary>전투 상태. 전투에서 빠져나오기 전까지 일반 행동 제한.</summary>
    Battle,
  }
  public enum UState {
    /// <summary>일반 상태 UI만 표시.</summary>
    Idle,
    /// <summary>사이드 메뉴가 열려 있을 때 해당. 메뉴 조작 + 이동 시 유지, 다른 UI 상태 호출 시 해제.</summary>
    Side,
    /// <summary>화면 전체를 가리는 UI가 열려 있을 때 해당. 다른 상태가 호출되거나 해당 UI가 닫히면 해제.</summary>
    FullScreen,
    /// <summary>단독 조작만 허용되는 UI가 열려 있을 때 해당. 해당 UI가 닫히면 해제.</summary>
    Solo,
    /// <summary>조작에 영향을 주지 않는 UI가 열려 있을 때 해당. 상태가 변경되면 해제.</summary>
    Misc,
    /// <summary>건설 프리뷰 상태. 전용 오버레이 작업 및 UI 적용. 건설 확정 또는 취소 시 해제.</summary>
    BuildPreview,
    /// <summary>상호작용 개체를 선택했을 때 해당. 전용 UI가 표시되며, 선택 또는 취소 시 해제.</summary>
    Interactable_Select,
    /// <summary>전투 UI 표시. 전투에서 빠져나오기 전까지 UI 제한.</summary>
    Battle,
  }
  #endregion

  #region variables
  MState _main = MState.Idle;
  CState _camera = CState.Track_Player;
  PState _player = PState.Idle;
  UState _ui = UState.Idle;
  public MState Main => _main;
  public CState Camera => _camera;
  public PState Player => _player;
  public UState UI => _ui;
  #endregion

  /// <summary>전체 상태를 조정합니다.</summary>
  public void Set(MState main) {
    //* 변화가 생기면 이전 상태 업데이트
    if (_main == main) return;
    else _main = main;

    switch (main) {
      case MState.Idle:
        _camera = CState.Track_Player;
        _player = PState.Idle;
        _ui = UState.Idle;
        break;
      case MState.Interactable_Select:
        _camera = CState.Track_Interactable;
        _player = PState.Action_Inactive;
        _ui = UState.Interactable_Select;
        break;
      case MState.Interactable_Action:
        _camera = CState.Track_Interactable;
        _player = PState.Action_Active;
        _ui = UState.Interactable_Select;
        break;
      case MState.Build_Action:
        _camera = CState.Track_Player;
        _player = PState.Action_Active;
        _ui = UState.Misc;
        break;
      case MState.Mode_BuildPreview:
        _camera = CState.Track_Player;
        _player = PState.Idle;
        _ui = UState.BuildPreview;
        break;
      case MState.Menu_FullScreen:
        _camera = CState.Track_Player;
        _player = PState.Action_Inactive;
        _ui = UState.FullScreen;
        break;
      case MState.Menu_Solo:
        _camera = CState.Track_Interactable;
        _player = PState.Action_Inactive;
        _ui = UState.Solo;
        break;
      case MState.Menu_Side:
        _camera = CState.Track_Player;
        _player = PState.Idle;
        _ui = UState.Side;
        break;
      default: Debug.Log($"정의되지 않은 상태 지정 시도 : {main}"); break;
    }
  }

  /// <summary>호출한 시점에 플레이어가 이동할 수 있는지 확인합니다.</summary>
  public bool CanMove() {
    switch (Player) {
      case PState.Idle:
      case PState.Battle:
      case PState.Action_Active: return true;
      case PState.Inactive:
      case PState.Action_Inactive: return false;
      default : Debug.Log($"정의되지 않은 상태 확인 시도 : {Player}"); return false;
    }
  }
  /// <summary>호출한 시점에 플레이어가 이동했을 때의 상태 변화를 기술합니다.</summary>
  public void DoMove() {
    switch (Player) {
      case PState.Action_Active : Set(MState.Idle); break;
    }
  }

  public bool IsCameraTrackingPlayer { get => Camera == CState.Track_Player; }
  
}
