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
      stat = new(100, 1, 50, 10, 2, 5, 25, 25, 100);
      info = new(true);

      base.Awake();
    }

    public readonly List<Item> inventory = new();
    protected override void Start() {
      base.Start();
      agent.updatePosition = false;
      MRB = new Skill.AbilityStorage.RunnersHigh(1);
      MRG = new Skill.AbilityStorage.IronStep(3);
    }

    public override bool RunIntent => Input.GetKey(KeyCode.LeftShift);
    protected override void Update() {
      Tick();

      #region 이동
      if (MainCamera.Cam.ClickRaycast(out var hit, 100, floorMask))
        Move(hit.point);
      #endregion

      //* X. 카메라 조정
      cam.SmoothUpdatePos(tr.position);

      //* 디버그
      if (Input.GetKeyDown(KeyCode.F4))
        stat.Load += 10;
      if (Input.GetKeyDown(KeyCode.F5))
        stat.Load -= 10;
      if (Input.GetKeyDown(KeyCode.F6))
        Debug.Log(stat);
      if (Input.GetKeyDown(KeyCode.F7)) {
        //todo: 나중에 정식으로 만들 땐 아래의 UI 작동 로직도 그대로 옮길 것
        //todo: 물론 ShowAbility를 다수의 효과에 대해 적합하게 바꿔야겠지
        MRG.ToggleOn(this);
        FieldUI.Instance.ShowAbility(MRG);
      }
      if (Input.GetKeyDown(KeyCode.F8)) {
        MRG.ToggleOff(this);
        FieldUI.Instance.HideAbility(MRG);
      }

      //TODO 애니메이션 제어
      //? if (_b is not null && _b.ShowConstructingModel()) { animator.SetTrigger("Build End"); _b = null; }
    }
    Ability MRG, MRB;

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