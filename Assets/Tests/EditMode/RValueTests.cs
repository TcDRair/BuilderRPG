using NUnit.Framework;

namespace Rair.Tests
{
  /// <summary>
  /// <see cref="RFloat"/> 계열의 성질 테스트.
  /// </summary>
  /// <remarks>
  /// 문서 05 P3-1이 지목한 두 성질을 고정합니다.
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

    #region P1-1 특성화 — RAFloat는 최종 클램프를 하지 않는다

    [Test]
    public void RAFloat는_RMFloat와_달리_최종_클램프를_하지_않는다() {
      //! 특성화 테스트 — 현재 동작을 고정할 뿐, 이것이 옳다는 뜻은 아닙니다.
      //! 문서 05 P1-1 참조. 상속 관계인 두 타입이 같은 식에 다르게 반응합니다.
      //!   RMFloat.Value => Clamp((value + add) * mul, Min, Max)
      //!   RAFloat.Value => RVValue.Value  (= (baseValue + Adder) * Multiplier, 클램프 없음)
      //! P1-1을 처리하면 이 테스트가 깨져야 정상입니다. 그때 기대값을 20으로 바꾸십시오.
      var bounded = new RMFloat(10f, 0, 20);
      var unbounded = new RAFloat(10f, 0, 20);

      bounded *= 3f;
      unbounded *= 3f;

      Assert.That(bounded.Value, Is.EqualTo(20f).Within(EPS), "RMFloat는 선언 범위 상한에서 잘립니다.");
      Assert.That(unbounded.Value, Is.EqualTo(30f).Within(EPS), "RAFloat는 선언 범위를 넘어섭니다.");
    }

    [Test]
    public void RAFloat의_기저값은_생성_시점에만_클램프된다() {
      //? 상한을 넘겨 생성하면 기저값은 잘리지만, 그 뒤 승산 결과는 잘리지 않습니다.
      var stat = new RAFloat(50f, 0, 20);

      Assert.That(stat.Value, Is.EqualTo(20f).Within(EPS), "생성 시점의 기저값은 클램프됩니다.");

      stat *= 2f;
      Assert.That(stat.Value, Is.EqualTo(40f).Within(EPS), "승산 결과는 클램프되지 않습니다.");
    }

    #endregion
  }
}
