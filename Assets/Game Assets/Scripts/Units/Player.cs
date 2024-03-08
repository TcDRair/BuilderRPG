using System.Collections.Generic;
using UnityEngine;

using static MainSetting;
using Rair.Field.Interact;
using Rair.Items;
using Rair.Skill;

namespace Rair.Field
{
  /// <summary>게임 플레이어가 조작하는 개체에 부착되어 동작하는 기능들이 포함됩니다.</summary>
  public class Player : FieldUnit
  {
    public MainCamera cam;

    public bool Immovable => false;

    public static Player Instance;
    protected override void Awake() {
      Instance = this;
      stat = new(100, 100, 2, 5, 6, 6, 100);
      info = new(true);

      base.Awake();
    }

    public readonly List<Item> inventory = new();
    protected override void Start() {
      base.Start();
      agent.updatePosition = false;
      MRA = new Skill.AbilityStorage.LightBreeze(2);
      MRB = new Skill.AbilityStorage.RunnersHigh(2);
      MRG = new Skill.AbilityStorage.IronStep(2);
    }

    public override bool RunIntent => Input.GetKey(KeyCode.LeftShift);
    protected override void Update() {
      base.Update();

      #region 이동
      if (MainCamera.Cam.ClickRaycast(out var hit, 100, floorMask))
        Move(hit.point);
      #endregion

      //* X. 카메라 조정
      cam.SmoothUpdatePos(tr.position);

      //* 디버그
      if (Input.GetKeyDown(KeyCode.F1)) { Debug.Log(stat); Debug.Log(status); Debug.Log(info); }
      if (Input.GetKeyDown(KeyCode.F2))
        Debug.Log(Load);
      if (Input.GetKeyDown(KeyCode.F3))
        stat.Load += .1f * stat.LoadMax.Value;
      if (Input.GetKeyDown(KeyCode.F4))
        stat.Load -= .1f * stat.LoadMax.Value;
      if (Input.GetKeyDown(KeyCode.F5))
        MRA.ToggleOn(this);
      if (Input.GetKeyDown(KeyCode.F6))
        MRA.ToggleOff(this);
      if (Input.GetKeyDown(KeyCode.F7))
        MRG.ToggleOn(this);
      if (Input.GetKeyDown(KeyCode.F8)) 
        MRG.ToggleOff(this);
      if (Input.GetKeyDown(KeyCode.F9)) 
        MRB.ToggleOn(this);
      if (Input.GetKeyDown(KeyCode.F10))
        MRB.ToggleOff(this);

      //TODO 애니메이션 제어
      //? if (_b is not null && _b.ShowConstructingModel()) { animator.SetTrigger("Build End"); _b = null; }
    }
    Ability MRG, MRB, MRA;

    public bool Enable_Run = true;

    private Quaternion previousRotation;
    const float VELOCITY_MODULAR = .25f, ANGULAR_MODULAR = .25f;
    void LateUpdate() {
      // 애니메이션 제어
      animator.SetFloat("Forward", agent.velocity.GetHorizontalMagnitude() * VELOCITY_MODULAR);
      animator.SetFloat("Turn", tr.rotation.GetAngularSpeed(previousRotation) * ANGULAR_MODULAR);
      previousRotation = tr.rotation;
    }

    void OnAnimatorMove()
      => tr.position = agent.nextPosition;
  }
}
