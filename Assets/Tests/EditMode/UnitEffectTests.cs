using NUnit.Framework;

using Rair.Skill;

namespace Rair.Tests
{
  /// <summary>
  /// 효과 중첩·갱신 규칙(<see cref="UnitEffect"/>)의 계약.
  /// </summary>
  /// <remarks>
  /// 문서 05 P1-3 · P2-6 처리 결과입니다.
  /// 기본 규칙은 <b>갱신</b> — 지속시간만 대조해 긴 쪽을 남깁니다.
  /// <br/>
  /// <see cref="FieldUnit"/>을 거치는 경로는 MonoBehaviour라 EditMode에서 만들 수 없어,
  /// 여기서는 <see cref="UnitEffect"/> 자체의 규칙만 검사합니다.
  /// </remarks>
  public class UnitEffectTests
  {
    /// <summary>Ability를 만들지 않고 UnitEffect만 세우기 위한 최소 구현.</summary>
    sealed class Bare : UnitEffect { }

    static UnitEffect Effect(int duration = -1, int maxStack = -1, int stack = -1)
      => new Bare { Duration = duration, MaxStack = maxStack, Stack = stack };

    #region 갱신 규칙

    [Test]
    public void 갱신은_지속시간이_긴_쪽을_남긴다() {
      var current = Effect(duration: 5);
      current.Refresh(Effect(duration: 12));

      Assert.That(current.Duration, Is.EqualTo(12));
    }

    [Test]
    public void 갱신은_더_짧은_지속시간으로_덮어쓰지_않는다() {
      //? 짧은 효과를 다시 걸었다고 남은 시간이 줄어들면 곤란합니다.
      var current = Effect(duration: 12);
      current.Refresh(Effect(duration: 5));

      Assert.That(current.Duration, Is.EqualTo(12));
    }

    [Test]
    public void 무기한_효과는_갱신해도_무기한으로_남는다() {
      //? -1은 무기한을 뜻하므로 어느 쪽이 -1이든 결과는 -1입니다.
      var indefinite = Effect(duration: -1);
      indefinite.Refresh(Effect(duration: 30));
      Assert.That(indefinite.Duration, Is.EqualTo(-1), "이미 무기한이면 유지되어야 합니다.");

      var finite = Effect(duration: 30);
      finite.Refresh(Effect(duration: -1));
      Assert.That(finite.Duration, Is.EqualTo(-1), "무기한이 들어오면 무기한이 되어야 합니다.");
    }

    #endregion

    #region P2-6 — Stack 초기값이 계산에 유입되지 않는다

    [Test]
    public void 스택을_쓰는_효과는_적용_시작에_스택이_0이_된다() {
      //! 초기값 -1은 "스택 없음"을 뜻하는 UI용 감시값입니다.
      //! 그대로 두면 첫 틱까지 약 1초 동안 -1이 계수 계산에 섞여 들어갑니다.
      var effect = Effect(maxStack: 50);
      Assert.That(effect.Stack, Is.EqualTo(-1), "생성 직후에는 아직 -1입니다.");

      effect.Begin(null);

      Assert.That(effect.Stack, Is.EqualTo(0));
    }

    [Test]
    public void 스택을_쓰지_않는_효과는_감시값_음수1을_유지한다() {
      //? UI가 "스택 표시 없음"을 판단하는 근거이므로 0으로 바꾸면 안 됩니다.
      var effect = Effect(maxStack: -1);
      effect.Begin(null);

      Assert.That(effect.Stack, Is.EqualTo(-1));
    }

    [Test]
    public void 이미_쌓인_스택은_적용_시작이_되돌리지_않는다() {
      var effect = Effect(maxStack: 50, stack: 7);
      effect.Begin(null);

      Assert.That(effect.Stack, Is.EqualTo(7));
    }

    #endregion

    #region 중첩 일부 제거

    [Test]
    public void 중첩을_일부만_걷어내면_효과가_남는다() {
      var effect = Effect(maxStack: 50, stack: 10);
      var expired = effect.Consume(3, null);

      Assert.That(expired, Is.False, "아직 만료되지 않아야 합니다.");
      Assert.That(effect.Stack, Is.EqualTo(7));
    }

    [Test]
    public void 중첩을_전부_걷어내면_만료된다() {
      var effect = Effect(maxStack: 50, stack: 3);
      var expired = effect.Consume(3, null);

      Assert.That(expired, Is.True);
      Assert.That(effect.Stack, Is.EqualTo(0));
    }

    [Test]
    public void 남은_것보다_많이_걷어내도_스택은_음수가_되지_않는다() {
      var effect = Effect(maxStack: 50, stack: 2);
      var expired = effect.Consume(99, null);

      Assert.That(expired, Is.True);
      Assert.That(effect.Stack, Is.EqualTo(0), "감시값 -1로 되돌아가면 UI가 스택 없음으로 오인합니다.");
    }

    [Test]
    public void 스택_개념이_없는_효과는_부분_제거_요청에_통째로_걷힌다() {
      var effect = Effect(maxStack: -1);
      var expired = effect.Consume(1, null);

      Assert.That(expired, Is.True);
    }

    #endregion

    #region 오버라이드 (4-c)

    sealed class Stacking : UnitEffect
    {
      public Stacking() { MaxStack = 3; Stack = 0; }
      /// <summary>갱신 대신 중첩하는 효과.</summary>
      public override void Refresh(UnitEffect incoming)
        => Stack = UnityEngine.Mathf.Min(Stack + 1, MaxStack);
    }

    [Test]
    public void 효과는_갱신_규칙을_재정의해_중첩할_수_있다() {
      //? 기본은 갱신이지만, 중첩이 필요한 효과는 Refresh를 재정의합니다.
      var effect = new Stacking();

      effect.Refresh(new Stacking());
      effect.Refresh(new Stacking());
      Assert.That(effect.Stack, Is.EqualTo(2));

      effect.Refresh(new Stacking());
      effect.Refresh(new Stacking());
      Assert.That(effect.Stack, Is.EqualTo(3), "MaxStack을 넘지 않아야 합니다.");
    }

    #endregion
  }
}
