using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using static MainSetting;
using Rair.Field.Interact;
using Rair.Items;

namespace Rair.Field
{
	/// <summary>게임 플레이어가 조작하는 개체에 부착되어 동작하는 기능들이 포함됩니다.</summary>
	public class Player : FieldUnit
	{
		public MainCamera cam;

		public bool Immovable => false;

		public static Player Instance;
		protected void Awake() { Instance = this; }

		List<Item> inventory;
		protected void Start()
		{
			agent.updatePosition = false;

			// var p = new Item("얼음");
		}

		protected void Update()
		{
			//* 이동 제어
			//** 1. 이동 조건 미충족 시
			agent.isStopped = Immovable;
      //** 2. 이동 조작 입력 시
      if (MainCamera.Cam.ClickRaycast(out var hit, 100, floorMask))
      {
				if (moveTask != default) {
					StopCoroutine(moveTask.c);
					moveTask = default;
				}
				agent.SetDestination(hit.point);
			}

			//** X. 카메라 조정
			cam.SmoothUpdatePos(tr.position);

			//* 다른 제어

			//TODO 애니메이션 제어
			//? if (_b is not null && _b.ShowConstructingModel()) { animator.SetTrigger("Build End"); _b = null; }
		}

		private Quaternion previousRotation;
		const float VELOCITY_MODULAR = .25f, ANGULAR_MODULAR = .25f;
		void LateUpdate()
		{
			// 애니메이션 제어
			animator.SetFloat("Forward", agent.velocity.GetHorizontalMagnitude() * VELOCITY_MODULAR);
			animator.SetFloat("Turn", tr.rotation.GetAngularSpeed(previousRotation) * ANGULAR_MODULAR);
			previousRotation = tr.rotation;
		}

		void OnAnimatorMove()
			=> tr.position = agent.nextPosition;
	}
}