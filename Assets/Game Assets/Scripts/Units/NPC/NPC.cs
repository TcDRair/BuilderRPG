using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Rair.Field
{
  public class NPC // : FieldUnit
  {

  }
}
namespace Rair.Character.NPC
{
  #region Motives
  // 모티브(행동 동기) 정의
  public enum Motive {
    // Generate some motive names which can let NPC do something
    // ex) Hunger
    Hunger,
    Energy,
    Bladder,
    Hygiene,
    Social,
    Fun,
    

  }
  // 모든 모티브에 대한 가중치 리스트
  // 행동 자체로 가질 수도 있고, NPC가 자체적으로 가지기도 함.
  // 스킬, 특성 등의 영향 요소는 일단 나중에 고려하는 것으로 하자.
  public struct MotiveWeights
  {
    public float hunger;
    public float sleep;
    public float toilet;
    public float bath;
    public float clean;
    public float fun;
    // Absolute weight : float.MaxValue
    public MotiveWeights(float hunger = 1, float sleep = 1, float toilet = 1, float bath = 1, float clean = 1, float fun = 1) {
      this.hunger = hunger;
      this.sleep = sleep;
      this.toilet = toilet;
      this.bath = bath;
      this.clean = clean;
      this.fun = fun;
    }

    public float this[Motive motive]
      => motive switch
      {
        Motive.Hunger => hunger,
        Motive.Energy => sleep,
        Motive.Bladder => toilet,
        Motive.Hygiene => bath,
        Motive.Social => clean,
        Motive.Fun => fun,
        // And so on...
        _ => throw new IndexOutOfRangeException($"[{motive}] is not implemented") // Out of Index
      };
  }
  #endregion

  #region Goals
  public enum Goal {  }

  #endregion
  // NPC가 시시때때로 수행하는 행동이 정의된 클래스.
  // 
  public class Activity
  {
    public int id;
    public MotiveWeights weights;
    // 행동 목표 가중치
     
    // 실제 행동 내용. 와 이거 어떻게 하지
  }
  // 행동 목표

  // TBD: NPC 메인 클래스
  // 미리 생각해 둔 로직 구현: (아래)
  // 행동 확률 결정 / 대화, 지식 전파 등
}