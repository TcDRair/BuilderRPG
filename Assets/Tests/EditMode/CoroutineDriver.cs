using System.Collections;

namespace Rair.Tests
{
  /// <summary>
  /// 코루틴으로 작성된 생성 절차를 테스트에서 동기적으로 끝까지 돌립니다.
  /// </summary>
  /// <remarks>
  /// 맵 생성기는 <c>yield return 다른코루틴()</c> 형태로 단계를 중첩합니다.
  /// 이 중첩을 펼치는 것은 Unity 런타임의 코루틴 스케줄러가 하는 일이라,
  /// 단순히 <c>while (e.MoveNext())</c>만 돌리면 안쪽 단계가 실행되지 않은 채
  /// 통과해 버립니다. 그러면 아무것도 검증하지 못하는 테스트가 됩니다.
  /// <br/>
  /// 여기서는 <c>Current</c>가 <see cref="IEnumerator"/>일 때 재귀로 내려가
  /// 같은 규칙을 직접 구현합니다. <c>yield return null</c>(= 한 프레임 양보)은
  /// 그냥 무시하면 되므로, 결과적으로 전체 절차가 한 번에 완주합니다.
  /// </remarks>
  public static class CoroutineDriver
  {
    /// <summary>중첩 코루틴을 포함해 끝까지 실행합니다.</summary>
    public static void RunToEnd(IEnumerator routine) {
      while (routine.MoveNext())
        if (routine.Current is IEnumerator nested) RunToEnd(nested);
    }
  }
}
