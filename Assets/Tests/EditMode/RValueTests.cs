using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Rair.Tests
{
  /// <summary>
  /// <see cref="RFloat"/> 계열의 성질 테스트.
  /// </summary>
  /// <remarks>
  /// 보완 기록 P3-1이 지목한 두 성질을 고정합니다.
  /// <list type="number">
  ///   <item>적용 후 해제하면 원래 값으로 돌아온다</item>
  ///   <item>적용 순서를 바꿔도 결과가 같다</item>
  /// </list>
  /// 두 성질이 성립하는 근거는 <see cref="RVFloat"/>의 승가산이
  /// <c>Adder</c>와 <c>Multiplier</c>라는 <b>서로 독립적인 누산기</b>에 쌓이기 때문입니다.
  /// <c>*</c>는 곱하는 것이 아니라 <c>Multiplier += (f - 1)</c>이고,
  /// <c>/</c>는 그 역인 <c>Multiplier -= (f - 1)</c>입니다.
  /// 따라서 교환법칙이 성립하고, 역연산이 정확히 상쇄됩니다.
  /// <br/>
  /// 이 구조를 곱셈/나눗셈으로 바꾸면 두 성질이 모두 깨지므로,
  /// 아래 테스트는 그 변경을 막는 방어선 역할을 합니다.
  /// </remarks>
  public class RValueTests
  {
    const float EPS = 1e-3f;

    #region 성질 1 — 적용 후 해제하면 원래 값으로 돌아온다

    [Test]
    public void RVFloat_가산과_승산을_역연산하면_원래값으로_돌아온다() {
      var origin = new RVFloat(100, 0, 1000);

      var applied = (origin + 30f) * 1.5f;
      Assert.That(applied.Value, Is.EqualTo(195f).Within(EPS), "적용 자체가 (100+30)*1.5로 계산되어야 합니다.");

      var restored = (applied - 30f) / 1.5f;
      Assert.That(restored.Value, Is.EqualTo(origin.Value).Within(EPS));
    }

    [Test]
    public void RAFloat_승산을_역연산하면_원래값으로_돌아온다() {
      //? LightBreeze.OnApply/OnRemove의 `RunSpeed *= 1+mod` / `/= 1+mod` 짝을 그대로 옮긴 것입니다.
      var stat = new RAFloat(6f, 0, 100);
      var origin = stat.Value;

      stat *= 1.3f;
      Assert.That(stat.Value, Is.EqualTo(7.8f).Within(EPS));

      stat /= 1.3f;
      Assert.That(stat.Value, Is.EqualTo(origin).Within(EPS));
    }

    [Test]
    public void RAFloat_배율이_0이어도_역연산으로_복원된다() {
      //? IronStep 3레벨의 WalkSpdMod가 0입니다.
      //? `*=`가 실제 곱셈이었다면 여기서 정보가 소실되어 복원이 불가능합니다.
      var stat = new RAFloat(4f, 0, 100);
      var origin = stat.Value;

      stat *= 0f;
      Assert.That(stat.Value, Is.EqualTo(0f).Within(EPS), "0배율은 결과를 0으로 만들어야 합니다.");

      stat /= 0f;
      Assert.That(stat.Value, Is.EqualTo(origin).Within(EPS), "0배율을 거쳐도 원래 값이 복원되어야 합니다.");
    }

    [Test]
    public void RAFloat_적용기를_해제하면_원래값으로_돌아온다() {
      //? RunnersHigh가 Apply에 ApplyFatigue/ApplySP를 붙였다 떼는 경로입니다.
      var stat = new RAFloat(10f, 0, 1000);
      var origin = stat.Value;

      RAFloat.Applier boost = v => v * 2f;
      stat.Apply += boost;
      Assert.That(stat.Value, Is.EqualTo(20f).Within(EPS));

      stat.Apply -= boost;
      Assert.That(stat.Value, Is.EqualTo(origin).Within(EPS));
    }

    [Test]
    public void RMFloat_Nullify를_되돌리면_원래값으로_돌아온다() {
      //? IronStep이 SPRegen.Nullify를 켰다 끄는 경로입니다.
      var stat = new RMFloat(25f, 0, 100);
      var origin = stat.Value;

      stat.Nullify = true;
      Assert.That(stat.Value, Is.EqualTo(0f).Within(EPS));

      stat.Nullify = false;
      Assert.That(stat.Value, Is.EqualTo(origin).Within(EPS));
    }

    [Test]
    public void RMFloat_범위_안에서는_가감산_왕복이_복원된다() {
      var stat = new RMFloat(100f, 0, 1000);
      var origin = stat.Value;

      stat += 30f;
      stat -= 30f;

      Assert.That(stat.Value, Is.EqualTo(origin).Within(EPS));
    }

    [Test]
    public void RMFloat_경계에_닿으면_가감산_왕복이_복원되지_않는다() {
      //! 특성화 테스트 — 결함이 아니라 현재 계약입니다.
      //! RMFloat의 +/- 는 기저값 자체를 Clamp하므로, 상한에 부딪히면 정보가 소실됩니다.
      //! 승가산(*, /)이 별도 누산기에 쌓여 복원되는 것과 대비됩니다.
      var stat = new RMFloat(990f, 0, 1000);

      stat += 30f;   // 1020 → 1000으로 잘림
      stat -= 30f;   // 970

      Assert.That(stat.Value, Is.EqualTo(970f).Within(EPS));
      Assert.That(stat.Value, Is.Not.EqualTo(990f).Within(EPS),
        "경계에서 왕복이 복원된다면 Clamp 지점이 바뀐 것이므로 의도를 재확인해야 합니다.");
    }

    #endregion

    #region 성질 2 — 적용 순서를 바꿔도 결과가 같다

    [Test]
    public void RVFloat_가산과_승산의_적용_순서는_결과를_바꾸지_않는다() {
      var origin = new RVFloat(100, 0, 1000);

      var addThenMul = (origin + 30f) * 1.5f;
      var mulThenAdd = (origin * 1.5f) + 30f;

      Assert.That(addThenMul.Value, Is.EqualTo(mulThenAdd.Value).Within(EPS));
      Assert.That(addThenMul.Value, Is.EqualTo(195f).Within(EPS), "양쪽 모두 (100+30)*1.5여야 합니다.");
    }

    [Test]
    public void RVFloat_승산끼리의_적용_순서는_결과를_바꾸지_않는다() {
      var origin = new RVFloat(100, 0, 1000);

      var ab = (origin * 1.5f) * 2f;
      var ba = (origin * 2f) * 1.5f;

      Assert.That(ab.Value, Is.EqualTo(ba.Value).Within(EPS));
      //? 승산은 누적 곱(1.5*2=3)이 아니라 누적 합(1+0.5+1=2.5)입니다.
      Assert.That(ab.Value, Is.EqualTo(250f).Within(EPS));
    }

    [Test]
    public void 계약을_어긴_적용기는_순서_의존성_검사에_걸린다() {
      //? P1-5 처리 결과. 컴파일러가 막을 수 없는 계약이라 실제로 순서를 바꿔 검사합니다.
      //? 아래 적용기는 받은 값에서 파생시키지 않고 새 RVFloat를 만들어 반환합니다.
      var stat = new RAFloat(100f, 0, 10000);
      RAFloat.Applier wellBehaved = v => v + 10f;
      RAFloat.Applier fabricating = v => new RVFloat(v.Value * 2f, 0, 10000);

      stat.Apply += wellBehaved;
      stat.Apply += fabricating;

      LogAssert.Expect(LogType.Error, new Regex("적용기 등록 순서가 결과를 바꿉니다"));
      _ = stat.Value;
    }

    [Test]
    public void 적용기가_하나뿐이면_순서_검사를_하지_않는다() {
      //? 순서라는 것이 없으므로 검사 비용도 들이지 않습니다.
      var stat = new RAFloat(100f, 0, 10000);
      RAFloat.Applier fabricating = v => new RVFloat(v.Value * 2f, 0, 10000);
      stat.Apply += fabricating;

      Assert.That(stat.Value, Is.EqualTo(200f).Within(EPS));
    }

    [Test]
    public void RAFloat_적용기_등록_순서는_결과를_바꾸지_않는다() {
      RAFloat.Applier add10 = v => v + 10f;
      RAFloat.Applier double_ = v => v * 2f;

      var first = new RAFloat(100f, 0, 10000);
      first.Apply += add10;
      first.Apply += double_;

      var second = new RAFloat(100f, 0, 10000);
      second.Apply += double_;
      second.Apply += add10;

      Assert.That(first.Value, Is.EqualTo(second.Value).Within(EPS));
      Assert.That(first.Value, Is.EqualTo(220f).Within(EPS), "양쪽 모두 (100+10)*2여야 합니다.");
    }

    #endregion

    #region P1-1 — RMFloat와 RAFloat가 같은 식에 같게 반응한다

    [Test]
    public void RAFloat도_RMFloat와_같이_최종_클램프를_한다() {
      //? P1-1 처리 결과. 이전에는 RMFloat만 클램프해서 상속 관계인 두 타입이
      //? 같은 식에 다르게 반응했습니다(20 대 30).
      var bounded = new RMFloat(10f, 0, 20);
      var applied = new RAFloat(10f, 0, 20);

      bounded *= 3f;
      applied *= 3f;

      Assert.That(applied.Value, Is.EqualTo(bounded.Value).Within(EPS), "두 타입의 결과가 같아야 합니다.");
      Assert.That(applied.Value, Is.EqualTo(20f).Within(EPS), "선언 범위 상한에서 잘려야 합니다.");
    }

    [Test]
    public void RAFloat의_클램프는_적용기_체인_이후에_걸린다() {
      //? 순서가 중요합니다. 체인 이전에 걸면 적용기가 상한을 다시 넘길 수 있습니다.
      var stat = new RAFloat(10f, 0, 20);
      RAFloat.Applier triple = v => v * 3f;
      stat.Apply += triple;

      Assert.That(stat.Value, Is.EqualTo(20f).Within(EPS), "적용기 결과까지 포함해 클램프되어야 합니다.");
      Assert.That(stat.RVValue.Value, Is.EqualTo(30f).Within(EPS), "RVValue는 클램프 이전의 원본입니다.");
    }

    [Test]
    public void RAFloat의_클램프는_하한에도_걸린다() {
      //! 실사용에서 실제로 문제가 되던 경로입니다.
      //! LightBreeze가 RunSPCost를 *0.5로 만든 상태에서 RunnersHigh 적용기가
      //! 최대 스택으로 *0 을 걸면 배율 합이 -0.5가 되어 소모값이 음수가 됩니다.
      //! SP를 소모하는 대신 회복하게 되므로, 하한 0에서 잘려야 합니다.
      var runSPCost = new RAFloat(6f, 0);
      runSPCost *= 0.5f;

      RAFloat.Applier drainToZero = v => v * 0f;
      runSPCost.Apply += drainToZero;

      Assert.That(runSPCost.RVValue.Value, Is.EqualTo(-3f).Within(EPS), "클램프 이전에는 음수입니다.");
      Assert.That(runSPCost.Value, Is.EqualTo(0f).Within(EPS), "최종값은 하한 0에서 잘려야 합니다.");
    }

    [Test]
    public void Nullify는_클램프_하한보다_우선한다() {
      //? 하한이 양수여도 Nullify는 0을 반환해야 합니다. Clamp를 그냥 씌우면 하한이 나옵니다.
      var stat = new RAFloat(50f, 10, 100);
      Assert.That(stat.Value, Is.EqualTo(50f).Within(EPS));

      stat.Nullify = true;
      Assert.That(stat.Value, Is.EqualTo(0f).Within(EPS), "하한 10이 아니라 0이어야 합니다.");
    }

    #endregion
  }
}
